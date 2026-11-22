// re-c web port — the remaining field-enemy AIs of bank 27 (enemy port leftovers (b), s87). REVERSE.md "Enemies —
// catalogue + generic enemy AIs" (AI table rows $44 $45 $58 $5C $5D $5E, SteerToward). Ported instruction by
// instruction (every zero-page temp the ROM writes is written here too) for obj_check.mjs. Handlers take the slot x
// with banks 26/27 mapped and never return true (none pops ObjUpdateAll's return address: AiShootCheck is JSRed).
//
//   AI $44 AiDasher 27:B436          AI $45 AiHomingFlyer 27:B4BA (+ SteerToward 27:B73E, AiOffscreenHalf 27:B769)
//   AI $48 AiHomingShooter 27:B55A   AI $58 AiOrbiter 27:B6AA     AI $5C AiPatrolGunner 27:B626
//   AI $5D AiDrifter 27:B690         AI $5E AiWaitClear 27:B5F3   AI $2B AiTrapChest 27:AC60 (+ ChestHop 27:B79F)
//   AI $7B AiCoin 27:B778            AI $38 AiPitShuttle 27:AFA9      AI $3C AiCrumblePlatform 27:AFF5 (s117)
//   AI $3A AiNpcBossReveal 27:A555 (s133)   AI $30 AiNpcWander 27:ACD9 / $31 AiNpcStill 27:AE16 / $78 RTS (s140)
//   s170: AI $34 AiPeriodicShot 27:B318, $39 AiArenaLock 27:A519, $40 AiWander16 27:B39E
//   s171: AI $07 AiElementWall 27:A85F, $12 AiWiggleShot 27:B174, $13 AiSpiralShot 27:B235, $32 AiPathWalk 27:AE31
import { cpuRd } from '../prg.js';
import {
  rand64, dirToTarget, spawnShot, objPosToTemp, tempToObjPos, tempAddOffset, tempAddStep, objStepVector,
  objBgProbe, objBecomeNext, objDespawn,
} from './util.js';
import { objMoveSlide } from './move.js';
import { aiShootCheck } from './ai_static.js';
import { playSound } from '../playsound.js';
import { farObjInit } from './objinit.js';

const X_LO = 0x70, Y_HI = 0xD0, FREEZE = 0x05A0, RIDE_SLOT = 0x0541;
const META_ID = 0x0300, FLAGS2 = 0x0340, META_DIR = 0x0360, FLAGS = 0x0380, COLL = 0x03A0, SHOT_ID = 0x0440;
const TIMER = 0x0480, ACTIVE = 0x04A0, ANIM = 0x04E0, ELEM = 0x0500, SPAWN_PARAM = 0x0560, PHASE = 0x0600;
const TIMER2 = 0x0620, ACT_TIMER = 0x0640, OVERLAY = 0x0580, PARAM1 = 0x0680, ORBIT = 0x06C0;

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

/** AI $38 AiPitShuttle 27:AFA9 (types $7E/$7F, s117): DEC obj_timer; probe the point PlatformProbeOfs 27:AFED[dir&6]
 *  ahead (26:9824, ObjBgProbe A = 0 -> no cache, no flag update); b0 (pit) set -> one free step in obj_meta_dir,
 *  else dir + 4 and obj_freeze $20 */
export function aiPitShuttle(m, x) {
  dec(m, TIMER + x);
  objPosToTemp(m, x);
  const y = m.rd(META_DIR + x) & 6;
  m.wr(0x30, cpuRd(m, 0xAFED + y));
  m.wr(0x31, cpuRd(m, 0xAFEE + y));
  tempAddOffset(m);
  objBgProbe(m, x, 0);
  if (m.rd(0x20) & 1) { stepFree(m, x); return; }
  m.wr(META_DIR + x, (m.rd(META_DIR + x) + 4) & 7);
  m.wr(FREEZE + x, 0x20);
}

/** AI $3C AiCrumblePlatform 27:AFF5 (type $9F, s117): 27:AFFB, then AiPitShuttle (also right after the despawn).
 *  Ridden (ride_platform_slot = X) -> obj_phase 1; else obj_anim $1E -> sound $64 (Y = $78 from ObjUpdateAll's
 *  TAY); phase != 0 -> obj_coll 0, meta $B7, DEC obj_anim, 0 -> obj_active 0 */
