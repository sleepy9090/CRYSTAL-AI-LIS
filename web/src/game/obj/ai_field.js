// re-c web port — the remaining field-enemy AIs of bank 27 (enemy port leftovers (b), s87). REVERSE.md "Enemies —
// catalogue + generic enemy AIs" (AI table rows $44 $45 $58 $5C $5D $5E, SteerToward). Ported instruction by
// instruction (every zero-page temp the ROM writes is written here too) for obj_check.mjs. Handlers take the slot x
// with banks 26/27 mapped and never return true (none pops ObjUpdateAll's return address: AiShootCheck is JSRed).
//
//   AI $44 AiDasher 27:B436          AI $45 AiHomingFlyer 27:B4BA (+ SteerToward 27:B73E, AiOffscreenHalf 27:B769)
//   AI $48 AiHomingShooter 27:B55A   AI $58 AiOrbiter 27:B6AA     AI $5C AiPatrolGunner 27:B626
//   AI $5D AiDrifter 27:B690         AI $5E AiWaitClear 27:B5F3   AI $2B AiTrapChest 27:AC60 (+ ChestHop 27:B79F)
//   AI $7B AiCoin 27:B778
import { cpuRd } from '../prg.js';
import {
  rand64, dirToTarget, spawnShot, objPosToTemp, tempToObjPos, tempAddOffset, tempAddStep, objStepVector,
  objBgProbe, objBecomeNext,
} from './util.js';
import { objMoveSlide } from './move.js';
import { aiShootCheck } from './ai_static.js';

const X_LO = 0x70, Y_HI = 0xD0;
const META_ID = 0x0300, FLAGS2 = 0x0340, META_DIR = 0x0360, FLAGS = 0x0380, COLL = 0x03A0, SHOT_ID = 0x0440;
const TIMER = 0x0480, ACTIVE = 0x04A0, ANIM = 0x04E0, ELEM = 0x0500, SPAWN_PARAM = 0x0560, PHASE = 0x0600;
const TIMER2 = 0x0620, ACT_TIMER = 0x0640;

const dec = (m, a) => m.wr(a, m.rd(a) - 1);

/** AiOffscreenHalf 27:B769: true = offscreen on an odd (X + $08) frame -> the caller returns (PLA PLA) */
const offscreenSkip = (m, x) => (m.rd(FLAGS + x) & 0x80) !== 0 && ((x + m.rd(0x08)) & 1) !== 0;

/** SteerToward 27:B73E: heading (0-15) one 22.5° step toward A; $10 = the turn (0 / $FF / 1); -> new heading */
function steerToward(m, x, a) {
  const t = (((a ^ 0xFF) + 1) + m.rd(META_DIR + x)) & 0x0F;
  m.wr(0x10, cpuRd(m, 0xB759 + t));
  const h = (m.rd(0x10) + m.rd(META_DIR + x)) & 0x0F;
  m.wr(META_DIR + x, h);
  return h;
}

/** move one step in obj_meta_dir with no BG test: 26:98A8 / 26:8480 / 26:9861 / 26:9897 */
function stepFree(m, x) {
  objPosToTemp(m, x);
  objStepVector(m, x, m.rd(META_DIR + x));
  tempAddStep(m);
  tempToObjPos(m, x);
}

/** AI $44 */
export function aiDasher(m, x) {
  if (!(m.rd(FLAGS2 + x) & 0xF0)) {                                  // B43B: knockback nibble -> pick
    if (offscreenSkip(m, x)) return;
    if (m.rd(PHASE + x)) {                                           // paused
      dec(m, PHASE + x);
      m.wr(ANIM + x, 0);
      return;
    }
    if (m.rd(TIMER + x)) {
      if (m.rd(FLAGS2 + x) === 6) {                                  // dash: meta $92, double animation rate
        m.wr(META_ID + x, 0x92);
        dec(m, ANIM + x);
      }
      dec(m, ANIM + x);
      dec(m, TIMER + x);
      objPosToTemp(m, x);
      objStepVector(m, x, m.rd(META_DIR + x));
      tempAddStep(m);
      if (!objMoveSlide(m, x)) return;                               // B477 BCC
    }
  }
  objPosToTemp(m, x);                                                // B479: pick
  objBgProbe(m, x, 0x40);
  m.wr(META_DIR + x, rand64(m) & 6);
  m.wr(META_ID + x, 0x65);
  const y = rand64(m);
  m.wr(TIMER + x, cpuRd(m, 0xB4AA + y));
  m.wr(FLAGS2 + x, cpuRd(m, 0xB4B2 + y));
  m.wr(PHASE + x, (m.rd(0x08) & 7) << 3);
}

