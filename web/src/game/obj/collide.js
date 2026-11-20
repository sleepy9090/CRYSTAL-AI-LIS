// re-c web port — object-vs-object collision (s113, "Web fix: gameplay frames" (e3)). REVERSE.md
// "Object-vs-object collision (session 30)", "Player damage — HitPlayer", "Hit handlers (s78)".
// Ported instruction by instruction (zero-page temps included) so RAM diffs against tools.emu stay byte-exact.
//
//   objCollideAll(m)      ObjCollideAll 26:8F6E (generator: the hit handlers reach HudRedraw / HudHpBar /
//                         StatRecalc, which spin on the PPU queue). Clears $0541/$0643, runs CollPairTable
//                         26:96F1 records 7..0 (even frames) or 14..8 (odd), each = ObjOverlapScan + the
//                         HitHandlerTable 26:8FBB handler on the first overlapping target. The loop counters live
//                         in $2E/$2F as in the ROM.
//   objOverlapScan(m, x)  ObjOverlapScan 26:95F8 -> target slot or -1 (C = 0); scans $10+1 .. $11-1
//   handlers              0 HitByPlayerShot 26:90AA, 2 HitPlayer 26:9357, 4 TalkProbeHit 26:94B0,
//                         6 HitHandler3 26:8FC4 (X = attacker slot, Y = target)
//
// PlaySound's Y argument (snd_saved_y $010F) is the target slot at every call except EnemyKilled's last one
// (HudRedraw(2) leaves Y = 2*6). HudRedraw's X argument only feeds $14.
import { cpuRd, farRestore, switchBank16 } from '../prg.js';
import { playSound } from '../playsound.js';
import { hudRedraw, hudHpBar, statRecalc, paletteBuildRom } from '../states/death.js';
import { farObjInit } from './objinit.js';
import { hitSetFlag } from './objects.js';
import { objMoveSlide } from './move.js';
import { rand64 } from './util.js';
import { spriteDraw } from './sprites.js';

const META = 0x0300, FLAGS2 = 0x0340, META_DIR = 0x0360, FLAGS = 0x0380, COLL = 0x03A0, HP = 0x03C0;
const ATTACK = 0x03E0, DEFENSE = 0x0400, HITFLAGS = 0x0420, ACTIVE = 0x04A0, DEATH = 0x04C0, ANIM = 0x04E0;
const ELEM = 0x0500, XP_VAL = 0x0520, SPAWN_B2 = 0x0540, SPAWN_PARAM = 0x0560, OVERLAY = 0x0580;
const FREEZE = 0x05A0, SCR_X = 0x05C0, SCR_Y = 0x05E0, PHASE = 0x0600, BEHAV = 0x06E0;
const FLASH = 0x0710, LEVEL = 0x0421, XP = 0x0704, XP_NEXT = 0x0706, SUB_STATE = 0x41;
const MSG_ID = 0x06C3, MSG_GROUP = 0x06E3, RIDE = 0x0541, PROBE_HIT = 0x0643, PROBE_NPC = 0x0623;

const rd = (m, a) => m.rd(a & 0xFFFF);
const box = (i) => 0x9691 + i;                          // HitboxTable {dx w dy h}

