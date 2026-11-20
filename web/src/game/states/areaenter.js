// re-c web port — the way into gameplay (session 108; "Web fix: gameplay frames" first part + (b)).
// REVERSE.md "Main states (session 63)", "New-game vs continue path", "Substates" rows 0/1, "Area entry".
//
//   mainStateNewGame(m)   generator = MainStateNewGame 30:C957 (main_state 0 -> 1, sub_state 0)
//   mainStateContinue(m)  generator = MainStateContinue 30:C9DA (main_state 8 -> 1, sub_state 1)
//   substNewGameInit(m)   generator = SubstNewGameInit 30:CA5F (sub_state 0 -> $13, or 8 when a button is held)
//   substAreaEnter(m)     generator = SubstAreaEnter 30:CA2E (sub_state 1 -> 8)
//   areaEnter(m)          generator = AreaEnter 31:E3E9 (AreaEntryTable 31:E3D9 by area_entry b7-b5)
//   playerPlaceBody(m, x) PlayerPlaceBody 26:9CEF
import { R } from '../ram.js';
import { cpuRd, setR6, setR7, switchBank16, farRestore } from '../prg.js';
import { SPIN, waitFrame, statRecalc, paletteBuildRom, fadeOut, fadeIn, hudHpBar } from './death.js';
import { waitPpuQueue } from './pause.js';
import { hudWindowSetup, saveGame, loadGameAndInit } from './saveload.js';
import { objInit } from '../obj/objinit.js';
import { objPosToTemp, tempToObjPos, tempAddOffset, objBgProbe } from '../obj/util.js';
import { spriteDraw } from '../obj/sprites.js';
import { playSound } from '../playsound.js';
import { readPads } from '../pads.js';
import { loadAreaMap } from '../world/area.js';
import { fullScreenDraw } from '../world/bgstream.js';
import { queueAttrAll } from '../world/ppuqueue.js';
import { cameraTick, cameraClampX } from '../world/camera.js';
import { mainState1 } from './play.js';

const NOFADE = 0x07E8, FLASH = 0x0710, ACTIVE = 0x04A0, FLAGS = 0x0380;

/** FullScreenDraw from AreaEnter (rendering off): measured in tools.emu (s108, area 0, f2340-2345) the NMIs
 *  fall after rows 7, 13, 19, 25 (~6 rows of ~43 scanlines per frame) and after the last row */
const FSD_LAG = (n) => n === 30 || (n > 1 && n % 6 === 1);

/** GameStartObjs 30:C9FF: all slots off, rendering + irq_mode 0, WaitFrame, ObjInit 3:$0A 0:$06 2:$05 1:$04 */
function* gameStartObjs(m) {
  for (let x = 0x1F; x >= 0; x--) m.wr(ACTIVE + x, 0);
  m.wr(R.ppumask_shadow, 0);
  m.wr(R.irq_mode, 0);
  yield* waitFrame(m);
  for (let y = 6; y >= 0; y -= 2) {                   // GameStartObjTable 30:CA26, read backwards
    m.wr(0x10, cpuRd(m, 0xCA26 + y));
    m.wr(0x11, cpuRd(m, 0xCA27 + y));
    objInit(m);
  }
}

/** MainStateNewGame 30:C957 */
export function* mainStateNewGame(m) {
  for (let x = 0x4F; x >= 0; x--) m.wr(0x6428 + x, 0xFF);           // inventory + selections
  for (let x = 0; x < 0x100; x++) { m.wr(0x6480 + x, 0); m.wr(0x6580 + x, 0); }
  m.wr(R.area_id, 0);
  m.wr(0x6D, 1);
  m.wr(R.main_state, 1);
  m.wr(R.sub_state, 0);
  yield* gameStartObjs(m);
  m.wr(0x0706, 0x1E);                                 // xp_next = 30
  for (const a of [0x0707, 0x0704, 0x0705, 0x0702, 0x0703, 0x0710, 0x0715, 0x0716, 0x0717, 0x0711, 0x0713,
    0x0714, 0x0712, 0x0718, 0x0719, NOFADE, 0x071A, 0x0743, 0x0744, 0x02, 0x03, 0x04, 0x05, R.irq_mode]) m.wr(a, 0);
  m.wr(0x0708, 0x22);                                 // MP cur / max = 34
  m.wr(0x0709, 0x22);
  yield* statRecalc(m, statX(m));
}

