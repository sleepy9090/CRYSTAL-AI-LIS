// re-c web port — the walker / chaser enemy AIs (Ph7 web 6/8 (a), s81). REVERSE.md "Enemies — catalogue + generic
// enemy AIs" (AiChase, AiWalkPause, the AI table). Ported instruction by instruction from bank 27 (every zero-page
// temp the ROM writes is written here too) for obj_check.mjs. Handlers take the slot x with banks 26/27 mapped.
//
//   AI $20 AiWalkPause 27:A915 (+ AiWalkPick 27:A94A)   AI $21 AiShooterWalk 27:A969   AI $22 AiDormant 27:AB42
//   AI $24 AiChase 27:AA57 (+ AiTryMove 27:AAFA, ChasePose 27:AB2D)   AI $25 AiChaseOnscreen 27:A9D2
//   AI $26 AiShooterChase 27:A9E4   AI $27 AiShooterChaseOnscreen 27:A9DB   AI $28 AiAimShooter 27:AB61
//   AI $29 AiBurrower 27:AB9E   AI $2A AiLunger 27:ABF8 (+ AiLungeCheck 27:AC40)   AI $2E AiSpawnChildLunger 27:A9B4
//   AI $41 AiShellOnHit 27:B339
// None of these pops ObjUpdateAll's return address: every AiShootCheck / AiLungeCheck here is reached by a JSR,
// so its PLA PLA only ends the AI handler itself (the JS callers simply return).
import { rand64, dirToTarget, spawnShot } from './util.js';
import { objMoveSlide } from './move.js';
import { aiShootCheck } from './ai_static.js';

const X_LO = 0x70, X_HI = 0x90, Y_LO = 0xB0, Y_HI = 0xD0;
const META_ID = 0x0300, TILE_OFS = 0x0320, FLAGS2 = 0x0340, META_DIR = 0x0360, FLAGS = 0x0380, COLL = 0x03A0;
const SHOT_ID = 0x0440, TIMER = 0x0480, ACTIVE = 0x04A0, ANIM = 0x04E0, FREEZE = 0x05A0, PHASE = 0x0600;
const TIMER2 = 0x0620, ACT_TIMER = 0x0640, AUX = 0x0660, PARAM1 = 0x0680, PARAM2 = 0x06A0;
const WALK_POSE = 0x06C0, BEHAV = 0x06E0;

const dec = (m, a) => m.wr(a, m.rd(a) - 1);

/** AiWalkPick 27:A94A: dir = Rand64 & 6; walk obj_timer = Rand64*4 + $20; pause obj_phase = ($08 & 7)*8 */
function aiWalkPick(m, x) {
  m.wr(META_DIR + x, rand64(m) & 6);
  m.wr(TIMER + x, ((rand64(m) << 2) + 0x20) & 0xFF);
  m.wr(PHASE + x, (m.rd(0x08) & 7) << 3);
}

/** AI $20 */
export function aiWalkPause(m, x) {
  if ((m.rd(FLAGS + x) & 0x80) && ((x + m.rd(0x08)) & 3)) return;   // offscreen: quarter rate
  if (m.rd(PHASE + x)) {                                             // A923: pausing
    dec(m, PHASE + x);
    if (m.rd(ANIM + x) & 7) dec(m, ANIM + x);
    return;
  }
  if (m.rd(TIMER + x) === 0) aiWalkPick(m, x);
  dec(m, ANIM + x);
  dec(m, TIMER + x);
  if (objMoveSlide(m, x)) aiWalkPick(m, x);                          // A947 BCS: blocked
}

