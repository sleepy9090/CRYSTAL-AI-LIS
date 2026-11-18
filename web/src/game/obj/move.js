// re-c web port — bank 26 movement with BG collision (Ph7 web 5/8 (b) / 6/8 (a), s80). REVERSE.md "World
// interaction" (ObjMoveStep / BgProbePoints / ObjScreenEdge, s46) and "Per-frame order inside ObjUpdateAll".
// Ported instruction by instruction (every zero-page temp is written) for obj_check.mjs.
//
//   objMoveSlide(m, x)   ObjMoveSlide 26:98D7 -> C (true = blocked). Steps in obj_meta_dir; a diagonal that is
//                        blocked also tries dir-1 then dir+1. $23 = the original dir, $25 = OR of the probes.
//   objMoveStep(m, x)    ObjMoveStep 26:9947 -> 'moved' | C. On success the ROM PLA PLAs the return address of
//                        ObjMoveSlide's JSR, so ObjMoveSlide itself returns to its caller with C = 0.
//   objScreenEdge(m, x)  ObjScreenEdge 26:99FF (slot 0 only) -> C (true = outside the screen box)
import { cpuRd } from '../prg.js';
import { objStepVector, tempAddStep, objBgProbe } from './util.js';

const X_LO = 0x70, X_HI = 0x90, Y_LO = 0xB0, Y_HI = 0xD0;
const FLAGS2 = 0x0340, META_DIR = 0x0360, FLAGS = 0x0380, BG_MASK = 0x0460;
const OBJ1_FLASH = 0x0710;

export function objScreenEdge(m, x) {
  const sx = (m.rd(0x34) - m.rd(0x02)) & 0xFF;
  let out = sx < 0x0A || sx >= 0xFC;
  if (!out) {
    let sy = m.rd(0x36) - m.rd(0x04);
    if (sy < 0) sy = (sy - 0x0F - 1) & 0xFF;               // 9A13 SBC #$0F with C = 0 (borrow) -> -16
    out = sy < 0x18 || sy >= 0xD0;
  }
  m.wr(0x20, (m.rd(FLAGS + x) & 0xA0) | (out ? 6 : 0));
  return out;
}

export function objMoveStep(m, x) {
  m.wr(0x34, m.rd(X_LO + x)); m.wr(0x35, m.rd(X_HI + x)); m.wr(0x36, m.rd(Y_LO + x)); m.wr(0x37, m.rd(Y_HI + x));
  const dir = m.rd(META_DIR + x) & 7;
  m.wr(META_DIR + x, dir);
  m.wr(0x24, dir << 3);
  objStepVector(m, x, dir);
  tempAddStep(m);
  m.wr(0x1C, m.rd(0x34)); m.wr(0x1D, m.rd(0x35)); m.wr(0x1E, m.rd(0x36)); m.wr(0x1F, m.rd(0x37));
  for (;;) {                                                 // 997D: walk the probe points
    let y = m.rd(0x24);
    m.wr(0x30, cpuRd(m, 0x9907 + y));
    m.wr(0x31, cpuRd(m, 0x9908 + y));
    if ((m.rd(0x30) | m.rd(0x31)) === 0) break;             // all points clear -> 99D4
    m.wr(0x24, (y + 2) & 0xFF);
    tempAddStep(m);
    if (x === 0) {
      if (objScreenEdge(m, x)) return true;                 // 999B BCS -> RTS (C = 1)
      const c = objBgProbe(m, x, 0x80);
      m.wr(0x25, m.rd(0x20) | m.rd(0x25));
      if (m.rd(FLAGS2 + x) === 0x08 && (m.rd(OBJ1_FLASH) & 0x40)) {   // flight / warp exit: mask $24
        if (c) continue;                                    // 99B4 BCS (the same-cell cache hit)
        if (m.rd(0x20) === 0) return false;                 // 99B8 BEQ -> RTS with C = 0
        if (!(m.rd(0x20) & 0x24)) continue;
        return false;                                       // 99BE RTS, C still 0
      }
    } else objBgProbe(m, x, 0x80);
    const mask = x === 0 ? 0x06 : 0x27;                     // 99C4 (CPX #0 leaves C = 1)
    if (m.rd(0x20) & mask & m.rd(BG_MASK + x)) return true;
  }
  // 99D4: commit (PLA PLA = return from ObjMoveSlide)
  m.wr(X_LO + x, m.rd(0x1C)); m.wr(0x34, m.rd(0x1C));
  m.wr(X_HI + x, m.rd(0x1D)); m.wr(0x35, m.rd(0x1D));
  m.wr(Y_LO + x, m.rd(0x1E)); m.wr(0x36, m.rd(0x1E));
  m.wr(Y_HI + x, m.rd(0x1F)); m.wr(0x37, m.rd(0x1F));
  objBgProbe(m, x, 0x40);
  const cur = m.rd(META_DIR + x);
  m.wr(META_DIR + x, m.rd(0x23));
  m.wr(0x23, cur);
  return 'moved';
}

export function objMoveSlide(m, x) {
  m.wr(0x25, 0);
  m.wr(0x23, m.rd(META_DIR + x));
  if (objMoveStep(m, x) === 'moved') return false;
  if (m.rd(META_DIR + x) & 1) {                             // 98E6 LSR: odd = diagonal
    m.wr(0x25, 0);
    m.wr(META_DIR + x, m.rd(META_DIR + x) - 1);
    if (objMoveStep(m, x) === 'moved') return false;
    m.wr(0x25, 0);
    m.wr(META_DIR + x, m.rd(META_DIR + x) + 2);
    if (objMoveStep(m, x) === 'moved') return false;
  }
  m.wr(META_DIR + x, m.rd(0x23));                           // 9900
  return true;
}