export function aiCrumblePlatform(m, x) {
  if (x === m.rd(RIDE_SLOT)) m.wr(PHASE + x, 1);
  else {
    if (m.rd(ANIM + x) === 0x1E) playSound(m, 0x64, 0x78);
    if (m.rd(PHASE + x)) {
      m.wr(COLL + x, 0);
      m.wr(META_ID + x, 0xB7);
      dec(m, ANIM + x);
      if (m.rd(ANIM + x) === 0) m.wr(ACTIVE + x, 0);
    }
  }
  aiPitShuttle(m, x);
}

/** AI $3A AiNpcBossReveal 27:A555 (s133; type $2A, area $6E slot $0E, hidden on the slot-$0D NPC): 3 sword hits
 *  -> messages $10.$10-$12, then slots X-1/X+1 off and ObjInit(X, $7D) (BossAiE6). Scenario `npc_reveal`. */
function aiNpcBossReveal(m, x) {
  if (m.rd(ACTIVE - 1 + x) === 0) { m.wr(ACTIVE + x, 0); return; }        // A555: the NPC in slot X-1 is gone
  if (m.rd(PHASE + x) === 3) {                                            // A561
    m.wr(ACTIVE - 1 + x, 0); m.wr(ACTIVE + 1 + x, 0);
    m.wr(0x11, 0x7D); m.wr(0x10, x);
    farObjInit(m);                                                         // A573 JMP FarCall_C25D
    return;
  }
  m.wr(META_DIR + x, (((m.rd(0x0301) + 2) & 3) << 1) & 0xFF);            // A576: face the player
  if (m.rd(TIMER2 + x)) { dec(m, TIMER2 + x); return; }
  if (!(m.rd(FLAGS2 + x) & 0x80) || m.rd(0xD1) !== 1) return;            // A58B: hit, player on screen row 1
  const yl = m.rd(0xB1);
  if (yl >= 0xB0 || yl < 0x28) return;
  m.wr(0x03C0 + x, 0xFF);                                                 // A5A0: HP back to $FF
  m.wr(TIMER2 + x, 0x10);
  m.wr(PHASE + x, (m.rd(PHASE + x) + 1) & 0xFF);
  m.wr(0x06C3, (m.rd(PHASE + x) + 0x0F) & 0xFF);                          // pending_msg_id
  m.wr(0x06E3, 0x10);                                                     // pending_msg_group
  m.wr(0x41, 0x10);                                                       // sub_state
}

/** 27:AE07: (Y:A) >> 4, low byte ($1F = scratch) */
function cell16(m, lo, hi) {
  let h = hi, a = lo;
  for (let i = 0; i < 4; i++) { const c = h & 1; h >>= 1; a = (a >> 1) | (c << 7); }
  m.wr(0x1F, h);
  return a;
}

/** 27:ADC8: slots $12..$13-1 (minus X): one active in the 16 px cell $10/$11 -> $20 |= 6; true = found (RTS) */
function npcCellScan(m, x) {
  do {
    const y = m.rd(0x12);
    if (x !== y && m.rd(ACTIVE + y)) {
      if (cell16(m, m.rd(X_LO + y), m.rd(0x90 + y)) === m.rd(0x10)
        && cell16(m, m.rd(0xB0 + y), m.rd(Y_HI + y)) === m.rd(0x11)) {
        m.wr(0x20, m.rd(0x20) | 6);
        return true;
      }
    }
    m.wr(0x12, m.rd(0x12) + 1);
  } while (m.rd(0x12) !== m.rd(0x13));
  return false;
}

/** 27:ADA4: the look-ahead cell is blocked by another object (slots $0D-$1E, then slot 0) */
function npcCellBlocked(m, x) {
  m.wr(0x10, cell16(m, m.rd(0x34), m.rd(0x35)));
  m.wr(0x11, cell16(m, m.rd(0x36), m.rd(0x37)));
  m.wr(0x12, 0x0D); m.wr(0x13, 0x1F);
  npcCellScan(m, x);                                                      // ADBE (its RTS returns to ADC1)
  m.wr(0x12, 0); m.wr(0x13, 1);
  npcCellScan(m, x);                                                      // falls into ADC8
}

