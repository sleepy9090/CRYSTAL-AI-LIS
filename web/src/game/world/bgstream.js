// re-c web port — BG row / column streaming into the PPU queue (Ph7 web 4/8 leftovers (a), s79).
// REVERSE.md "Screen / metatile pipeline" + "Background details". Ported instruction by instruction, including
// every zero-page temp and the bank registers ($50 $6E $6F, m.prgR6/R7): web/test/obj_check.mjs diffs the whole
// RAM + WRAM and the nametables (CIRAM) against tools.emu (cases from tools/emu/routine_capture.py).
//
// $34-$37 (pos_x_lo/hi, pos_y_lo/hi) is the position being drawn; $30/$31 the step that moved it there.
//   bgRowRender(m)        BgRowRender 31:EB91 -> ScreenRowRender 31:EBC1: 16/17 metatiles -> 32 tiles at $6000,
//                         attribute merge MetatileAttr 31:EDE5 -> $6100, record (NT row, len $20, src 0);
//                         rendering off -> PpuQueueFlush at once
//   bgColRender(m)        BgColRender 31:EC87: 15/16 metatiles -> 30 tiles at $6020, MetatileAttrCol 31:EE31,
//                         record (hi b6 = +32 increment, len $1E, src $20)
//   fullScreenDraw(m, a)  30:C831: 30 rows bottom-up from pos_y_hi+1 (TempAddOffset 26:9824 with $31 = -8);
//                         A != 0 -> 30:C739 queues the attribute table
// All three are generators: they `yield` where the ROM spins on the queue (30:C72B, WaitPpuQueue 30:C676) until
// an NMI drains it, so callers use `yield*` (mainloop.js resumes a pending generator on the next frame).
import { cpuRd, indY, setR6, setR7, switchBank16 } from '../prg.js';
import { tempAddOffset } from '../obj/util.js';
import { SPIN } from '../states/death.js';
import { queueHasSrc, queueEmpty, queueAttrAll, queuePush, ppuQueueFlush, ppuCtrlNmiOn, ppuCtrlNmiOff } from './ppuqueue.js';

const BUF = 0x6000, ATTR = 0x6100, GRID = 0x6300, VISITED = 0x62F0;
const P_XL = 0x34, P_XH = 0x35, P_YL = 0x36, P_YH = 0x37;

/** ScreenAtCamera 31:EF1F -> [screen id, grid index Y] */
function screenAtCamera(m) {
  m.wr(0x10, (m.rd(P_YH) << 3) & 0x78);
  const y = m.rd(0x10) | (m.rd(P_XH) & 7);
  return [m.rd(GRID + y), y];
}

/** ScreenVisitedBit 31:EC6C: $62F0[Y>>3] & $EC7F[Y&7] */
function screenVisitedBit(m, y) {
  m.wr(0x10, y & 7);
  return m.rd(VISITED + (y >> 3)) & cpuRd(m, 0xEC7F + (y & 7));
}

/** ScreenPagePtr 31:EF36: $11 = page hi, SwitchBank16(S>>6), $12 = pos_y_lo & $F0 */
function screenPagePtr(m, s) {
  m.wr(0x11, (s & 0x3F) | 0x80);
  switchBank16(m, (s >> 6) & 3);
  m.wr(0x12, m.rd(P_YL) & 0xF0);
}

/** ScreenSubstPtr 31:ED89: $23/$24 = $BE00 + ($67 & $3C) * 8 (the ASL carry feeds ADC #$BE) */
function screenSubstPtr(m) {
  const v = (m.rd(0x67) & 0x3C) << 3;
  m.wr(0x23, v & 0xFF);
  m.wr(0x24, 0xBE + (v >> 8));
}

/** ScreenTileSubst 31:ED99 (Y = page index, kept in $25) */
function screenTileSubst(m, a, y) {
  if (a >= 0x20) return a;
  m.wr(0x25, y);
  if (m.rd(0x1E) === 0) return a;
  const r7 = m.rd(0x6F);
  setR7(m, 9);
  m.wr(0x26, indY(m, 0x23, a));
  setR7(m, r7);
  return m.rd(0x26);
}

