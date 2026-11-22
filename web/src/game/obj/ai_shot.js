// re-c web port — the boss shot / effect AIs of bank 27 (enemy port leftovers (b), s87). REVERSE.md "Boss shot /
// part AIs (session 86)". Ported instruction by instruction for obj_check.mjs; none pops its caller.
//
//   AI $14 AiCountdown 27:B2C5     AI $16 AiCurveShot 27:B0F6    AI $17 AiHoverShot 27:B11B
//   AI $1B AiBounceShot 27:B12D    AI $1E AiDirFrameShot 27:B094 AI $1F AiShotAnimDec 27:B0A2
// $16 $17 $1E $1F end in AiStraightMoverTail 27:B0BE (obj/ai_static.js).
import { objDespawn, objPosToTemp, objStepVector, tempAddStep, tempToObjPos, objBgProbe, spawnShot } from './util.js';
import { aiStraightMoverTail } from './ai_static.js';
import { cpuRd } from '../prg.js';

const META_ID = 0x0300, META_DIR = 0x0360, FLAGS = 0x0380, SHOT_ID = 0x0440, BG_MASK = 0x0460, TIMER = 0x0480;
const ACTIVE = 0x04A0, ANIM = 0x04E0, PHASE = 0x0600, TIMER2 = 0x0620;

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

/** AI $15 27:B27A (type $19): obj_timer--, 0 -> ObjDespawn; on even timers SpawnShot(obj_shot_id, dir);
 *  obj_anim--, then one step along obj_meta_dir with no BG test (98A8 / 8480 / 9861 / 9897) */
export function aiTrailShot(m, x) {
  dec(m, TIMER + x);
  if (m.rd(TIMER + x) === 0) return void objDespawn(m, x);   // B27F JMP 26:98C7
  if ((m.rd(TIMER + x) & 1) === 0) spawnShot(m, x, m.rd(SHOT_ID + x), m.rd(META_DIR + x));
  dec(m, ANIM + x);
  objPosToTemp(m, x);
  objStepVector(m, x, m.rd(META_DIR + x));
  tempAddStep(m);
  tempToObjPos(m, x);                                       // B2A1 JMP 26:9897
}

/** AI $1C 27:B2A4 (type $1A): on every other obj_timer2 step SpawnShot($17, dir = obj_phase), obj_phase++;
 *  the spawn at obj_phase 7 clears obj_active first (8 shots, dirs 0-7, then the spawner is gone) */
export function aiRingSpawner(m, x) {
  dec(m, TIMER2 + x);
  if (m.rd(TIMER2 + x) & 1) return;                         // B2AD RTS
  const p = m.rd(PHASE + x);
  if (p === 7) m.wr(ACTIVE + x, 0);
  m.wr(PHASE + x, p + 1);
  spawnShot(m, x, 0x17, p);                                 // B2C2 JMP 26:972D
}

/** AI $18 27:B2D5 (types $1C $1D): obj_timer2++, obj_meta_id |= dir >> 1, then AI $19 */
export function aiShotTurnMeta(m, x) {
  m.wr(TIMER2 + x, m.rd(TIMER2 + x) + 1);
  m.wr(META_ID + x, m.rd(META_ID + x) | (m.rd(META_DIR + x) >> 1));
  aiShotTurn(m, x);
}

/** AI $19 27:B2E2 (type $1E): dir = (dir * 2 + ROM[$B2F1 + X]) & $0F, obj_active = $10 (straight mover).
 *  The "table" at B2F1 is the code bytes from this routine's own STA (9D A0 04 60 00 FF 01 FE ...), indexed by slot. */
export function aiShotTurn(m, x) {
  m.wr(META_DIR + x, (((m.rd(META_DIR + x) << 1) & 0xFF) + cpuRd(m, 0xB2F1 + x)) & 0x0F);
  m.wr(ACTIVE + x, 0x10);
}

export const AI_SHOT = {
  0x14: aiCountdown, 0x15: aiTrailShot, 0x16: aiCurveShot, 0x17: aiHoverShot, 0x18: aiShotTurnMeta,
  0x19: aiShotTurn, 0x1A: () => {},                         // 27:B2F4 RTS (ObjUpdateAll takes $1A to 30:CD8A first)
  0x1B: aiBounceShot, 0x1C: aiRingSpawner, 0x1E: aiDirFrameShot, 0x1F: aiShotAnimDec,
};
