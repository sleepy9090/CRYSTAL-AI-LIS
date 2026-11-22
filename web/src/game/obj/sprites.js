// re-c web port — object sprite pass (Ph7 web 5/8, s54): ObjSpriteDraw 28:800E, ObjDrawMeta 28:822E,
// ObjHideUnusedOam 28:80A5, OrbitSprites 28:80D6. REVERSE.md "Object sprite pass" + "Metasprite drawing".
//
//   spriteDraw(m)   one call of 28:800E: OAM shadow $0200 (sprite 0 untouched), obj $05C0/$05E0, $0380 b7, $07EF
//
// Ported instruction by instruction. The zero-page temps $10-$1E are written to RAM like the ROM does, so
// web/test/sprite_check.mjs can diff the whole RAM against tools.emu (tools/emu/sprite_capture.py).
// ROM tables + metasprite records are read through CPU addresses from sprites_b28.bin (banks 28-29 = $8000-$BFFF).
//
// Quirks kept (all checked against the emulator):
//  - an entry whose x wraps is skipped, but its OAM x byte is already written (the next entry overwrites it,
//    or it stays when it was the object's last entry);
//  - "lower part" entries (attr b2) of a sinking object ($0380 b6) end the object after writing y + attr;
//  - mirrored records ($FF redirect) place x at (~dx - 8) = -dx - 9, not -dx - 8;
//  - the orbit ring skips wrapped x without writing y; the burst writes x before its y wrap check;
//  - the orbit/burst always writes OAM $E0-$FC after the hide pass (objects drawn there are overwritten);
//  - $1D/$1E ($0340 b7 palette-cycle value) are computed and never used.

const OBJ_X_LO = 0x70, OBJ_X_HI = 0x90, OBJ_Y_LO = 0xB0, OBJ_Y_HI = 0xD0;
const OBJ_META_ID = 0x0300, OBJ_TILE_OFS = 0x0320, OBJ_FLAGS2 = 0x0340, OBJ_META_DIR = 0x0360, OBJ_FLAGS = 0x0380;
const OBJ_ACTIVE = 0x04A0, OBJ_ANIM = 0x04E0, OBJ_OVERLAY = 0x0580, OBJ_SCREEN_X = 0x05C0, OBJ_SCREEN_Y = 0x05E0;
const OAM = 0x0200;
const OBJ1_FLASH = 0x0710, OBJ1_OAM_OFS = 0x07EF, OBJ_DRAW_ROT = 0x07FF;
const ORBIT_LEVEL = 0x06C0, ORBIT_BURST = 0x06C1;
// bank 28 tables (labels.csv)
const DRAW_ORDER_OFS = 0x83D8, DRAW_ORDER = 0x83DC, META_PTR_A = 0x845C, META_PTR_B = 0x855C;
const ORBIT_SINE = 0x81BA, ORBIT_TILE = 0x81DA, BURST_DIR = 0x81DE, BURST_RADIUS = 0x81FE;
// zero page temps
const T_OAM = 0x10, T11 = 0x11, T_SX = 0x12, T_SY = 0x13, T_PTR = 0x15, T17 = 0x17, T18 = 0x18, T_PRI = 0x19,
  T_TILE = 0x1A, T_LEFT = 0x1B, T_MIRROR = 0x1C, T1D = 0x1D, T1E = 0x1E;

/** bank 28/29 byte at CPU address a ($8000-$BFFF). Meta id $9A's pointer is $0000 (tools/meta_ptr_check.py,
 *  the only id leaving the banks), so its "record" is zero-page RAM, as on the 6502. */
function rom(m, a) {
  a &= 0xFFFF;
  if (a < 0x2000) return m.rd(a);
  if (a < 0x8000 || a >= 0xC000) throw new Error(`sprite pass: read $${a.toString(16)} outside banks 28/29 / RAM`);
  return m.data.spritesB28[a - 0x8000];
}

// The 6502 Y register as the pass leaves it (s198). AreaEnter's PlaySound($2F) 31:E4EC stores it in
// snd_saved_y $010F (nothing reads it back there). Set at every Y write of 28:800E's paths, see spriteDrawY().
let regY = 0;
/** Y at ObjSpriteDraw's last RTS: the last loop slot's Y (LDY $11 index, TAY screen x, or ObjDrawMetaMain's
 *  record index), 0 after the obj1_flash loop, then OrbitSprites' TAYs (orbit: tile index; burst: BurstRadius). */