/** AiNpcStill 27:AE16 (AI $31, AiNpcWander with obj_behav b2): b0 -> obj_anim = $08>>3; $0603 = 0 -> meta = $06C0,X */
function aiNpcStill(m, x) {
  if (m.rd(0x06E0 + x) & 1) m.wr(ANIM + x, m.rd(0x08) >> 3);
  if (m.rd(0x0603) === 0) m.wr(META_ID + x, m.rd(0x06C0 + x));
}

/** AI $30 AiNpcWander 27:ACD9 (s140; the town NPCs): pause timer $0440; obj_behav b2 -> AiNpcStill; moves on
 *  BitMask[$08&7] & obj_phase frames; every 16 steps a Rand64 dir (bits 0/3 clear) with a 16 px look-ahead
 *  (NpcLookAhead 27:AD9C, ObjBgProbe A=0 + the object cell scan 27:ADA4), blocked -> turn +2 (even slot) / -2 (odd),
 *  4 tries; random 32-frame pause; step = 26:98A8 / 8480 / 9861 / 9A30($C0) / 9897, obj_anim-- */
function aiNpcWander(m, x) {
  if (m.rd(SHOT_ID + x)) { dec(m, SHOT_ID + x); return; }
  if (m.rd(0x06E0 + x) & 4) { aiNpcStill(m, x); return; }
  if (!(cpuRd(m, 0xC000 + (m.rd(0x08) & 7)) & m.rd(PHASE + x))) return;
  if ((m.rd(ANIM + x) & 0x0F) === 0) {
    m.wr(0x21, 4);
    if (!(m.rd(ANIM + x) & 0x10)) {
      const r = rand64(m);
      m.wr(0x10, r);
      if (!(r & 9)) m.wr(META_DIR + x, r & 6);
    }
    for (;;) {                                                            // AD1E
      const d = m.rd(META_DIR + x) & 6;
      m.wr(META_DIR + x, d);
      m.wr(0x30, cpuRd(m, 0xAD9C + d));
      m.wr(0x31, cpuRd(m, 0xAD9D + d));
      objPosToTemp(m, x);
      tempAddOffset(m);
      objBgProbe(m, x, 0);
      npcCellBlocked(m, x);
      if (!(m.rd(0x20) & 6)) break;
      m.wr(0x21, m.rd(0x21) - 1);
      if (m.rd(0x21) === 0) return;
      m.wr(META_DIR + x, (d + ((x & 1) ? -2 : 2)) & 6);                   // AD4A
    }
    m.wr(0x10, m.rd(META_DIR + x) >> 1);                                  // AD60
    m.wr(META_ID + x, (m.rd(META_ID + x) & 0xFC) | m.rd(0x10));
    if ((m.rd(0x08) & 0xD6) === 0 && (((x + m.rd(X_LO)) ^ m.rd(0xB0)) & 2) === 0) m.wr(SHOT_ID + x, 0x20);
  }
  objPosToTemp(m, x);                                                     // AD84
  objStepVector(m, x, m.rd(META_DIR + x));
  tempAddStep(m);
  objBgProbe(m, x, 0xC0);
  tempToObjPos(m, x);
  dec(m, ANIM + x);
}

// ---- s170: AI $34 $39 $40 (ai_missing.py) ----

/** AI $34 27:B318 (type $8F): obj_anim--; every 8th step, onscreen: SpawnShot(obj_shot_id, Y = x_lo b7 ? 6 : 2)
 *  (ROL x4 of the 9-bit A:C ring puts old b7 in b2, so the caller's carry never matters) */
export function aiPeriodicShot(m, x) {
  dec(m, ANIM + x);
  if (m.rd(ANIM + x) & 7) return;
  if (m.rd(FLAGS + x) & 0x80) return;
  spawnShot(m, x, m.rd(SHOT_ID + x), (m.rd(X_LO + x) & 0x80) ? 6 : 2);   // B336 JMP 26:972D
}

/** AI $39 AiArenaLock 27:A519 (type $29): slot X-1 inactive -> obj_active = 0, boss_cam_lock = 0. Phase 0:
 *  onscreen with screen y < $C0 -> phase 1, boss_cam_lock = $FF. Phase != 0: $07D5 = 0 -> boss_cam_lock = $FF and
 *  slot 0 obj_freeze = 5; else boss_cam_lock = $10 */
