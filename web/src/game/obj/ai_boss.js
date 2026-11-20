// re-c web port — bank 15 bosses (session 100, "boss AI port, rest" (b), first group). REVERSE.md "Boss phase
// bodies, bank 15" + "Bank 15 bosses (web port, session 100)". ObjUpdateAll runs these ids with SetR7($0F).
//
//   bossPhaseDispatch(m, x, base)  BossPhaseDispatch 15:A400: JMP (BossPhaseTable 15:A413[(base + obj_phase)*2]),
//                                  $10/$11 = the handler address, Y = the table index. The table is shared, so an
//                                  out-of-range phase runs another boss's handler: BOSS_PHASES is keyed by address.
//   clearEnemySlots(m, x)          ClearEnemySlots 15:BB29
//   AI $60 (= $E0) BossAiE0  15:A477  teleporter (phases A48B A4C3 A4CC A4D8 A50B A54A A565 A581)
//   AI $61 BossHomingShot     15:A5EE  + HomingMove 15:A602 / ClampVel3 15:A665
//   AI $63 (= $E3) BossAiE3  15:A95D  slam / flame-sweep boss (phases A971 A9C6 AA46 AAA7 AAE6)
//   AI $64 BossRockShot       15:AB3E  rocks offset by ROM bytes from a random PRG bank
import { cpuRd, setR6 } from '../prg.js';
import { playSound } from '../playsound.js';
import { paletteBuildRom } from '../states/death.js';
import { hitFlashPal } from './ai_final.js';
import {
  rand64, objPosToTemp, tempToObjPos, tempAddOffset, dirToTarget, objStepVector, spawnShot,
} from './util.js';

const X_LO = 0x70, X_HI = 0x90, Y_LO = 0xB0, Y_HI = 0xD0;
const META = 0x0300, FLAGS2 = 0x0340, META_DIR = 0x0360, FLAGS = 0x0380, COLL = 0x03A0, HP = 0x03C0;
const SHOT_ID = 0x0440, TIMER = 0x0480, ACTIVE = 0x04A0, ANIM = 0x04E0, FREEZE = 0x05A0, PHASE = 0x0600;
const TIMER2 = 0x0620, ACT_TIMER = 0x0640, AUX = 0x0660, PARAM1 = 0x0680, PARAM2 = 0x06A0;
const CAM_LOCK = 0x07D7;

function unported(m, what) {
  if (!m.unported.has(what)) { m.unported.add(what); m.log(`${what} not ported yet`); }
}
const inc = (m, a) => { const v = (m.rd(a) + 1) & 0xFF; m.wr(a, v); return v; };
const dec = (m, a) => { const v = (m.rd(a) - 1) & 0xFF; m.wr(a, v); return v; };

/** ClearEnemySlots 15:BB29: slots $0D-$1E except X and AIs $78/$7F/$30/$39 off, then slots 4-11 off; Y ends $0C */
export function clearEnemySlots(m, x) {
  for (let y = 0x0D; y !== 0x1F; y++) {
    m.wr(0x10, y);
    if (x === y) continue;
    const a = m.rd(ACTIVE + y);
    if (a === 0x78 || a === 0x7F || a === 0x30 || a === 0x39) continue;
    m.wr(ACTIVE + y, 0);
  }
  for (let y = 4; y < 0x0C; y++) m.wr(ACTIVE + y, 0);
}

/** the shared intro tail: once the camera is aligned ($07D5): player freeze 0 and boss music (Y = $0C) */
function introMusic(m) {
  m.wr(FREEZE, 0);
  playSound(m, 0x12, 0x0C);
}

/** ObjStepVector(meta_dir) + 26:98A8 + 26:9824 + 26:9897 (the move tail at 15:AA37) */
function stepMove(m, x) {
  objStepVector(m, x, m.rd(META_DIR + x));
  objPosToTemp(m, x);
  tempAddOffset(m);
  tempToObjPos(m, x);
}

// ---- AI $E0 BossAiE0 15:A477 -------------------------------------------------------------------------------

/** BossSmokeRing 15:A5D5: 8 x SpawnShot($3C, Y = $1F) with obj_freeze = $1F (8 ... 1) */
function bossSmokeRing(m, x) {
  m.wr(0x1F, 8);
  do {
    if (!spawnShot(m, x, 0x3C, m.rd(0x1F))) return;
    m.wr(FREEZE + m.rd(0x10), m.rd(0x1F));
  } while (dec(m, 0x1F));
}

