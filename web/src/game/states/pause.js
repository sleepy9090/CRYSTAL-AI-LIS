// re-c web port — the pause / status screen (session 94, "Ph7 menu screens" (c), pause part):
// PauseScreen 15:BC40 + PauseWait 15:BDDB (bank pair 7), StatusCondText 15:BE11, EquipNameRow 15:BE7A,
// StatusPlayerSprite 15:BEBF, ObjSlot1FDraw 30:C1AE, PpuQueueBlock 30:C482. NMI side: mainloop.js nmiMode2
// (31:F751 + ApplyChrAlt 31:F64F). REVERSE.md "State-1 substates" rows 12/13 + "Pause / status screen (web port)".
//
//   pauseScreen(m)     generator = PauseScreen + PauseWait (sub_state 12 entry of SubstateHandlerTable too);
//                      returns the Y register ReadPads leaves (SubstPlay goes on with it, states/play.js)
//   ppuQueueBlock(m,a) generator = PpuQueueBlock 30:C482 (block table 30:C518, 5 bytes per id)
import { R } from '../ram.js';
import { cpuRd, indY, setR6, setR7, switchBank16 } from '../prg.js';
import { readPads, readPadsMenu } from '../pads.js';
import { drawMeta, hideUnusedOam } from '../obj/sprites.js';
import { queueEmpty, ppuCtrlNmiOff, ppuCtrlNmiOn } from '../world/ppuqueue.js';
import { SPIN, waitFrame, statRecalc, hudRedraw, queueFlushNow } from './death.js';

const BUF = 0x6000;

/** WaitPpuQueue 30:C676 (spin: frame_ready stays set) */
export function* waitPpuQueue(m) { while (!queueEmpty(m)) yield SPIN; }

/** PpuQueueBlock 30:C482: A = block id; record (hi lo len src count) from 30:C518 + 5*id into $20-$24, then
 *  count&$7F + 1 records, each len bytes on (lo += max(len, $20), src += len); waits for the queue to drain
 *  after each record unless hi b7 (chained records), and at the end unless count b7. */
export function* ppuQueueBlock(m, a) {
  m.wr(0x10, a);
  yield* waitPpuQueue(m);
  const p = 0xC518 + a * 5;
  m.wr(0x26, (p >> 8) & 0xFF);
  for (let y = 0; y < 5; y++) m.wr(0x20 + y, cpuRd(m, p + y));
  m.wr(0x25, m.rd(0x24) & 0x7F);
  do {
    ppuCtrlNmiOff(m);                                  // C4BE
    const x = m.rd(0x0B);
    for (let i = 0; i < 4; i++) m.wr(0x6200 + x + i, m.rd(0x20 + i));
    m.wr(0x0B, (x + 4) & 0x1F);
    if (!(m.rd(R.ppumask_shadow) & 0x18)) queueFlushNow(m);   // C4E5 JSR C8B2
    ppuCtrlNmiOn(m);
    const len = m.rd(0x22);
    const lo = m.rd(0x21) + (len < 0x20 ? 0x20 : len);       // C4EB: next row (at least 32 bytes on)
    m.wr(0x21, lo & 0xFF);
    if (lo > 0xFF) m.wr(0x20, (m.rd(0x20) + 1) & 0xFF);
    m.wr(0x23, (m.rd(0x23) + len) & 0xFF);
    if (!(m.rd(0x20) & 0x80)) yield* waitPpuQueue(m);        // C503
    m.wr(0x25, (m.rd(0x25) - 1) & 0xFF);
  } while (!(m.rd(0x25) & 0x80));
  if (!(m.rd(0x24) & 0x80)) yield* waitPpuQueue(m);          // C50E
}

/** 30:C1AE: $20+1 sprite records of 8 bytes at $6000 (x y meta tile_ofs flags flags2 dir anim) drawn one by
 *  one through object slot $1F (28:8000 -> ObjDrawMeta with $1B = 1), OAM from offset 4, then
 *  ObjHideUnusedOam; banks restored. */
