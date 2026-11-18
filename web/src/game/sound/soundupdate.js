// re-c web port — SoundUpdate 31:F7FE (Ph7 web 7/8, s56): one driver frame. Called from IRQ handler 1 (gameplay
// HUD split, F4CF), IRQ handler 5 (F611) and the NMI-mode tails F6DC (mode 0/7, rendering off) / F76B / F77F
// (mode 3, title). On an area-load frame both F4CF and F6DC run it, so the driver ticks twice (REVERSE.md s42).
//
// Maps R6 = 24, R7 = 25 directly (no shadow update); re-issues the interrupted music when snd_cur_music $0102 b7
// is set and the jingle has ended; else runs at most one queued command; then SndTick; restores the banks
// from $6E/$6F (SetR7, SetR6: $50 ends as 6).
import { sndCommand, sndTick } from './driver.js';
import { setR6, setR7 } from '../prg.js';

export function soundUpdate(m) {
  m.prgR6 = 0x18; m.prgR7 = 0x19;
  let resume = false;
  if (m.rd(0x0102) & 0x80) {                               // F810
    if (m.rd(0xFE) & 0x80) {
      const v = (m.rd(0x0168) | m.rd(0x0169) | m.rd(0x016A) | m.rd(0x016B)) & 0x0F;
      resume = v === 0;
    } else resume = m.rd(0x0111) === 0xFF;                 // F82B
  }
  if (resume) {                                            // F832
    const id = m.rd(0x0102) & 0x7F;
    m.wr(0x0102, id); m.wr(0x0110, id);
    sndCommand(m);
  } else if (m.rd(0x0101) === 0 && m.rd(0x0100) !== 0) {   // F843
    m.wr(0x0110, m.rd(0x0103));
    sndCommand(m);
    for (let y = 0; y < 6; y++) m.wr(0x0103 + y, m.rd(0x0104 + y));
    m.wr(0x0100, (m.rd(0x0100) - 1) & 0xFF);
  }
  m.prgR7 = 0x19;                                          // F866
  sndTick(m);
  setR7(m, m.rd(0x6F));                                    // F873
  setR6(m, m.rd(0x6E));
}
