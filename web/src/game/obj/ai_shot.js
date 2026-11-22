// re-c web port — the boss shot / effect AIs (and since s167 other small missing AIs) of bank 27 (enemy port leftovers (b), s87). REVERSE.md "Boss shot /
// part AIs (session 86)". Ported instruction by instruction for obj_check.mjs; none pops its caller.
//
//   AI $14 AiCountdown 27:B2C5     AI $16 AiCurveShot 27:B0F6    AI $17 AiHoverShot 27:B11B
//   AI $1B AiBounceShot 27:B12D    AI $1E AiDirFrameShot 27:B094 AI $1F AiShotAnimDec 27:B0A2
// s166: $15 $18 $19 $1A $1C (27:B27A-B2F4). s167: $0C $0D $0E $0F (27:A5C0-A615), $1D $2F $36 (27:B02C-B093).
// $16 $17 $1E $1F $36 end in AiStraightMoverTail 27:B0BE (obj/ai_static.js).
// s168: $0A $33 $51 $52 $53 $54 $55 (27:A4C6-A85E); $77 = AiMpDrain99 27:A4E0 (obj/ai_effect.js).
import {
  objDespawn, objPosToTemp, objStepVector, tempAddStep, tempAddOffset, tempToObjPos, objBgProbe, spawnShot, rand64,
  dirToTarget,
} from './util.js';
import { aiStraightMoverTail } from './ai_static.js';
import { farObjInit } from './objinit.js';
import { playSound } from '../playsound.js';
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

// ---- s167: small AIs from ai_missing.py ($0C-$0F shop / message, $1D $2F $36 shots) ----

/** AI $0E 27:A5C0 (type $4E): sub_state = $24 */
export function aiShopEmpty(m) { m.wr(0x41, 0x24); }

/** AI $0F 27:A5C5 (shop types $40-$48): sub_state = obj_timer2 */
export function aiSetSubstate(m, x) { m.wr(0x41, m.rd(TIMER2 + x)); }

/** AI $0C 27:A5CB: $0561 | $0641 = 0 -> obj_active++ (AI $0D); else obj_timer2 = $10, meta $4C and the
 *  object sits on the player (98A8 with X = 0, 9897 with X = the slot, $10 = slot) */
export function aiFollowPlayerWait(m, x) {
  if ((m.rd(0x0561) | m.rd(0x0641)) === 0) return void m.wr(ACTIVE + x, m.rd(ACTIVE + x) + 1);
  m.wr(TIMER2 + x, 0x10);
  m.wr(META_ID + x, 0x4C);
  m.wr(0x10, x);
  objPosToTemp(m, 0);
  tempToObjPos(m, x);                                       // A5EA JMP 26:9897
}

/** AI $0D 27:A5ED: meta $8A; obj_timer2 counts down, at 0: obj_active = $30, pending message 12.4
 *  (12.5 in area $68), sub_state = $10 */
export function aiMessageAfterWait(m, x) {
  m.wr(META_ID + x, 0x8A);
  if (m.rd(TIMER2 + x) !== 0) return void dec(m, TIMER2 + x);
  m.wr(ACTIVE + x, 0x30);
  m.wr(0x06C3, m.rd(0x6C) === 0x68 ? 5 : 4);                // pending_msg_id
  m.wr(0x06E3, 0x0C);                                       // pending_msg_group
  m.wr(0x41, 0x10);
}

/** AI $1D 27:B02C (type $BE): obj_anim--, 0 -> obj_active = 0; on odd obj_anim SpawnShot(obj_shot_id, dir) */
export function aiShotEmitter(m, x) {
  dec(m, ANIM + x);
  if (m.rd(ANIM + x) === 0) return void m.wr(ACTIVE + x, 0);
  if (m.rd(ANIM + x) & 1) spawnShot(m, x, m.rd(SHOT_ID + x), m.rd(META_DIR + x));   // B044 JMP 26:972D
}

/** AI $2F 27:B048 (type $B9): falls with dy = obj_timer2 (0, 1, .. capped at 8), dx 0; y_lo >= $C8 -> obj_active = 0 */
export function aiFallingObj(m, x) {
  objPosToTemp(m, x);
  m.wr(0x30, 0);
  const t = m.rd(TIMER2 + x);
  m.wr(0x31, t);
  m.wr(TIMER2 + x, t === 8 ? 8 : t + 1);
  tempAddStep(m);
  tempToObjPos(m, x);
  if (m.rd((0xB0 + x) & 0xFF) >= 0xC8) m.wr(ACTIVE + x, 0);
}

/** AI $36 27:B070 (type $33): dir = 0; first frame (obj_timer2 0): timer2++, SpawnShot($14, 0) and the new
 *  object ($10) 24 px lower (LDA $00B0,Y absolute); then AiStraightMoverTail */
