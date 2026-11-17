// re-c web port — browser entry: load data, canvas blit at integer scale, fixed-step 60.0988 Hz loop.
// Serve the repo root (python3 -m http.server) and open /web/index.html.
import { loadData } from './data.js';
import { Machine, FRAME_HZ } from './game/machine.js';
import { Input } from './input.js';

const $ = (id) => document.getElementById(id);
const status = (s) => { $('status').textContent = s; };

async function start() {
  status('loading data…');
  const data = await loadData((n) => fetch('data/' + n).then((r) => {
    if (!r.ok) throw new Error(`data/${n}: HTTP ${r.status}`);
    return r.arrayBuffer();
  }));
  const m = new Machine(data, { log: (s) => console.warn('[re-c]', s) });
  m.reset();
  const input = new Input(window);

  const canvas = $('screen');
  const ctx = canvas.getContext('2d');
  const img = ctx.createImageData(256, 240);
  const px = new Uint32Array(img.data.buffer);
  // NES colour index -> ABGR (little-endian ImageData): palettes.json nesRgb is 0xRRGGBB
  const lut = Uint32Array.from(data.palettes.nesRgb, (c) => 0xFF000000 | ((c & 0xFF) << 16) | (c & 0xFF00) | (c >> 16));
  const blit = (fb) => { for (let i = 0; i < fb.length; i++) px[i] = lut[fb[i] & 0x3F]; ctx.putImageData(img, 0, 0); };

  const fit = () => {
    const s = Math.max(1, Math.floor(Math.min(innerWidth / 256, (innerHeight - 40) / 240)));
    canvas.style.width = `${256 * s}px`; canvas.style.height = `${240 * s}px`;
  };
  addEventListener('resize', fit); fit();

  const step = 1000 / FRAME_HZ;
  let acc = 0, last = performance.now(), paused = false;
  document.addEventListener('visibilitychange', () => { paused = document.hidden; last = performance.now(); acc = 0; });
  addEventListener('keydown', (e) => { if (e.code === 'KeyP') { paused = !paused; status(paused ? 'paused (P)' : ''); } });

  function tick(now) {
    if (!paused) {
      acc += Math.min(now - last, 250);       // never run more than ~15 frames to catch up
      let ran = false;
      while (acc >= step) { m.runFrame(input.read()); acc -= step; ran = true; }
      if (ran) blit(m.ppu.fb);
    }
    last = now;
    requestAnimationFrame(tick);
  }
  status('');
  requestAnimationFrame(tick);
  window.reC = m;                              // debugging handle
}

start().catch((e) => { status(String(e)); console.error(e); });