/** BossE0Fire 15:A5A2: obj_aux & 3 = 0 -> 2 shots with param1 = +2 / -2, param2 = -4 above the player else +4 */
function bossE0Fire(m, x) {
  if (m.rd(AUX + x) & 3) return;
  m.wr(0x2F, 2);
  do {
    if (!spawnShot(m, x, m.rd(SHOT_ID + x), m.rd(0x2F))) return;
    const n = m.rd(0x10);
    m.wr(PARAM1 + n, ((m.rd(0x2F) << 2) - 6) & 0xFF);
    m.wr(PARAM2 + n, m.rd(Y_LO + x) < m.rd(Y_LO) ? 0xFC : 0x04);
  } while (dec(m, 0x2F));
}

function e0Intro(m, x) {                                     // A48B
  if (m.rd(FLAGS + x) & 0x80) return;
  clearEnemySlots(m, x);
  m.wr(0x07E6, 0x21); m.wr(0x07E7, 0x21);
  m.wr(0x07F4, 0x6C); m.wr(0x07F5, 0x6C);
  m.wr(FREEZE, 0x10);
  m.wr(CAM_LOCK, 0xFF);
  if (!m.rd(0x07D5)) return;
  introMusic(m);
  m.wr(PHASE + x, (m.rd(PHASE + x) + 2) & 0xFF);
}

function e0Wait(m, x) {                                      // A4C3 (unused phase 1)
  if (!dec(m, TIMER + x)) inc(m, PHASE + x);
}

function e0Smoke(m, x) {                                     // A4CC
  bossSmokeRing(m, x);
  m.wr(ANIM + x, 0);
  inc(m, PHASE + x);
}

function e0Vanish(m, x) {                                    // A4D8
  if (inc(m, ANIM + x) & 0x0F) return;
  m.wr(ANIM + x, 0);
  m.wr(COLL + x, m.rd(COLL + x) & 0x0F);
  m.wr(FLAGS + x, 1);
  m.wr(META + x, 0x4C);
  inc(m, PHASE + x);
}

function e0Teleport(m, x) {                                  // A50B
  const a = (m.rd(AUX + x) + 1) & 0x0F;
  m.wr(AUX + x, a);
  if (a < 0x0C && inc(m, ANIM + x) !== 0x80) return;
  inc(m, PHASE + x);                                         // A524
  m.wr(TIMER2 + x, 0);
  const s = cpuRd(m, 0xA4FB + (m.rd(0x08) & 0x0F));          // BossE0Spots
  m.wr(X_LO + x, s & 0xF0);
  m.wr(Y_LO + x, (s << 4) & 0xFF);
  m.wr(X_HI + x, m.rd(X_HI));
  m.wr(Y_HI + x, m.rd(Y_HI));
}

function e0Appear(m, x) {                                    // A54A
  m.wr(META + x, 0x65);
  m.wr(COLL + x, m.rd(COLL + x) | 0x30);
  bossSmokeRing(m, x);
  m.wr(ANIM + x, 0);
  inc(m, PHASE + x);
  bossE0Fire(m, x);
}

function e0Hold(m, x) {                                      // A565
  if (inc(m, ANIM + x) & 0x0F) return;
  m.wr(ANIM + x, ((m.rd(HP + x) << 2) + 0x10) & 0xFF);
  inc(m, PHASE + x);
}

function e0Linger(m, x) {                                    // A581
  if (!(m.rd(FLAGS2 + x) & 0x80)) {
    if (dec(m, ANIM + x)) return;
  }
  dirToTarget(m, x, 1);                                      // A592
  m.wr(META_DIR + x, m.rd(0x20));
  m.wr(PHASE + x, 2);
}

export function bossAiE0(m, x) {
  hitFlashPal(m, x, 0xC0);
  if (m.rd(PHASE + x)) m.wr(CAM_LOCK, 0x10);
  return bossPhaseDispatch(m, x, 0x00);
}

// ---- AI $61 BossHomingShot 15:A5EE ---------------------------------------------------------------------------

/** ClampVel3 15:A665 */
function clampVel3(a) {
  if (a & 0x80) return a >= 0xFE ? a : 0xFD;
  return a < 3 ? a : 3;
}

/** HomingMove 15:A602: velocity (param1, param2) nudged +-1 toward $34-$37 every 8 obj_anim steps */
export function homingMove(m, x) {
  m.wr(META_DIR + x, (m.rd(META_DIR) + 4) & 7);
  m.wr(0x30, m.rd(PARAM1 + x));
  m.wr(0x31, m.rd(PARAM2 + x));
  if (!(m.rd(ANIM + x) & 7)) {
    const sx = ((m.rd(0x34) < m.rd(X_LO + x) ? 1 : 0) + m.rd(X_HI + x)) & 0xFF;   // SBC hi with the lo borrow
    const hx = (m.rd(0x35) - sx) & 0xFF;
    const vx = clampVel3(((hx & 0x80 ? 0xFF : 1) + m.rd(PARAM1 + x)) & 0xFF);
    m.wr(PARAM1 + x, vx); m.wr(0x30, vx);
    const sy = ((m.rd(0x36) < m.rd(Y_LO + x) ? 1 : 0) + m.rd(Y_HI + x)) & 0xFF;
    const hy = (m.rd(0x37) - sy) & 0xFF;
    const vy = clampVel3(((hy & 0x80 ? 0xFF : 1) + m.rd(PARAM2 + x)) & 0xFF);
    m.wr(PARAM2 + x, vy); m.wr(0x31, vy);
  }
  objPosToTemp(m, x);
  tempAddOffset(m);
  tempToObjPos(m, x);
  dec(m, ANIM + x);
}