/** MetatileAttrBits 31:EECC: AttrReplicate[(tbl[M>>2] >> 2*(M&3)) & 3] for M = $6000[X]; Y -> $21 */
function metatileAttrBits(m, x, y) {
  m.wr(0x21, y);
  const mt = m.rd(BUF + x);
  m.wr(0x22, mt);
  let a = indY(m, 0x10, mt >> 2);
  m.wr(0x20, mt & 3);
  for (;;) {
    m.wr(0x20, m.rd(0x20) - 1);
    if (m.rd(0x20) & 0x80) break;
    a >>= 2;
  }
  return cpuRd(m, 0xEF17 + (a & 3));
}

/** 31:EE80 (row, Y step 1) / 31:EEA4 (column, Y step 8): merge two metatiles per attribute byte into $6100 */
function attrMerge(m, x, y, ystep) {
  do {
    m.wr(0x15, metatileAttrBits(m, x, y) & m.rd(0x12));
    x = (x + 2) & 0xFF;
    m.wr(0x15, (metatileAttrBits(m, x, y) & m.rd(0x13)) | m.rd(0x15));
    m.wr(ATTR + y, (m.rd(0x14) & m.rd(ATTR + y)) | m.rd(0x15));
    y = (y + ystep) & 0xFF;
    x = (x + 2) & 0xFF;
  } while (x !== m.rd(0x16));
}

function attrSetup(m) {
  setR7(m, 9);
  const i = (m.rd(0x67) & 0x3C) >> 1;            // MetatileAttrPtrTable 31:EEEF
  m.wr(0x10, cpuRd(m, 0xEEEF + i));
  m.wr(0x11, cpuRd(m, 0xEEF0 + i));
}

/** MetatileAttr 31:EDE5 (row): $2F |= (($31>>4) ^ pos_y_lo) & 8; nothing to do while $2F = 0 */
function metatileAttrRow(m) {
  m.wr(0x2F, (((m.rd(0x31) >> 4) ^ m.rd(P_YL)) & 8) | m.rd(0x2F));
  if (!m.rd(0x2F)) return;
  attrSetup(m);
  const a = m.rd(P_YL) >> 4, h = a & 1;
  const y = cpuRd(m, 0xEF07 + a);                // AttrRowOffset
  m.wr(0x12, cpuRd(m, 0xEF1B + h));
  m.wr(0x13, cpuRd(m, 0xEF1D + h));
  m.wr(0x14, ~(m.rd(0x12) | m.rd(0x13)));
  m.wr(0x16, 0x20);
  attrMerge(m, 0, y, 1);
}

/** MetatileAttrCol 31:EE31 (column): $2F |= (($30>>4) ^ pos_x_lo) & 8 */
function metatileAttrCol(m) {
  m.wr(0x2F, (((m.rd(0x30) >> 4) ^ m.rd(P_XL)) & 8) | m.rd(0x2F));
  if (!m.rd(0x2F)) return;
  attrSetup(m);
  const y = m.rd(P_XL) >> 5, h = (m.rd(P_XL) & 0x10) >> 3;
  m.wr(0x12, cpuRd(m, 0xEF1B + h));
  m.wr(0x13, cpuRd(m, 0xEF1C + h));
  m.wr(0x14, ~(m.rd(0x12) | m.rd(0x13)));
  m.wr(0x16, 0x40);
  attrMerge(m, 0x20, y, 8);
}

/** MetatileExpand 31:EDBA: 16 metatiles at $6000+X -> tile pairs via ($66),Y with $67 = ($67&$FC)|$12 (+$13) */
function metatileExpand(m, x) {
  m.wr(0x14, (m.rd(0x67) & 0xFC) | m.rd(0x12));
  m.wr(0x15, 0x10);
  do {
    m.wr(0x67, m.rd(0x14));
    const y = m.rd(BUF + x);
    m.wr(BUF + x, indY(m, 0x66, y));
    m.wr(0x67, m.rd(0x67) + m.rd(0x13));
    m.wr(BUF + 1 + x, indY(m, 0x66, y));
    x = (x + 2) & 0xFF;
    m.wr(0x15, m.rd(0x15) - 1);
  } while (m.rd(0x15));
}

