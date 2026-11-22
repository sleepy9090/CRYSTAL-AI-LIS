// re-c web port — magic 5, cure (session 147). REVERSE.md substate table row 24 and the s147 note there.
//
//   substCureMagic(m)  SubstCureMagic 19:BB39 (sub_state $18, set by MagicCure 27:A17D and the stone path
//                      31:F019, both after SpendMp($18)):
//     BgBlackClearBuf 30:C874 (black BG, $6000-$603F = 0, PpuQueueBlock($2B), WaitFrame), status low nibble
//     cleared (obj1_flash &= $F0), obj_freeze = 0, CHR R2/R3 play = $40/$42, slot 1 meta pushed, slot 0 meta $02,
//     slot 1 overlay $CB / anim $3F / meta $4C / tile ofs 0, SFX $25, then 63 x (WaitFrame + FarCall_B14_800E)
//     while obj_anim_s01 counts down; slot 1 meta pulled, StatRecalc 30:C008, slot 0 meta $A7,
//     sub_state $10 (ShowPendingMessage) with message 32.2, JMP PpuQueueAttrAll 30:C739.
import { R } from '../ram.js';
import { switchBank16, farRestore } from '../prg.js';
import { playSound } from '../playsound.js';
import { spriteDraw } from '../obj/sprites.js';
import { waitFrame, statRecalc } from './death.js';
import { blackenBg } from './telepathy.js';
import { queueAttrAll } from '../world/ppuqueue.js';

/** SubstCureMagic 19:BB39. `y` = the Y register at the dispatch (PlaySound stores it in $010F). */
export function* substCureMagic(m, y = 0) {
  yield* blackenBg(m);                                  // BgBlackClearBuf 30:C874
  m.wr(0x0710, m.rd(0x0710) & 0xF0);                    // obj1_flash: status cleared
  m.wr(0x05A0, 0);                                      // obj_freeze
  m.wr(0x07F2, 0x40);                                   // chr_r2_play
  m.wr(0x07F3, 0x42);                                   // chr_r3_play
  const meta1 = m.rd(0x0301);                           // PHA
  m.wr(0x0300, 0x02);
  m.wr(0x0581, 0xCB);                                   // obj_overlay_id_s01
  m.wr(0x04E1, 0x3F);                                   // obj_anim_s01
  m.wr(0x0301, 0x4C);
  m.wr(0x0321, 0x00);                                   // obj_tile_ofs_s01
  playSound(m, 0x25, y);
  do {                                                  // BB75
    yield* waitFrame(m);
    const r6 = m.rd(0x6E), r7 = m.rd(0x6F);            // FarCall_B14_800E
    switchBank16(m, 0x0E);
    spriteDraw(m);
    farRestore(m, r6, r7);
    m.wr(0x04E1, (m.rd(0x04E1) - 1) & 0xFF);
  } while (m.rd(0x04E1));
  m.wr(0x0301, meta1);                                  // PLA
  yield* statRecalc(m, 0);                              // X = 0 after the sprite pass (s147 RAM diff)
  m.wr(0x0300, 0xA7);
  m.wr(R.sub_state, 0x10);
  m.wr(0x06C3, 0x02);                                   // pending_msg_id
  m.wr(0x06E3, 0x20);                                   // pending_msg_group: msg 32.2
  queueAttrAll(m);                                      // JMP PpuQueueAttrAll 30:C739
}
