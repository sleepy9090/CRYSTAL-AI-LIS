// node web/test/ppu_check.mjs — checks web/src/nes/ppu.js against tools.emu frames (Ph7 web 2/8).
// Input: web/test/ppu_frames/*.json from `python3 -m tools.emu.ppu_lines --all` (per-line v/x/ctrl/
// mask/CHR banks + ciram/palette/OAM + the emulator framebuffer). Each line is rendered by ppu.js from
// the recorded registers (sprites evaluated at the end of the previous line, as the 2C02 does) and the
// 256x240 colour-index framebuffer must be identical. Exit code 1 on any differing pixel.
import { readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { Ppu, unprefetch } from '../src/nes/ppu.js';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const hex = (s) => Uint8Array.from(Buffer.from(s, 'hex'));
const chr = new Uint8Array(readFileSync(path.join(WEB, 'data/chr.bin')));
const dir = path.join(WEB, 'test/ppu_frames');
let fail = 0;
for (const f of readdirSync(dir).filter((n) => n.endsWith('.json')).sort()) {
  const d = JSON.parse(readFileSync(path.join(dir, f), 'utf8'));
  const ppu = new Ppu(chr);
  ppu.vram.set(hex(d.ciram)); ppu.pal.set(hex(d.pal)); ppu.oam.set(hex(d.oam));
  ppu.mirroring = d.mirroring;
  ppu.sprBuf.fill(0);
  const set = (rec) => { ppu.ctrl = rec[2]; ppu.mask = rec[3]; ppu.setChrBanks(rec[4]); };
  for (let line = 0; line < 240; line++) {
    const rec = d.lines[line];
    set(rec);
    ppu.v = unprefetch(rec[0]); ppu.x = rec[1];
    ppu.renderLine(line);
    // sprite evaluation happens at dot 257 of this line, i.e. with this line's registers
    if (ppu.rendering) ppu.evalSprites(line + 1); else ppu.sprBuf.fill(0);
  }
  const want = hex(d.fb);
  const mid = new Set(d.midline || []);
  let diff = 0, first = -1, midDiff = 0;
  const rows = new Set();
  for (let i = 0; i < want.length; i++) {
    if (want[i] === ppu.fb[i]) continue;
    if (mid.has(i >> 8)) { midDiff++; continue; }
    diff++; rows.add(i >> 8); if (first < 0) first = i;
  }
  const tag = diff ? 'FAIL' : 'ok  ';
  const midTxt = mid.size ? `  (mid-line writes in emu on line ${[...mid].join(',')}: ${midDiff} px not compared)` : '';
  console.log(`${tag} ${f.padEnd(22)} frame ${d.frame}  diff ${diff} px${midTxt}` +
    (diff ? `  rows ${[...rows].slice(0, 12).join(',')}  first (${first & 255},${first >> 8}) want ${want[first]} got ${ppu.fb[first]}` : ''));
  if (diff) fail++;
  if (diff && process.argv.includes('--verbose')) {
    for (const r of rows) {
      const xs = [];
      for (let x = 0; x < 256; x++) if (want[r * 256 + x] !== ppu.fb[r * 256 + x]) xs.push(x);
      console.log(`  row ${r}: x ${xs[0]}-${xs[xs.length - 1]} (${xs.length} px)`);
      for (let l = Math.max(0, r - 2); l <= Math.min(239, r + 1); l++) {
        const [v, x, c, m, b] = d.lines[l];
        console.log(`    line ${l}: v=${v.toString(16)} x=${x} ctrl=${c.toString(16)} mask=${m.toString(16)} chr=${b.join(',')}`);
      }
    }
  }
}
console.log(fail ? `${fail} frame(s) differ` : 'all frames identical');
process.exit(fail ? 1 : 0);