export function aiDropSpawner(m, x) {
  m.wr(META_DIR + x, 0);
  if (m.rd(TIMER2 + x) === 0) {
    m.wr(TIMER2 + x, 1);
    if (spawnShot(m, x, 0x14, 0)) {
      const y = m.rd(0x10);
      m.wr(0xB0 + y, m.rd(0xB0 + y) + 0x18);
    }
  }
  aiStraightMoverTail(m, x);
}

// ---- s168: AIs $0A $33 $51-$55 (27:A4C6, A616-A85E) and $77 (AiMpDrain's second entry) ----

/** AI $33 27:A4C6 (type $31): obj_phase++; at $40 obj_timer2++ and obj_flags b2 cleared */
export function aiPhaseUnhide(m, x) {
  m.wr(PHASE + x, m.rd(PHASE + x) + 1);
  if (m.rd(PHASE + x) !== 0x40) return;
  m.wr(TIMER2 + x, m.rd(TIMER2 + x) + 1);
  m.wr(FLAGS + x, m.rd(FLAGS + x) & 0xFB);
}

/** AI $55 27:A616 (type $3A): only in area $51 or area $60 entry 0. Walks 1 px left (16-bit x), obj_anim--, and
 *  carries slots 0 and 1 along (98A8 this slot, 9897 X = 0 and 1); clears $0600 $0620 $06C0 (slot 0);
 *  at x = $0680: player y_lo = $88, obj_active = $FF */
export function aiCarryLeft(m, x) {
  const a = m.rd(0x6C);
  if (!(a === 0x51 || (a === 0x60 && m.rd(0x6D) === 0))) return;
  if (m.rd(0x70 + x) === 0) m.wr(0x90 + x, m.rd(0x90 + x) - 1);
  m.wr(0x70 + x, m.rd(0x70 + x) - 1);
  dec(m, ANIM + x);
  m.wr(0x10, x);
  objPosToTemp(m, x);
  tempToObjPos(m, 0);
  tempToObjPos(m, 1);
  m.wr(0x0600, 0); m.wr(0x0620, 0); m.wr(0x06C0, 0);
  if (m.rd(0x70 + x) !== 0x80 || m.rd(0x90 + x) !== 0x06) return;
  m.wr(0xB0, 0x88);
  m.wr(ACTIVE + x, 0xFF);
}

/** 27:A6FB: $10 = story_flags[$06C0,X]; false (the ROM's PLA PLA: the AI returns) when
 *  $10 & BitMask[obj_behav,X] = 0 */
function flagGate(m, x) {
  m.wr(0x10, m.rd(0x6480 + m.rd(0x06C0 + x)));
  return (m.rd(0x10) & cpuRd(m, 0xC000 + m.rd(0x06E0 + x))) !== 0;
}

/** AI $51 27:A6C5 (types $B0 $B1 $D8 $D9): story flag gate; obj_anim--; on screen: area 3 with $62F1 = 0 and
 *  slot $0D not AI 8 -> slot $0D = ObjInit(its death type $04CD); every 32 obj_anim steps PlaySound($37) */
export function aiFlagGatedSpawn(m, x) {
  if (!flagGate(m, x)) return;
  dec(m, ANIM + x);
  if (m.rd(FLAGS + x) & 0x80) return;
  if (m.rd(0x62F1) === 0 && m.rd(0x6C) === 0x03 && m.rd(0x04AD) !== 0x08) {
    m.wr(0x11, m.rd(0x04CD));
    m.wr(0x10, 0x0D);
    farObjInit(m);                                          // ObjInit keeps X / Y (stack)
  }
  if ((m.rd(ANIM + x) & 0x1F) === 0) playSound(m, 0x37, m.rd(0x06E0 + x));   // Y = obj_behav from 27:A703
}

/** AI $0A AiDuelStart 27:A70E (slot 0) */
export function aiDuelStart(m, x) {
  if (m.rd(0x0660 + x) === 0) {                             // obj_aux
    m.wr(0x0660 + x, m.rd(0x0660 + x) + 1);
    m.wr(0x06C3, 3); m.wr(0x06E3, 3);                       // pending message 3.3
    m.wr(0x41, 0x10);
    return;
  }
  dec(m, 0xB0); dec(m, 0xB1); dec(m, 0x04E1);
  m.wr(0x0301, 0);
  if (m.rd(0xB0) !== 0x86) return;
  m.wr(0x03AD, 0x60);                                       // obj_coll slot $0D
  m.wr(0x04AD, 0x52);
  m.wr(0x04A0, 0x0B);
  m.wr(0xB0, 0x86);
  m.wr(0x07F5, 0x4E);                                       // chr_r5_play
  m.wr(0x657F, m.rd(0x657F) + 1);                           // story_flags_F7F8
}

/** AI $52 AiDuelOpponent 27:A753 (slot $0D): swing (timer2 counts down by 2, meta $56, shot $5B dir 4 at anim
 *  $0E) or follow the player's x 1 px a frame (meta $55, x skips $27 / $98); every 8th frame a Rand64 roll
 *  >= ((F7F8 * 2 + 6) & 7) | 1 within 8 px of the player starts a swing (timer2 = $1E) */