export function bossHomingShot(m, x) {
  m.wr(0x10, x);
  objPosToTemp(m, 1);
  if (m.rd(FLAGS + x) & 0x80) { m.wr(ACTIVE + x, 0); return; }
  homingMove(m, x);
}

// ---- AI $E3 BossAiE3 15:A95D ---------------------------------------------------------------------------------

/** BossE3NewDir 15:AA89: meta_dir = 15:AA9F[Rand64], phase - 1 */
function e3NewDir(m, x) {
  m.wr(META_DIR + x, cpuRd(m, 0xAA9F + rand64(m)));
  dec(m, PHASE + x);
}

/** TurnToward16 15:AB25: obj_act_timer one 16-dir step toward A (sign table 15:AD86) */
function turnToward16(m, x, a) {
  const t = m.rd(ACT_TIMER + x);
  const y = (((a ^ 0xFF) + 1) + t) & 0x0F;
  m.wr(ACT_TIMER + x, (cpuRd(m, 0xAD86 + y) + t) & 0x0F);
}

function e3Intro(m, x) {                                     // A971
  if (m.rd(FLAGS + x) & 0x80) return;
  m.wr(FREEZE, 0x10);
  m.wr(CAM_LOCK, 0xFF);
  m.wr(0x07F4, 0x77);
  m.wr(0x07E7, 0x1C);
  paletteBuildRom(m, 0);                                     // JSR 26:8C0E
  clearEnemySlots(m, x);
  if (!m.rd(0x07D5)) return;
  introMusic(m);
  m.wr(0x06C3, 0);                                           // pending message 6.0 (area $28) or 24.0
  m.wr(0x06E3, m.rd(0x6C) === 0x28 ? 0x06 : 0x18);
  m.wr(0x41, 0x10);
  m.wr(FLAGS + x, 9);
  m.wr(COLL + x, 0x30);
  inc(m, PHASE + x);
}

function e3Approach(m, x) {                                  // A9C6
  if (!(m.rd(FLAGS + x) & 0x80) && !(m.rd(0x08) & 7) && rand64(m) === 0) {
    m.wr(PHASE + x, 3);
    m.wr(TIMER + x, 0x4F);
    m.wr(ANIM + x, 0x4F);
    if (m.rd(0x6C) === 0x28) return;
    dirToTarget(m, x, 1);
    m.wr(ACT_TIMER + x, (m.rd(0x20) << 1) & 0xFF);
    inc(m, PHASE + x);
    return;
  }
  m.wr(META + x, 0xD3);                                      // A9FA
  const t = dec(m, TIMER + x);
  dec(m, ANIM + x);
  if (!(t & 7)) {
    if (m.rd(Y_LO + x) < 0x48) { inc(m, PHASE + x); e3NewDir(m, x); return; }
    dirToTarget(m, x, 0);
    m.wr(META_DIR + x, m.rd(0x20));
    if (m.rd(0x08) < 0xA0 && m.rd(0x21) < 2) {
      inc(m, PHASE + x);
      m.wr(TIMER + x, 0x30);
    }
  }
  stepMove(m, x);
}

function e3BackOff(m, x) {                                   // AA46
  dec(m, ANIM + x);
  const t = dec(m, TIMER + x);
  if (!t) {
    dec(m, PHASE + x);
    m.wr(META_DIR + x, m.rd(META_DIR + x) | 4);
    return;
  }
  if (t & 7) { stepMove(m, x); return; }
  dirToTarget(m, x, 0);
  m.wr(META_DIR + x, cpuRd(m, 0xAA97 + ((m.rd(0x20) + 4) & 7)));
  if (m.rd(Y_LO + x) >= 0x48) {
    const xl = m.rd(X_LO + x);
    if (xl >= 0x30 && xl < 0xD0) { stepMove(m, x); return; }
    m.wr(META_DIR + x, 0);                                   // AA84
  }
  e3NewDir(m, x);
}