/** X at the JSR StatRecalc after GameStartObjs (-> $14 in HudRedraw): the last ObjInit leaves X = its slot
 *  $10 mod $20 (objinit.js, s97), here slot 1 */
const statX = (m) => m.rd(0x10) & 0x1F;

/** LoadGameAndInit 23:BCDA as called from state 8: the copies run through one NMI (tools.emu s108: f1602 L28 ->
 *  f1603 L11; that NMI finds frame_ready = 0 after GameStartObjs' WaitFrame), $6D / sub_state are written after it */
function* loadGameAndInitLag(m) {
  const e = m.rd(0x6D), s = m.rd(R.sub_state);
  loadGameAndInit(m);
  m.wr(0x6D, e); m.wr(R.sub_state, s);
  yield;
  m.wr(0x6D, 0xFF);
  m.wr(R.sub_state, 1);
}

/** MainStateContinue 30:C9DA */
export function* mainStateContinue(m) {
  yield* gameStartObjs(m);
  setR7(m, 0x17);
  yield* loadGameAndInitLag(m);
  yield* waitPpuQueue(m);
  yield* statRecalc(m, statX(m));
  switchBank16(m, 9);
  yield* hudWindowSetup(m);
  m.wr(R.main_state, 1);
  m.wr(R.sub_state, 1);
  m.wr(NOFADE, 0);
}

/** SubstNewGameInit 30:CA5F */
export function* substNewGameInit(m) {
  switchBank16(m, 9);
  yield* hudWindowSetup(m);
  yield* waitPpuQueue(m);
  setR6(m, 0x1A);
  yield* hudHpBar(m);
  yield* waitPpuQueue(m);
  m.wr(R.pal_buf, 0x0F);
  for (let i = 0; i < 4; i++) { m.wr(0x07E0 + i, 0xAF); m.wr(0x07E4 + i, 0x4F); }
  m.wr(NOFADE, 0xFF);
  yield* substAreaEnter(m);
  m.wr(R.sub_state, 0x13);
  yield* newGameSkipCheck(m);
}

/** 30:CAA2: pal_nofade_mask 0; ReadPads; a held button skips the wake-up cut-scene (sub_state 8 + StatRecalc) */
function* newGameSkipCheck(m) {
  m.wr(NOFADE, 0);
  readPads(m);
  if (m.rd(R.pad_prev) === 0) return;
  m.wr(R.sub_state, 8);
  yield* statRecalc(m, 0xFF);                         // ReadPads ends its pad loop with X = $FF
}

/** SubstAreaEnter 30:CA2E */
export function* substAreaEnter(m) {
  yield* areaEnter(m);
  setR6(m, 0x1A);
  objPosToTemp(m, 1);
  objBgProbe(m, 1, 0x40);
  m.wr(R.irq_mode, 0);
  m.wr(NOFADE, 0);
  m.wr(FLAGS + 1, m.rd(FLAGS + 1) & 0xBF);
  m.wr(R.sub_state, 8);
  if ((m.rd(0x6D) & 0xE0) === 0x40) {                 // CA5B: JMP MainStateGameplay (same frame)
    const g = mainState1(m);
    if (g && typeof g.next === 'function') yield* g;
  }
}

/** FarCall_B14_800E 31:FF8C */
function farSpriteDraw(m) {
  const r6 = m.rd(0x6E), r7 = m.rd(0x6F);
  switchBank16(m, 0x0E);
  spriteDraw(m);
  farRestore(m, r6, r7);
}

/** 31:E61B: object slots 4-$1E off */
function clearSlots(m) { for (let x = 4; x < 0x1F; x++) m.wr(ACTIVE + x, 0); }

