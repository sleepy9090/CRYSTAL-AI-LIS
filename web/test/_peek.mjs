// scratch (s53): where does the emulator screen sit in the port's static area render?
import { readFileSync } from 'node:fs';
import { inflateSync } from 'node:zlib';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { renderArea } from '../src/game/world/bg.js';

const [ramF, wramF, pngF] = process.argv.slice(2);
function readPng(file) {
  const b = readFileSync(file); let p = 8, w = 0, h = 0; const idat = [];
  while (p < b.length) {
    const len = b.readUInt32BE(p), tag = b.toString('latin1', p + 4, p + 8), d = b.subarray(p + 8, p + 8 + len);
    if (tag === 'IHDR') { w = d.readUInt32BE(0); h = d.readUInt32BE(4); } else if (tag === 'IDAT') idat.push(d);
    p += 12 + len;
  }
  const raw = inflateSync(Buffer.concat(idat)), rgb = new Uint8Array(w * h * 3);
  for (let y = 0; y < h; y++) rgb.set(raw.subarray(y * (w * 3 + 1) + 1, (y + 1) * (w * 3 + 1)), y * w * 3);
  return { w, h, rgb, f: raw[0] };
}
const data = await loadData((n) => readFileSync(new URL('../data/' + n, import.meta.url)));
const m = new Machine(data);
m.mem.ram.set(readFileSync(ramF)); m.mem.wram.set(readFileSync(wramF));
const r = m.mem.ram, hx = (a) => r[a].toString(16).padStart(2, '0');
console.log('area', hx(0x6C), 'cam $02-05', [2, 3, 4, 5].map(hx).join(' '), '$07D8-DB', [0x7D8, 0x7D9, 0x7DA, 0x7DB].map(hx).join(' '),
  'pos $34-37', [0x34, 0x35, 0x36, 0x37].map(hx).join(' '), '$00', hx(0), 'v67', hx(0x67), 'R0/R1', hx(0x7F0), hx(0x7F1), 'visited', [...m.mem.wram.subarray(0x2F0, 0x2FC)].map((v) => v.toString(16)).join(' '));
const { w, h, fb } = renderArea(m, { visited: 'ram' });
const scr = readPng(pngF);
console.log('png', scr.w, scr.h, 'filter', scr.f, 'area', w, h);
const nes = data.palettes.nesRgb;
const score = (dx, dy, step = 1, rows = new Set()) => {
  let ok = 0, n = 0;
  for (let y = 0; y < 190; y += step) {
    if (dy + y < 0 || dy + y >= h) continue;
    for (let x = 0; x < 256; x += step) {
      if (dx + x < 0 || dx + x >= w) continue;
      n++;
      const c = nes[fb[(dy + y) * w + dx + x]], o = (y * 256 + x) * 3;
      if (scr.rgb[o] === (c >> 16 & 255) && scr.rgb[o + 1] === (c >> 8 & 255) && scr.rgb[o + 2] === (c & 255)) ok++;
      else rows.add(y);
    }
  }
  return [ok, n];
};
let best = [-1, 0, 0];
for (let dy = -40; dy < h - 100; dy++) {
  for (let dx = -16; dx <= w - 240; dx++) {
    const [ok, n] = score(dx, dy, 3);
    if (n > 2000 && ok / n > best[0]) best = [ok / n, dx, dy];
  }
}
console.log('best match fraction', best);
const rows = new Set(), [ok, n] = score(best[1], best[2], 1, rows);
console.log(`at best: ${ok}/${n} px equal`);
const j = JSON.parse(readFileSync(new URL('ppu_frames/area1_enter.json', import.meta.url)));
const efb = Buffer.from(j.fb, 'hex');
for (const L of [0, 1, 8, 100, 189, 190, 191]) {
  const [v, x, c, mk, b] = j.lines[L];
  console.log(`line ${L}: v=${v.toString(16)} (ny=${(v >> 11) & 1} nx=${(v >> 10) & 1} cy=${(v >> 5) & 31} fy=${v >> 12} cx=${v & 31}) x=${x} ctrl=${c.toString(16)} mask=${mk.toString(16)} chr=${b}`);
}
console.log('emu pal', j.pal, 'port pal_buf', [...m.mem.wram.subarray(0x140, 0x160)].map((v) => v.toString(16).padStart(2, '0')).join(''));
const pairs = {}, perRow = [];
const [dx, dy] = [best[1], best[2]];
for (let y = 0; y < 190; y++) {
  let d = 0;
  for (let x = 0; x < 256; x++) {
    if (dy + y < 0) continue;
    const got = fb[(dy + y) * w + dx + x], want = efb[y * 256 + x];
    if (got !== want) { d++; const k = `${want.toString(16)}<-${got.toString(16)}`; pairs[k] = (pairs[k] || 0) + 1; }
  }
  perRow.push(d);
}
console.log('emu<-port colour pairs', pairs);
console.log('diffs per row', perRow.join(' '));