/** ObjOverlapScan 26:95F8: attacker x vs targets $10+1 .. $11-1 -> first overlapping target or -1 */
export function objOverlapScan(m, x) {
  m.wr(0x1B, x);
  if (m.rd(FLAGS + x) & 0x80 || m.rd(FLAGS2 + x) & 0x80) return -1;
  const grp = m.rd(COLL + x) & 0xF0;
  if (!grp) return -1;
  m.wr(0x1F, grp);
  m.wr(0x15, (m.rd(COLL + x) & 0x0F) << 2);
  const i = (m.rd(HITFLAGS + x) & 0x40) | m.rd(0x15);
  m.wr(0x15, (m.rd(SCR_Y + x) + cpuRd(m, box(i + 2))) & 0xFF);
  m.wr(0x16, (m.rd(0x15) + cpuRd(m, box(i + 3))) & 0xFF);
  m.wr(0x13, (m.rd(SCR_X + x) + cpuRd(m, box(i))) & 0xFF);
  m.wr(0x14, (m.rd(0x13) + cpuRd(m, box(i + 1))) & 0xFF);
  for (let y = (m.rd(0x10) + 1) & 0xFF; y !== m.rd(0x11); y = (y + 1) & 0xFF) {
    if (rd(m, FLAGS + y) & 0x80) continue;
    if (!(rd(m, COLL + y) & m.rd(0x1F))) continue;
    if (rd(m, FLAGS2 + y) & 0x80) continue;
    if (y === m.rd(0x1B)) continue;
    m.wr(0x10, (rd(m, COLL + y) & 0x0F) << 2);
    const j = (rd(m, HITFLAGS + y) & 0x40) | m.rd(0x10);
    let a = rd(m, SCR_X + y) + cpuRd(m, box(j));
    if ((a & 0xFF) >= m.rd(0x14)) continue;            // BCS (the ADC carry is dropped by CMP)
    a = ((a & 0xFF) + cpuRd(m, box(j + 1))) & 0xFF;      // C = 0 after the BCS not taken
    if (a < m.rd(0x13)) continue;
    a = (rd(m, SCR_Y + y) + cpuRd(m, box(j + 2))) & 0xFF;
    if (a >= m.rd(0x16)) continue;
    a = (a + cpuRd(m, box(j + 3))) & 0xFF;
    if (a < m.rd(0x15)) continue;
    return y;
  }
  return -1;
}

/** HitKnockbackSet 26:95C0: attacker $10, target $11 (saves X/Y in $12/$13) */
function hitKnockbackSet(m, x, y) {
  m.wr(0x12, x); m.wr(0x13, y);
  const ax = m.rd(0x10), ty = m.rd(0x11);
  const f = rd(m, FLAGS2 + ty);
  if (!(f & 0x80)) m.wr(FLAGS2 + ty, (f & 0x0F) | 0x80);
  let d = rd(m, META_DIR + ax);
  if (rd(m, FLAGS2 + ax) >= 0x0B) d >>= 1;
  m.wr(0x10, (d << 4) & 0xFF);
  m.wr(META_DIR + ty, (rd(m, META_DIR + ty) & 0x0F) | m.rd(0x10));
}

const isEnemyShot = (m, y) => m.rd(SPAWN_B2 + y) === 0xFF;   // IsEnemyShot 26:907C

/** HitShotEater 26:906C */
function hitShotEater(m, y) {
  if (!isEnemyShot(m, y)) return;
  m.wr(ACTIVE + y, 0);
  playSound(m, 0x3A, y);
}

/** HitHandler3 26:8FC4 (attacker slot $0C) */
function* hitHandler3(m, x, y) {
  if (m.rd(PHASE + x) === 1) { hitShotEater(m, y); return; }
  if (isEnemyShot(m, y)) return;
  if (m.rd(ACTIVE + y) === 0x78) return;
  m.wr(ACTIVE + x, 0);
  if (m.rd(HITFLAGS + y) & 0x20) return;
  m.wr(0x13, m.rd(SPAWN_PARAM + y));
  m.wr(0x12, 0xFF);
  m.wr(FREEZE + y, 0xFF);
  m.wr(0x10, y);
  if (m.rd(SPAWN_B2 + y) !== 1) return;
  for (let i = 0x13; i >= 0; i--) {
    if (cpuRd(m, 0x9058 + i) === m.rd(0x13)) { m.wr(FREEZE + m.rd(0x10), 0); return; }   // HitImmuneParams
  }
  hitSetFlag(m);
}

/** ThunderTransform 26:9229: true = ObjInit(target, obj_behav) and PLA PLA (the hit handler ends) */
function thunderTransform(m, y) {
  if (m.rd(0x0711) !== 4 || m.rd(SPAWN_B2 + y) !== 0) return false;
  const p = m.rd(SPAWN_PARAM + y);
  if (p !== 0 && p !== 3) return false;
  m.wr(0x11, m.rd(BEHAV + y));
  m.wr(0x10, y);
  farObjInit(m);
  return true;
}