/** AI $21 */
export function aiShooterWalk(m, x) {
  if (m.rd(ACT_TIMER + x)) {
    dec(m, AUX + x);
    if (!(m.rd(AUX + x) & 1)) return;                                // every other frame
    dec(m, ACT_TIMER + x);
    m.wr(ANIM + x, m.rd(ACT_TIMER + x) >> 1);
    if (m.rd(ACT_TIMER + x) !== 0x0F) return;
    spawnShot(m, x, m.rd(SHOT_ID + x), (m.rd(META_ID + x) << 1) & 7);
    return;
  }
  if (aiShootCheck(m, x)) return;                                    // A998: attack starts, skip the walk
  aiWalkPause(m, x);
  m.wr(0x10, m.rd(META_ID + x) & 0xFC);
  m.wr(META_ID + x, (m.rd(META_DIR + x) >> 1) | m.rd(0x10));
  m.wr(PHASE + x, 0);
}

/** AI $2E: one linked child (the spawned slot's obj_param2 = this slot), then AI $2A from the next frame */
export function aiSpawnChildLunger(m, x) {
  if (m.rd(PARAM2 + x) === 0 && spawnShot(m, x, m.rd(SHOT_ID + x), 0)) {
    m.wr(PARAM2 + m.rd(0x10), x);
    m.wr(SHOT_ID + x, 0);
    return;
  }
  aiLunger(m, x);
}

/** AI $25 */
export function aiChaseOnscreen(m, x) {
  if (!(m.rd(FLAGS + x) & 0x80)) aiChase(m, x);
}

/** AI $27 */
export function aiShooterChaseOnscreen(m, x) {
  if (!(m.rd(FLAGS + x) & 0x80)) aiShooterChase(m, x);
}

/** facing nibble used by the shooter / lunger poses: obj_flags b3 -> 0, else (dir & 7) >> 1 */
const poseDir = (m, x) => ((m.rd(FLAGS + x) & 8) ? 0 : (m.rd(META_DIR + x) & 7) >> 1);

/** AI $26 */
export function aiShooterChase(m, x) {
  if (m.rd(ACT_TIMER + x)) {
    m.wr(META_ID + x, poseDir(m, x) | m.rd(BEHAV + x));             // attack pose
    dec(m, ACT_TIMER + x);
    m.wr(ANIM + x, m.rd(ACT_TIMER + x) >> 1);
    if (m.rd(ACT_TIMER + x) !== 0x0F) return;
    const y = (m.rd(FLAGS + x) & 8) ? m.rd(META_DIR + x) & 6 : (m.rd(META_ID + x) & 3) << 1;
    spawnShot(m, x, m.rd(SHOT_ID + x), y);
    return;
  }
  m.wr(META_ID + x, poseDir(m, x) | m.rd(WALK_POSE + x));           // AA31: walk pose
  m.wr(TILE_OFS + x, m.rd(TILE_OFS + x) & 3);
  if (aiShootCheck(m, x)) return;
  aiChase(m, x);
}

/** 27:AB2D: walk pose from the 4-way dir unless obj_flags b3 */
function chasePose(m, x) {
  if (m.rd(FLAGS + x) & 8) return;
  m.wr(META_ID + x, ((m.rd(META_DIR + x) & 6) >> 1) | m.rd(WALK_POSE + x));
}

/** 27:AAAB (A = obj_phase ^ 1) / 27:AAB0 (A = obj_phase): dir += A b0 ? +1 : -1, & 7 */
function turn(m, x, a) {
  m.wr(META_DIR + x, (m.rd(META_DIR + x) + ((a & 1) ? 1 : -1)) & 7);
}
const turnAway = (m, x) => turn(m, x, m.rd(PHASE + x) ^ 1);         // AAAB
const turnBack = (m, x) => turn(m, x, m.rd(PHASE + x));             // AAB0

/** AiTryMove 27:AAFA: ObjMoveSlide at speed $0A, position + obj_flags2 restored ($2B-$2F); -> C */
function aiTryMove(m, x) {
  m.wr(0x2B, m.rd(X_LO + x)); m.wr(0x2C, m.rd(X_HI + x)); m.wr(0x2D, m.rd(Y_LO + x)); m.wr(0x2E, m.rd(Y_HI + x));
  m.wr(0x2F, m.rd(FLAGS2 + x));
  m.wr(FLAGS2 + x, 0x0A);
  const c = objMoveSlide(m, x);
  m.wr(X_LO + x, m.rd(0x2B)); m.wr(X_HI + x, m.rd(0x2C)); m.wr(Y_LO + x, m.rd(0x2D)); m.wr(Y_HI + x, m.rd(0x2E));
  m.wr(FLAGS2 + x, m.rd(0x2F));
  return c;
}

