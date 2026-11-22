// re-c web port — small effect AIs reached from ObjUpdateAll (s93, "boss / effect AI port" (a)).
// REVERSE.md "Enemies — catalogue + generic enemy AIs" (ObjAiTable 27:A314) + "Boss / effect AIs (s93)".
//
//   AI $04 AiPlayerFollowC 27:B2FC  slot $0C <- player (slot 0) x/y; slot $0C obj_anim -= obj_timer2, <0 -> off
//   AI $05 AiFollowFacing  27:A219  this slot <- slot 0 + 4 px in slot 1's facing; DEC $0603 (s110)
//   AI $57 = AiStraightMoverTail 27:B0BE (no obj_anim step; obj/ai_static.js)
//   AI $76 AiMpDrain       27:A4DC  overlay $A3 on the player, every 16th frame sound $31 + 1 MP + HUD MP digits;
//                                   obj_anim -1 per frame (-3 while a button is newly pressed), borrow -> off
//
// AiMpDrain is a generator: HudRedraw 26:8E46 spins in PpuQueueWaitSrc($7B) while an MP / HUD record is queued.
import { objPosToTemp, tempToObjPos, tempAddOffset, spawnShot, objBecomeNext } from './util.js';
import { cpuRd, indY, switchBank16, farRestore } from '../prg.js';
import { aiStraightMoverTail } from './ai_static.js';
import { playSound } from '../playsound.js';
import { hudRedraw, paletteBuildRom, waitFrame } from '../states/death.js';
import { waitPpuQueue } from '../states/pause.js';
import { areaSubTable } from '../world/camera.js';
import { bgRowRender, fullScreenDraw, FSD_LAG } from '../world/bgstream.js';
import { queueAttrAll } from '../world/ppuqueue.js';

const ANIM = 0x04E0, ACTIVE = 0x04A0, TIMER2 = 0x0620, PHASE = 0x0600, ACT_TIMER = 0x0640;
const META_ID = 0x0300, TILE_OFS = 0x0320;

/** AI $04 27:B2FC: ignores its own slot X; always moves / ages slot $0C, and returns with X = $0C, so
 *  ObjUpdateAll's INX goes on at slot $0D (returns $0C: objUpdateAll takes a number as the new X). */
export function aiPlayerFollowC(m) {
  objPosToTemp(m, 0);
  tempToObjPos(m, 0x0C);
  const a = (m.rd(ANIM + 0x0C) - m.rd(TIMER2 + 0x0C)) & 0xFF;
  m.wr(ANIM + 0x0C, a);
  if (a & 0x80) m.wr(ACTIVE + 0x0C, 0);
  return 0x0C;
}

/** AI $76 27:A4DC (27:A4E0 = an unreferenced second entry with overlay $99) */
export function* aiMpDrain(m, x, overlay = 0xA3, id = 0x76) {
  m.wr(0x0581, overlay);                                    // obj_overlay_id slot 1
  if ((m.rd(0x08) & 0x0F) === 0) {
    playSound(m, 0x31, (id << 1) & 0xFF);               // Y = id*2 from ObjUpdateAll's TAY 30:CD07
    m.wr(0x05A0, 2);                                        // obj_freeze slot 0
    if (m.rd(0x0708)) {                                     // mp_cur
      m.wr(0x0708, m.rd(0x0708) - 1);
      yield* hudRedraw(m, 4, x);                            // A4FF JSR 26:8E46 (MP digits); X back from $14
    }
  }
  const s = m.rd(ANIM + x) + (m.rd(0x4B) ? 0xFD : 0xFF);   // pad_new
  m.wr(ANIM + x, s & 0xFF);
  if (s < 0x100) m.wr(ACTIVE + x, 0);
}

/** AI $05 27:A219 (s110; slot 3 in the start room): DEC obj_phase slot 3 $0603 while != 0 (slot 3 fixed, not X),
 *  then this slot = player body (slot 0) + 4 px toward slot 1's facing (obj_meta_id_s01 & 3 -> 27:A242 dx/dy) */
export function aiFollowFacing(m, x) {
  if (m.rd(0x0603)) m.wr(0x0603, m.rd(0x0603) - 1);
  m.wr(0x10, x);                                            // STX $10 (X saved around 26:98A8)
  objPosToTemp(m, 0);
  const y = (m.rd(0x0301) & 3) << 1;
  m.wr(0x30, cpuRd(m, 0xA242 + y));
  m.wr(0x31, cpuRd(m, 0xA243 + y));
  tempAddOffset(m);                                         // 26:9824
  tempToObjPos(m, x);                                       // JMP 26:9897
}

