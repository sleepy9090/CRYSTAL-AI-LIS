// re-c web port — form revert (session 148). REVERSE.md substate table row 25.
//
//   substFormRevert(m)  SubstFormRevert 19:BB9D (sub_state $19, set by PlayerMagic 27:A022 when obj1_flash b7 =
//                       changed form): any magic press turns the player back into a human.
//     BgBlackClearBuf 30:C874, WaitFrame, CHR R2 play = $40 (no shield) / $41 (equip_shield != 0), R3 play = $5C,
//     slot 1 flags |= $02, slot 0 meta $02, slot 1 tile ofs 0 / overlay $D0 / anim $1F / meta $4C,
//     obj1_flash &= $4F (b7 / b5-4 = form bits cleared), SFX $39, OamClipTop (the LDA #$F0 before it is dead:
//     30:C17D reloads A), then 31 x (WaitFrame + FarCall_B14_800E) while obj_anim_s01 counts down; StatRecalc
//     30:C008, slot 0 meta $A7, one more sprite pass (no WaitFrame), sub_state 8, JMP PpuQueueAttrAll 30:C739.
//     Unlike SubstCureMagic the slot 1 meta is NOT pushed / restored: it stays $4C.
import { R } from '../ram.js';
import { switchBank16, farRestore } from '../prg.js';
import { playSound } from '../playsound.js';
import { spriteDraw } from '../obj/sprites.js';
import { waitFrame, statRecalc } from './death.js';
import { blackenBg } from './telepathy.js';
import { oamClipTop } from './talk.js';
import { queueAttrAll } from '../world/ppuqueue.js';

/** FarCall_B14_800E: sprite pass with banks 14/15 mapped, then $6E/$6F restored */
function farSpriteDraw(m) {
  const r6 = m.rd(0x6E), r7 = m.rd(0x6F);
  switchBank16(m, 0x0E);
  spriteDraw(m);
  farRestore(m, r6, r7);
}

/** SubstFormRevert 19:BB9D. `y` = the Y register at the dispatch (PlaySound stores it in $010F). */
export function* substFormRevert(m, y = 0) {
  yield* blackenBg(m);                                  // BgBlackClearBuf 30:C874
  yield* waitFrame(m);                                  // BBA0
  m.wr(0x07F2, m.rd(0x0714) ? 0x41 : 0x40);             // chr_r2_play by equip_shield
  m.wr(0x07F3, 0x5C);                                   // chr_r3_play
  m.wr(0x0381, m.rd(0x0381) | 0x02);                    // obj_flags_s01
  m.wr(0x0300, 0x02);
  m.wr(0x0321, 0x00);                                   // obj_tile_ofs_s01
  m.wr(0x0581, 0xD0);                                   // obj_overlay_id_s01
  m.wr(0x04E1, 0x1F);                                   // obj_anim_s01
  m.wr(0x0301, 0x4C);
  m.wr(0x0710, m.rd(0x0710) & 0x4F);                    // obj1_flash: form bits cleared
  playSound(m, 0x39, y);
  oamClipTop(m);                                        // BBE4 (A = $F0 is overwritten at 30:C17D)
  do {                                                  // BBE7
    yield* waitFrame(m);
    farSpriteDraw(m);
    m.wr(0x04E1, (m.rd(0x04E1) - 1) & 0xFF);
  } while (m.rd(0x04E1));
  yield* statRecalc(m, 0);                              // X = 0 after the sprite pass
  m.wr(0x0300, 0xA7);
  farSpriteDraw(m);                                     // BBFA
  m.wr(R.sub_state, 0x08);
  queueAttrAll(m);                                      // JMP PpuQueueAttrAll 30:C739
}
