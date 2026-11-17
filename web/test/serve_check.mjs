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
Object.assign(globalThis, {
  window: globalThis, innerWidth: 800, innerHeight: 600, addEventListener() {},
  requestAnimationFrame: (cb) => { rafCb = cb; },
  document: { getElementById: (id) => (id === 'status' ? statusEl : el()), addEventListener() {}, hidden: false },
  fetch: (u, o) => realFetch(new URL(u, base), o),
});
if (!globalThis.performance) globalThis.performance = { now: () => Date.now() };
const { BTN } = await import('../src/input.js');
await import('../src/main.js');
for (let i = 0; i < 100 && !rafCb; i++) await new Promise((r) => setTimeout(r, 20));
const t0 = performance.now();
for (let i = 1; i <= 60 && rafCb; i++) { const cb = rafCb; rafCb = null; cb(t0 + i * (1000 / 60)); }
server.close();
const m = globalThis.reC;
const lit = lastImg ? lastImg.data.some((v, i) => (i & 3) !== 3 && v) : false;
const okMain = m && m.frame >= 55 && blits > 0 && lastImg && !statusEl.textContent && Object.keys(BTN).length === 8;
if (!okMain) bad++;
console.log(`${okMain ? 'ok  ' : 'FAIL'} main.js under DOM stubs: ${m ? m.frame : 0} frames in 60 rAF ticks, ${blits} blits, ` +
  `status "${statusEl.textContent}", main_state ${m ? m.rd(0x40) : '-'}, image ${lit ? 'has colour' : 'all black (stub states draw nothing yet)'}`);
console.log(`${bad ? 'FAIL' : 'ok  '} index.html + ${seen.size} ES modules served; loadData via fetch: ` +
  `${Object.keys(data.bin).length} bin + ${Object.keys(data.json).length} json files, chr ${data.chr.length} B`);
process.exit(bad ? 1 : 0);