/** HitByPlayerShot 26:90AA (HitHandlerTable 0: player shot / sword slot x hits y) */
function* hitByPlayerShot(m, x, y) {
  if (m.rd(0x6C) === 0x5F) { m.wr(0x04A4, 0); m.wr(0x04A5, 0); m.wr(0x04A6, 0); }
  if (thunderTransform(m, y)) return;
  const act = m.rd(ACTIVE + y);
  if (act === 0x52) {                                    // 90C5: the sparring duel partner
    const v = (m.rd(0x066D) - 1) & 0xFF;
    m.wr(0x066D, v);
    if (!(v & 3)) {
      m.wr(0xB0, (m.rd(0xB0) - 8) & 0xFF);
      m.wr(0xB0 + y, (m.rd(0xB0 + y) - 8) & 0xFF);
    }
    playSound(m, 0x21, y);
    return;
  }
  if (act === 0x10) return;
  if (act === 0x87) {                                    // HitElemWall 26:9094
    if ((m.rd(ELEM + y) & m.rd(ELEM + x)) & 0x0F) return;
    if (m.rd(HITFLAGS + x) === 1) return;
    yield* enemyKilled(m, x, y);
    return;
  }
  if (m.rd(META + y) === 0xA7) {                         // HitRevealA7 26:9082
    if (m.rd(HITFLAGS + x) !== 3) return;
    m.wr(0x11, m.rd(DEATH + y));
    m.wr(0x10, y);
    farObjInit(m);
    return;
  }
  m.wr(0x10, m.rd(HITFLAGS + y) & 0x1F);
  let miss = m.rd(LEVEL) < m.rd(0x10);
  if (!miss) {
    m.wr(0x10, (m.rd(0x03E1) + m.rd(ATTACK + x)) & 0xFF);
    if ((m.rd(ELEM + y) & m.rd(ELEM + x)) & 0x0F) miss = true;
    else {
      const d = m.rd(0x10) - m.rd(DEFENSE + y);
      m.wr(0x10, d & 0xFF);
      miss = d < 0;
    }
  }
  if (miss) {                                            // 9125
    m.wr(ACTIVE + x, 0);
    m.wr(0x10, x); m.wr(0x11, y);
    if (!(m.rd(ACTIVE + y) & 0x80)) hitKnockbackSet(m, x, y);
    playSound(m, 0x3A, y);
    return;
  }
  const hp = m.rd(HP + y) - m.rd(0x10);                  // 913B
  m.wr(HP + y, hp & 0xFF);
  if (hp < 0) { yield* enemyKilled(m, x, y); return; }
  m.wr(0x10, x); m.wr(0x11, y);
  hitKnockbackSet(m, x, y);
  playSound(m, 0x21, y);
}

/** EnemyKilled 26:9152 (X = attacker, Y = the killed object) */
function* enemyKilled(m, x, y) {
  addXp(m, y);
  if (m.rd(DEATH + y) === 0) enemyDeathFx(m, y);
  else {
    m.wr(0x10, y);
    m.wr(0x11, m.rd(DEATH + y));
    farObjInit(m);
  }
  const xp = m.rd(XP) | m.rd(XP + 1) << 8, next = m.rd(XP_NEXT) | m.rd(XP_NEXT + 1) << 8;
  if (xp >= next) {
    m.wr(XP, 0); m.wr(XP + 1, 0);
    if (!(m.rd(LEVEL) & 0xF0)) {                         // 9188 level up
      const lv = (m.rd(LEVEL) + 1) & 0xFF;
      m.wr(LEVEL, lv);
      m.wr(0x03C0, cpuRd(m, 0x8B7F + lv));
      m.wr(0x0709, cpuRd(m, 0x8B8F + lv));
      m.wr(0x03C1, (cpuRd(m, 0x8B7F + lv) - cpuRd(m, 0x8B7E + lv) + m.rd(0x03C1)) & 0xFF);
      m.wr(0x0708, (cpuRd(m, 0x8B8F + lv) - cpuRd(m, 0x8B8E + lv) + m.rd(0x0708)) & 0xFF);
      const yy = (lv << 1) & 0xFF;
      m.wr(XP_NEXT, cpuRd(m, 0x8B9E + yy));
      m.wr(XP_NEXT + 1, cpuRd(m, 0x8B9F + yy));
      yield* hudHpBar(m);
      for (const e of [0, 3, 4, 5]) yield* hudRedraw(m, e, x);
      m.wr(SUB_STATE, 0x10);
      m.wr(MSG_ID, 0x0D);
      m.wr(MSG_GROUP, 0x20);
      yield* statRecalc(m, x);
    }
  }
  const ry = yield* hudRedraw(m, 2, x);                  // 91EF
  playSound(m, 0x21, ry);                                // Y = HudRedraw's $18 (2*6) or the flush's Y
}