/** 27:B850 (ObjAiExplode's tail, also the head of AI $08): camera lock $10, DEC obj_phase; on borrow
 *  (obj_active b2: sound $54, R5 $45, tile ofs $80, pal 3 = 4, PaletteBuild -> Y = 4) spawn shot $3E
 *  (dir = Y) unless area $E4/$73 or (area $28/$7C and X = $0E); spawned -> place it from the step record
 *  27:B8E6[timer2*4] (dx, dy, meta id, next phase); not spawned -> only the phase 27:B8E9[timer2*4];
 *  dx / phase 0 = done -> ObjBecomeNext 26:98CD. y = the caller's Y. */
function explodeStep(m, x, y) {
  m.wr(0x07D7, 0x10);                                       // boss_cam_lock
  const ph = (m.rd(PHASE + x) - 1) & 0xFF;
  m.wr(PHASE + x, ph);
  if (!(ph & 0x80)) return;
  if (m.rd(ACTIVE + x) & 4) {                               // B85B
    playSound(m, 0x54, y);
    m.wr(0x07F5, 0x45);
    m.wr(TILE_OFS + x, 0x80);
    m.wr(0x07E7, 4);
    paletteBuildRom(m, 0);                                  // 26:8C0E
    y = 4;                                                  // PaletteBuild leaves Y = 4 (see collide.js)
  }
  const area = m.rd(0x6C);
  const skip = area === 0xE4 || area === 0x73 || ((area === 0x28 || area === 0x7C) && x === 0x0E);
  const t = m.rd(TIMER2 + x);
  if (!skip && spawnShot(m, x, 0x3E, y)) {                  // B8A8
    m.wr(0x11, x);
    m.wr(TIMER2 + x, (t + 1) & 0xFF);
    const r = (t << 2) & 0xFF;
    m.wr(0x30, cpuRd(m, 0xB8E6 + r));
    if (m.rd(0x30) === 0) { objBecomeNext(m, x); return true; }
    m.wr(0x31, cpuRd(m, 0xB8E7 + r));
    m.wr(PHASE + x, cpuRd(m, 0xB8E9 + r));
    objPosToTemp(m, x);
    const n = m.rd(0x10);
    m.wr(META_ID + n, cpuRd(m, 0xB8E8 + r));
    m.wr(TILE_OFS + n, m.rd(TILE_OFS + x));
    tempAddOffset(m);
    tempToObjPos(m, n);
    return true;
  }
  m.wr(TIMER2 + x, (t + 1) & 0xFF);                         // B896
  const p = cpuRd(m, 0xB8E9 + ((t << 2) & 0xFF));
  if (p === 0) return objBecomeNext(m, x);
  m.wr(PHASE + x, p);
}

/** AI $74 ObjAiExplode 27:B83B (Y = $E8 from ObjUpdateAll's TAY): area $5F -> irq_mode 0; area != $1A -> shake $10 */
export function aiExplode(m, x) {
  if (m.rd(0x6C) === 0x5F) m.wr(0x51, 0);
  if (m.rd(0x6C) !== 0x1A) m.wr(0x07D6, 0x10);
  explodeStep(m, x, 0xE8);
}

/** AI $08 AiScreenRedraw08 27:B7F6 (Y = $10): explodeStep, then DEC obj_act_timer; below $0A each frame
 *  MarkScreenVisited + WaitPpuQueue + one BG row at the object's y + (timer*8 - $28) (x = camera $3C/$3D),
 *  and the attribute table on the frame the timer reaches 0 */
export function* aiScreenRedraw08(m, x, y = 0x10) {
  const spawned = explodeStep(m, x, y) === true;
  const t = (m.rd(ACT_TIMER + x) - 1) & 0xFF;
  m.wr(ACT_TIMER + x, t);
  if (t >= 0x0A) return;
  m.wr(0x1F, 0x28);
  markScreenVisited(m, x);
  yield* waitPpuQueue(m);
  objPosToTemp(m, x);
  m.wr(0x31, ((t << 3) - 0x28) & 0xFF);                    // ASL x3, SEC SBC $1F ($30 as left)
  tempAddOffset(m);
  m.wr(0x30, 0xFF); m.wr(0x31, 0xFF);
  m.wr(0x34, m.rd(0x3C)); m.wr(0x35, m.rd(0x3D));
  const r6 = m.rd(0x6E), r7 = m.rd(0x6F);                   // FarCall_EB91
  yield* bgRowRender(m);
  farRestore(m, r6, r7);
  if (t === 0) queueAttrAll(m);                             // PpuQueueAttrAll 30:C739
  // s114 lag-frame heuristic (the port has no CPU cycle count). In tools.emu a frame that spawns a shot AND
  // renders a row ends ObjCollideAll at about L236 with 11 active slots (f2463). With 12 or more (f2466, f2470)
  // it runs past the NMI at L241, so that frame is a lag frame.
  if (spawned && countActive(m) >= 12) m.overrun = true;
}