function e3Slam(m, x) {                                      // AAA7
  if (!dec(m, TIMER + x)) { dec(m, PHASE + x); e3NewDir(m, x); return; }
  m.wr(META + x, 0xD5);
  m.wr(META_DIR + x, ((m.rd(0x0301) << 1) + 4) & 7);
  if (m.rd(0x08) & 1) dec(m, ANIM + x);
  if ((m.rd(ANIM + x) ^ 7) & 0x0F) return;
  m.wr(0x07D6, 8);
  playSound(m, 0x3D, ((0x0A + m.rd(PHASE + x)) << 1) & 0xFF);   // Y from BossPhaseDispatch
  spawnShot(m, x, 0x38, 0);
}

function e3Sweep(m, x) {                                     // AAE6
  m.wr(META + x, 0xD3);
  const t = dec(m, TIMER + x);
  if (!t) { m.wr(PHASE + x, 2); e3NewDir(m, x); return; }
  if (t >= 0x20) return;
  m.wr(META + x, 0xD4);
  if (t & 1) return;
  if (!(t & 6)) {
    dirToTarget(m, x, 1);
    turnToward16(m, x, (m.rd(0x20) << 1) & 0xFF);
  }
  spawnShot(m, x, 0x36, m.rd(ACT_TIMER + x));
}

export function bossAiE3(m, x) {
  if (m.rd(PHASE + x)) m.wr(CAM_LOCK, 4);
  hitFlashPal(m, x, 0xC0);
  return bossPhaseDispatch(m, x, 0x0A);
}

// ---- AI $64 BossRockShot 15:AB3E -----------------------------------------------------------------------------

/** 15:AB8A: low 6 bits, sign from b7 */
const rockByte = (v) => (v & 0x3F) | (v & 0x80 ? 0xC0 : 0);

export function bossRockShot(m, x) {
  const p = m.rd(PHASE + x);
  if (p === 0) {
    inc(m, PHASE + x);
    const r = rand64(m);
    m.wr(0x10, r);
    const saved = m.rd(0x6E);
    setR6(m, rand64(m));
    const y = ((((r << 1) & 0xFF) + m.rd(X_LO + x) + (r >> 7)) & 0xFF) ^ m.rd(Y_LO + x) ^ m.rd(0x08);
    m.wr(0x10, 0); m.wr(0x11, 0x80);
    m.wr(0x30, cpuRd(m, 0x8000 + y));
    m.wr(0x31, cpuRd(m, 0x8000 + ((y + 1) & 0xFF)));
    setR6(m, saved);
    objPosToTemp(m, x);
    m.wr(0x10, m.rd(0x30)); m.wr(0x30, rockByte(m.rd(0x30)));
    m.wr(0x10, m.rd(0x31)); m.wr(0x31, rockByte(m.rd(0x31)));
    tempAddOffset(m);
    tempToObjPos(m, x);
    return;
  }
  if (p === 1) {
    if (dec(m, ANIM + x)) return;
    m.wr(ANIM + x, 0x20);
    m.wr(META + x, 0x9E);
    inc(m, PHASE + x);
    return;
  }
  dec(m, Y_LO + x);                                          // ABAD
  if (dec(m, ANIM + x)) return;
  m.wr(ANIM + x, 0x80);
  m.wr(ACTIVE + x, 0x10);
  dirToTarget(m, x, 0);
  m.wr(0x20, (m.rd(0x20) << 1) & 0xFF);
  m.wr(META_DIR + x, (cpuRd(m, 0xABD6 + ((x + m.rd(0x08)) & 3)) + m.rd(0x20)) & 0xFF);
}

// ---- BossPhaseDispatch 15:A400 -------------------------------------------------------------------------------

const BOSS_PHASES = {
  0xA48B: e0Intro, 0xA4C3: e0Wait, 0xA4CC: e0Smoke, 0xA4D8: e0Vanish, 0xA50B: e0Teleport, 0xA54A: e0Appear,
  0xA565: e0Hold, 0xA581: e0Linger,
  0xA971: e3Intro, 0xA9C6: e3Approach, 0xAA46: e3BackOff, 0xAAA7: e3Slam, 0xAAE6: e3Sweep,
};

export function bossPhaseDispatch(m, x, base) {
  const y = ((base + m.rd(PHASE + x)) << 1) & 0xFF;
  m.wr(0x10, cpuRd(m, 0xA413 + y));
  m.wr(0x11, cpuRd(m, 0xA414 + y));
  const a = m.rd(0x11) << 8 | m.rd(0x10);
  const h = BOSS_PHASES[a];
  if (!h) { unported(m, `AI boss phase handler 15:${a.toString(16).toUpperCase().padStart(4, '0')}`); return; }
  return h(m, x);
}

export const AI_BOSS = { 0x60: bossAiE0, 0x61: bossHomingShot, 0x63: bossAiE3, 0x64: bossRockShot };