/** EnemyDeathFx 26:91FA: the object becomes the AI $7D death puff */
function enemyDeathFx(m, y) {
  m.wr(OVERLAY + y, 0xAB);
  m.wr(FLAGS + y, (m.rd(FLAGS + y) | 0x02) & 0xEF);
  m.wr(FLAGS2 + y, 0x08);
  m.wr(ACTIVE + y, 0x7D);
  m.wr(ANIM + y, 0x1F);
  m.wr(PHASE + y, 0); m.wr(COLL + y, 0); m.wr(FREEZE + y, 0);
  m.wr(DEATH + y, 0x0B);
}

/** AddXp 26:924B */
function addXp(m, y) {
  const v = m.rd(XP_VAL + y);
  let add;
  if (!(v & 0x80)) add = v;
  else {
    m.wr(0x11, (v >> 4) & 7);
    add = ((v & 0x7F) << 4);
  }
  const s = (m.rd(XP) | m.rd(XP + 1) << 8) + add;
  m.wr(XP, s & 0xFF);
  m.wr(XP + 1, (s >> 8) & 0xFF);
}

/** PlayerInvuln 26:9431 */
function playerInvuln(m) {
  m.wr(0x071A, 0x1E);
  m.wr(0x0381, m.rd(0x0381) | 0x04);
}

/** PlayerPoisonRoll 26:927E */
function playerPoisonRoll(m, x, y) {
  if (m.rd(FLAGS + x) & 0x04) return;
  if (m.rd(0x0713) === 7) return;
  if (!(m.rd(HITFLAGS + y) & 0x80)) return;
  m.wr(0x10, y);
  if (rand64(m) >= 4) return;
  m.wr(FLASH, (m.rd(FLASH) & 0xF0) | 0x03);
  m.wr(SUB_STATE, 0x10);
  m.wr(MSG_ID, 0x19);
  m.wr(MSG_GROUP, 0x20);
  playerInvuln(m);
  playSound(m, 0x36, y);
}

/** HitPlayerShot 26:92BF; returns true to go on with HitPlayer at 9361 (param 0) */
function* hitPlayerShot(m, x, y) {
  const p = m.rd(SPAWN_PARAM + y);
  if (p === 0) return true;
  let despawn = () => m.wr(ACTIVE + y, 0);             // 92F5
  if (p === 1) {
    const sh = m.rd(0x0714);
    if (sh !== 6 && sh !== 8) {
      playSound(m, 0x44, y);
      m.wr(SUB_STATE, 0x10);
      m.wr(MSG_ID, 0x1A);
      m.wr(MSG_GROUP, 0x20);
      m.wr(FLASH, (m.rd(FLASH) & 0xF0) | 0x01);
    }
    despawn();
    return false;
  }
  if (p === 2) {
    const sh = m.rd(0x0714);
    if ((m.rd(FLASH) & 0x0F) !== 2 && sh !== 4 && sh !== 8) {
      m.wr(SUB_STATE, 0x10);
      m.wr(MSG_ID, 0x1B);
      m.wr(MSG_GROUP, 0x20);
      playSound(m, 0x45, y);
      m.wr(0x05A0, 0xFF);
      m.wr(0x07E4, 0x4D);
      paletteBuildRom(m, 0);                             // 26:8C0E
      // ROM quirk: PaletteBuild leaves Y = 4, so 92F5 clears slot 4's obj_active, not the stone shot's
      despawn = () => m.wr(ACTIVE + 4, 0);
      m.wr(FLASH, (m.rd(FLASH) & 0xF0) | 0x02);
    }
    despawn();
    return false;
  }
  if (p === 3) {
    m.wr(0x10, y);
    m.wr(0x11, 0x0B);
    farObjInit(m);
    return false;
  }
  despawn();
  m.wr(FLASH, 4);
  yield* statRecalc(m, x);
  return false;
}