function objSlot1FDraw(m) {
  const r6 = m.rd(0x6E), r7 = m.rd(0x6F);
  switchBank16(m, 0x0E);
  m.wr(0x10, 4);
  do {
    const x = (m.rd(0x20) << 3) & 0xFF;
    const b = (i) => m.rd(BUF + x + i);
    m.wr(0x12, b(0)); m.wr(0x13, b(1));
    m.wr(0x031F, b(2)); m.wr(0x059F, b(3)); m.wr(0x039F, b(4));
    m.wr(0x17, (b(4) << 1) & 0xFF);
    m.wr(0x033F, b(5)); m.wr(0x037F, b(6)); m.wr(0x04FF, b(7));
    m.wr(0x04BF, 1);
    m.wr(0x1B, 1);                                     // 28:8000 LDX #$1F, LDY #1, STY $1B
    drawMeta(m, 0x1F);
    m.wr(0x04BF, 0);
    m.wr(0x20, (m.rd(0x20) - 1) & 0xFF);
  } while (!(m.rd(0x20) & 0x80));
  hideUnusedOam(m);                                    // C205 JSR 28:80A5
  setR7(m, r7);
  setR6(m, r6);
}

/** 15:BEBF: the player figure on the status screen: normal = meta $0B at (48,54) with the sword (6) or
 *  swordless (2) tile set and shield (CHR offset $0E/$0A); changed form (b7) / stone (4) = 8-byte record
 *  from 15:BF1E. */
function* statusPlayerSprite(m) {
  yield* waitPpuQueue(m);
  const f = m.rd(0x0710);
  let y = -1;
  if (f & 0x80) y = (f >> 1) & 0x38;
  else if ((f & 0x0F) === 4) y = 0x20;
  if (y >= 0) {
    for (let x = 0; x < 8; x++) m.wr(BUF + x, cpuRd(m, 0xBF1E + y + x));
  } else {
    m.wr(BUF, 0x30); m.wr(BUF + 1, 0x36);
    m.wr(BUF + 2, m.rd(0x0711) ? 0x06 : 0x02);
    m.wr(BUF + 3, m.rd(0x0714) ? 0x0E : 0x0A);
    m.wr(BUF + 4, 0x0B);
    m.wr(BUF + 5, 0); m.wr(BUF + 6, 0); m.wr(BUF + 7, 0);
  }
  m.wr(0x20, 0);
  objSlot1FDraw(m);
}

/** 15:BE11: 11-char condition text ("NORMAL" / "PARALYZED" / "STONE" / "POISONED" / "MUTATED") -> $601C */
function statusCondText(m) {
  const y = ((m.rd(0x0710) & 0x0F) << 4) & 0xFF;
  for (let x = 0; x < 11; x++) m.wr(BUF + 0x1C + x, cpuRd(m, 0xBE2A + y + x));
}

/** EquipNameRow 15:BE7A: 16-char name of equipment class $18 (sword/magic... from EquipNameBase 15:BEB7 in
 *  bank 6, + id * $100) -> $6010; id 0 -> the blank row at $80F0 */
function equipNameRow(m) {
  setR6(m, 6);
  const c = m.rd(0x18);
  m.wr(0x10, cpuRd(m, 0xBEB7 + c * 2));
  m.wr(0x11, cpuRd(m, 0xBEB8 + c * 2));
  let a = m.rd(0x0711 + c);
  if (a === 0) { m.wr(0x10, 0xF0); m.wr(0x11, 0x80); }
  m.wr(0x11, (m.rd(0x11) + a) & 0xFF);
  for (let x = 0; x < 16; x++) m.wr(BUF + 0x10 + x, indY(m, 0x10, x));
  setR6(m, 0x1A);
}