export function spriteDrawY() { return regY; }

/** ObjSpriteDraw 28:800E. Returns the X register at its RTS (s172, AI $0B uses it): 0 after a full OrbitSprites
 *  loop, else ObjHideUnusedOam's X (0, or obj1_oam_ofs + 4 * n when obj1_flash b7 is set) */
export function spriteDraw(m) {
  const rd = (a) => m.rd(a), wr = (a, v) => m.wr(a, v);
  wr(T1E, (rd(0x08) & 2) ? 3 : 0);                          // 800E: $1E = $08 b1 ? 3 : 0 (dead, see ObjDrawMeta)
  wr(OBJ_DRAW_ROT, rd(OBJ_DRAW_ROT) - 1);                   // 801A
  wr(T11, rom(m, DRAW_ORDER_OFS + (rd(OBJ_DRAW_ROT) & 3)));
  wr(T_LEFT, 0x20);
  wr(T_OAM, 4);
  do {
    regY = rd(T11);                                         // 8030 LDY $11
    const x = rom(m, DRAW_ORDER + regY);
    drawSlot(m, x);
    wr(T11, rd(T11) + 1);                                   // 808E
    wr(T_LEFT, rd(T_LEFT) - 1);
  } while (rd(T_LEFT) !== 0);
  const hx = hideUnusedOam(m);                              // 8094
  return orbitSprites(m) ?? hx;                             // 8097 JMP
}

function setOffscreen(m, x) { m.wr(OBJ_FLAGS + x, m.rd(OBJ_FLAGS + x) | 0x80); }   // 809A

/** loop body 8036-808B for slot x */
function drawSlot(m, x) {
  const rd = (a) => m.rd(a), wr = (a, v) => m.wr(a, v);
  if (rd(OBJ_ACTIVE + x) === 0) return setOffscreen(m, x);
  let d = rd(OBJ_X_LO + x) - rd(0x02);
  const sx = d & 0xFF;
  regY = sx;                                                // 8040 TAY
  if (((rd(OBJ_X_HI + x) - rd(0x03) - (d < 0 ? 1 : 0)) & 0xFF) !== 0) return setOffscreen(m, x);
  wr(T_SX, sx);
  wr(OBJ_SCREEN_X + x, sx);
  const ylo = rd(OBJ_Y_LO + x), cyl = rd(0x04);             // 804D CMP / SBC: 16-bit y - cam_y, hi must be 0
  if (((rd(OBJ_Y_HI + x) - rd(0x05) - (ylo < cyl ? 1 : 0)) & 0xFF) !== 0) return setOffscreen(m, x);
  d = ylo - cyl;
  let sy = d & 0xFF;
  if (d < 0) sy = (sy - 0x0F - 1) & 0xFF;                   // 805E SBC #$0F with C = 0: 240-row wrap
  if (sy >= 0xE8) return;                                   // 8060: skipped, flags untouched
  if (sy < 0x10) return setOffscreen(m, x);
  wr(T_SY, sy);
  wr(OBJ_SCREEN_Y + x, sy);
  const fl = rd(OBJ_FLAGS + x) & 0x7F;
  wr(OBJ_FLAGS + x, fl);
  wr(T17, fl << 1);
  if (rd(T_OAM) === 0) return;                              // OAM full
  if ((fl & 4) && ((x + rd(OBJ_DRAW_ROT)) & 1)) return;     // 807F half-rate flicker (ADC low bit, C = 0)
  drawMeta(m, x);
}

/** ObjDrawMeta 28:822E (also reached through 28:8000 for slot $1F: states/pause.js objSlot1FDraw) */
export function drawMeta(m, x) {
  const rd = (a) => m.rd(a), wr = (a, v) => m.wr(a, v);
  const t17 = rd(T17);
  if (t17 & 0x80) wr(T_SY, rd(T_SY) + 3);                   // 8230 ASL -> C = $0380 b6; ADC #$02 + C
  if (!(t17 & 4)) return drawMetaMain(m, x);                // $0380 b1: overlay first
  const id = rd(OBJ_META_ID + x), fl = rd(OBJ_FLAGS + x);
  wr(OBJ_FLAGS + x, (fl & 0xDF) | 1);
  wr(OBJ_META_ID + x, rd(OBJ_OVERLAY + x));
  drawMetaMain(m, x);
  wr(OBJ_FLAGS + x, fl);
  wr(OBJ_META_ID + x, id);
  if (rd(T_OAM) !== 0) drawMetaMain(m, x);
}

