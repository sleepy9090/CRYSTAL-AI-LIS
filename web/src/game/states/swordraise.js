// re-c web port — sword raise pose (session 149). REVERSE.md substate table row 26.
//
//   swordRaisePose(m)  SwordRaisePose 19:BC04 (sub_state $1A, stored by EventActionTable act 22 EvActSwordState
//                      30:D78C; event act 1 EvActThunderWarp 30:D5B4 JSRs it directly before msg 28.17):
//     ObjClear4to11 30:C867 (obj_active slots 4-11 = 0), WaitFrame, CHR R2/R3 play = $5C/$42, orbit_level 0,
//     slot 1 meta + overlay pushed, then meta $B8 (the raise pose) / overlay $4C / anim $7F, snd_cur_music |= $80,
//     jingle $93, OamClipTop (the LDA #$F0 is dead: 30:C17D reloads A), 127 x (WaitFrame + FarCall_B14_800E) while
//     obj_anim_s01 counts down, WaitFrame, overlay + meta pulled, StatRecalc 30:C008 (X = 0), OamClipTop, one
//     sprite pass, WaitFrame, sub_state 8, RTS (no PpuQueueAttrAll, unlike rows 24/25).
import { R } from '../ram.js';
import { playSound } from '../playsound.js';
import { waitFrame, statRecalc } from './death.js';
import { oamClipTop } from './talk.js';
import { farSpriteDraw } from './formrevert.js';

/** SwordRaisePose 19:BC04. `y` = the Y register at the dispatch (PlaySound stores it in $010F). */
export function* swordRaisePose(m, y = 0) {
  for (let s = 4; s < 0x0C; s++) m.wr(0x04A0 + s, 0);   // 30:C867
  yield* waitFrame(m);                                  // BC07
  m.wr(0x07F2, 0x5C);                                   // chr_r2_play
  m.wr(0x07F3, 0x42);                                   // chr_r3_play
  m.wr(0x06C0, 0x00);                                   // orbit_level
  const meta1 = m.rd(0x0301), over1 = m.rd(0x0581);     // PHA PHA
  m.wr(0x0301, 0xB8);
  m.wr(0x0581, 0x4C);                                   // obj_overlay_id_s01
  m.wr(0x04E1, 0x7F);                                   // obj_anim_s01
  m.wr(0x0102, m.rd(0x0102) | 0x80);                    // snd_cur_music b7
  playSound(m, 0x93, y);
  oamClipTop(m);                                        // BC3F
  do {                                                  // BC42
    yield* waitFrame(m);
    farSpriteDraw(m);
    m.wr(0x04E1, (m.rd(0x04E1) - 1) & 0xFF);
  } while (m.rd(0x04E1));
  yield* waitFrame(m);                                  // BC4D
  m.wr(0x0581, over1);                                  // PLA
  m.wr(0x0301, meta1);                                  // PLA
  yield* statRecalc(m, 0);                              // X = 0 after the sprite pass (WaitFrame keeps X)
  oamClipTop(m);                                        // BC5D
  farSpriteDraw(m);                                     // BC60
  yield* waitFrame(m);                                  // BC63
  m.wr(R.sub_state, 0x08);
}
