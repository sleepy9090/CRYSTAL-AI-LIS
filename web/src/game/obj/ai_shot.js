// re-c web port — the boss shot / effect AIs of bank 27 (enemy port leftovers (b), s87). REVERSE.md "Boss shot /
// part AIs (session 86)". Ported instruction by instruction for obj_check.mjs; none pops its caller.
//
//   AI $14 AiCountdown 27:B2C5     AI $16 AiCurveShot 27:B0F6    AI $17 AiHoverShot 27:B11B
//   AI $1B AiBounceShot 27:B12D    AI $1E AiDirFrameShot 27:B094 AI $1F AiShotAnimDec 27:B0A2
// $16 $17 $1E $1F end in AiStraightMoverTail 27:B0BE (obj/ai_static.js).
import { objDespawn, objPosToTemp, objStepVector, tempAddStep, tempToObjPos, objBgProbe } from './util.js';
import { aiStraightMoverTail } from './ai_static.js';

const META_ID = 0x0300, META_DIR = 0x0360, FLAGS = 0x0380, BG_MASK = 0x0460, TIMER = 0x0480, ACTIVE = 0x04A0;
const ANIM = 0x04E0, PHASE = 0x0600;

const dec = (m, a) => m.wr(a, m.rd(a) - 1);

/** AI $14: obj_anim -= obj_phase; borrow -> ObjDespawn 26:98C7 */
export function aiCountdown(m, x) {
  const a = m.rd(ANIM + x), p = m.rd(PHASE + x);
  m.wr(ANIM + x, a - p);
  if (a < p) objDespawn(m, x);
}

/** AI $1E: obj_anim = (dir & 7) * 8 (fixed frame per direction) */
export function aiDirFrameShot(m, x) {
  m.wr(ANIM + x, (m.rd(META_DIR + x) & 7) << 3);
  aiStraightMoverTail(m, x);
}

/** AI $1F */
export function aiShotAnimDec(m, x) {
  dec(m, ANIM + x);
  aiStraightMoverTail(m, x);
}

/** AI $16: every 16 obj_timer steps the 16-dir heading turns +1 (even slot) / -1 (odd slot) */
export function aiCurveShot(m, x) {
  m.wr(ANIM + x, m.rd(ANIM + x) - 2);
  if ((m.rd(TIMER + x) & 0x0F) === 0) {
    m.wr(META_DIR + x, ((m.rd(META_DIR + x) + ((x & 1) ? -1 : 1)) & 0xFF) & 0x0F);
  }
  aiStraightMoverTail(m, x);
}

/** AI $17: obj_anim -= 2; when it goes negative: meta++ and obj_active = $10 (straight mover) */
export function aiHoverShot(m, x) {
  m.wr(ANIM + x, m.rd(ANIM + x) - 2);
  if (m.rd(ANIM + x) & 0x80) {
    m.wr(META_ID + x, m.rd(META_ID + x) + 1);
    m.wr(ACTIVE + x, 0x10);
  }
  aiStraightMoverTail(m, x);
}

/** AI $1B: step, ObjBgProbe($40); wall (bits & bg_mask & 6) -> heading += 4 (of 16) and retry, 5 tries;
 *  obj_anim reaching 0 clears obj_bg_mask (then it flies through walls) */
export function aiBounceShot(m, x) {
  if (m.rd(FLAGS + x) & 0x80) return void objDespawn(m, x);
  m.wr(0x1F, 5);
  for (;;) {
    dec(m, TIMER + x);
    dec(m, ANIM + x);
    if (m.rd(ANIM + x) === 0) { m.wr(BG_MASK + x, 0); return; }
    objPosToTemp(m, x);
    objStepVector(m, x, m.rd(META_DIR + x));
    tempAddStep(m);
    tempToObjPos(m, x);
    objBgProbe(m, x, 0x40);
    if ((m.rd(0x20) & m.rd(BG_MASK + x) & 6) === 0) return;
    m.wr(META_DIR + x, (m.rd(META_DIR + x) + 4) & 0x0F);
    dec(m, 0x1F);
    if (m.rd(0x1F) === 0) return;
  }
}

export const AI_SHOT = {
  0x14: aiCountdown, 0x16: aiCurveShot, 0x17: aiHoverShot, 0x1B: aiBounceShot, 0x1E: aiDirFrameShot,
  0x1F: aiShotAnimDec,
};
