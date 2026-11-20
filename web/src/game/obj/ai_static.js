// re-c web port — the static / projectile enemy AIs (Ph7 web 6/8, s55). REVERSE.md "Enemies — catalogue +
// generic enemy AIs" + "Shots". Each handler takes the slot x (the ROM's X) with banks 26/27 mapped, like
// ObjUpdateAll 30:CCCC calls them through ObjAiTable 27:A314.
//
//   AI $10 AiStraightMover 27:B0B5   AI $11 AiDirShot 27:B0A8   AI $2C AiBurst 27:ACC0
//   AI $4C/$4D AiFanTurret 27:B595   AI $4E AiTurret 27:B5D9    + AiShootCheck 27:B714 (shared)
//   AI $50 AiS0DTransform 27:A660    AI $7F 27:A419 (RTS)       (s112)
import { cpuRd } from '../prg.js';
import {
  rand64, dirToTarget, spawnShot, objDespawn, objBecomeNext, objPosToTemp, tempToObjPos, tempAddStep,
  objStepVector, objBgProbe,
} from './util.js';
import { objMoveSlide } from './move.js';
import { farObjInit } from './objinit.js';

const META_ID = 0x0300, META_DIR = 0x0360, FLAGS = 0x0380, SHOT_ID = 0x0440, BG_MASK = 0x0460, TIMER = 0x0480;
const ACTIVE = 0x04A0, ANIM = 0x04E0, TIMER2 = 0x0620, ACT_TIMER = 0x0640;

/** AiShootCheck 27:B714 -> true when it starts an attack ($0640 = $1F) and pops its caller (skip the walk) */
export function aiShootCheck(m, x) {
  if (m.rd(FLAGS + x) & 0x80) return false;
  if ((((x << 4) & 0xFF) + m.rd(0x08)) & 0x1F) return false;
  if (rand64(m) !== 0) return false;
  dirToTarget(m, x, 1);
  if (m.rd(0x21) >= 5) return false;
  m.wr(ACT_TIMER + x, 0x1F);
  return true;
}

/** AI $4E: aimed shot (16-direction units = dir*2) every 16 obj_anim steps */
export function aiTurret(m, x) {
  m.wr(ANIM + x, m.rd(ANIM + x) - 1);
  if (m.rd(ANIM + x) & 0x0F) return;
  dirToTarget(m, x, 1);
  spawnShot(m, x, m.rd(SHOT_ID + x), (m.rd(0x20) << 1) & 0xFF);
}

/** AI $4C/$4D: idle = obj_anim-- + AiShootCheck; attacking = 3-shot fan at $0640 = $18 / $10 / $08.
 *  Returns true when ObjUpdateAll must return at once: the idle path JMPs (not JSRs) to AiShootCheck, so its
 *  "skip the caller" PLA PLA pops ObjUpdateAll's own return address (JSR $0010 at 30:CD28). On the frame a fan
 *  turret starts an attack, the slots after it get no AI update (checked: routine_capture "ret": "popped"). */
export function aiFanTurret(m, x) {
  if (m.rd(ACT_TIMER + x) === 0) {                          // B5D3
    m.wr(ANIM + x, m.rd(ANIM + x) - 1);
    return aiShootCheck(m, x);
  }
  m.wr(ACT_TIMER + x, m.rd(ACT_TIMER + x) - 1);
  const act = m.rd(ACT_TIMER + x), anim = (act << 1) & 0xFF;
  m.wr(ANIM + x, anim);
  if (!(anim & 0xF0) || (anim & 0x0F)) return;
  dirToTarget(m, x, 1);
  const ofs = cpuRd(m, 0xB5CF + ((act & 0x18) >> 3));      // 00 FF 00 01: $08 -> -1, $10 -> 0, $18 -> +1
  m.wr(0x20, m.rd(0x20) << 1);
  spawnShot(m, x, m.rd(SHOT_ID + x), (ofs + m.rd(0x20)) & 0x0F);
}

/** AI $2C: 3 x SpawnShot(obj_shot_id, Rand64) then despawn (counter in $1F) */
export function aiBurst(m, x) {
  m.wr(0x1F, 3);
  do {
    const d = rand64(m);
    spawnShot(m, x, m.rd(SHOT_ID + x), d);
    m.wr(0x1F, m.rd(0x1F) - 1);
  } while (m.rd(0x1F) !== 0);
  m.wr(ACTIVE + x, 0);
}

