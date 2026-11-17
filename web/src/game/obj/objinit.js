// re-c web port — ObjInit 30:C25D + FarCall_C25D 31:FF80 (Ph7 web 6/8, s55). REVERSE.md "Object type record".
//
//   objInit(m)      slot = $10, type = $11. Type 1 = no-op. SetR7(13) (left mapped: the far-call restores it),
//                   ptr $12/$13 = ObjTypeTable 13:AC00[type*2] ($AD00 for types >= $80), PlaySound(rec[0]) if
//                   non-zero, clear all 32 columns $0300-$06E0 of the slot, then (type != $FF) 4 groups of
//                   (bitmask byte, values for the set bits, MSB first) for the columns $0300+g*$100+b*$20.
//   farObjInit(m)   FarCall_C25D: the same, then RestoreBanks (R7 then R6 back, $50 ends = 6)
//
// Reads go through cpuRd ((zp),Y like the 6502), so the record pointer is followed exactly.
import { cpuRd, indY, setR7, farRestore } from '../prg.js';
import { playSound } from '../playsound.js';

export function objInit(m) {
  const type = m.rd(0x11);
  if (type === 1) return;
  setR7(m, 0x0D);                                           // C268
  const yy = (type << 1) & 0xFF, base = type & 0x80 ? 0xAD00 : 0xAC00;
  m.wr(0x12, cpuRd(m, base + yy));
  m.wr(0x13, cpuRd(m, base + yy + 1));
  const snd = indY(m, 0x12, 0);
  if (snd !== 0) playSound(m, snd, 0);                      // C28D (Y = 0 at the call)
  const x = m.rd(0x10);
  for (let c = x; c < 0x100; c += 0x20) {                   // C295: carry ends the loop
    m.wr(0x0300 + c, 0); m.wr(0x0400 + c, 0); m.wr(0x0500 + c, 0); m.wr(0x0600 + c, 0);
  }
  if (m.rd(0x11) === 0xFF) return;                          // C2A9 (type re-read: PlaySound keeps $11)
  let y = 1;
  for (let g = 0; g < 4; g++) {
    m.wr(0x11, (indY(m, 0x12, y) << 1) & 0xFF);             // header ASL A -> $11, bit 7 in C
    let c = indY(m, 0x12, y) & 0x80;
    y = (y + 1) & 0xFF;
    for (let b = 0; b < 8; b++) {
      if (b > 0) {                                          // ASL $11
        const v = m.rd(0x11);
        c = v & 0x80; m.wr(0x11, (v << 1) & 0xFF);
      }
      if (c) {
        m.wr(0x0300 + g * 0x100 + b * 0x20 + x, indY(m, 0x12, y));
        y = (y + 1) & 0xFF;
      }
    }
  }
  m.wr(0x11, type);                                         // C406 PLA
}

export function farObjInit(m) {
  const r6 = m.rd(0x6E), r7 = m.rd(0x6F);
  objInit(m);
  farRestore(m, r6, r7);
}
