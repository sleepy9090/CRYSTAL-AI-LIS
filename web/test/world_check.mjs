// node web/test/world_check.mjs [--areas 01,3C,..] [--visited] [--verbose] — Ph7 web 4/8 (s53).
// For every area in web/data/areas.json with a usable k0 header: Machine.reset(), $6C = area, $6D = 0,
// world/area.js loadAreaMap() (k0 -> k4 -> k2 -> k1 -> record, all into RAM at the ROM addresses),
// palette.js paletteBuild(), then world/bg.js renderArea() -> RGB, diffed pixel for pixel against
// gfx/areas/area_NNN.png from `python3 tools/render_level.py --all` (or area_NNN_v.png with --visited,
// from `render_level.py --all --visited`). Also checks the RAM the loaders wrote against areas.json.
// Exit code 1 on any differing pixel or RAM value.
import { readFileSync, existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { inflateSync } from 'node:zlib';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { loadAreaMap } from '../src/game/world/area.js';
import { renderArea } from '../src/game/world/bg.js';
import { paletteBuild } from '../src/game/palette.js';
import { unprefetch } from '../src/nes/ppu.js';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const ROOT = path.dirname(WEB);
const argv = process.argv.slice(2);
const visited = argv.includes('--visited');
const verbose = argv.includes('--verbose');
const ai = argv.indexOf('--areas');
const only = ai >= 0 ? argv[ai + 1].split(',').map((s) => parseInt(s, 16)) : null;
// --ref plain|visited picks the reference independently of the render mode (negative test of the check)
const ri = argv.indexOf('--ref');
const refVisited = ri >= 0 ? argv[ri + 1] === 'visited' : visited;

/** 8-bit RGB PNG, every row filter 0 (tools/meta_dump.write_png) -> {w, h, rgb} */
function readPng(file) {
  const b = readFileSync(file);
  let p = 8, w = 0, h = 0; const idat = [];
  while (p < b.length) {
    const len = b.readUInt32BE(p), tag = b.toString('latin1', p + 4, p + 8), d = b.subarray(p + 8, p + 8 + len);
    if (tag === 'IHDR') {
      w = d.readUInt32BE(0); h = d.readUInt32BE(4);
      if (d[8] !== 8 || d[9] !== 2 || d[12] !== 0) throw new Error(`${file}: not 8-bit RGB non-interlaced`);
    } else if (tag === 'IDAT') idat.push(d);
    p += 12 + len;
  }
  const raw = inflateSync(Buffer.concat(idat)), rgb = new Uint8Array(w * h * 3);
  for (let y = 0; y < h; y++) {
    const o = y * (w * 3 + 1);
    if (raw[o] !== 0) throw new Error(`${file}: row ${y} filter ${raw[o]} (only filter 0 supported)`);
    rgb.set(raw.subarray(o + 1, o + 1 + w * 3), y * w * 3);
  }
  return { w, h, rgb };
}

const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const nesRgb = data.palettes.nesRgb;
const m = new Machine(data);
let fail = 0, ok = 0, skipped = 0;
for (const key of Object.keys(data.areas.areas).sort()) {
  const id = parseInt(key, 16);
  if (only && !only.includes(id)) continue;
  const a = data.areas.areas[key];
  if (!a.header || !a.header.k0 || a.header.k0.garbage) { skipped++; continue; }
  const ref = path.join(ROOT, 'gfx/areas', `area_${String(id).padStart(3, '0')}${refVisited ? '_v' : ''}.png`);
  if (!existsSync(ref)) { console.log(`skip $${key}: no ${path.relative(ROOT, ref)}`); skipped++; continue; }

  m.reset();
  m.wr(0x6C, id); m.wr(0x6D, 0);
  const sounds = [];
  m.playSound = (s) => sounds.push(s);
  try { loadAreaMap(m); } catch (e) { console.log(`FAIL $${key}: ${e.message}`); fail++; continue; }
  paletteBuild(m);

  // RAM written by the loaders vs areas.json
  const k0 = a.header.k0, k1 = a.header.k1, errs = [];
  const want = { 0x62FC: k0.W - 1, 0x62FD: k0.H - 1, 0x62FE: k0.f62FE, 0x62FF: k0.f62FF,
    0x07E0: k1.pal[0], 0x07E1: k1.pal[1], 0x07E2: k1.pal[2], 0x07E3: 0x7F, 0x67: k1.v67, 0x69: k1.v69,
    0x07F0: k1.R0, 0x07F1: k1.R1 };
  if (a.record && 'e6' in a.record) Object.assign(want, { 0x07E6: a.record.e6, 0x07E7: a.record.e7, 0x07F4: a.record.r4, 0x07F5: a.record.r5 });
  if (a.header.k2.length && a.header.k2[0]) {
    const p = a.header.k2[0];
    Object.assign(want, { 0x34: p.x & 255, 0x35: p.x >> 8, 0x36: p.y & 255, 0x37: p.y >> 8 });
  }
  for (const [ad, v] of Object.entries(want)) if (m.rd(+ad) !== v) errs.push(`$${(+ad).toString(16)}=${m.rd(+ad)} want ${v}`);
  for (let i = 0; i < 0x80; i++) {
    const r = i >> 3, c = i & 7, v = (k0.grid[r] && c < k0.W) ? k0.grid[r][c] : 0x80;
    if (m.rd(0x6300 + i) !== v) { errs.push(`grid[${i}]`); break; }
  }
  if (sounds.length !== 1 || sounds[0] !== k0.snd) errs.push(`PlaySound ${sounds} want ${k0.snd}`);

  const { w, h, fb } = renderArea(m, { visited });
  const png = readPng(ref);
  let diff = 0, first = -1;
  if (png.w !== w || png.h !== h) errs.push(`size ${w}x${h} vs ref ${png.w}x${png.h}`);
  else {
    for (let i = 0; i < w * h; i++) {
      const c = nesRgb[fb[i]], o = i * 3;
      if (png.rgb[o] !== (c >> 16 & 255) || png.rgb[o + 1] !== (c >> 8 & 255) || png.rgb[o + 2] !== (c & 255)) {
        diff++; if (first < 0) first = i;
      }
    }
  }
  if (diff || errs.length) {
    fail++;
    console.log(`FAIL $${key} ${k0.W}x${k0.H}: ${diff} px` + (diff ? ` first (${first % w},${(first / w) | 0})` : '') +
      (errs.length ? `  RAM: ${errs.slice(0, 6).join(', ')}` : ''));
  } else {
    ok++;
    if (verbose) console.log(`ok   $${key} ${k0.W}x${k0.H}`);
  }
}
console.log(`${ok} area(s) identical, ${fail} differ, ${skipped} skipped (null/garbage k0 or no reference)`);

// ---------------------------------------------------------------- emulator frames (world_frames/README.md)
// Machine RAM/WRAM = tools.emu after the frame; renderArea(visited: $62F0 bitmap) cropped at the drawn
// camera: screen line 0 = world y signed($04/$05) + 16, column 0 = world x signed($02/$03). That offset is
// cross-checked against the PPU v/x the emulator latched for line 0 (2 screens = 480 px vertical ring).
// Compared: lines before the first IRQ split (mask/CHR change), BG-enabled columns, outside every OAM box.
const FR = path.join(WEB, 'test/world_frames');
const s16 = (lo, hi) => ((hi << 8 | lo) << 16) >> 16;
let ffail = 0, fok = 0;
if (!only && !visited) {
  for (const name of ['area1_enter', 'gameplay_first']) {
    const ramF = path.join(FR, `${name}.ram`);
    if (!existsSync(ramF)) { console.log(`skip ${name}: no ${path.relative(ROOT, ramF)}`); continue; }
    const j = JSON.parse(readFileSync(path.join(WEB, 'test/ppu_frames', `${name}.json`), 'utf8'));
    const efb = Buffer.from(j.fb, 'hex'), oam = Buffer.from(j.oam, 'hex');
    m.reset();
    m.mem.ram.set(readFileSync(ramF)); m.mem.wram.set(readFileSync(path.join(FR, `${name}.wram`)));
    const r = m.mem.ram;
    const dx = s16(r[2], r[3]), dy = s16(r[4], r[5]) + 16;
    const errs = [];
    {   // loaders run fresh for this area/entry (WRAM flags from the dump) must reproduce the emulator's RAM
      const m2 = new Machine(data);
      m2.reset();
      m2.mem.wram.set(m.mem.wram);
      m2.wr(0x6C, r[0x6C]); m2.wr(0x6D, r[0x6D]);
      loadAreaMap(m2);
      const addrs = [0x62F0, 0x62F1, 0x62F2, 0x62F3, 0x62F4, 0x62F5, 0x62F6, 0x62F7, 0x62F8, 0x62F9, 0x62FA, 0x62FB,
        0x62FC, 0x62FD, 0x62FE, 0x62FF, 0x07E0, 0x07E1, 0x07E2, 0x07E3, 0x07E6, 0x07E7, 0x07F0, 0x07F1, 0x07F4, 0x07F5,
        0x66, 0x69, 0x6A, 0x6B];   // not $68: ObjBgProbe 26:9A30 reuses $68/$69 as its page ptr (26:9A87/9AB1)
      for (let i = 0; i < 0x80; i++) addrs.push(0x6300 + i);
      for (const ad of addrs) if (m2.rd(ad) !== m.rd(ad)) errs.push(`RAM $${ad.toString(16)} port ${m2.rd(ad)} emu ${m.rd(ad)}`);
      if ((m2.rd(0x67) & 0xFC) !== (m.rd(0x67) & 0xFC)) errs.push('$67&$FC');
      // PaletteBuild: sprite ids $07E4/$07E5 + no-fade mask $07E8 are not area data -> take the emulator's
      for (const ad of [0x07E4, 0x07E5, 0x07E8]) m2.wr(ad, m.rd(ad));
      for (let i = 0; i < 32; i++) m2.wr(0x6140 + i, 0xFF);
      paletteBuild(m2);
      for (let i = 0; i < 32; i++) if (m2.rd(0x6140 + i) !== m.rd(0x6140 + i)) { errs.push(`pal_buf[${i}] port ${m2.rd(0x6140 + i)} emu ${m.rd(0x6140 + i)}`); break; }
    }
    const { w, h, fb } = renderArea(m, { visited: 'ram' });
    const [v0, x0, , mask0, chr0] = j.lines[0];
    const v = unprefetch(v0);
    const ppuY = ((v >> 11) & 1) * 240 + ((v >> 5) & 31) * 8 + (v >> 12);
    const ppuX = ((v >> 10) & 1) * 256 + (v & 31) * 8 + x0;
    if (ppuY !== ((dy % 480) + 480) % 480) errs.push(`line-0 PPU y ${ppuY} != camera ${dy} mod 480`);
    if ((ppuX & 255) !== (dx & 255)) errs.push(`line-0 PPU x ${ppuX} != camera ${dx} mod 256`);
    const spr = new Uint8Array(256 * 240), tall = (j.lines[0][2] & 0x20) ? 16 : 8;
    for (let i = 0; i < 64; i++) {
      const sy = oam[i * 4] + 1, sx = oam[i * 4 + 3];
      for (let y = sy; y < Math.min(240, sy + tall); y++) for (let x = sx; x < Math.min(256, sx + 8); x++) spr[y * 256 + x] = 1;
    }
    let n = 0, diff = 0, first = '', lines = 0;
    for (let L = 0; L < 240; L++) {
      const [, , , mk, chr] = j.lines[L];
      if (mk !== mask0 || String(chr) !== String(chr0)) break;
      lines++;
      const wy = dy + L;
      if (wy < 0 || wy >= h) continue;
      for (let x = (mk & 2) ? 0 : 8; x < 256; x++) {
        const wx = dx + x;
        if (wx < 0 || wx >= w || spr[L * 256 + x]) continue;
        n++;
        if (fb[wy * w + wx] !== efb[L * 256 + x]) { diff++; if (!first) first = `(${x},${L}) emu ${efb[L * 256 + x]} port ${fb[wy * w + wx]}`; }
      }
    }
    if (n < 10000) errs.push(`only ${n} px comparable`);
    const bad = diff || errs.length;
    bad ? ffail++ : fok++;
    console.log(`${bad ? 'FAIL' : 'ok  '} ${name.padEnd(15)} area $${r[0x6C].toString(16)} cam (${dx},${dy}) lines 0-${lines - 1}: ` +
      `${diff}/${n} BG px differ${first ? ' first ' + first : ''}${errs.length ? '  ' + errs.join('; ') : ''}`);
  }
  console.log(`${fok} emulator frame(s) identical, ${ffail} differ`);
}
process.exit(fail || ffail ? 1 : 0);