export function aiArenaLock(m, x) {
  if (m.rd(0x049F + x) === 0) {
    m.wr(ACTIVE + x, 0);
    m.wr(0x07D7, 0);
    return;
  }
  if (m.rd(PHASE + x) === 0) {
    if (m.rd(FLAGS + x) & 0x80) return;
    if (m.rd(0x05E0 + x) >= 0xC0) return;
    m.wr(PHASE + x, 1);
    m.wr(0x07D7, 0xFF);
    return;
  }
  if (m.rd(0x07D5) === 0) {
    m.wr(0x07D7, 0xFF);
    m.wr(FREEZE, 5);                                                       // A54B STA $05A0 (no ,X)
    return;
  }
  m.wr(0x07D7, 0x10);
}

/** AI $40 AiWander16 27:B39E (types $55 $7C): AiOffscreenHalf; phase 0: phase = $CF, heading = DirToTarget(Y = 1)*2
 *  + B41E[(x_lo + y_lo + timer of slot 0) & 3]; phase $01-$7F: anim -= 2, every 8 anim steps heading = B40E[(x +
 *  anim >> 3) & $F], flags2 $0C; phase >= $80: anim -= 3, phase--, flags2 $0E; both then timer--, one free step,
 *  phase-- */
export function aiWander16(m, x) {
  if (offscreenSkip(m, x)) return;
  const p = m.rd(PHASE + x);
  if (p === 0) {
    m.wr(PHASE + x, 0xD0);
    dirToTarget(m, x, 1);                                                  // B3AD LDY #1, JSR 26:8409
    m.wr(0x10, (m.rd(0x20) << 1) & 0xFF);
    const y = (m.rd(X_LO) + m.rd(0xB0) + m.rd(TIMER)) & 3;
    m.wr(META_DIR + x, (cpuRd(m, 0xB41E + y) + m.rd(0x10)) & 0x0F);
    dec(m, PHASE + x);
    return;
  }
  if (p & 0x80) {                                                          // B422
    m.wr(ANIM + x, m.rd(ANIM + x) - 3);
    dec(m, PHASE + x);
    m.wr(FLAGS2 + x, 0x0E);
  } else {
    m.wr(ANIM + x, m.rd(ANIM + x) - 2);
    if ((m.rd(ANIM + x) & 6) === 0) {
      m.wr(0x10, m.rd(ANIM + x) >> 3);
      m.wr(META_DIR + x, cpuRd(m, 0xB40E + ((x + m.rd(0x10)) & 0x0F)));
      m.wr(FLAGS2 + x, 0x0C);
    }
  }
  dec(m, TIMER + x);                                                       // B3F9
  objPosToTemp(m, x);
  objStepVector(m, x, m.rd(META_DIR + x));
  tempAddStep(m);
  dec(m, PHASE + x);
  tempToObjPos(m, x);                                                      // B40B JMP 26:9897
}

// ---- s171: AI $07 $12 $13 $32 (ai_missing.py) ----

/** AI $12 27:B174 (type $12, the wind level-$18 shot): phase 0 -> phase 1, timer2 = B1E5[X], start offset
 *  ($30, $31) = (B1E9[X], B1ED[X]) swapped when meta_dir b1, TempAddOffset. Then anim -= 2, timer-- 0 -> despawn;
 *  wiggle (B1F5/B1F6[(timer2 & $1F) * 2], same swap) timer2++, TempAddStep, then the meta_dir step */
export function aiWiggleShot(m, x) {
  const swap = () => { const a = m.rd(0x30); m.wr(0x30, m.rd(0x31)); m.wr(0x31, a); };
  if (m.rd(PHASE + x) === 0) {                                            // B1BC
    m.wr(PHASE + x, 1);
    m.wr(TIMER2 + x, cpuRd(m, 0xB1E5 + x));
    m.wr(0x30, cpuRd(m, 0xB1E9 + x));
    m.wr(0x31, cpuRd(m, 0xB1ED + x));
    if (m.rd(META_DIR + x) & 2) swap();
    objPosToTemp(m, x);
    tempAddOffset(m);
    tempToObjPos(m, x);
    return;
  }
  m.wr(ANIM + x, m.rd(ANIM + x) - 2);
  dec(m, TIMER + x);
  if (m.rd(TIMER + x) === 0) return void objDespawn(m, x);               // B184 JMP 26:98C7
  const y = (m.rd(TIMER2 + x) & 0x1F) << 1;
  m.wr(0x30, cpuRd(m, 0xB1F5 + y));
  m.wr(0x31, cpuRd(m, 0xB1F6 + y));
  if (m.rd(META_DIR + x) & 2) swap();
  m.wr(TIMER2 + x, m.rd(TIMER2 + x) + 1);
  objPosToTemp(m, x);
  tempAddStep(m);
  objStepVector(m, x, m.rd(META_DIR + x));
  tempAddStep(m);
  tempToObjPos(m, x);                                                     // B1B9 JMP 26:9897
}