/** AreaEnter 31:E3E9 */
export function* areaEnter(m) {
  farSpriteDraw(m);
  yield* waitPpuQueue(m);
  m.wr(R.irq_mode, 0);
  m.wr(0x07D7, 0);
  m.wr(0x07D6, 0);
  setR6(m, 0x1A);
  if (!(m.rd(NOFADE) & 0x80)) paletteBuildRom(m, 0);
  const k = (m.rd(0x6D) & 0xE0) >> 5;                 // AreaEntryDispatch 31:E40A (LSR x4 of b7-b5 = k*2)
  m.wr(0x10, cpuRd(m, 0xE3D9 + k * 2));
  m.wr(0x11, cpuRd(m, 0xE3DA + k * 2));
  const to = m.rd(0x10) | m.rd(0x11) << 8;
  switch (to) {
    case 0xE422:                                      // normal entry
      setR6(m, 0x1A);                                 // 31:E611
      yield* fadeOut(m, 2);
      clearSlots(m);
      if ((m.rd(FLASH) & 0x0F) === 4) {
        m.wr(FLASH, m.rd(FLASH) & 0xF0);
        yield* statRecalc(m, 0x1F);                   // X = $1F after the E61B loop
      }
      break;
    case 0xE41C:                                      // $6D = $E0-$FF (continue / menu load)
      clearSlots(m);
      break;
    case 0xE51C:                                      // $2x: keep the position, map only
      for (let x = 0x0D; x < 0x20; x++) m.wr(ACTIVE + x, 0);
      loadAreaMap(m);
      setR6(m, 0x1A);
      paletteBuildRom(m, 0);
      return;
    case 0xE533:                                      // $4x: save + attribute wipe
      setR7(m, 0x17);
      saveGame(m);
      yield* attrWipe(m);
      clearSlots(m);
      break;
    default:                                          // $6x AreaPitFall 31:E544 (k5) and the null entries
      throw new Error(`AreaEnter: area_entry $${m.rd(0x6D).toString(16)} -> $${to.toString(16)} not ported`);
  }
  yield* areaEnterCommon(m);
}

/** 31:E439 (the common tail of AreaEnter) */
function* areaEnterCommon(m) {
  loadAreaMap(m);
  switchBank16(m, 0x0D);
  tempToObjPos(m, 0);
  tempToObjPos(m, 1);
  flashAreaCheck(m);
  areaFollowerHook(m);
  areaChrOverride(m);
  m.wr(R.scroll_shadow0, 0);
  m.wr(R.cam_y_lo, 0);
  m.wr(0x30, 0x80);
  m.wr(0x31, 0x80);
  switchBank16(m, 0x0D);
  tempAddOffset(m);
  cameraClampX(m);
  for (let x = 3; x >= 0; x--) m.wr(0x3C + x, m.rd(0x34 + x));
  const s = m.rd(R.sub_state);
  if (s !== 0x14 && s !== 0x06) {
    switchBank16(m, 0x0D);
    playerPlaceBody(m, 1);
  }
  for (let x = 0x1F; x > 0; x--) m.wr(0x6200 + x, 0xFF);   // PPU queue ring ($6200 itself is kept)
  yield* cameraTick(m);
  m.wr(0x3A, 0);
  m.wr(0x3B, 0);
  for (let x = 3; x >= 0; x--) m.wr(0x34 + x, m.rd(0x3C + x));
  m.wr(R.frame_ready, 0);
  m.wr(R.irq_mode, 0x80);
  m.wr(R.ppumask_shadow, m.rd(R.ppumask_shadow) & 0x06);
  yield* waitFrame(m);
  yield* waitPpuQueue(m);
  yield* fullScreenDraw(m, 1, FSD_LAG);
  yield* waitPpuQueue(m);
  for (let x = 8; x >= 0; x--) yield* cameraTick(m);
  switchBank16(m, 0x0E);
  spriteDraw(m);
  yield* waitFrame(m);
  m.wr(R.irq_mode, 0);
  m.wr(R.ppumask_shadow, m.rd(R.ppumask_shadow) | 0x18);
  yield* waitFrame(m);
  if (!(m.rd(NOFADE) & 0x80)) playSound(m, 0x2F);
  setR6(m, 0x1A);
  if (!(m.rd(NOFADE) & 0x80)) yield* fadeIn(m, 2);
  setR7(m, 0x17);
  if (saveGame(m)) { yield; yield SPIN; }             // JSR $BC00: the full save runs through 2 NMIs (s108)
  m.wr(0x05A0, 4);                                    // obj_freeze
  m.wr(FLAGS + 1, m.rd(FLAGS + 1) | 4);
  m.wr(0x071A, m.rd(0x071A) | 4);
}

/** 31:E799: stone-flag b6 of $0710 only survives in areas $60/$64/$68 (then slot $1E = $0C at the player) */
function flashAreaCheck(m) {
  if (!(m.rd(FLASH) & 0x40)) return;
  const a = m.rd(R.area_id);
  if (a === 0x60 || a === 0x64 || a === 0x68) {
    m.wr(ACTIVE + 0x1E, 0x0C);
    tempToObjPos(m, 0x1E);
  } else m.wr(FLASH, m.rd(FLASH) & 0xBF);
}