const countActive = (m) => { let n = 0; for (let s = 0; s < 0x1F; s++) if (m.rd(ACTIVE + s)) n++; return n; };

/** MarkScreenVisited 30:C75C: visited_map[y_hi] |= bit[x_hi]; the area's k4 list (flag, screen) pairs ->
 *  screen_flags $64C0 bit for the screen ($12 = y_hi<<4 | x_hi); ends SwitchBank16($0D) */
export function markScreenVisited(m, x) {
  const xh = m.rd(0x90 + x), yh = m.rd(0xD0 + x);
  m.wr(0x62F0 + yh, m.rd(0x62F0 + yh) | cpuRd(m, 0xC000 + xh));
  m.wr(0x12, ((yh << 4) | m.rd(0x90 + x)) & 0xFF);
  areaSubTable(m, 4);
  let y = 0;
  for (;;) {
    const f = indY(m, 0x10, y);
    if (f === 0xFF) break;
    m.wr(0x13, f);
    const s = indY(m, 0x10, y + 1);
    y = (y + 2) & 0xFF;
    if (s !== m.rd(0x12)) continue;
    const bit = cpuRd(m, 0xC000 + (f & 7));
    m.wr(0x12, bit);
    m.wr(0x64C0 + (f >> 3), m.rd(0x64C0 + (f >> 3)) | bit);
    break;
  }
  switchBank16(m, 0x0D);
}

/** 27:A459, AI $5F's reveal tail: off, visited row 0, player pos <- camera, rendering off for a full redraw */
function* floorRevealTail(m, x) {
  m.wr(ACTIVE + x, 0);
  m.wr(0x62F0, 0xFF);
  for (let i = 0; i < 4; i++) m.wr(0x34 + i, m.rd(0x3C + i));
  m.wr(0x01, m.rd(0x01) & 0xE7);                            // ppumask_shadow
  yield* waitFrame(m);
  yield* fullScreenDraw(m, 1, FSD_LAG);                     // 30:C831
  m.wr(0x01, m.rd(0x01) | 0x18);
}

/** AI $5F AiFloorReveal 27:A41A (s118; type $DA in tower areas $59-$5C). Phase 0 (27:A48C): visited rows
 *  ($5D - area [-1 if player y_lo < $10])..3 = $FF; area $5C instead: inventory[0] = 4 -> reveal tail; off.
 *  Then every frame: no active slot $0D-$1E on the player's y_hi row with spawn param $50/$51 ->
 *  visited rows 0-5 = $FF, sound $5D, reveal tail. */
export function* aiFloorReveal(m, x) {
  if (m.rd(PHASE + x) === 0) {
    m.wr(PHASE + x, (m.rd(PHASE + x) + 1) & 0xFF);
    const area = m.rd(0x6C);
    let y = (0x5D - area) & 0xFF;
    if (area === 0x5C) {
      if (m.rd(0x6430) === 4) yield* floorRevealTail(m, x);
      m.wr(ACTIVE + x, 0);
      return;                                               // PLA PLA RTS
    }
    if (m.rd(0xB0) < 0x10) y = (y - 1) & 0xFF;
    while (y !== 4) {                                       // A4B9: INY wrapping to 0 also ends it (BNE)
      m.wr(0x62F0 + y, 0xFF);
      y = (y + 1) & 0xFF;
      if (y === 0) break;
    }
  }
  const row = m.rd(0xD0);
  for (let y = 0x0D; y < 0x1F; y++) {
    if (m.rd(0xD0 + y) !== row || !m.rd(ACTIVE + y)) continue;
    const p = m.rd(0x0560 + y);
    if (p === 0x51 || p === 0x50) return;
  }
  for (let i = 0; i < 6; i++) m.wr(0x62F0 + i, 0xFF);
  playSound(m, 0x5D, 0x1F);
  yield* floorRevealTail(m, x);
}

export const AI_EFFECT = {
  0x04: aiPlayerFollowC, 0x05: aiFollowFacing, 0x08: aiScreenRedraw08, 0x57: aiStraightMoverTail, 0x5F: aiFloorReveal,
  0x74: aiExplode, 0x76: aiMpDrain,
  0x77: (m, x) => aiMpDrain(m, x, 0x99, 0x77),              // s168: AiMpDrain99 27:A4E0 (no object type uses it)
  0x7C: (m, x) => aiScreenRedraw08(m, x, 0xF8),             // s166: same entry 27:B7F6, Y = $7C*2 (obj_active b2 set)
};