export function aiDuelOpponent(m, x) {
  const t2 = m.rd(TIMER2 + x);
  if (t2 !== 0) {
    m.wr(ANIM + x, t2);
    m.wr(TIMER2 + x, t2 - 2);
    m.wr(META_ID + x, 0x56);
    if (m.rd(ANIM + x) === 0x0E) spawnShot(m, x, 0x5B, 4); // A771 JMP 26:972D
    return;
  }
  m.wr(META_ID + x, 0x55);
  const px = m.rd(0x71), ox = m.rd(0x70 + x);
  if (px !== ox) {
    const v = (ox + (px < ox ? 0xFF : 0x01)) & 0xFF;
    m.wr(0x70 + x, v);
    if (v === 0x27) m.wr(0x70 + x, m.rd(0x70 + x) + 1);
    if (v === 0x98) m.wr(0x70 + x, m.rd(0x70 + x) - 1);
    dec(m, ANIM + x);
  }
  if (m.rd(0x08) & 7) return;
  m.wr(0x10, 2);
  m.wr(0x10, ((((m.rd(0x657F) << 1) & 0xFF) + 6) & 7) | 1);
  const r = rand64(m);
  if ((r - m.rd(0x10)) & 0x80) return;                      // CMP $10 / BMI
  let d = (m.rd(0x71) - m.rd(0x70 + x)) & 0xFF;
  if (d & 0x80) d = ((d ^ 0xFF) + 1) & 0xFF;
  if (d < 8) m.wr(TIMER2 + x, 0x1E);
}

/** AI $53 27:A7CD (type $2B): obj_aux 0 waits for sub_state 8, then $0575 = $43, $0623 = $15, sub_state 7;
 *  after that obj_flags b2 set and obj_aux counts to $40 -> obj_active = 0 */
export function aiHideAfterEvent(m, x) {
  if (m.rd(0x0660 + x) !== 0) {
    m.wr(FLAGS + x, m.rd(FLAGS + x) | 4);
    m.wr(0x0660 + x, m.rd(0x0660 + x) + 1);
    if (m.rd(0x0660 + x) === 0x40) m.wr(ACTIVE + x, 0);
    return;
  }
  if (m.rd(0x41) !== 0x08) return;
  m.wr(0x0660 + x, m.rd(0x0660 + x) + 1);
  m.wr(0x0575, 0x43);
  m.wr(0x0623, 0x15);
  m.wr(0x41, 0x07);
}

/** AI $54 27:A802: idle while slot 0 flags2 b7 clear and pad_dir b7 set; every 4th frame aims at the player
 *  (DirToTarget Y = 0, flags b4, flags2 5 / 7 at distance >= 2 / 8 if the player's flags2 b7, meta low 2 bits =
 *  dir/2 & 3); then timer--, anim--, one step (9824 keeps the y wrap) */
export function aiChasePlayer(m, x) {
  if (!(m.rd(0x0340) & 0x80) && (m.rd(0x49) & 0x80)) return;
  if ((m.rd(0x08) & 3) === 0) {
    m.wr(FLAGS + x, m.rd(FLAGS + x) | 0x10);
    dirToTarget(m, x, 0);
    m.wr(META_DIR + x, m.rd(0x20));
    let a = m.rd(0x21) < 2 ? 5 : 7;
    if (m.rd(0x0340) & 0x80) a = 8;
    m.wr(0x0340 + x, a);
    m.wr(0x10, (m.rd(META_DIR + x) >> 1) & 3);
    m.wr(META_ID + x, (m.rd(META_ID + x) & 0xFC) | m.rd(0x10));
  }
  dec(m, TIMER + x);
  dec(m, ANIM + x);
  objPosToTemp(m, x);
  objStepVector(m, x, m.rd(META_DIR + x));
  tempAddOffset(m);
  tempToObjPos(m, x);                                       // A85B JMP 26:9897
}

export const AI_SHOT = {
  0x0A: aiDuelStart, 0x33: aiPhaseUnhide, 0x51: aiFlagGatedSpawn, 0x52: aiDuelOpponent, 0x53: aiHideAfterEvent,
  0x54: aiChasePlayer, 0x55: aiCarryLeft,
  0x0C: aiFollowPlayerWait, 0x0D: aiMessageAfterWait, 0x0E: aiShopEmpty, 0x0F: aiSetSubstate,
  0x14: aiCountdown, 0x15: aiTrailShot, 0x16: aiCurveShot, 0x17: aiHoverShot, 0x18: aiShotTurnMeta,
  0x19: aiShotTurn, 0x1A: () => {},                         // 27:B2F4 RTS (ObjUpdateAll takes $1A to 30:CD8A first)
  0x1B: aiBounceShot, 0x1C: aiRingSpawner, 0x1D: aiShotEmitter, 0x1E: aiDirFrameShot, 0x1F: aiShotAnimDec,
  0x2F: aiFallingObj, 0x36: aiDropSpawner,
};
