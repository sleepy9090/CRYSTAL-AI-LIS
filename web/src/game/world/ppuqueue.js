// re-c web port — the PPU queue ring $6200 (Ph7 web 4/8 leftovers (a), s79). REVERSE.md "NMI tail in full"
// (PpuQueueFlush) and "Screen / metatile pipeline" step 6. Ported instruction by instruction: every RAM/WRAM byte
// the ROM writes is written here too (web/test/obj_check.mjs diffs the whole RAM + WRAM and CIRAM vs tools.emu).
//
// Ring: 8 records of 4 bytes at $6200 (hi, lo, len, src), read index $0A, write index $0B (both += 4 & $1F).
//   ppuQueueFlush(m)     PpuQueueFlush 30:C67D (NMI F40A; also called by ScreenRowRender with rendering off)
//   queueHasSrc(m, a)    the 30:C72B test: true while a record with src byte == a is still queued (the ROM spins)
//   queueEmpty(m)        WaitPpuQueue 30:C676 test ($0A == $0B)
//   queueAttrAll(m)      30:C739: record $23C0 / len $BF (attribute path) / src 0, under INC/DEC nmi_disable
//   ppuCtrlNmiOn/Off     30:C436 / 30:C43E: PPUCTRL <- ppuctrl_shadow | $80 / & $7F
import { cpuRd } from '../prg.js';

const Q = 0x6200, BUF = 0x6000, ATTR = 0x6100;

export function ppuCtrlNmiOn(m) { m.ppu.writeReg(0, m.rd(0x00) | 0x80); }
export function ppuCtrlNmiOff(m) { m.ppu.writeReg(0, m.rd(0x00) & 0x7F); }

export const queueEmpty = (m) => m.rd(0x0A) === m.rd(0x0B);

export function queueHasSrc(m, a) {
  for (let y = 0x1C; y >= 0; y -= 4) if (m.rd(Q + 3 + y) === a) return true;
  return false;
}

/** PpuQueueFlush 30:C67D: one record per call unless its hi byte has b7 (chain); the attribute path never chains */
export function ppuQueueFlush(m) {
  const ppu = m.ppu;
  for (;;) {
    const x = m.rd(0x0A);
    if (x === m.rd(0x0B)) return;
    const hi = m.rd(Q + x);
    m.wr(0x0C, hi);
    ppu.writeReg(0, (m.rd(0x00) & 0xFB) | (hi & 0x40 ? 4 : 0));
    ppu.writeReg(6, hi);
    ppu.writeReg(6, m.rd(Q + 1 + x));
    const len = m.rd(Q + 2 + x);
    m.wr(0x0D, len);
    let y = m.rd(Q + 3 + x);
    m.wr(Q + 3 + x, 0xFF);
    if (len & 0x80) {                              // PpuQueueAttrCopy 30:C6E7: 8 x 8 bytes, abs,Y (no 8-bit wrap)
      for (let g = 0; g < 8; g++) {
        for (let k = 0; k < 8; k++) ppu.writeReg(7, cpuRd(m, ATTR + y + k));
        y = (y + 8) & 0xFF;
      }
      m.wr(0x0A, (m.rd(0x0A) + 4) & 0x1F);
      return;
    }
    let n = len;                                   // DEX / BNE: len 0 copies 256
    do {
      ppu.writeReg(7, m.rd(BUF + y));
      y = (y + 1) & 0xFF;
      n = (n - 1) & 0xFF;
    } while (n !== 0);
    m.wr(0x0A, (m.rd(0x0A) + 4) & 0x1F);
    if (!(m.rd(0x0C) & 0x80)) return;
  }
}

/** 30:C739: queue the whole attribute table ($23C0, 64 bytes from $6100) */
export function queueAttrAll(m) {
  m.wr(0x60, m.rd(0x60) + 1);
  const x = m.rd(0x0B);
  m.wr(Q + x, 0x23); m.wr(Q + 1 + x, 0xC0); m.wr(Q + 2 + x, 0xBF); m.wr(Q + 3 + x, 0x00);
  m.wr(0x0B, (x + 4) & 0x1F);
  m.wr(0x60, m.rd(0x60) - 1);
}

/** append one record at $0B (the row / column renders' tail) */
export function queuePush(m, hi, lo, len, src) {
  const x = m.rd(0x0B);
  m.wr(Q + x, hi); m.wr(Q + 1 + x, lo); m.wr(Q + 2 + x, len); m.wr(Q + 3 + x, src);
  m.wr(0x0B, (x + 4) & 0x1F);
}