/** CamNtAddr 31:ED64: $10/$11 = $2000 + (pos_y_lo>>3)*32 + (pos_x_lo>>3) */
function camNtAddr(m) {
  m.wr(0x10, m.rd(P_XL) >> 3);
  m.wr(0x11, 0x20);
  m.wr(0x10, (((m.rd(P_YL) & 0xF8) << 2) & 0xFF) | m.rd(0x10));
  m.wr(0x11, ((m.rd(P_YL) >> 6) + 0x20) | m.rd(0x11));
}

/** ScreenRowRender 31:EBC1 (entered from BgRowRender with $1E/$1F/$20/$21 and the first page set up) */
function screenRowRender(m) {
  m.wr(0x10, m.rd(P_YL) & 0xF0);
  let y = m.rd(P_XL) >> 4, x = (m.rd(P_XL) & 0xF0) >> 3;
  m.wr(0x13, ((m.rd(P_XL) & 8) >> 3) + 0x10);
  screenSubstPtr(m);
  if (m.rd(0x62FF) & 0x80) setR6(m, 0x0A);       // bank-10 pages: first screen of a row only (the quirk)
  for (;;) {
    m.wr(BUF + x, screenTileSubst(m, indY(m, 0x10, y), y));
    y = (y + 1) & 0xFF;
    x = (x + 2) & 0xFF;
    m.wr(0x13, m.rd(0x13) - 1);
    if (!m.rd(0x13)) break;
    if (y !== 0x10) continue;
    screenPagePtr(m, m.rd(0x21));                // EC04: next screen, no SetR6($0A)
    m.wr(0x1E, m.rd(0x1F));
    x = 0; y = 0;
  }
  metatileAttrRow(m);
  switchBank16(m, 4);
  m.wr(0x12, (m.rd(P_YL) & 8) >> 2);
  m.wr(0x13, 1);
  metatileExpand(m, 0);
  camNtAddr(m);
  ppuCtrlNmiOff(m);
  queuePush(m, m.rd(0x11) | m.rd(0x0744), m.rd(0x10) & 0xE0, 0x20, 0);
  if (m.rd(0x01) & 0x18) { ppuCtrlNmiOn(m); return; }
  const ctrl = m.rd(0x00);                       // rendering off: flush now (NMI bit clear in the shadow)
  m.wr(0x00, ctrl & 0x7F);
  ppuQueueFlush(m);
  m.wr(0x00, ctrl);
  ppuCtrlNmiOn(m);
}

/** BgRowRender 31:EB91 */
export function* bgRowRender(m) {
  while (queueHasSrc(m, 0)) yield;               // 30:C72B (A = 0): wait until no queued record uses $6000
  if (!(m.rd(0x31) & 0x80)) m.wr(P_YH, m.rd(P_YH) + 1);
  let [s, y] = screenAtCamera(m);
  m.wr(0x20, s);
  m.wr(0x1E, screenVisitedBit(m, y));
  m.wr(0x10, y & 0xF8);
  y = ((y + 1) & 7) | m.rd(0x10);                // next screen to the right, same grid row
  m.wr(0x21, m.rd(GRID + y));
  m.wr(0x1F, screenVisitedBit(m, y));
  screenPagePtr(m, m.rd(0x20));
  screenRowRender(m);
}