/** PauseScreen 15:BC40 + PauseWait 15:BDDB */
export function* pauseScreen(m) {
  const savedIrq = m.rd(R.irq_mode);                   // PHA
  yield* waitPpuQueue(m);
  yield* waitFrame(m);
  const im = m.rd(R.irq_mode) | 0x80;                  // b7: NMI skips PaletteUpload
  m.wr(R.irq_mode, im);
  m.wr(R.frame_ready, im);
  m.wr(R.ppumask_shadow, m.rd(R.ppumask_shadow) & 0xEF);   // sprites off
  m.wr(0x04AC, 0);
  for (let x = 0; x < 0x100; x++) m.wr(BUF + x, 0x20);
  for (let x = 0xE0; x >= 0; x -= 0x20) { m.wr(BUF + 1 + x, 0x1E); m.wr(BUF + 0x1F + x, 0x1F); }
  yield* ppuQueueBlock(m, 5);
  yield* ppuQueueBlock(m, 6);
  for (let x = 0x1F; x >= 0; x--) {
    m.wr(BUF + x, 0xFF); m.wr(BUF + 0x20 + x, 0xFF); m.wr(BUF + 0x40 + x, 0x19); m.wr(BUF + 0xA0 + x, 0x1C);
  }
  m.wr(BUF + 0x41, 0x18); m.wr(BUF + 0x5F, 0x1A); m.wr(BUF + 0xA1, 0x1B); m.wr(BUF + 0xBF, 0x1D);
  yield* ppuQueueBlock(m, 7);
  yield* ppuQueueBlock(m, 8);
  for (let x = 0x3F; x >= 0; x--) m.wr(BUF + 0x60 + x, 0xFF);
  yield* ppuQueueBlock(m, 3);
  yield* waitFrame(m);
  yield* statusPlayerSprite(m);
  m.wr(R.ppumask_shadow, m.rd(R.ppumask_shadow) | 0x10);
  m.wr(R.irq_mode, 2);
  m.wr(0x58, 0x3C); m.wr(0x59, 0x38);
  m.wr(0x5A, m.rd(0x07F2)); m.wr(0x5B, m.rd(0x07F3));
  m.wr(R.ppuctrl_shadow, (m.rd(R.ppuctrl_shadow) & 0xFC) | 2);
  for (let x = 0; x < 0x100; x++) m.wr(BUF + x, cpuRd(m, 0xBD84 + x));   // status window text
  statusCondText(m);
  yield* ppuQueueBlock(m, 0x0A);
  for (let x = 5; x >= 0; x--) { m.wr(BUF + x, m.rd(0x6410 + x)); m.wr(BUF + 6 + x, m.rd(0x6418 + x)); }
  yield* ppuQueueBlock(m, 9);
  for (let b = 0x0F; b < 0x14; b++) yield* ppuQueueBlock(m, b);
  setR6(m, 0x1A);
  for (const e of [6, 7, 8, 9, 0x0A, 0x10]) yield* hudRedraw(m, e, 0x14);   // X = $14 from the block loop
  m.wr(0x17, 0x0B); m.wr(0x18, 0); m.wr(0x19, 3);
  do {                                                 // 4 equipment rows: icon + name, blocks $0B-$0E
    let y = (m.rd(0x18) << 2) + 3;
    for (let x = 3; x >= 0; x--, y--) m.wr(BUF + 0x0C + x, cpuRd(m, 0xBDCB + y));
    equipNameRow(m);
    yield* ppuQueueBlock(m, m.rd(0x17));
    m.wr(0x17, m.rd(0x17) + 1); m.wr(0x18, m.rd(0x18) + 1);
    m.wr(0x19, (m.rd(0x19) - 1) & 0xFF);
  } while (!(m.rd(0x19) & 0x80));
  m.wr(R.irq_mode, 2);
  m.wr(R.ppumask_shadow, m.rd(R.ppumask_shadow) | 0x18);
  m.wr(R.sub_state, (m.rd(R.sub_state) + 1) & 0xFF);   // 15:BD7F INC $41 (8 -> 9 from play)
  // PauseWait 15:BDDB
  for (;;) {
    yield* waitFrame(m);
    yield* statusPlayerSprite(m);
    readPadsMenu(m);
    if (m.rd(R.pad_new) & 0x10) break;                 // START
  }
  for (let x = 0x3F; x >= 0; x--) m.wr(BUF + 0x80 + x, 0xFF);
  yield* ppuQueueBlock(m, 0x14);
  yield* waitPpuQueue(m);
  yield* statRecalc(m, 0xFF);                          // X = $FF from the fill loop
  m.wr(0x04BF, 0);
  m.wr(R.sub_state, 8);
  m.wr(R.irq_mode, savedIrq);                          // PLA
  return readPads(m);                                  // JMP ReadPads (returns its Y register)
}
// s110: pauseFromPlay (the 15:BC40 state entry) moved to states/play.js, which now runs the rest of SubstPlay
