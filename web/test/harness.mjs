// re-c web port — headless harness helpers (Ph7 web 3/8), shared by web/test/run.mjs and the checks.
//   parseKeys(specs)       tools.emu --keys syntax -> Map(frame -> ROM-order button byte)
//   compareColumns()/compareRow(m, frame)   the tools/emu/compare.py CSV, same columns + values
//   encodePng(w, h, rgb)   8-bit RGB PNG (node:zlib only), fbToRgb(fb, nesRgb, scale)
import { readFileSync } from 'node:fs';
import { deflateSync, crc32 } from 'node:zlib';
import { BTN } from '../src/input.js';

// ---------------------------------------------------------------- key scripts
// tools.emu masks put A in b0 (pads.py order A B Sel St U D L R). The port takes the ROM-order byte
// ReadPadRaw 31:FF28 assembles (A b7 ... Right b0, input.js BTN), so names map straight to BTN here
// and an emu numeric mask has to go through rev8().
export const rev8 = (b) => parseInt((b & 0xFF).toString(2).padStart(8, '0').split('').reverse().join(''), 2);

export function parseButtons(s) {
  let m = 0;
  for (let part of s.replace(/,/g, '+').split('+')) {
    part = part.trim().toUpperCase();
    if (!part || part === 'NONE') continue;
    if (!(part in BTN)) throw new Error(`unknown button "${part}" (use ${Object.keys(BTN).join(' ')})`);
    m |= BTN[part];
  }
  return m;
}

/** ['120:START', '300:A+RIGHT:8', '@file'] -> Map(frame -> mask); default hold 2 frames (= debug.parse_keys) */
export function parseKeys(specs) {
  const flat = [];
  for (const s of specs) {
    if (s.startsWith('@')) {
      for (let ln of readFileSync(s.slice(1), 'utf8').split('\n')) {
        ln = ln.split('#', 1)[0].trim();
        if (ln) flat.push(...ln.split(/\s+/));
      }
    } else flat.push(...s.split(/\s+/).filter(Boolean));
  }
  const keys = new Map();
  for (const s of flat) {
    const p = s.split(':');
    const start = parseInt(p[0], 10), mask = parseButtons(p[1] || ''), n = p.length > 2 ? parseInt(p[2], 10) : 2;
    if (!Number.isFinite(start) || !Number.isFinite(n)) throw new Error(`bad key spec "${s}"`);
    for (let f = start; f < start + n; f++) keys.set(f, (keys.get(f) || 0) | mask);
  }
  return keys;
}

// ---------------------------------------------------------------- compare CSV (tools/emu/compare.py)
export const SLOTS = 32;
export function compareColumns() {
  const cols = ['frame', 'state', 'sub', 'area', 'entry', 'pos_x', 'pos_y', 'cam_x', 'cam_y', 'music'];
  for (let c = 0; c < 4; c++) cols.push(`snd${c}_period`);
  cols.push('pal_crc');
  for (let s = 0; s < SLOTS; s++) {
    const n = s.toString(16).toUpperCase().padStart(2, '0');
    cols.push(`o${n}_ai`, `o${n}_x`, `o${n}_y`);
  }
  return cols;
}

/** one row after a finished frame; `frame` = the script frame number of that frame */
export function compareRow(m, frame) {
  const r = m.mem.ram, w = m.mem.wram;
  const out = [frame, r[0x40], r[0x41], r[0x6C], r[0x6D],
    r[0x34] | r[0x35] << 8, r[0x36] | r[0x37] << 8,
    r[0x02] | r[0x03] << 8, r[0x04] | r[0x05] << 8, r[0x102]];
  for (let c = 0; c < 4; c++) out.push(r[0x174 + c] | r[0x17A + c] << 8);
  out.push(crc32(w.subarray(0x140, 0x160)) >>> 0);
  for (let s = 0; s < SLOTS; s++) {
    const ai = r[0x4A0 + s];
    if (ai) out.push(ai, r[0x70 + s] | r[0x90 + s] << 8, r[0xB0 + s] | r[0xD0 + s] << 8);
    else out.push(0, 0, 0);
  }
  return out;
}

// ---------------------------------------------------------------- PNG
const chunk = (tag, data) => {
  const td = Buffer.concat([Buffer.from(tag, 'latin1'), data]);
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td) >>> 0);
  return Buffer.concat([len, td, crc]);
};

/** rgb: Uint8Array w*h*3 -> PNG bytes (colour type 2, filter 0 on every row) */
export function encodePng(w, h, rgb) {
  const raw = Buffer.alloc((w * 3 + 1) * h);
  for (let y = 0; y < h; y++) {
    raw[y * (w * 3 + 1)] = 0;
    raw.set(rgb.subarray(y * w * 3, (y + 1) * w * 3), y * (w * 3 + 1) + 1);
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4);
  ihdr[8] = 8; ihdr[9] = 2; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
  return Buffer.concat([Buffer.from([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
    chunk('IHDR', ihdr), chunk('IDAT', deflateSync(raw, { level: 9 })), chunk('IEND', Buffer.alloc(0))]);
}

/** 256x240 NES colour indices -> RGB bytes (palettes.json nesRgb, 0xRRGGBB), nearest-neighbour scale */
export function fbToRgb(fb, nesRgb, scale = 1) {
  const W = 256 * scale, H = 240 * scale, out = new Uint8Array(W * H * 3);
  for (let y = 0; y < H; y++) {
    for (let x = 0; x < W; x++) {
      const c = nesRgb[fb[(y / scale | 0) * 256 + (x / scale | 0)] & 0x3F], o = (y * W + x) * 3;
      out[o] = c >> 16 & 255; out[o + 1] = c >> 8 & 255; out[o + 2] = c & 255;
    }
  }
  return { w: W, h: H, rgb: out };
}