/** AI $13 27:B235 (type $16, a spiral shot): timer |= $80; timer2++; (timer2 & act_timer) = 0 -> orbit_level--
 *  (0 -> despawn), meta_dir = ((X & 7) * 2 + phase) & $0F, phase++, every 16 turns act_timer = act_timer << 1 | 1;
 *  then one free step */
export function aiSpiralShot(m, x) {
  m.wr(TIMER + x, m.rd(TIMER + x) | 0x80);
  m.wr(TIMER2 + x, m.rd(TIMER2 + x) + 1);
  if ((m.rd(TIMER2 + x) & m.rd(ACT_TIMER + x)) === 0) {
    dec(m, ORBIT + x);
    if (m.rd(ORBIT + x) === 0) return void objDespawn(m, x);              // B24D JMP 26:98C7
    m.wr(META_DIR + x, (((x & 7) << 1) + m.rd(PHASE + x)) & 0x0F);
    m.wr(PHASE + x, m.rd(PHASE + x) + 1);
    if ((m.rd(PHASE + x) & 0x0F) === 0) m.wr(ACT_TIMER + x, (m.rd(ACT_TIMER + x) << 1) | 1);   // SEC ROL
  }
  stepFree(m, x);
}

/** AI $32 AiPathWalk 27:AE31 (type $38): slot 0 obj_freeze = 4; moves on BitMask[$08 & 7] & phase frames; anim--,
 *  every 16 steps the next byte of path AiPathTable[obj_param1] at timer2: < $80 = new meta_dir (timer2++), $FC =
 *  slots $0D/$0E restart on path $0D, $FD = player AI $0A + this slot AI $30 (behav 6, orbit $2E), $FE = off,
 *  other = AI $30. meta_id low 2 bits = dir >> 1, one free step; area $D4 path $0D slot $0D carries the player */
export function aiPathWalk(m, x) {
  m.wr(FREEZE, 4);
  if (!(cpuRd(m, 0xC000 + (m.rd(0x08) & 7)) & m.rd(PHASE + x))) return;
  dec(m, ANIM + x);
  if ((m.rd(ANIM + x) & 0x0F) === 0) {
    const y = m.rd(PARAM1 + x) << 1 & 0xFF;
    m.wr(0x10, cpuRd(m, 0xAF04 + y));
    m.wr(0x11, cpuRd(m, 0xAF05 + y));
    const b = cpuRd(m, ((m.rd(0x11) << 8 | m.rd(0x10)) + m.rd(TIMER2 + x)) & 0xFFFF);
    if (b & 0x80) {
      if (b === 0xFC) {                                                   // AE7B
        m.wr(ACTIVE + 0x0D, 0x32); m.wr(ACTIVE + 0x0E, 0x32);
        m.wr(PARAM1 + 0x0D, 0x0D); m.wr(PARAM1 + 0x0E, 0x0D);
        m.wr(ANIM + 0x0D, 1); m.wr(ANIM + 0x0E, 1);
        m.wr(TIMER2 + 0x0D, 0); m.wr(TIMER2 + 0x0E, 0);
        return;
      }
      if (b === 0xFD) {                                                   // AE9C
        m.wr(ACTIVE, 0x0A);
        m.wr(ACTIVE + x, 0x30);
        m.wr(0x06E0 + x, 6);
        m.wr(ORBIT + x, 0x2E);
        return;
      }
      m.wr(ACTIVE + x, b === 0xFE ? 0 : 0x30);
      return;
    }
    m.wr(META_DIR + x, b);                                                // AEB1
    m.wr(TIMER2 + x, m.rd(TIMER2 + x) + 1);
  }
  m.wr(0x10, m.rd(META_ID + x) & 0xFC);                                   // AEB7
  m.wr(META_ID + x, ((m.rd(META_DIR + x) >> 1) & 3) | m.rd(0x10));
  objStepVector(m, x, m.rd(META_DIR + x));
  objPosToTemp(m, x);
  tempAddStep(m);
  tempToObjPos(m, x);
  if (m.rd(0x6C) !== 0xD4 || m.rd(PARAM1 + x) !== 0x0D || x !== 0x0D) return;
  const yl = m.rd(0xB0 + x);                                              // AEE9: the player rides slot $0D
  m.wr(0xB1, yl); m.wr(0xB0, yl);
  const xl = (m.rd(X_LO + x) + 0x10) & 0xFF;
  m.wr(0x71, xl); m.wr(X_LO, xl);
  m.wr(TIMER2, 0); m.wr(ACT_TIMER, 0); m.wr(ORBIT, 0);
}

