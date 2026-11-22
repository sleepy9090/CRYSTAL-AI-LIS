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
//   AI $65 (= $E5) AiTalkEventE5 15:ABDA  area $43 talk event (phases ABE4 ABFC AC0F AC1A AC1E AC31 AC41 AC5B) (s157)
//   AI $66 (= $E6) BossAiE6  15:AC7B  flyer with a fire fan (phases AC8F ACF5; 2/3 are null)       (session 104)
//   AI $67 (= $E7) AiBoss88  15:ADD4  bouncer + curve-shot volley (phases ADE8 AE35 AE43 AE5A AEC9 AEEC)
//   AI $68 (= $E8) BossAiE8  15:AF48  waypoint flyer (route 15:B02B in slot $1F, HomingMove) (phases AF5C AFAD B051)
//   AI $69 (= $E9) BossAiE9  15:B0B6  statues (phases B0C5 B0F4 B105)                                (session 156)
//   AI $6A (= $EA) BossAiEA  15:B130  patrol / dive / rise + fire fan 15:B278 (phases B14F B1A5 B215 B22C)
import { cpuRd, setR6 } from '../prg.js';
import { playSound } from '../playsound.js';
import { paletteBuildRom } from '../states/death.js';
import { hitFlashPal } from './ai_final.js';
import { farObjInit } from './objinit.js';
import {
  rand64, objPosToTemp, tempToObjPos, tempAddOffset, tempAddStep, dirToTarget, objStepVector, spawnShot,
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

// ---- shared by $E6 / $E7 / $E8 (session 104) -----------------------------------------------------------------

const OVERLAY = 0x0580;

/** the intro's "aligned" tail with a pending message: group/id, sub_state $10 (15:ACE6 / AE26 / AF8A) */
function introMessage(m, group, id) {
  m.wr(0x06E3, group);
  m.wr(0x06C3, id);
  m.wr(0x41, 0x10);
}

/** ObjPosToTemp + ObjStepVector(meta_dir) + TempAddStep + TempToObjPos (BossE7Step 15:AEBA, tail 15:AD73) */
function stepMove16(m, x) {
  objPosToTemp(m, x);
  objStepVector(m, x, m.rd(META_DIR + x));
  tempAddStep(m);
  tempToObjPos(m, x);
}

// ---- AI $E5 AiTalkEventE5 15:ABDA (area $43 talk event, session 157) -----------------------------------------
// phases 4 / 6 are set by talk acts 26 / 27 ($060D = 4 / 6); phase 3 waits for them.

function e5Rise(m, x) {                                      // ABE4
  m.wr(OVERLAY + x, 0xBA);
  if (inc(m, ANIM + x) !== 0x1F) return;
  m.wr(META + x, 0xBB);
  inc(m, PHASE + x);
}

function e5Settle(m, x) {                                    // ABFC
  if (dec(m, ANIM + x)) return;
  inc(m, PHASE + x);
  m.wr(META + x, 0xB0);
  m.wr(OVERLAY + x, 0x4C);
}

function e5Talk(m, x) {                                      // AC0F: NpcTalk with obj_timer2_s03 = this slot
  m.wr(0x41, 0x11);
  m.wr(0x0623, x);
  inc(m, PHASE + x);
}

function e5Wait(m, x) { dec(m, ANIM + x); }                  // AC1A

function e5Leave(m, x) {                                     // AC1E
  m.wr(META + x, 0xBB);
  m.wr(OVERLAY + x, 0xBA);
  m.wr(ANIM + x, 0);
  inc(m, PHASE + x);
}

function e5Sink(m, x) {                                      // AC31
  if (inc(m, ANIM + x) !== 0x1F) return;
  m.wr(ACTIVE + x, 0);
}

function e5ThrowStart(m, x) {                                // AC41: SpawnShot($0E, dir 4), 8-frame wait
  m.wr(META + x, 0xB1);
  m.wr(OVERLAY + x, 0x4C);
  spawnShot(m, x, 0x0E, 4);
  m.wr(ANIM + x, 8);
  inc(m, PHASE + x);
}

/** AC5B: after the wait, slot 0's y + 8 -> slots 0 and 1 every frame until >= $B0; then $0640 = $FF, phase 4 */
function e5Throw(m, x) {
  if (dec(m, ANIM + x)) return;
  const y = (m.rd(Y_LO) + 8) & 0xFF;
  m.wr(Y_LO, y); m.wr(Y_LO + 1, y);
  inc(m, ANIM + x);
  if (y < 0xB0) return;
  m.wr(ACT_TIMER, 0xFF);
  m.wr(PHASE + x, 4);
}

export function aiTalkEventE5(m, x) {
  m.wr(FREEZE, 0x10);
  return bossPhaseDispatch(m, x, 0x0F);
}

// ---- AI $E6 BossAiE6 15:AC7B ---------------------------------------------------------------------------------

function e6Intro(m, x) {                                     // AC8F
  if (m.rd(FLAGS + x) & 0x80) return;
  m.wr(CAM_LOCK, 0xFF);
  m.wr(FREEZE, 0x10);
  clearEnemySlots(m, x);
  m.wr(0x07F4, 0x75);
  if (!m.rd(0x07D5)) return;
  introMusic(m);                                             // every frame until obj_timer2 reaches $40
  inc(m, TIMER2 + x);
  m.wr(FREEZE, 0);
  if (m.rd(TIMER2 + x) !== 0x40) return;
  inc(m, PHASE + x);
  m.wr(FLAGS + x, 0x0B);
  m.wr(COLL + x, 0x30);
  m.wr(OVERLAY + x, 0x4C);
  m.wr(META_DIR + x, 8);
  m.wr(TIMER2 + x, 0x50);
  if (m.rd(0x6C) === 0xAC) introMessage(m, 0x18, 0x02);
}

/** 15:AD96: the fire fan (meta $DC + overlay $DD), a shot every 8 obj_timer2 steps in (timer2>>4) - 2 + dir */
function e6Fire(m, x) {
  m.wr(OVERLAY + x, 0xDD);
  m.wr(META + x, 0xDC);
  if (!dec(m, TIMER2 + x)) { m.wr(META + x, 0xDB); m.wr(OVERLAY + x, 0x4C); }
  inc(m, ANIM + x); inc(m, ANIM + x);
  const t = m.rd(TIMER2 + x);
  if (t & 7) return;
  spawnShot(m, x, m.rd(SHOT_ID + x), ((t >> 4) - 2 + m.rd(META_DIR + x)) & 0x0F);
}

function e6Fight(m, x) {                                     // ACF5
  if (!(m.rd(0x08) & 0x3F) && m.rd(0x6C) === 0xAC) spawnShot(m, x, 0x35, 0);
  if (m.rd(TIMER2 + x)) { e6Fire(m, x); return; }
  if (!(dec(m, ANIM + x) & 7)) {
    const py = m.rd(Y_LO);                                   // a fake player y of $30 / $70
    m.wr(Y_LO, ((m.rd(0x08) & 0x80) >> 1) | 0x30);
    dirToTarget(m, x, 0);
    m.wr(Y_LO, py);
    const d = m.rd(META_DIR + x);
    const y = ((((m.rd(0x20) << 1) & 0xFF) ^ 0xFF) + 1 + d) & 0x0F;
    m.wr(META_DIR + x, (cpuRd(m, 0xAD86 + y) + d) & 0x0F);
    if (rand64(m) === 0) {
      dirToTarget(m, x, 1);
      m.wr(TIMER2 + x, 0x50);
      m.wr(META_DIR + x, (m.rd(0x20) << 1) & 0xFF);
    }
  }
  const h = m.rd(HP + x) >> 4;                               // AD5E: speed $0E $0D $0F once HP < $30
  if (h < 3) m.wr(FLAGS2 + x, cpuRd(m, 0xAD82 + h));
  dec(m, TIMER + x);
  stepMove16(m, x);
}

export function bossAiE6(m, x) {
  if (m.rd(PHASE + x)) m.wr(CAM_LOCK, 4);
  hitFlashPal(m, x, 0xC0);
  return bossPhaseDispatch(m, x, 0x17);
}

// ---- AI $E7 AiBoss88 15:ADD4 ---------------------------------------------------------------------------------

function e7Intro(m, x) {                                     // ADE8
  if (m.rd(FLAGS + x) & 0x80) return;
  clearEnemySlots(m, x);
  m.wr(0x07F4, 0x76);
  m.wr(0x07E6, 0x1E);
  m.wr(FREEZE, 0x10);
  m.wr(CAM_LOCK, 0xFF);
  if (!m.rd(0x07D5)) return;
  introMusic(m);
  inc(m, PHASE + x);
  m.wr(COLL + x, 0x30);
  m.wr(FLAGS + x, 9);
  if (m.rd(0x6C) === 0xB9) introMessage(m, 0x18, 0x04);
}

function e7Pose(m, x) {                                      // AE35
  inc(m, PHASE + x);
  m.wr(META + x, 0xD8);
  m.wr(ANIM + x, 0x0F);
}

function e7Wait(m, x) {                                      // AE43
  if (dec(m, ANIM + x)) return;
  inc(m, PHASE + x);
  m.wr(META + x, 0xD7);
  m.wr(ACT_TIMER + x, (m.rd(HP + x) + 0x20) & 0xFF);
}

function e7Bounce(m, x) {                                    // AE5A
  dec(m, TIMER + x);
  dec(m, ANIM + x); dec(m, ANIM + x);
  stepMove16(m, x);
  const yl = m.rd(Y_LO + x), xl = m.rd(X_LO + x);
  let edge = -1;
  if (yl < 0x15) edge = 0;
  else if (yl >= 0xD0) edge = 0x10;
  else if (xl < 0x20) edge = 0x18;
  else if (xl >= 0xE0) edge = 0x08;
  if (edge >= 0) {                                           // AE82
    m.wr(0x10, edge);
    playSound(m, 0x3D, edge);
    m.wr(0x07D6, 8);
    const r = rand64(m);
    m.wr(META_DIR + x, cpuRd(m, 0xAF28 + ((r + m.rd(0x10)) & 0xFF)));   // BossE7BounceDirs
    stepMove16(m, x);
  }
  if (!(dec(m, TIMER2 + x) & 1)) return;                     // AE9E
  if (dec(m, ACT_TIMER + x)) return;
  inc(m, PHASE + x);
  m.wr(META + x, 0xD8);
  m.wr(ANIM + x, 0);
}

function e7Aim(m, x) {                                       // AEC9
  if (inc(m, ANIM + x) !== 0x0F) return;
  inc(m, PHASE + x);
  m.wr(ANIM + x, 0x28);
  dirToTarget(m, x, 1);
  m.wr(ACT_TIMER + x, (m.rd(0x20) << 1) & 0xFF);
  m.wr(META + x, 0xD9);
}

function e7Volley(m, x) {                                    // AEEC
  if (!m.rd(ANIM + x)) { m.wr(PHASE + x, 1); return; }
  const a = dec(m, ANIM + x);
  if ((m.rd(0x6C) === 0xB9 ? 3 : 7) & a) return;
  const d = (m.rd(ACT_TIMER + x) - 2) & 0x0F;               // the shifted masked value is always 0: aim - 2
  m.wr(0x1F, d);
  if (!spawnShot(m, x, 0x5C, d >> 1)) return;
  m.wr(META_DIR + m.rd(0x10), m.rd(0x1F));
}

export function aiBoss88(m, x) {
  if (m.rd(PHASE + x)) m.wr(CAM_LOCK, 4);
  hitFlashPal(m, x, 0xC0);
  return bossPhaseDispatch(m, x, 0x1B);
}

// ---- AI $E8 BossAiE8 15:AF48 ---------------------------------------------------------------------------------

function e8Intro(m, x) {                                     // AF5C
  if (m.rd(FLAGS + x) & 0x80) return;
  m.wr(CAM_LOCK, 0xFF);
  m.wr(0x07E7, 0x1F);
  m.wr(0x07F4, 0x74);
  m.wr(FREEZE, 0x10);
  clearEnemySlots(m, x);
  if (!m.rd(0x07D5)) return;
  introMusic(m);
  inc(m, PHASE + x);
  introMessage(m, 0x18, 0x08);
  m.wr(FLAGS2 + x, 0x0D);
  m.wr(COLL + x, 0x30);
  m.wr(FLAGS + x, 0x2B);
  m.wr(OVERLAY + x, 0x4C);
}

function e8Route(m, x) {                                     // AFAD
  m.wr(OVERLAY + x, 0x4C);
  m.wr(META + x, 0xDB);
  m.wr(META_DIR + x, (m.rd(0x0301) << 2) & 6);
  let y = (m.rd(TIMER2 + x) << 1) & 0xFF;
  let a = cpuRd(m, 0xB02B + y);                              // BossE8Route
  if (a === 0xFF) {
    inc(m, PHASE + x);
    m.wr(ACT_TIMER + x, 0x50);
    m.wr(AUX + x, rand64(m));
    return;
  }
  if (a === 0xFE) {
    m.wr(ANIM + x, 0); m.wr(TIMER2 + x, 0);
    y = 0; a = cpuRd(m, 0xB02B);
  }
  const b = cpuRd(m, 0xB02C + y);
  m.wr(0x34, (a << 4) & 0xFF); m.wr(0x35, 0);
  m.wr(0x36, (b << 4) & 0xFF); m.wr(0x37, b >> 4);
  m.wr(0x1F, x);
  tempToObjPos(m, 0x1F);                                     // the waypoint as a dummy object in slot $1F
  homingMove(m, x);
  dirToTarget(m, x, 0x1F);
  if (dec(m, ANIM + x) && m.rd(0x21)) return;
  inc(m, TIMER2 + x);
  m.wr(ANIM + x, 0);
}

/** 15:B0A5 */
function e8AttackPose(m, x) {
  m.wr(OVERLAY + x, 0xF1);
  m.wr(META + x, 0xDE);
  dec(m, ANIM + x); dec(m, ANIM + x);
}

/** 15:B077: past the FF, dir 8, phase 1 */
function e8LegEnd(m, x) {
  inc(m, TIMER2 + x);
  m.wr(PHASE + x, 1);
  m.wr(META_DIR + x, 8);
  m.wr(ANIM + x, 0);
}

function e8Attack(m, x) {                                    // B051
  e8AttackPose(m, x);
  const t = dec(m, ACT_TIMER + x);
  if (!t) { e8LegEnd(m, x); return; }
  if (m.rd(AUX + x) < 2) {                                   // B08A: one aimed shot at act_timer $20
    if (t !== 0x20) return;
    dirToTarget(m, x, 1);
    spawnShot(m, x, 0x46, m.rd(0x20));
    return;
  }
  if (t & 0x0F) return;
  spawnShot(m, x, 0x33, ((t >> 2) + 2) & 0x0F);
}

export function bossAiE8(m, x) {
  if (m.rd(PHASE + x)) m.wr(CAM_LOCK, 4);
  hitFlashPal(m, x, 0xC0);
  return bossPhaseDispatch(m, x, 0x21);
}

// ---- AI $E9 BossAiE9 15:B0B6 (statues, session 156) ----------------------------------------------------------

function e9Intro(m, x) {                                     // B0C5
  if (m.rd(FLAGS + x) & 0x80) return;
  m.wr(CAM_LOCK, 0xFF);
  m.wr(FREEZE, 0x10);
  if (!m.rd(0x07D5)) return;
  m.wr(FREEZE, 0);
  inc(m, PHASE + x);
  if (x === 0x0D) introMessage(m, 0x1B, 0x03);
}

/** B105 (also phase 2): obj_anim -= 4; at $20 one shot in DirToTarget*2 + 15:B12C[Rand64 & 3] */
function e9Fire(m, x) {
  const a = (m.rd(ANIM + x) - 4) & 0xFF;
  m.wr(ANIM + x, a);
  if (a !== 0x20) return;
  dirToTarget(m, x, 1);
  m.wr(0x20, (m.rd(0x20) << 1) & 0xFF);
  const y = (cpuRd(m, 0xB12C + (rand64(m) & 3)) + m.rd(0x20)) & 0xFF;
  return spawnShot(m, x, m.rd(SHOT_ID + x), y);
}

function e9Watch(m, x) {                                     // B0F4
  if (m.rd(ANIM + x)) return e9Fire(m, x);
  if ((m.rd(X_LO + x) ^ m.rd(0x71)) & 0x80) return;          // EOR $71 (slot 1's x lo)
  m.wr(ANIM + x, 0x24);
}

export function bossAiE9(m, x) {
  if (m.rd(PHASE + x)) m.wr(CAM_LOCK, 0x7F);
  return bossPhaseDispatch(m, x, 0x24);
}

// ---- AI $EA BossAiEA 15:B130 (session 156) -------------------------------------------------------------------

/** 15:B203: obj_timer--, then BossE7Step's move (98A8 / 8480 / 9861 / 9897) */
function eaMove(m, x) {
  dec(m, TIMER + x);
  stepMove16(m, x);
}

/** 15:B278 fire: wind-up above $20, then every 8 frames a 4-way fan (dir, dy) from 15:B2D2 */
function eaFire(m, x) {
  if (dec(m, TIMER2 + x) >= 0x20) {
    m.wr(META + x, 0xE0); m.wr(OVERLAY + x, 0xE1);
    dec(m, ANIM + x);
    return;
  }
  m.wr(META + x, 0xE2); m.wr(OVERLAY + x, 0xF2);
  m.wr(ANIM + x, (m.rd(ANIM + x) - 4) & 0xFF);
  if (m.rd(0x08) & 7) return;
  m.wr(0x1E, 3);
  for (;;) {
    const y = (m.rd(0x1E) << 1) & 0xFF;
    m.wr(0x1F, cpuRd(m, 0xB2D3 + y));
    if (!spawnShot(m, x, m.rd(SHOT_ID + x), cpuRd(m, 0xB2D2 + y))) return;
    const n = m.rd(0x10);
    m.wr(Y_LO + n, (m.rd(Y_LO + n) + m.rd(0x1F)) & 0xFF);
    if (dec(m, 0x1E) & 0x80) return;
  }
}

function eaIntro(m, x) {                                     // B14F
  if (m.rd(FLAGS + x) & 0x80) return;
  m.wr(FREEZE, 0x10);
  m.wr(0x07F4, 0x79);
  m.wr(0x07E6, 0x41); m.wr(0x07E7, 0x40);
  m.wr(CAM_LOCK, 0xFF);
  if (!m.rd(0x07D5)) return;
  m.wr(FLAGS + x, 0x0B);
  m.wr(OVERLAY + x, 0x4C);
  m.wr(COLL + x, 0x30);
  m.wr(FREEZE, 0);
  playSound(m, 0x12, 0x4E);                                  // Y = BossPhaseDispatch's ($27 + 0) * 2
  m.wr(0x06C3, m.rd(0x6C) === 0xA6 ? 0x04 : 0x00);
  m.wr(0x06E3, 0x1B);
  m.wr(0x41, 0x10);
  inc(m, PHASE + x);
}

function eaPatrol(m, x) {                                    // B1A5
  if (m.rd(TIMER2 + x)) return eaFire(m, x);
  m.wr(META + x, 0xDB); m.wr(OVERLAY + x, 0x4C);
  m.wr(FLAGS2 + x, 4);
  const a = dec(m, ANIM + x);
  let face = true;
  if (a < 0xC0) {
    if (a & 0x1F) face = false;
    else if (rand64(m) & 1) m.wr(TIMER2 + x, 0x40);
  }
  if (face) {                                                // B1D5: x >= player x (16 bit) -> left
    const c = m.rd(X_LO + x) >= m.rd(X_LO) ? 1 : 0;
    const ge = m.rd(X_HI + x) - m.rd(X_HI) - (1 - c) >= 0;
    m.wr(META_DIR + x, ge ? 6 : 2);
  }
  const lo = (m.rd(X_LO) - m.rd(X_LO + x)) & 0xFF;          // B1E6
  m.wr(0x10, lo);
  const b = m.rd(X_LO) < m.rd(X_LO + x) ? 1 : 0;
  const hi = (m.rd(X_HI) - m.rd(X_HI + x) - b) & 0xFF;
  const d = hi & 0x80 ? ((lo ^ 0xFF) - 1) & 0xFF : lo;      // EOR #$FF / SEC / SBC #1 (= -dx - 2)
  if (d < 8) { inc(m, PHASE + x); return; }
  eaMove(m, x);
}

function eaDive(m, x) {                                      // B215
  if (m.rd(Y_LO + x) >= 0xD0) { inc(m, PHASE + x); return; }
  m.wr(FLAGS2 + x, 8);
  m.wr(META_DIR + x, 4);
  eaMove(m, x);
}

function eaRise(m, x) {                                      // B22C
  dec(m, ANIM + x);
  m.wr(META + x, 0xE0); m.wr(OVERLAY + x, 0xE1);
  if (m.rd(TIMER2 + x)) return eaFire(m, x);
  const yl = m.rd(Y_LO + x);
  if (yl < 0xA0) {
    if ((((yl - 0x20) & 0xFF) & 0x3F) === 0x20) m.wr(TIMER2 + x, 0x40);
    if (yl < 0x30) {
      m.wr(PHASE + x, 1);
      m.wr(META_DIR + x, rand64(m) & 1 ? 6 : 2);
      return;
    }
  }
  m.wr(FLAGS2 + x, 2);                                       // B26B
  m.wr(META_DIR + x, 0);
  eaMove(m, x);
}

export function bossAiEA(m, x) {
  hitFlashPal(m, x, 0xC0);
  if (m.rd(0x6C) === 0xA6) m.wr(HP + x, 0xFF);
  if (m.rd(PHASE + x)) m.wr(CAM_LOCK, 0x10);
  return bossPhaseDispatch(m, x, 0x27);
}

// ---- AI $6C-$6E AiReward6C 15:B728 (the boss reward drop, type $0F; obj_phase = BossAreaList index) -----------
// Waits until slots $0D-$1E (except itself) hold only AI 0/$78/$70/$7F/$30/$39, then (once: obj_timer2++) replays
// the area music and applies its 5-byte RewardRecs 15:B7C1 record (pal id 3, CHR R5, tile ofs, pending msg).
// Phase 3 (BossAreaList $FF) skips the obj_timer2/music step, so it re-applies every frame. Every path except the
// spawn_param b7 one stores boss_cam_lock $10.

const REWARD_IDLE = new Set([0x78, 0x70, 0x7F, 0x30, 0x39, 0x00]);

export function aiReward6C(m, x) {
  if (m.rd(0x0560 + x) & 0x80) {                              // B72B: becomes type $B3 (AI $70 phase 1)
    m.wr(0x10, x); m.wr(0x11, 0xB3);
    farObjInit(m);
    return;
  }
  if (m.rd(TIMER2 + x) === 0) {
    let busy = false;
    for (let y = 0x0D; y !== 0x1F; y++) {                     // B740: $10 = Y each pass
      m.wr(0x10, y);
      if (x === y) continue;
      if (!REWARD_IDLE.has(m.rd(ACTIVE + y))) { busy = true; break; }
    }
    if (!busy) {
      const ph = m.rd(PHASE + x);
      if (ph !== 3) {
        inc(m, TIMER2 + x);
        const mus = m.rd(0x0102);
        m.wr(0x0102, 0);
        playSound(m, mus, 0);                                 // B778
      }
      m.wr(COLL + x, 0x8D);
      m.wr(META + x, 0xAA);
      const r = 0xB7C1 + ((ph * 5) & 0xFF);
      if (cpuRd(m, r) !== 0) {
        m.wr(0x07E7, cpuRd(m, r));
        m.wr(0x07F5, cpuRd(m, r + 1));
        m.wr(0x0320 + x, cpuRd(m, r + 2));
        m.wr(0x06E3, cpuRd(m, r + 3));
        m.wr(0x06C3, cpuRd(m, r + 4));
        if (cpuRd(m, r + 3) | cpuRd(m, r + 4)) m.wr(0x41, 0x10);
        paletteBuildRom(m, 0);                                // B7B8 JSR 26:8C0E
      }
    }
  }
  m.wr(CAM_LOCK, 0x10);                                       // B7BB
}

// ---- BossPhaseDispatch 15:A400 -------------------------------------------------------------------------------

const BOSS_PHASES = {
  0xA48B: e0Intro, 0xA4C3: e0Wait, 0xA4CC: e0Smoke, 0xA4D8: e0Vanish, 0xA50B: e0Teleport, 0xA54A: e0Appear,
  0xA565: e0Hold, 0xA581: e0Linger,
  0xA971: e3Intro, 0xA9C6: e3Approach, 0xAA46: e3BackOff, 0xAAA7: e3Slam, 0xAAE6: e3Sweep,
  0xABE4: e5Rise, 0xABFC: e5Settle, 0xAC0F: e5Talk, 0xAC1A: e5Wait, 0xAC1E: e5Leave, 0xAC31: e5Sink,
  0xAC41: e5ThrowStart, 0xAC5B: e5Throw,
  0xAC8F: e6Intro, 0xACF5: e6Fight,
  0xADE8: e7Intro, 0xAE35: e7Pose, 0xAE43: e7Wait, 0xAE5A: e7Bounce, 0xAEC9: e7Aim, 0xAEEC: e7Volley,
  0xAF5C: e8Intro, 0xAFAD: e8Route, 0xB051: e8Attack,
  0xB0C5: e9Intro, 0xB0F4: e9Watch, 0xB105: e9Fire,
  0xB14F: eaIntro, 0xB1A5: eaPatrol, 0xB215: eaDive, 0xB22C: eaRise,
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

export const AI_BOSS = {
  0x60: bossAiE0, 0x61: bossHomingShot, 0x63: bossAiE3, 0x64: bossRockShot, 0x65: aiTalkEventE5,
  0x66: bossAiE6, 0x67: aiBoss88, 0x68: bossAiE8, 0x69: bossAiE9, 0x6A: bossAiEA,
  0x6C: aiReward6C, 0x6D: aiReward6C, 0x6E: aiReward6C,
};