/** ObjDrawMetaMain 28:8268 */
function drawMetaMain(m, x) {
  const rd = (a) => m.rd(a), wr = (a, v) => m.wr(a, v);
  if (x === 1) wr(OBJ1_OAM_OFS, rd(T_OAM));
  wr(T_TILE, 0);
  wr(T_MIRROR, 0);
  let id = rd(OBJ_META_ID + x);
  if (!(rd(OBJ_FLAGS + x) & 1)) id = (id + rd(OBJ_META_DIR + x)) & 0xFF;
  const tab = (id & 0x80) ? META_PTR_B : META_PTR_A, yi = (id << 1) & 0xFF;
  let ptr = rom(m, tab + yi) | rom(m, tab + yi + 1) << 8;
  let n;
  for (;;) {                                                // 829D
    n = rom(m, ptr);
    if (n === 0) { wr(T_PTR, ptr); wr(T_PTR + 1, ptr >> 8); regY = 0; return; }   // 82B8 RTS, Y = 0
    if (n < 0x80) break;
    ptr = rom(m, ptr + 1) | rom(m, ptr + 2) << 8;           // FF lo hi: mirrored redirect
    wr(T_MIRROR, 0x80);
  }
  // 82B9: frame f = ($04E0 >> 3) & mask; data += ((N * f) & $FF) * 4 (shift-add on 8-bit A)
  wr(T17, n);
  const f = (rd(OBJ_ANIM + x) >> 3) & rom(m, ptr + 1);
  let a = 0, t18 = n;                                       // $1D = f, LSR'd once per bit
  for (let b = 0; b < 4; b++) {
    if ((f >> b) & 1) a = (a + t18) & 0xFF;
    if (b < 3) t18 = (t18 << 1) & 0xFF;
  }
  wr(T1D, f >> 4);
  ptr = (ptr + a * 4) & 0xFFFF;
  wr(T_PTR, ptr); wr(T_PTR + 1, ptr >> 8);
  const fl = rd(OBJ_FLAGS + x);
  const t18f = (fl << 1) & 0xFF;                            // 8301: b7 = $0380 b6 (sink)
  wr(T18, t18f);
  const pri = t18f & 0x20;                                  // $0380 b4 -> attr b5 (behind BG)
  wr(T_PRI, pri);
  const tileAdd = (rd(OBJ_TILE_OFS + x) + ((fl & 0x20) << 1)) & 0xFF;
  wr(T_TILE, tileAdd);
  wr(T1D, (rd(OBJ_FLAGS2 + x) & 0x80) ? rd(T1E) : 0);       // 8320: dead store
  const mirror = rd(T_MIRROR) & 0x80;
  const sx = rd(T_SX), sy = rd(T_SY);
  let o = rd(T_OAM), y = 2, left = n;
  // Y at the RTS = y: the $80 end marker's index, a skipped wrapped entry's index, or the next entry after a
  // full write / full OAM; a sinking object's lower part ends at y + 3 (8379, after the attr INY)
  for (;;) {                                                // MetaEmitNormal 8331 / MetaEmitHFlip 8380
    const dx = rom(m, ptr + y);
    if (dx === 0x80) break;
    const ex = mirror ? (((dx ^ 0xFF) - 8) & 0xFF) : dx;
    if (mirror) wr(T_MIRROR, ex);
    const sum = ex + sx;
    wr(OAM + 3 + o, sum);
    // ROR A puts C in b7; EOR dx: b7 = C ^ dx.b7 -> the entry wrapped horizontally, skip it
    if ((((sum & 0x100) ? 0x80 : 0) ^ ex) & 0x80) {
      left = (left - 1) & 0xFF;
      if (left === 0) break;
      y += 4;
      continue;
    }
    wr(OAM + o, rom(m, ptr + y + 1) + sy);
    let attr = rom(m, ptr + y + 2) | pri;
    if (mirror) attr ^= 0x40;
    wr(OAM + 2 + o, attr);
    if ((attr & 4) && (t18f & 0x80)) { y += 3; break; }     // 8379 BIT $18: lower part of a sinking object
    wr(OAM + 1 + o, rom(m, ptr + y + 3) + tileAdd);
    y += 4;
    o = (o + 4) & 0xFF;
    if (o === 0) break;                                     // OAM full
    left = (left - 1) & 0xFF;
    if (left === 0) break;
  }
  regY = y & 0xFF;
  wr(T17, left);
  wr(T_OAM, o);
}

