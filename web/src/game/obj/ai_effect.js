// re-c web port — small effect AIs reached from ObjUpdateAll (s93, "boss / effect AI port" (a)).
// REVERSE.md "Enemies — catalogue + generic enemy AIs" (ObjAiTable 27:A314) + "Boss / effect AIs (s93)".
//
//   AI $04 AiPlayerFollowC 27:B2FC  slot $0C <- player (slot 0) x/y; slot $0C obj_anim -= obj_timer2, <0 -> off
//   AI $57 = AiStraightMoverTail 27:B0BE (no obj_anim step; obj/ai_static.js)
//   AI $76 AiMpDrain       27:A4DC  overlay $A3 on the player, every 16th frame sound $31 + 1 MP + HUD MP digits;
//                                   obj_anim -1 per frame (-3 while a button is newly pressed), borrow -> off
//
// AiMpDrain is a generator: HudRedraw 26:8E46 spins in PpuQueueWaitSrc($7B) while an MP / HUD record is queued.
import { objPosToTemp, tempToObjPos } from './util.js';
import { aiStraightMoverTail } from './ai_static.js';
import { playSound } from '../playsound.js';
import { hudRedraw } from '../states/death.js';

const ANIM = 0x04E0, ACTIVE = 0x04A0, TIMER2 = 0x0620;

/** AI $04 27:B2FC: ignores its own slot X; always moves / ages slot $0C, and returns with X = $0C, so
 *  ObjUpdateAll's INX goes on at slot $0D (returns $0C: objUpdateAll takes a number as the new X). */
export function aiPlayerFollowC(m) {
  objPosToTemp(m, 0);
  tempToObjPos(m, 0x0C);
  const a = (m.rd(ANIM + 0x0C) - m.rd(TIMER2 + 0x0C)) & 0xFF;
  m.wr(ANIM + 0x0C, a);
  if (a & 0x80) m.wr(ACTIVE + 0x0C, 0);
  return 0x0C;
}

/** AI $76 27:A4DC (27:A4E0 = an unreferenced second entry with overlay $99) */
export function* aiMpDrain(m, x, overlay = 0xA3) {
  m.wr(0x0581, overlay);                                    // obj_overlay_id slot 1
  if ((m.rd(0x08) & 0x0F) === 0) {
    playSound(m, 0x31, (0x76 << 1) & 0xFF);                 // Y = id*2 from ObjUpdateAll's TAY 30:CD07
    m.wr(0x05A0, 2);                                        // obj_freeze slot 0
    if (m.rd(0x0708)) {                                     // mp_cur
      m.wr(0x0708, m.rd(0x0708) - 1);
      yield* hudRedraw(m, 4, x);                            // A4FF JSR 26:8E46 (MP digits); X back from $14
    }
  }
  const s = m.rd(ANIM + x) + (m.rd(0x4B) ? 0xFD : 0xFF);   // pad_new
  m.wr(ANIM + x, s & 0xFF);
  if (s < 0x100) m.wr(ACTIVE + x, 0);
}

export const AI_EFFECT = { 0x04: aiPlayerFollowC, 0x57: aiStraightMoverTail, 0x76: aiMpDrain };