/** BgColRender 31:EC87 */
export function* bgColRender(m) {
  while (queueHasSrc(m, 0x20)) yield;            // 30:C72B (A = $20)
  m.wr(0x18, 0);
  m.wr(0x19, 1);
  if (!(m.rd(0x30) & 0x80)) m.wr(P_XH, m.rd(P_XH) + 1);
  let [s, y] = screenAtCamera(m);
  m.wr(0x20, s);
  m.wr(0x1E, screenVisitedBit(m, y));
  let a = (y + 8) & 0xFF;                        // next screen down: grid index + 8, 12 rows ($60) wrap
  if (a & 0x80) a &= 7;
  if (a >= 0x60) a -= 0x5F;
  y = a;
  m.wr(0x21, m.rd(GRID + y));
  m.wr(0x1F, screenVisitedBit(m, y));
  screenPagePtr(m, m.rd(0x20));
  m.wr(0x10, (m.rd(P_XL) & 0xF0) >> 4);
  y = (m.rd(P_YL) & 0xF0) | m.rd(0x10);
  m.wr(0x10, 0);
  let x = (((m.rd(P_YL) & 0xF0) >> 3) + 0x20) & 0xFF;
  m.wr(0x13, ((m.rd(P_YL) & 8) >> 3) + 0x0F);
  m.wr(0x14, 0);
  screenSubstPtr(m);
  for (;;) {
    m.wr(BUF + x, screenTileSubst(m, indY(m, 0x10, y), y));
    x = (x + 2) & 0xFF;
    y = (y + 0x10) & 0xFF;
    m.wr(0x13, m.rd(0x13) - 1);
    if (!m.rd(0x13)) break;
    if (y < 0xF0) continue;
    m.wr(0x1E, m.rd(0x1F));
    screenPagePtr(m, m.rd(0x21));
    y &= 0x0F;
    x = 0x20;
  }
  metatileAttrCol(m);
  switchBank16(m, 4);
  m.wr(0x12, (m.rd(P_XL) & 8) >> 3);
  m.wr(0x13, 2);
  metatileExpand(m, 0x20);
  camNtAddr(m);
  ppuCtrlNmiOff(m);
  queuePush(m, (m.rd(0x11) & 0x30) | 0x40, m.rd(0x10) & 0x1F, 0x1E, 0x20);
  ppuCtrlNmiOn(m);
}

/** FullScreenDraw with rendering off (AreaEnter): measured in tools.emu (s108, area 0, f2340-2345) the NMIs
 *  fall after rows 7, 13, 19, 25 (~6 rows of ~43 scanlines per frame) and after the last row.
 *  s118: also used for AI $5F's redraw (not measured there). */
export const FSD_LAG = (n) => n === 30 || (n > 1 && n % 6 === 1);
/** s161: the same draw started right after a WaitFrame (BossHookA6 15:B8FD, area $A6 f2505-2510): the NMIs
 *  fall after rows 9, 15, 21, 27 (tools.emu pos cursor 488/440/392/344 at the frame ends) */
export const FSD_LAG_WF = (n) => n === 29 || (n < 30 && n % 6 === 0);

/** 30:C831: draw the 30 rows of the screen at $34-$37 (bottom-up), banks restored; A != 0 -> 30:C739 */
export function* fullScreenDraw(m, a, lagRows = null) {
  const r6 = m.rd(0x6E), r7 = m.rd(0x6F);
  m.wr(P_YH, m.rd(P_YH) + 1);
  m.wr(0x2E, 0x1E);
  let row = 0;
  do {
    while (!queueEmpty(m)) yield;                // WaitPpuQueue 30:C676
    setR6(m, 0x1A);
    m.wr(0x30, 0);
    m.wr(0x31, 0xF8);
    tempAddOffset(m);                            // 26:9824: one row (8 px) up
    yield* bgRowRender(m);
    m.wr(0x2E, m.rd(0x2E) - 1);
    // s108: with rendering off one row costs ~43 scanlines of CPU, so the draw spans 5 NMIs; lagRows(n) says
    // whether an NMI falls after row n (it finds frame_ready set: no DMA / scroll copy -> SPIN)
    if (lagRows && lagRows(++row)) yield SPIN;
  } while (m.rd(0x2E));
  setR7(m, r7);
  setR6(m, r6);
  if (a & 0xFF) queueAttrAll(m);
}

/** run a generator to the end within this frame; throws if it would wait for an NMI (checks / tools) */
export function runNow(gen) {
  for (let r = gen.next(); !r.done; r = gen.next()) throw new Error('routine waits for the PPU queue (NMI)');
}