/** AreaFollowerHook 31:E7C4 */
function areaFollowerHook(m) {
  const a = m.rd(R.area_id);
  if (a === 0xCE) return;
  if (a === 0x1C && !((m.rd(0x6488) << 2) & 0x80)) {
    if (!(m.rd(0x648A) & 0x08)) return;
    m.wr(0x6488, m.rd(0x6488) | 0x20);
    const x = 0x0E;
    m.wr(ACTIVE + x, 0x32);
    m.wr(0x03A0 + x, 0); m.wr(0x0620 + x, 0); m.wr(0x0640 + x, 0);
    m.wr(FLAGS + x, m.rd(FLAGS + x) & 0xEF);
    m.wr(0x0680 + x, 0x0C);
    m.wr(0x04E0 + x, 1);
    m.wr(0x0600 + x, 0xFF);
    m.wr(0x0340 + x, 3);
    m.wr(0x0600 + x, 0xFF);
    tempToObjPos(m, x);
    return;
  }
  m.wr(0x648A, m.rd(0x648A) & 0xF7);
}

/** 31:E823: area $8C with $6484 b7 -> CHR R4/R5 $FF/$51, sprite palettes 2/3 = $06/$0A */
function areaChrOverride(m) {
  if (m.rd(R.area_id) !== 0x8C || !(m.rd(0x6484) & 0x80)) return;
  m.wr(0x07F4, 0xFF); m.wr(0x07F5, 0x51);
  m.wr(0x07E6, 0x06); m.wr(0x07E7, 0x0A);
}

/** 31:E845 (area_entry $4x): bg palette 3 = $AD, then 16 steps of OR-ing 31:E8B6 masks into the attribute
 *  shadow (4 x 4 blocks of 2 x 2 bytes), each queued + 2 frames; fade out (2); bg palette 3 restored */
function* attrWipe(m) {
  yield* waitPpuQueue(m);
  setR6(m, 0x1A);
  const save = m.rd(0x07E3);
  m.wr(0x07E3, 0xAD);
  paletteBuildRom(m, 0);
  for (let x = 0; x !== 0x40; x += 4) {
    m.wr(0x11, 4);
    let y = 0;
    do {
      m.wr(0x10, 4);
      do {
        for (const [o, t] of [[0, 0], [1, 1], [8, 2], [9, 3]]) {
          m.wr(0x6100 + y + o, m.rd(0x6100 + y + o) | cpuRd(m, 0xE8B6 + x + t));
        }
        y += 2;
        m.wr(0x10, m.rd(0x10) - 1);
      } while (m.rd(0x10));
      y += 8;
      m.wr(0x11, m.rd(0x11) - 1);
    } while (m.rd(0x11));
    m.wr(0x10, x);
    queueAttrAll(m);
    for (let i = 0; i < 2; i++) yield* waitFrame(m);  // 30:C176 (Y = 2)
  }
  yield* fadeOut(m, 2);
  m.wr(0x07E3, save);
}

/** PlayerPlaceBody 26:9CEF (bank 26 mapped): slot x = slot 0 + JumpHeightTable 26:9D4D[$0620] */
export function playerPlaceBody(m, x) {
  m.wr(0x31, cpuRd(m, 0x9D4D + m.rd(0x0620)));
  if (m.rd(0x31) !== 0) {
    m.wr(0x03A0 + x, 0x60);
    objPosToTemp(m, x);
    objBgProbe(m, x, 0x40);
    m.wr(FLAGS + x, m.rd(FLAGS + x) & 0xBF);
  } else {
    m.wr(0x03A0 + x, 0x50);
    m.wr(0x10, m.rd(FLAGS + x) & 7);
    m.wr(FLAGS + x, (m.rd(FLAGS) & 0xF8) | m.rd(0x10));
  }
  if (m.rd(0x31) === 0xF9) {                          // landing from the highest jump
    playSound(m, 0x56);
    m.wr(0x07D7, 0x10);
    m.wr(0x07D6, 0x10);
  }
  m.wr(0x10, x);
  objPosToTemp(m, 0);
  m.wr(0x30, 0);
  tempAddOffset(m);
  tempToObjPos(m, x);
}
