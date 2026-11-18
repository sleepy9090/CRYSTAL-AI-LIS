// node web/test/serve_check.mjs — serves the repo root like `python3 -m http.server` would (static
// files, correct JS MIME type) and checks that web/index.html, every ES module it imports (followed
// recursively through `import ... from './x.js'`), and every file in web/data/manifest.json resolve.
// Then it runs web/src/data.js loadData() through fetch() against that server, i.e. the browser code path.
import { createServer } from 'node:http';
import { readFileSync, existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';

const ROOT = path.dirname(path.dirname(path.dirname(fileURLToPath(import.meta.url))));
const MIME = { '.html': 'text/html', '.js': 'text/javascript', '.json': 'application/json', '.bin': 'application/octet-stream' };
const server = createServer((req, res) => {
  const p = path.join(ROOT, decodeURIComponent(new URL(req.url, 'http://x').pathname));
  if (!p.startsWith(ROOT) || !existsSync(p)) { res.writeHead(404); res.end(); return; }
  res.writeHead(200, { 'content-type': MIME[path.extname(p)] || 'application/octet-stream' });
  res.end(readFileSync(p));
});
await new Promise((r) => server.listen(0, '127.0.0.1', r));
const base = `http://127.0.0.1:${server.address().port}/web/`;
let bad = 0;
const get = async (rel) => { const r = await fetch(new URL(rel, base)); if (!r.ok) { bad++; console.log(`FAIL ${r.status} ${rel}`); } return r; };

const html = await (await get('index.html')).text();
const seen = new Set();
const queue = [...html.matchAll(/<script type="module" src="([^"]+)"/g)].map((m) => new URL(m[1], base).href);
while (queue.length) {
  const u = queue.shift();
  if (seen.has(u)) continue;
  seen.add(u);
  const r = await fetch(u);
  if (!r.ok) { bad++; console.log(`FAIL ${r.status} ${u}`); continue; }
  if (!/javascript/.test(r.headers.get('content-type'))) { bad++; console.log(`FAIL MIME ${u}`); }
  const src = await r.text();
  for (const m of src.matchAll(/^\s*import\s[^'"]*['"](\.[^'"]+)['"]/gm)) queue.push(new URL(m[1], u).href);
}
const data = await loadData((n) => fetch(new URL('data/' + n, base)).then((r) => {
  if (!r.ok) throw new Error(`data/${n} ${r.status}`);
  return r.arrayBuffer();
}));
// run main.js end to end under minimal DOM stubs: its fetch('data/..') is resolved against the
// test server, requestAnimationFrame callbacks are driven by hand (1 s of 60 Hz ticks)
let blits = 0, lastImg = null, rafCb = null;
const el = () => ({
  textContent: '', style: {},
  getContext: () => ({
    createImageData: (w, h) => (lastImg = { data: new Uint8ClampedArray(w * h * 4) }),
    putImageData() { blits++; },
  }),
});
const realFetch = globalThis.fetch;
const statusEl = el();
// s57: window listeners are kept so the test can fire keydown / pagehide; localStorage is a Map seeded with a
// WRAM image (marker bytes) that main.js must restore before Reset; AudioContext records the scheduled buffers.
const listeners = {};
const fire = (type, ev = {}) => (listeners[type] || []).forEach((f) => f(ev));
const store = new Map();
const { KEY: WRAM_KEY } = await import('../src/save.js');
const seed = new Uint8Array(0x2000); seed[0x1DFD] = 0x41; seed[0x1DFE] = 0x4D; seed[0x1DFF] = 0x49; seed[0x1000] = 0x5A;
store.set(WRAM_KEY, Buffer.from(seed).toString('base64'));
const scheduled = [];
class FakeAudioContext {
  constructor() { this.sampleRate = 48000; this.currentTime = 0; this.state = 'running'; this.destination = {}; }
  createGain() { return { gain: { value: 1 }, connect() {} }; }
  createBuffer(ch, n, rate) {
    const d = new Float32Array(n);
    return { duration: n / rate, copyToChannel: (src) => d.set(src), data: d };
  }
  createBufferSource() {
    return { connect() {}, start(t) { scheduled.push({ t, buf: this.buffer }); } };
  }
  resume() {}
}
Object.defineProperty(globalThis, 'localStorage', {
  configurable: true, value: { getItem: (k) => store.get(k) ?? null, setItem: (k, v) => store.set(k, String(v)) },
});
Object.assign(globalThis, {
  window: globalThis, innerWidth: 800, innerHeight: 600,
  addEventListener: (type, f) => (listeners[type] ||= []).push(f),
  AudioContext: FakeAudioContext,
  requestAnimationFrame: (cb) => { rafCb = cb; },
  document: { getElementById: (id) => (id === 'status' ? statusEl : el()), addEventListener() {}, hidden: false },
  fetch: (u, o) => realFetch(new URL(u, base), o),
});
if (!globalThis.performance) globalThis.performance = { now: () => Date.now() };
const { BTN } = await import('../src/input.js');
await import('../src/main.js');
for (let i = 0; i < 100 && !rafCb; i++) await new Promise((r) => setTimeout(r, 20));
const restored = globalThis.reC && globalThis.reC.mem.wram[0x1000] === 0x5A && globalThis.reC.mem.wram[0x1DFD] === 0x41;
fire('keydown', { code: 'KeyX', preventDefault() {} });                   // first gesture: AudioContext + APU synthesis on
const t0 = performance.now();
for (let i = 1; i <= 60 && rafCb; i++) { const cb = rafCb; rafCb = null; cb(t0 + i * (1000 / 60)); }
fire('pagehide');
server.close();
const m = globalThis.reC;
{ // audio: back-to-back buffers, ~1 s of samples at 48 kHz for ~60 frames, all finite
  const n = scheduled.reduce((s, x) => s + x.buf.data.length, 0);
  let gap = 0, finite = true;
  for (let i = 1; i < scheduled.length; i++) gap = Math.max(gap, Math.abs(scheduled[i].t - (scheduled[i - 1].t + scheduled[i - 1].buf.duration)));
  for (const x of scheduled) if (!x.buf.data.every(Number.isFinite)) finite = false;
  const want = m ? (m.frame * 48000 / 60.0988) : 0;
  const ok = scheduled.length > 1 && n >= want - 2048 && n <= want + 64 && gap < 1e-9 && finite;
  if (!ok) bad++;
  console.log(`${ok ? 'ok  ' : 'FAIL'} audio: ${scheduled.length} buffers, ${n} samples for ${m ? m.frame : 0} frames (expect ~${want.toFixed(0)}), ` +
    `max gap ${gap.toExponential(1)} s, ${finite ? 'all finite' : 'NaN/Inf'}`);
}
{ // save: the seeded image was restored before Reset; pagehide wrote the current WRAM back byte for byte
  const s = store.get(WRAM_KEY);
  const back = s ? new Uint8Array(Buffer.from(s, 'base64')) : null;
  const same = back && m && back.length === 0x2000 && back.every((v, i) => v === m.mem.wram[i]);
  // pal_buf $6140: Reset fills $FF, then MainState2Init's MenuPalBlack 18:82B3 sets $0F (s82: state 2 ported);
  // the opening's first 60 frames (step 0 wait $3C) leave it black
  const ok = restored && same && back[0x1000] === 0x5A && back[0x0140] === 0x0F;
  if (!ok) bad++;
  console.log(`${ok ? 'ok  ' : 'FAIL'} save: WRAM restored from localStorage before Reset (${restored}), pagehide image = WRAM (${same}), ` +
    `pal_buf $6140 = $0F (MenuPalBlack) in the stored image`);
}
const lit = lastImg ? lastImg.data.some((v, i) => (i & 3) !== 3 && v) : false;
const okMain = m && m.frame >= 55 && blits > 0 && lastImg && !statusEl.textContent && Object.keys(BTN).length === 8;
if (!okMain) bad++;
console.log(`${okMain ? 'ok  ' : 'FAIL'} main.js under DOM stubs: ${m ? m.frame : 0} frames in 60 rAF ticks, ${blits} blits, ` +
  `status "${statusEl.textContent}", main_state ${m ? m.rd(0x40) : '-'}, image ${lit ? 'has colour' : 'all black (stub states draw nothing yet)'}`);
console.log(`${bad ? 'FAIL' : 'ok  '} index.html + ${seen.size} ES modules served; loadData via fetch: ` +
  `${Object.keys(data.bin).length} bin + ${Object.keys(data.json).length} json files, chr ${data.chr.length} B`);
process.exit(bad ? 1 : 0);