/** ObjHideUnusedOam 28:80A5 */
export function hideUnusedOam(m) {
  const rd = (a) => m.rd(a), wr = (a, v) => m.wr(a, v);
  let o = rd(T_OAM);
  if (o !== 0) for (; o < 0x100; o += 4) wr(OAM + o, 0xF0);
  if (!(rd(OBJ1_FLASH) & 0x80)) return 0;                   // X = 0 (BEQ, or the INX loop wrapped)
  let n = (rd(OBJ_FLAGS + 1) & 0x40) ? 4 : 6;               // $0381 ASL -> N = b6
  let x = rd(OBJ1_OAM_OFS);
  do {
    wr(OAM + 2 + x, rd(OAM + 2 + x) & 0xFC);
    x = (x + 4) & 0xFF;
  } while (--n);
  regY = 0;                                                 // 80D2 DEY loop ends at 0
  return x;
}

/** OrbitSprites 28:80D6 */
function orbitSprites(m) {
  const rd = (a) => m.rd(a), wr = (a, v) => m.wr(a, v);
  const cx = rd(OBJ_SCREEN_X + 1), cy = rd(OBJ_SCREEN_Y + 1);
  // signed add with wrap check (CLC ADC; ROR; EOR v; BMI = skip): true if base + v (v signed) leaves 0-255
  const wraps = (v, base) => ((((v + base) & 0x100) ? 0x80 : 0) ^ v) & 0x80;
  let burst = rd(ORBIT_BURST);
  if (burst === 0) {
    if ((rd(ORBIT_LEVEL) & 0xF8) === 0) return;
    let o = 0xE0;
    for (let k = 7; k >= 0; k--) {
      wr(T_OAM, k);
      let yi = (((k & 6) << 2) + rd(0x08)) & 0x1F;
      regY = yi;                                            // 80F3 TAY
      let s = rom(m, ORBIT_SINE + yi);
      if (!(k & 1)) s = ((s ^ 0xFF) + 1) & 0xFF;
      const v = (s - 4) & 0xFF;
      wr(T11, v);
      wr(T_SX, v + cx);
      if (!wraps(v, cx)) {
        wr(OAM + 3 + o, v + cx);
        yi = (yi + 2) & 0x1F;
        wr(OAM + o, rom(m, ORBIT_SINE + yi) + cy - 0x0C);
        regY = rd(ORBIT_LEVEL) >> 3;                        // 8130 TAY
        wr(OAM + 1 + o, rom(m, ORBIT_TILE + regY));
        wr(OAM + 2 + o, 1);
      }
      o += 4;
    }
    wr(T_OAM, 0xFF);
    return 0;                                               // X = $E0 + 8 * 4 wrapped
  }
  wr(ORBIT_BURST, burst - 1);                               // 814A DEC; CMP uses the old value
  if (burst >= 0x0E) return;
  burst = rd(ORBIT_BURST);
  const radius = (yi) => {                                  // BurstRadius 28:81A5
    regY = (rom(m, BURST_DIR + yi) | burst) & 0xFF;         // 81AF TAY
    const r = rom(m, BURST_RADIUS + regY);
    return (rom(m, BURST_DIR + 1 + yi) & 0x80) ? ((r ^ 0xFF) + 1) & 0xFF : r;
  };
  let o = 0xE0;
  for (let k = 7; k >= 0; k--) {
    wr(T_OAM, k);
    const vx = (radius(k * 4) - 4) & 0xFF;
    wr(T11, vx);
    wr(T_SX, vx + cx);
    if (!wraps(vx, cx)) {
      wr(OAM + 3 + o, vx + cx);
      const vy = (radius(k * 4 + 2) - 0x0C) & 0xFF;
      wr(T_SX, vy);
      wr(T11, vy + cy);
      if (!wraps(vy, cy)) {
        wr(OAM + o, vy + cy);
        wr(OAM + 1 + o, 0x29);
        wr(OAM + 2 + o, 1);
      }
    }
    o += 4;
  }
  wr(T_OAM, 0xFF);
  return 0;
}