/** 27:B4E5: re-aim (16-dir) and pick the speed level obj_phase 0-3 (B550 clamp) -> obj_flags2 = B54C[level] */
function homingSteer(m, x) {
  dirToTarget(m, x, 1);
  const h = steerToward(m, x, (m.rd(0x20) << 1) & 0xFF);
  m.wr(PHASE + x, m.rd(PHASE + x) + 1);
  if (h & m.rd(0x10)) m.wr(PHASE + x, m.rd(PHASE + x) - 2);         // B4F3 BIT $10 (A = the new heading)
  const p = cpuRd(m, 0xB550 + ((m.rd(PHASE + x) + 1) & 0xFF));
  m.wr(PHASE + x, p);
  m.wr(FLAGS2 + x, cpuRd(m, 0xB54C + p));
}

/** 27:B50E: obj_anim -= B556[level]; step; 1-in-64 aimed shot on ($08 & 7) = 0 when obj_shot_id != 0 */
function homingMove(m, x) {
  m.wr(ANIM + x, m.rd(ANIM + x) - cpuRd(m, 0xB556 + m.rd(PHASE + x)));
  dec(m, TIMER + x);
  stepFree(m, x);
  if (m.rd(SHOT_ID + x) === 0 || (m.rd(0x08) & 7) || rand64(m) !== 0) return;
  dirToTarget(m, x, 1);
  spawnShot(m, x, m.rd(SHOT_ID + x), m.rd(0x20));
}

/** AI $45 */
export function aiHomingFlyer(m, x) {
  if (m.rd(SPAWN_PARAM + x) === 1 && !(m.rd(ACTIVE + 0x0D) & 0x80)) {   // B4BA: param 1 dies with slot $0D's b7
    m.wr(ACTIVE + x, 0);
    return;
  }
  if (offscreenSkip(m, x)) return;
  m.wr(FLAGS + x, m.rd(FLAGS + x) & 0xEF);
  if ((m.rd(TIMER + x) & 0x0F) === 0) homingSteer(m, x);
  homingMove(m, x);
}

/** AI $48 (no object type uses it): attack = aimed shot at $0640 = $18; else AiShootCheck, AI $45, and a second
 *  steer + step when obj_timer reached 0 (B591 JMP $B4E5) */
export function aiHomingShooter(m, x) {
  if (m.rd(ACT_TIMER + x)) {
    dec(m, ACT_TIMER + x);
    m.wr(ANIM + x, m.rd(ANIM + x) - 2);
    if (m.rd(ACT_TIMER + x) !== 0x18) return;
    dirToTarget(m, x, 1);
    m.wr(META_DIR + x, (m.rd(0x20) << 1) & 0xFF);
    spawnShot(m, x, m.rd(SHOT_ID + x), m.rd(0x20));
    return;
  }
  if (aiShootCheck(m, x)) return;
  aiHomingFlyer(m, x);
  if (m.rd(TIMER + x) !== 0) return;
  homingSteer(m, x);
  homingMove(m, x);
}

/** AI $5E: every 64 frames (per slot) look for a live spawn-param $50 object on the player's y_hi; none -> AI $2A */
export function aiWaitClear(m, x) {
  if ((((x << 4) & 0xFF) + m.rd(0x08)) & 0x3F) return;
  for (let y = 0x0D; y < 0x1F; y++) {
    if (m.rd(ACTIVE + y) && m.rd(Y_HI + y) === m.rd(Y_HI) && m.rd(SPAWN_PARAM + y) === 0x50) return;
  }
  m.wr(ACTIVE + x, 0x2A);
  m.wr(COLL + x, 0x10);
}

/** AI $5C */
export function aiPatrolGunner(m, x) {
  m.wr(ELEM + x, 0x0F);
  if (m.rd(TIMER2 + x)) {                                            // B66E: attack burst, elem 0
    m.wr(ELEM + x, 0);
    m.wr(ANIM + x, 8);
    dec(m, TIMER2 + x);
    if (m.rd(TIMER2 + x) & 7) return;
    dirToTarget(m, x, 1);
    spawnShot(m, x, m.rd(SHOT_ID + x), m.rd(0x20));
    return;
  }
  m.wr(ANIM + x, 0);
  const xl = m.rd(X_LO + x);
  if (xl < 0x36 || xl >= 0xE8) m.wr(META_DIR + x, (m.rd(META_DIR + x) + 4) & 7);
  dec(m, TIMER + x);
  if ((m.rd(TIMER + x) & 0x1F) === 0 && (rand64(m) & 1)) m.wr(TIMER2 + x, 0x20);
  patrolStep(m, x);
}