/** HitPlayer 26:9357 (HitHandlerTable 2: X = 1 the player, Y = the touching object) */
function* hitPlayer(m, x, y) {
  if (isEnemyShot(m, y) && !(yield* hitPlayerShot(m, x, y))) return;
  playerPoisonRoll(m, x, y);                             // 9361
  if (m.rd(ACTIVE) === 0x0B) {                           // the sparring duel
    const v = (m.rd(0x0661) - 1) & 0xFF;
    m.wr(0x0661, v);
    if (!(v & 3)) {
      m.wr(0xB0, (m.rd(0xB0) + 8) & 0xFF);
      m.wr(0xBD, (m.rd(0xBD) + 8) & 0xFF);
    }
    playSound(m, 0x5F, y);
    return;
  }
  const id = m.rd(ACTIVE + y) & 0x7F;
  if (id === 0x06 || id === 0x07) return;
  if (id === 0x7B) { yield* pickupGold(m, y); return; }
  if (id === 0x38 || id === 0x3C) { ridePlatform(m, y); return; }
  if (m.rd(0x0381) & 0x04) return;                       // invulnerable
  m.wr(0x11, x); m.wr(0x10, y);
  hitKnockbackSet(m, x, y);
  if (m.rd(0x6C) === 0x1A && (m.rd(0x07D7) & 0x80)) m.wr(META_DIR + x, m.rd(META_DIR + x) & 0x0F);
  m.wr(META_DIR, m.rd(META_DIR + x));
  m.wr(FLAGS2, m.rd(FLAGS2 + x));
  m.wr(0x0640, 0xFF);
  m.wr(0x10, m.rd(DEFENSE + x));
  if (isEnemyShot(m, y)) m.wr(0x10, m.rd(DEFENSE));
  const dmg = (((m.rd(0x10) ^ 0xFF) + 1) + m.rd(ATTACK + y)) & 0xFF;
  if (dmg & 0x80) playSound(m, 0x3A, y);
  else {
    m.wr(0x10, dmg);
    if (m.rd(LEVEL) !== 0x11) {
      const hp = m.rd(HP + x) - dmg;
      m.wr(HP + x, hp < 0 ? 0 : hp);
      playSound(m, 0x5F, y);
      playerInvuln(m);
    }
  }
  if (isEnemyShot(m, y)) m.wr(ACTIVE + y, 0);            // 9422
  yield* hudHpBar(m);
}

/** RidePlatform 26:943F */
function ridePlatform(m, y) {
  if (m.rd(0x0620) !== 0) return;
  m.wr(RIDE, y);
  if (m.rd(FREEZE + y) !== 0) return;
  const d = m.rd(META_DIR), f = m.rd(FLAGS2);
  m.wr(FLAGS2, 2);
  m.wr(META_DIR, m.rd(META_DIR + y));
  objMoveSlide(m, 0);
  m.wr(FLAGS2, f);
  m.wr(META_DIR, d);
}

/** PickupGold 26:946D */
function* pickupGold(m, y) {
  const x = (m.rd(ELEM + y) >> 4) << 1;
  const g = (m.rd(0x0702) | m.rd(0x0703) << 8) + (cpuRd(m, 0x8BDE + x) | cpuRd(m, 0x8BDF + x) << 8);
  m.wr(0x0702, g > 0xFFFF ? 0xFF : g & 0xFF);
  m.wr(0x0703, g > 0xFFFF ? 0xFF : g >> 8);
  m.wr(ACTIVE + y, 0);
  playSound(m, 0x24, y);
  yield* hudRedraw(m, 1, x);
}

/** TalkProbeHit 26:94B0 (HitHandlerTable 4: the slot 3 probe touched y) */
function* talkProbeHit(m, x0, y) {
  m.wr(PROBE_HIT, y);
  const id = m.rd(ACTIVE + y) & 0x7F;
  if (id === 0x78 || id === 0x6E) {                      // TalkProbeItemUse 26:94A2
    if (m.rd(SUB_STATE) === 8) { m.wr(PROBE_NPC, y); m.wr(SUB_STATE, 7); }
    return;
  }
  const x = y;
  if (((m.rd(ACTIVE + x) << 1) & 0xFF) !== 0x60) return; // AI $30 NPC
  if (m.rd(0x49) & 0x80) return;                         // pad_dir: no direction held
  if (m.rd(BEHAV + x) & 0x04) {
    npcPushBack(m, x);
    talkStart(m, x);
    return;
  }
  if (m.rd(0x0620) !== 0) return;
  if (m.rd(0x0603) !== 0 && x === m.rd(PROBE_NPC)) return;
  const c = m.rd(COLL + x);
  m.wr(COLL + x, c | 0x10);
  m.wr(0x10, 0); m.wr(0x11, 2);
  const hit = objOverlapScan(m, x) >= 0;                 // the NPC against the player body (slot 1)
  m.wr(COLL + x, c);
  if (hit) return;
  talkStart(m, x);
}