/** AI $24 */
export function aiChase(m, x) {
  m.wr(0x10, (m.rd(FLAGS + x) & 0x80) ? 7 : 1);
  if ((x + m.rd(0x08)) & m.rd(0x10)) return;                         // every 2nd frame (8th offscreen)
  m.wr(ANIM + x, m.rd(ANIM + x) - 2);
  dec(m, TIMER + x);
  if (m.rd(TIMER2 + x) === 0) {
    if ((m.rd(TIMER + x) & 0x0F) === 0) {                            // re-aim at the player body (slot 0)
      dirToTarget(m, x, 0);
      m.wr(META_DIR + x, m.rd(0x20) & 0xFE);
      if (!(m.rd(FLAGS + x) & 8)) chasePose(m, x);
    }
    if (!objMoveSlide(m, x)) return;
    m.wr(TIMER2 + x, 0x60);                                          // AA9C: blocked -> 96-step detour
    m.wr(PHASE + x, (((x + m.rd(0x08) + 1) & 0xFF) >> 2) & 1);      // ADC with C = 1 left by the BCS
    turnAway(m, x);
    return;
  }
  dec(m, TIMER2 + x);                                                // AAC0: detour
  if ((m.rd(ANIM + x) & 7) === 0) {
    turnBack(m, x);
    if (aiTryMove(m, x)) {
      turnAway(m, x);
      if (aiTryMove(m, x)) { turnAway(m, x); return; }               // AADD JMP $AAAB
    } else {                                                         // AAEF: take the free way
      m.wr(META_DIR + x, m.rd(0x23));
      objMoveSlide(m, x);
      chasePose(m, x);
      return;
    }
  }
  objMoveSlide(m, x);                                                // AAE0
  if (m.rd(0x20) === 0) m.wr(META_DIR + x, m.rd(0x23));
  chasePose(m, x);
}

/** AI $22 */
export function aiDormant(m, x) {
  if (m.rd(FLAGS + x) & 0x80) return;
  dec(m, ANIM + x);
  if (m.rd(ANIM + x)) return;
  m.wr(ACTIVE + x, m.rd(PARAM2 + x));
  m.wr(META_ID + x, m.rd(WALK_POSE + x));
  m.wr(COLL + x, m.rd(COLL + x) | 0x10);
}

/** AI $28 */
export function aiAimShooter(m, x) {
  if (m.rd(AUX + x) === 0) {                                         // start delay
    dec(m, ANIM + x);
    if (m.rd(ANIM + x) === 0) m.wr(AUX + x, m.rd(AUX + x) + 1);
    return;
  }
  m.wr(META_ID + x, 0x65);
  if (m.rd(ACT_TIMER + x)) {
    m.wr(META_ID + x, 0x91);
    dec(m, ACT_TIMER + x);
    m.wr(ANIM + x, m.rd(ACT_TIMER + x));
    if (m.rd(ACT_TIMER + x) !== 0x0F) return;
    dirToTarget(m, x, 1);
    spawnShot(m, x, m.rd(SHOT_ID + x), m.rd(0x20));
    return;
  }
  if (aiShootCheck(m, x)) return;
  aiChase(m, x);
}