/** 27:B65F (shared by AI $5C / $5D): ObjStepVector before ObjPosToTemp, then step, no BG test */
function patrolStep(m, x) {
  objStepVector(m, x, m.rd(META_DIR + x));
  objPosToTemp(m, x);
  tempAddStep(m);
  tempToObjPos(m, x);
}

/** AI $5D */
export function aiDrifter(m, x) {
  dec(m, ANIM + x);
  dec(m, TIMER + x);
  if ((m.rd(TIMER + x) & 0x3F) === 0) {
    dirToTarget(m, x, 1);
    m.wr(META_DIR + x, m.rd(0x20));
  }
  patrolStep(m, x);
}

/** AI $58: position = slot $0D + circle table 27:B6F4 (radius 24); 1-in-64 aimed shot every 64 obj_anim steps */
export function aiOrbiter(m, x) {
  let y = (((x << 3) & 0xFF) + m.rd(0x08)) & 0x1F;
  m.wr(0x30, cpuRd(m, 0xB6F4 + y));
  y = (y + 0xF8) & 0x1F;
  m.wr(0x31, cpuRd(m, 0xB6F4 + y) - 8);
  m.wr(0x10, x);
  objPosToTemp(m, 0x0D);
  tempAddOffset(m);
  tempToObjPos(m, x);
  dec(m, ANIM + x);
  if ((m.rd(ANIM + x) & 0x3F) || rand64(m) !== 0) return;
  dirToTarget(m, x, 1);
  spawnShot(m, x, m.rd(SHOT_ID + x), m.rd(0x20));
}

/** ChestHop 27:B79F (A = counter): dy = B7E6[nibble of B7C6[(A >> 1) & $1F], low nibble on odd A]; y move only */
function chestHop(m, x, a) {
  m.wr(0x10, a);
  let v = cpuRd(m, 0xB7C6 + ((a >> 1) & 0x1F));
  const c = m.rd(0x10) & 1;
  m.wr(0x10, m.rd(0x10) >> 1);
  if (!c) v >>= 4;
  objPosToTemp(m, x);
  m.wr(0x31, cpuRd(m, 0xB7E6 + (v & 0x0F)));
  m.wr(0x30, 0);
  tempAddStep(m);
  tempToObjPos(m, x);
}

/** AI $2B (type $73): closed chest (meta $AA) until the player is in its cell, then hops around (meta $90) */
export function aiTrapChest(m, x) {
  if (m.rd(PHASE + x) === 0) {                                       // ACAE
    m.wr(META_ID + x, 0xAA);
    dirToTarget(m, x, 1);
    if (m.rd(0x21) === 0) m.wr(PHASE + x, m.rd(PHASE + x) + 1);
    return;
  }
  dec(m, TIMER + x);
  m.wr(META_ID + x, 0x90);
  dec(m, ANIM + x);
  dec(m, TIMER2 + x);
  chestHop(m, x, m.rd(TIMER2 + x));
  if (m.rd(TIMER2 + x) & 0x3F) {                                     // AC80: step from the hopped position
    objStepVector(m, x, m.rd(META_DIR + x));
    tempAddStep(m);
    tempToObjPos(m, x);
    return;
  }
  objBgProbe(m, x, 0x40);                                            // AC8C: every 64 frames: wall -> reverse
  if (m.rd(0x20) & 6) {
    m.wr(META_DIR + x, (m.rd(META_DIR + x) + 4) & 7);
    return;
  }
  dirToTarget(m, x, 1);                                              // else re-aim (8-dir)
  m.wr(META_DIR + x, m.rd(0x20));
}

/** AI $7B coin: lifetime obj_timer -> ObjBecomeNext; blinks (obj_flags b2) below $30; hops while >= $C0 */
export function aiCoin(m, x) {
  m.wr(ANIM + x, m.rd(ANIM + x) - 2);
  dec(m, TIMER + x);
  if (m.rd(TIMER + x) === 0) return void objBecomeNext(m, x);
  if (m.rd(TIMER + x) < 0x30) {
    dec(m, ANIM + x);
    m.wr(FLAGS + x, m.rd(FLAGS + x) | 4);
  }
  if (m.rd(TIMER + x) >= 0xC0) chestHop(m, x, m.rd(TIMER + x));
}

export const AI_FIELD = {
  0x2B: aiTrapChest, 0x44: aiDasher, 0x45: aiHomingFlyer, 0x48: aiHomingShooter, 0x58: aiOrbiter,
  0x5C: aiPatrolGunner, 0x5D: aiDrifter, 0x5E: aiWaitClear, 0x7B: aiCoin,
};