/** AI $07 27:A85F (types $D0-$D3, the element walls): shot_id != 0 -> outside areas $A8/$8F overlay = $4C; inside,
 *  behav != 0 -> behav--, anim = old behav >> 1, new behav $30 -> SpawnShot(shot_id, 0), spawned -> its meta_dir =
 *  A8A2[Rand64 & 3]; behav 0 and onscreen -> behav $3E. Then: visited screen -> obj_active 0; meta_id = overlay;
 *  onscreen inside the box A8FC-A90B[phase] -> obj_coll |= $10 */
export function aiElementWall(m, x) {
  if (m.rd(SHOT_ID + x)) {
    const a = m.rd(0x6C);
    if (a !== 0xA8 && a !== 0x8F) m.wr(OVERLAY + x, 0x4C);
    else if (m.rd(0x06E0 + x)) {                                          // A87A
      const b = m.rd(0x06E0 + x);
      m.wr(0x06E0 + x, b - 1);
      m.wr(ANIM + x, b >> 1);
      if (m.rd(0x06E0 + x) === 0x30 && spawnShot(m, x, m.rd(SHOT_ID + x), 0)) {
        const d = cpuRd(m, 0xA8A2 + (rand64(m) & 3));
        m.wr(META_DIR + m.rd(0x10), d);
      }
    } else if (!(m.rd(FLAGS + x) & 0x80)) m.wr(0x06E0 + x, 0x3E);       // A8A6
  }
  m.wr(0x10, cpuRd(m, 0xC000 + m.rd(0x90 + x)));                         // A8B0
  if (m.rd(0x62F0 + m.rd(Y_HI + x)) & m.rd(0x10)) m.wr(ACTIVE + x, 0);
  m.wr(META_ID + x, m.rd(OVERLAY + x));                                   // A8C5
  if (m.rd(FLAGS + x) & 0x80) return;
  const p = m.rd(PHASE + x), sy = m.rd(0x05E0 + x), sx = m.rd(0x05C0 + x);
  if (sy < cpuRd(m, 0xA8FC + p) || sy >= cpuRd(m, 0xA900 + p)) return;
  if (sx < cpuRd(m, 0xA904 + p) || sx >= cpuRd(m, 0xA908 + p)) return;
  m.wr(COLL + x, m.rd(COLL + x) | 0x10);
}

export const AI_FIELD = {
  0x07: aiElementWall, 0x12: aiWiggleShot, 0x13: aiSpiralShot, 0x32: aiPathWalk,   // s171
  0x34: aiPeriodicShot, 0x39: aiArenaLock, 0x40: aiWander16,             // s170
  0x30: aiNpcWander, 0x31: aiNpcStill, 0x78: () => {},                    // $78 = 27:A914 RTS (event triggers)
  0x3A: aiNpcBossReveal, 0x2B: aiTrapChest, 0x44: aiDasher, 0x45: aiHomingFlyer, 0x48: aiHomingShooter, 0x58: aiOrbiter,
  0x5C: aiPatrolGunner, 0x5D: aiDrifter, 0x5E: aiWaitClear, 0x7B: aiCoin, 0x38: aiPitShuttle, 0x3C: aiCrumblePlatform,
};