/** AI $11: meta |= dir>>1 (directional sprite), obj_active-- (-> $10), then AI $10 this frame */
export function aiDirShot(m, x) {
  m.wr(META_ID + x, (m.rd(META_DIR + x) >> 1) | m.rd(META_ID + x));
  m.wr(ACTIVE + x, m.rd(ACTIVE + x) - 1);
  aiStraightMover(m, x);
}

/** AI $10: obj_anim -= 4; offscreen -> despawn; lifetime obj_timer -> death type; step; optional BG test */
export function aiStraightMover(m, x) {
  m.wr(ANIM + x, m.rd(ANIM + x) - 4);
  aiStraightMoverTail(m, x);
}

/** AiStraightMoverTail 27:B0BE (also the tail of AIs $16 $17 $1E $1F, obj/ai_shot.js) */
export function aiStraightMoverTail(m, x) {
  if (m.rd(FLAGS + x) & 0x80) return objDespawn(m, x);
  m.wr(TIMER + x, m.rd(TIMER + x) - 1);
  if (m.rd(TIMER + x) === 0) return objBecomeNext(m, x);
  objPosToTemp(m, x);
  objStepVector(m, x, m.rd(META_DIR + x));
  tempAddStep(m);
  if (m.rd(BG_MASK + x) === 0) return tempToObjPos(m, x);
  objBgProbe(m, x, 0xC0);
  if (m.rd(0x20) & m.rd(BG_MASK + x) & 6) return objBecomeNext(m, x);   // B0E7: wall -> death type
  tempToObjPos(m, x);
}

/** AI $50 AiS0DTransform 27:A660 (s112; reached once the area spawns run). obj_timer2 steps:
 *  0: slot $0D <- ObjInit(type = slot $0D's death type $04CD) via FarCall_C25D, act_timer = $70;
 *  1: act_timer-- with slot 0 frozen (obj_freeze = 5) until 0, then meta $C0, sub_state = $11, $0623 = X,
 *     act_timer = $10;  >= 2: on screen -> meta $3B, anim -= 2, timer--, dir 6, flags2 7, ObjMoveSlide
 *     (blocked -> x_hi = $FF). */
export function aiS0DTransform(m, x) {
  const t2 = m.rd(TIMER2 + x);
  if (t2 === 0) {
    m.wr(0x11, m.rd(0x04CD));
    m.wr(0x10, 0x0D);
    farObjInit(m);
    m.wr(ACT_TIMER + x, 0x70);
    m.wr(TIMER2 + x, m.rd(TIMER2 + x) + 1);
    return;
  }
  if (t2 === 1) {
    const v = (m.rd(ACT_TIMER + x) - 1) & 0xFF;
    m.wr(ACT_TIMER + x, v);
    if (v !== 0) { m.wr(0x05A0, 5); return; }
    m.wr(META_ID + x, 0xC0);
    m.wr(TIMER2 + x, m.rd(TIMER2 + x) + 1);
    m.wr(0x41, 0x11);                                       // sub_state
    m.wr(0x0623, x);
    m.wr(ACT_TIMER + x, 0x10);
    return;
  }
  if (m.rd(FLAGS + x) & 0x80) return;
  m.wr(META_ID + x, 0x3B);
  m.wr(ANIM + x, (m.rd(ANIM + x) - 2) & 0xFF);
  m.wr(TIMER + x, (m.rd(TIMER + x) - 1) & 0xFF);
  m.wr(META_DIR + x, 6);
  m.wr(0x0340 + x, 7);                                      // obj_flags2
  if (objMoveSlide(m, x)) m.wr(0x90 + x, 0xFF);
}

/** ObjAiTable ids handled here (the rest of ObjAiTable 27:A314 arrives with later 6/8 steps) */
export const AI_HANDLERS = {
  0x10: aiStraightMover, 0x11: aiDirShot, 0x2C: aiBurst, 0x4C: aiFanTurret, 0x4D: aiFanTurret, 0x4E: aiTurret,
  0x50: aiS0DTransform,
  0x7F: () => {},                                           // 27:A419 RTS (dormant boss, s112)
};