/** TalkStart 26:9508 */
function talkStart(m, x) {
  m.wr(0x0603, 0x10);
  m.wr(PROBE_NPC, x);
  m.wr(SUB_STATE, 0x11);
  if (m.rd(FREEZE + x) !== 0) return;
  if (!(m.rd(BEHAV + x) & 0x02)) return;
  m.wr(0x10, (m.rd(0x0301) + 2) & 3);
  m.wr(META + x, (m.rd(META + x) & 0xFC) | m.rd(0x10));
}

/** NpcPushBack 26:9535: up to 4 passes; while the player body touches the solid NPC, slots 3..0 slide one step
 *  away from the player's facing, then a sprite pass (FarCall_B14_800E) */
function npcPushBack(m, x) {
  m.wr(0x28, 4);
  do {
    const c = m.rd(COLL + x);
    m.wr(COLL + x, c | 0xF0);
    m.wr(0x10, (x - 1) & 0xFF);
    m.wr(0x11, (x + 1) & 0xFF);
    const y = objOverlapScan(m, 1);
    m.wr(COLL + x, c);
    if (y < 0) {                                         // 9558
      m.wr(0x0603, 0);
      if (m.rd(0x0620) !== 0) m.wr(0x0640, 0xFF);
      return;
    }
    if (m.rd(SPAWN_B2 + y) === 1 && m.rd(SPAWN_PARAM + y) === 0x3A && !(m.rd(0x0301) & 3)) {
      m.wr(0x0301, m.rd(0x0301) & 0xFC);
    }
    for (let s = 3; s >= 0; s--) {
      const f = m.rd(FLAGS2 + s), d = m.rd(META_DIR + s);
      m.wr(META_DIR + s, (((m.rd(0x0301) & 3) << 1) + 4) & 6);
      objMoveSlide(m, s);
      m.wr(META_DIR + s, d);
      m.wr(FLAGS2 + s, f);
    }
    const r6 = m.rd(0x6E), r7 = m.rd(0x6F);              // FarCall_B14_800E
    switchBank16(m, 0x0E);
    spriteDraw(m);
    farRestore(m, r6, r7);
    m.wr(0x28, (m.rd(0x28) - 1) & 0xFF);
  } while (m.rd(0x28) !== 0);
}

const HANDLERS = [hitByPlayerShot, hitPlayer, talkProbeHit, hitHandler3];
/** the HitHandlerTable entries by name (web/test/obj_check.mjs) */
export const HIT_HANDLERS = { hitByPlayerShot, hitPlayer, talkProbeHit, hitHandler3 };

/** ObjCollideAll 26:8F6E */
export function* objCollideAll(m) {
  m.wr(RIDE, 0);
  m.wr(PROBE_HIT, 0);
  const odd = m.rd(0x08) & 1;
  m.wr(0x2F, odd ? 0x0E : 0x07);
  m.wr(0x2E, odd ? 0x07 : 0x08);
  do {
    const r = (m.rd(0x2F) << 2) & 0xFF;
    const x = cpuRd(m, 0x96F1 + r);
    m.wr(0x10, cpuRd(m, 0x96F2 + r));
    m.wr(0x11, cpuRd(m, 0x96F3 + r));
    const h = cpuRd(m, 0x96F4 + r);
    m.wr(0x20, 0x4C);
    m.wr(0x21, cpuRd(m, 0x8FBB + h));
    m.wr(0x22, cpuRd(m, 0x8FBC + h));
    const y = objOverlapScan(m, x);
    if (y >= 0) {
      const fn = HANDLERS[h >> 1];
      if (h & 1 || !fn) throw new Error(`ObjCollideAll: handler index ${h} not in HitHandlerTable`);
      yield* fn(m, x, y);
    }
    m.wr(0x2F, (m.rd(0x2F) - 1) & 0xFF);
    m.wr(0x2E, (m.rd(0x2E) - 1) & 0xFF);
  } while (m.rd(0x2E) !== 0);
}