/** 27:ABAC: the AiBurrower body (its AiShootCheck pop returns here, to AB9E) */
function burrowerBody(m, x) {
  m.wr(COLL + x, 0x41);
  m.wr(META_ID + x, 0x68);
  if (m.rd(ACT_TIMER + x) === 0) {
    if (aiShootCheck(m, x)) return;
    aiChase(m, x);
    return;
  }
  m.wr(COLL + x, 0x11);
  m.wr(META_ID + x, 0x6B);
  if (m.rd(ACT_TIMER + x) === 0x10) {
    dirToTarget(m, x, 1);
    spawnShot(m, x, m.rd(SHOT_ID + x), m.rd(0x20));
  }
  dec(m, ANIM + x);
  dec(m, TIMER + x);
  if ((m.rd(TIMER + x) & 3) === 0) dec(m, ACT_TIMER + x);
}

/** AI $29 */
export function aiBurrower(m, x) {
  burrowerBody(m, x);
  if (m.rd(TIMER2)) m.wr(COLL + x, 0);                               // ABA1: LDA $0620 (slot 0, absolute)
}

/** AiLungeCheck 27:AC40 -> true = attack started (PLA PLA: the caller's walk is skipped) */
function aiLungeCheck(m, x) {
  dirToTarget(m, x, 1);
  if (m.rd(0x21) >= 2) return false;
  if (rand64(m) !== 0) return false;
  m.wr(META_DIR + x, m.rd(0x20));
  m.wr(ACT_TIMER + x, 0x0F);
  m.wr(ANIM + x, 0x0F);
  return true;
}

/** AI $2A */
export function aiLunger(m, x) {
  if (m.rd(FLAGS + x) & 0x80) return;
  if (m.rd(ACT_TIMER + x) === 0) {                                   // AC30
    m.wr(META_ID + x, (m.rd(META_DIR + x) >> 1) | m.rd(WALK_POSE + x));
    if (aiLungeCheck(m, x)) return;
    aiChase(m, x);
    return;
  }
  m.wr(META_ID + x, (m.rd(META_DIR + x) >> 1) | m.rd(BEHAV + x));
  dec(m, AUX + x);
  if (!(m.rd(AUX + x) & 1)) return;
  dec(m, ACT_TIMER + x);
  m.wr(ANIM + x, m.rd(ACT_TIMER + x));
  if (m.rd(ACT_TIMER + x) !== 8) return;
  spawnShot(m, x, m.rd(SHOT_ID + x), (m.rd(META_ID + x) & 3) << 1);
}

/** AI $41 */
export function aiShellOnHit(m, x) {
  const p2 = m.rd(PARAM2 + x);
  if (p2 === 0) {
    if (!(m.rd(FLAGS2 + x) & 0x80)) return aiShooterChase(m, x);
    m.wr(PARAM1 + x, m.rd(META_ID + x));                             // B346: hit -> close the shell
    m.wr(ACTIVE + x, 0x41);
    m.wr(COLL + x, m.rd(COLL + x) & 0x0F);
    m.wr(PARAM2 + x, 0x3F);
    m.wr(META_ID + x, 0x97);
    return;
  }
  dec(m, PARAM2 + x);                                                // B364 (A keeps the old value)
  const n = m.rd(PARAM2 + x);
  if (n === 0) {                                                     // B383: reopen
    m.wr(COLL + x, m.rd(COLL + x) | 0x10);
    m.wr(PARAM2 + x, 0);
    m.wr(ACTIVE + x, 0xC1);
    m.wr(META_ID + x, m.rd(PARAM1 + x));
    return aiShooterChase(m, x);
  }
  if (p2 >= 0x1F) {
    m.wr(ANIM + x, n);
    if (n === 0x20) m.wr(FREEZE + x, 8);
  } else m.wr(ANIM + x, p2 ^ 0xFF);
}

export const AI_WALK = {
  0x20: aiWalkPause, 0x21: aiShooterWalk, 0x22: aiDormant, 0x24: aiChase, 0x25: aiChaseOnscreen,
  0x26: aiShooterChase, 0x27: aiShooterChaseOnscreen, 0x28: aiAimShooter, 0x29: aiBurrower, 0x2A: aiLunger,
  0x2E: aiSpawnChildLunger, 0x41: aiShellOnHit,
};
