// re-c web port — the save / load page of the SELECT menu and the save module (session 102).
// REVERSE.md "Save / load module, bank 23 `$BC00`" + "Save / load page (web port, session 102)".
//
//   saveLoadPage(m)    generator = SaveLoadPage 16:9AEE (START in the SELECT menu): returns when the ROM
//                      JMPs to SaveLoadExit 16:9C7A ($7000 = 0 / $AB / $FB / $FA tells equipMenu what next)
//   menuExitFade(m)    generator = 30:C80C (the $7000 = $FB / $AB exit: fade out, 30 frames, fade in)
//   loadTail(m)        generator = 30:D930 + EquipMenuLoadTail 30:D942 (after a menu LOAD)
//   hudWindowSetup(m)  generator = 19:BA65 (HUD window rebuild; also used by states 2/3/8 + SubstRestoreScreen)
//   substRestoreScreen(m) generator = SubstRestoreScreen 30:CC4D (sub_state $1F; area $5F redraw, s120)
//   saveGame / loadGame / loadGameAndInit   23:BC09 / 23:BC8E / 23:BCDA (bank 23 at $A000)
//
// Everything is literal, including the copy-length quirks: BlockCopy 23:BCF7 copies lo + 1 + hi bytes,
// SaveSlotCopy 16:9CE0 copies $2FF (not $300) bytes, and SaveGame's not-allowed path copies the mirror
// $7D00-$7FFF -> $6D00-$6FFF only from offset X = area_id (SaveAllowed 23:BDF6 leaves X = $6C).
import { R } from '../ram.js';
import { cpuRd, setR6, switchBank16 } from '../prg.js';
import { readPadsMenu } from '../pads.js';
import { SPIN, waitFrame, statRecalc, fadeOut, fadeIn, hudRedraw, hudHpBar, hudQueue } from './death.js';
import { queueHasSrc } from '../world/ppuqueue.js';
import { waitPpuQueue, ppuQueueBlock } from './pause.js';
import { objInit } from '../obj/objinit.js';
import { fullScreenDraw } from '../world/bgstream.js';
import { menuPrint, menuSnd, menuSprDraw, hideOam, frameRows, confirm, menuFlush } from './equip.js';

const SAVE_STATE = 0x7000;    // menu_save_state: 0 / $AB (closed) / $FB (save tried) / $FA (loaded)
const MODE = 0x646A;          // b0: 0 = SAVE, 1 = LOAD
const SLOT = 0x646B;          // save_slot_sel b0

/** Lag frames of the copy loops (CPU time > 1 frame; measured in tools.emu s102 with web_state.py --until,
 *  both starting at line 251 after the confirm's WaitFrame + ReadPadsMenu): YES on SAVE 16:9C44 -> SaveGame +
 *  2 x SaveSlotCopy -> SaveLoadExit 16:9C7A = f+0 L251 .. f+5 L33 (NMIs of f+1..f+4 = 4); YES on LOAD 16:9BBA
 *  -> SaveSlotCopy f+0 L251 .. f+1 L253 (NMI of f+1) -> LoadGame 23:BC8E .. 9C7A at f+2 L175 (before f+2's NMI)
 *  = 1. The first NMI finds frame_ready = 0 (DMA + scroll copy), later ones find it set (skip): k x `yield SPIN`. */
const SAVE_LAG = 4, LOAD_LAG = 1;
function* busy(m, k) { for (let i = 0; i < k; i++) yield SPIN; }

/** `LSR frame_ready / BCC` spin (16:9BFF, 30:C816): returns once bit 0 was set, shifting it out */
function* lsrWait(m) {
  for (;;) {
    const v = m.rd(R.frame_ready);
    m.wr(R.frame_ready, v >> 1);
    if (v & 1) return;
    yield;
  }
}

/** 16:9BFD: 48 x lsrWait */
function* wait48(m) { for (let x = 0x30; x > 0; x--) yield* lsrWait(m); }

/** 16:88A8: 3x3 picture at column $26 / row $27 of the $2800 window, tiles $29.. ($FF = blank 16:892A) */
export function* slotPicture(m) {
  let c = (m.rd(0x26) << 3) & 0xFF, r = m.rd(0x27);
  for (let i = 0; i < 3; i++) { const cb = r & 1; r >>= 1; c = (c >> 1) | (cb << 7); }
  m.wr(0x26, c); m.wr(0x27, r);
  m.wr(0x20, c); m.wr(0x21, (r + 0x28) & 0xFF);
  m.wr(0x24, 3); m.wr(0x25, 0);
  const blank = m.rd(0x29) === 0xFF;
  if (blank) m.wr(0x29, 0);
  for (let row = 0; row < 3; row++) {                 // 88D9 / 8905 x 3
    yield* waitPpuQueue(m);
    for (let x = 0; x < 3; x++) {
      const t = m.rd(0x29);
      m.wr(0x6000 + x, blank ? cpuRd(m, 0x892A + t) : t);
      m.wr(0x29, (t + 1) & 0xFF);
    }
    menuFlush(m);
    const v = m.rd(0x20) + 0x20;
    m.wr(0x20, v & 0xFF);
    m.wr(0x21, (m.rd(0x21) + (v >> 8)) & 0xFF);
  }
}

/** 16:9D52: hide OAM, strings $0F $13 $35, CHR alt $3C/$38, 6 x frame rows (X = 9) from $28C0 */
function* pageSetup(m) {
  yield* hideOam(m);
  for (const s of [0x0F, 0x13, 0x35]) yield* menuPrint(m, s);
  yield* waitFrame(m);
  m.wr(0x58, 0x3C); m.wr(0x59, 0x38);
  m.wr(0x21, 0x28); m.wr(0x20, 0xC0); m.wr(0x25, 0); m.wr(0x24, 0x40);
  m.wr(0x19, 6);
  do { yield* frameRows(m, 9); m.wr(0x19, (m.rd(0x19) - 1) & 0xFF); } while (m.rd(0x19) !== 0);
}

/** 16:9C85 (X = 0 SAVE / 1 LOAD): the mode cursor (shape 3, tables 16:9D9C-9DA3) at OAM slot $39 + SFX $4B */
function modeCursor(m, x) {
  m.wr(0x60FB, cpuRd(m, 0x9D9C + x));
  m.wr(0x60F8, cpuRd(m, 0x9D9E + x));
  m.wr(0x60F9, cpuRd(m, 0x9DA2 + x));
  m.wr(0x60FA, cpuRd(m, 0x9DA0 + x));
  m.wr(0x12, 3); m.wr(0x2E, 0x39);
  menuSprDraw(m);
  menuSnd(m, 0x4B);
}

/** 16:9CAE: the two slot arrows (OAM $027C / $0280, tile $FE, y 16:9CDE[$646B & 1]) + SFX $4B */
function slotCursor(m) {
  const y = cpuRd(m, 0x9CDE + (m.rd(SLOT) & 1));
  m.wr(0x0280, y); m.wr(0x027C, y);
  m.wr(0x0281, 0xFE); m.wr(0x027D, 0xFE);
  m.wr(0x0282, 0x00); m.wr(0x027E, 0x40);
  m.wr(0x0283, 0x30); m.wr(0x027F, 0xD0);
  menuSnd(m, 0x4B);
}

/** SaveSlotCopy 16:9CE0: descriptor A of SaveSlotDescs 16:9D32 (src, dst); $10-$15 kept in $60C0-$60C5.
 *  The DEC/BNE count ($14 = $FF, $15 = 2) copies 255 + 256 + 256 = $2FF bytes. */
export function saveSlotCopy(m, a) {
  for (let x = 0; x < 6; x++) m.wr(0x60C0 + x, m.rd(0x10 + x));
  const p = 0x9D32 + ((a << 2) & 0xFF);
  const src = cpuRd(m, p) | cpuRd(m, p + 1) << 8, dst = cpuRd(m, p + 2) | cpuRd(m, p + 3) << 8;
  for (let i = 0; i < 0x2FF; i++) m.wr((dst + i) & 0xFFFF, m.rd((src + i) & 0xFFFF));
  for (let x = 0; x < 6; x++) m.wr(0x10 + x, m.rd(0x60C0 + x));
}

/** BlockCopy 23:BCF7: ($10) -> ($12), lo + 1 + hi bytes ($14 / $15) */
function blockCopy(m) {
  let s = m.rd(0x10) | m.rd(0x11) << 8, d = m.rd(0x12) | m.rd(0x13) << 8;
  let lo = m.rd(0x14), hi = m.rd(0x15);
  for (;;) {
    m.wr(d, m.rd(s));
    s = (s + 1) & 0xFFFF; d = (d + 1) & 0xFFFF;
    lo = (lo - 1) & 0xFF;
    if (lo !== 0xFF) continue;
    lo = 0;
    hi = (hi - 1) & 0xFF;
    if (hi === 0xFF) break;
  }
  m.wr(0x10, s & 0xFF); m.wr(0x11, s >> 8); m.wr(0x12, d & 0xFF); m.wr(0x13, d >> 8);
  m.wr(0x14, lo); m.wr(0x15, hi);
}

/** the descriptor loops of SaveGame (5 x 23:BDC0) / LoadGame (4 x 23:BDDE) */
function copyDescs(m, table, n) {
  for (let t = 0; t < n; t++) {
    m.wr(0x17, t);
    const y = t * 6;
    m.wr(0x10, (t << 1) & 0xFF);                        // ASL A / STA $10 (then overwritten)
    for (let x = 0; x < 6; x++) m.wr(0x10 + x, cpuRd(m, table + y + x));
    blockCopy(m);
  }
  m.wr(0x17, n);
}

const PSTATE = [[0x6C, 0x7DF0], [0x70, 0x7DF1], [0x90, 0x7DF2], [0xB0, 0x7DF3], [0xD0, 0x7DF4],
  [0x03C0, 0x7DF5], [0x03C1, 0x7DF6], [0x0421, 0x7DF7], [0x0620, 0x7DF8]];

/** SaveChecksum 23:BD92: 8-bit sum of $7D00-$7FFF -> $70F5 and $70F4 */
function saveChecksum(m) {
  let s = 0;
  for (let x = 0; x < 0x100; x++) s = (s + m.rd(0x7D00 + x) + m.rd(0x7E00 + x) + m.rd(0x7F00 + x)) & 0xFF;
  m.wr(0x70F5, s); m.wr(0x70F4, s);
}

/** SaveGame 23:BC09 -> true when the area allows saving (the full image rebuild, ~2 frames of CPU, s108) */
export function saveGame(m) {
  const flash = m.rd(0x0710);
  m.wr(0x0710, flash & 0xF0);
  for (let x = 0; x < 0x20; x++) m.wr(0x6000 + x, m.rd(0x10 + x));
  let x = m.rd(R.area_id);                              // SaveAllowed: LDX $6C
  const full = (cpuRd(m, 0xBF00 + x) & 0x80) !== 0;
  if (full) {
    for (let i = 0; i < 0x100; i++) { m.wr(0x7D00 + i, 0); m.wr(0x7E00 + i, 0); m.wr(0x7F00 + i, 0); }
    for (const [src, dst] of PSTATE) m.wr(dst, m.rd(src));     // SavePlayerState 23:BD1F
    m.wr(0x7DFD, 0x41); m.wr(0x7DFE, 0x4D); m.wr(0x7DFF, 0x49);
    copyDescs(m, 0xBDC0, 5);
    saveChecksum(m);
    x = 0;
  }
  for (; x < 0x100; x++) {                              // BC65: mirror (from X = area_id when not allowed)
    m.wr(0x6D00 + x, m.rd(0x7D00 + x)); m.wr(0x6E00 + x, m.rd(0x7E00 + x)); m.wr(0x6F00 + x, m.rd(0x7F00 + x));
  }
  for (let i = 0; i < 0x20; i++) m.wr(0x10 + i, m.rd(0x6000 + i));
  m.wr(0x0710, flash);
  return full;
}

/** LoadGame 23:BC8E */
export function loadGame(m) {
  for (let x = 0; x < 0x20; x++) m.wr(0x6000 + x, m.rd(0x10 + x));
  copyDescs(m, 0xBDDE, 4);
  for (const [dst, src] of PSTATE) m.wr(dst, m.rd(src));     // LoadPlayerState 23:BD60
  for (let x = 0; x < 0x20; x++) m.wr(0x10 + x, m.rd(0x6000 + x));
}

/** LoadGameAndInit 23:BCDA */
export function loadGameAndInit(m) {
  loadGame(m);
  for (let x = 0; x < 0x100; x++) { m.wr(0x6480 + x, m.rd(0x7E00 + x)); m.wr(0x6580 + x, m.rd(0x7F00 + x)); }
  m.wr(0x6D, 0xFF);
  m.wr(R.sub_state, 1);
}

/** MenuSave 16:9BE1: returns true to go back to SaveLoadLoop (confirm = NO), false = SaveLoadExit */
function* menuSave(m) {
  m.wr(SAVE_STATE, 0xFB);
  let msg = 0;
  if (!(cpuRd(m, 0xBF00 + m.rd(R.area_id)) & 1)) msg = 0x16;          // "Cannot save!"
  else if (m.rd(0x0710) & 0x0F) msg = 0x38;                            // player status
  if (msg) {
    menuSnd(m, 0x4E);
    yield* menuPrint(m, 0x0F);
    yield* menuPrint(m, msg);
    yield* wait48(m);                                   // 9BFD
    m.wr(R.pad_new, 0);
    m.wr(SAVE_STATE, 0xAB);
    return false;
  }
  yield* menuPrint(m, 0x37);
  yield* menuPrint(m, 0x0E);
  if (yield* confirm(m, 0xA1)) { yield* menuPrint(m, 0x0F); return true; }
  const s = m.rd(SLOT) & 1;
  saveGame(m);
  saveSlotCopy(m, s ? 6 : 4);
  saveSlotCopy(m, s ? 7 : 5);
  const c = m.rd(0x70F5);
  m.wr(s ? 0x70F2 : 0x70F0, c); m.wr(s ? 0x70F3 : 0x70F1, c);
  yield* busy(m, SAVE_LAG);
  return false;
}

/** SaveLoadPage 16:9AEE + SaveLoadLoop 16:9B3A (returns at SaveLoadExit 16:9C7A) */
export function* saveLoadPage(m) {
  m.wr(SAVE_STATE, 0); m.wr(MODE, 0); m.wr(SLOT, 0);
  yield* pageSetup(m);
  modeCursor(m, 0);
  slotCursor(m);
  yield* menuPrint(m, 0x39);
  yield* menuPrint(m, 0x17);
  m.wr(0x19, 0);
  do {                                                  // 9B14: the SAVE / LOAD pictures
    const x = m.rd(0x19);
    m.wr(0x26, cpuRd(m, 0x9D8D + x)); m.wr(0x27, cpuRd(m, 0x9D92 + x)); m.wr(0x29, cpuRd(m, 0x9D97 + x));
    yield* slotPicture(m);
    m.wr(0x19, (m.rd(0x19) + 1) & 0xFF);
  } while ((m.rd(0x19) - 2) & 0x80);
  yield* menuPrint(m, 0x14);
  yield* menuPrint(m, 0x15);
  for (;;) {                                            // SaveLoadLoop
    m.wr(SAVE_STATE, 0);
    yield* waitFrame(m);
    readPadsMenu(m);
    const p = m.rd(R.pad_new);
    if (p & 0x30) return;                               // SELECT / START
    if (p & 0x03) {                                     // LEFT / RIGHT: SAVE <-> LOAD
      m.wr(MODE, (m.rd(MODE) + 1) & 0xFF);
      modeCursor(m, m.rd(MODE) & 1);
      continue;
    }
    if (p & 0x0C) {                                     // UP / DOWN: slot (re-print the old slot's row)
      yield* menuPrint(m, (m.rd(SLOT) & 1) ? 0x39 : 0x3A);
      m.wr(SLOT, (m.rd(SLOT) + 1) & 0xFF);
      slotCursor(m);
      continue;
    }
    if (p & 0x40) { m.wr(SAVE_STATE, 0xAB); return; }   // B
    if (!(p & 0x80)) continue;
    if (!(m.rd(MODE) & 1)) {                            // A on SAVE
      if (yield* menuSave(m)) continue;
      return;
    }
    yield* menuPrint(m, 0x36);                          // A on LOAD: "Load this game?"
    yield* menuPrint(m, 0x0E);
    if (yield* confirm(m, 0xA1)) { yield* menuPrint(m, 0x0F); continue; }
    saveSlotCopy(m, (m.rd(SLOT) & 1) ? 2 : 0);
    yield* busy(m, LOAD_LAG);                           // the NMI falls inside SaveSlotCopy
    loadGame(m);
    m.wr(SAVE_STATE, 0xFA);
    return;
  }
}

/** 30:C80C: SwitchBank16(13), fade out (3), 1 frame, irq_mode 0, 30 frames, fade in (3), SwitchBank16(8) */
export function* menuExitFade(m) {
  switchBank16(m, 0x0D);
  yield* fadeOut(m, 3);
  yield* lsrWait(m);
  m.wr(R.irq_mode, 0);
  for (let x = 0x1E; x > 0; x--) yield* lsrWait(m);
  yield* fadeIn(m, 3);
  switchBank16(m, 8);
}

/** 26:8D59: the charge meter ($6060-$6070: 7 x $8F, orbit level x $8E, 9 tiles 26:8DF5[26:8DDD[..]]) -> $2B46 */
export function* hudChargeMeter(m) {
  yield* waitSrc60(m);
  for (let x = 6; x >= 0; x--) m.wr(0x6060 + x, 0x8F);
  const orb = m.rd(0x06C0);
  for (let x = orb & 7; x > 0; x--) m.wr(0x605F + x, 0x8E);
  let v = (m.rd(0x0719) << 3) & 0xFF;
  m.wr(0x10, v);
  if (orb) { v |= (m.rd(0x08) & 4) >> 2; m.wr(0x10, v); }
  const x = cpuRd(m, 0x8DDD + (((orb & 0x18) >> 2) | v));
  for (let y = 0; y < 9; y++) m.wr(0x6067 + y, cpuRd(m, 0x8DF5 + x + y));
  hudQueue(m, 0x2B, 0x46, 0x10, 0x60);
}
/** PpuQueueWaitSrc 30:C72B ($60) */
function* waitSrc60(m) { while (queueHasSrc(m, 0x60)) yield SPIN; }

/** 19:BA65: HUD window rebuild (needs banks 18/19 in: SwitchBank16(9)) */
export function* hudWindowSetup(m) {
  yield* waitPpuQueue(m);
  for (let x = 0x1F; x >= 0; x--) {
    m.wr(0x6000 + x, 0x19); m.wr(0x6020 + x, 0x1C); m.wr(0x6040 + x, 0xFF);
    m.wr(0x6060 + x, 0xFF); m.wr(0x6080 + x, 0xFF);
  }
  m.wr(0x6001, 0x18); m.wr(0x601F, 0x1A);
  yield* ppuQueueBlock(m, 0);
  m.wr(0x6021, 0x1B); m.wr(0x603F, 0x1D);
  for (const b of [1, 2, 3]) yield* ppuQueueBlock(m, b);
  yield* waitPpuQueue(m);
  for (let x = 0x5F; x >= 0; x--) m.wr(0x6000 + x, cpuRd(m, 0xBAD9 + x));
  yield* ppuQueueBlock(m, 4);
  setR6(m, 0x1A);
  yield* hudHpBar(m);
  yield* hudChargeMeter(m);
  for (let x = 5; x >= 0; x--) yield* hudRedraw(m, x, x);
  yield* waitPpuQueue(m);
}

/** SubstRestoreScreen 30:CC4D (s120; sub_state $1F after AI $70 phase 2, JMP from 30:C8AF / 30:CBB1, JSR from
 *  30:D912): redraw the area-$5F screen after a window covered it. Rendering is off during FullScreenDraw and
 *  every PpuQueueBlock flushes at once (mask off). Lag rows measured in tools.emu (s120 traces): the draw starts
 *  right after a WaitFrame; AI $70 path (restore5f.json) NMIs after rows 6/12/18/24/30, pause path
 *  (pause5f.json) after 6/12/18/23/29 (row cost depends on the tiles). Both = 5 NMIs, so only one mid-draw
 *  frame's pos_y can be 8 px off; everything after the draw lines up. */
const RESTORE_LAG = (n) => n % 6 === 0;
export function* substRestoreScreen(m) {
  yield* waitPpuQueue(m);
  m.wr(R.ppumask_shadow, m.rd(R.ppumask_shadow) & 0xE7);
  yield* waitFrame(m);
  for (let i = 0; i < 4; i++) m.wr(0x34 + i, m.rd(0x3C + i));   // pos $34-$37 <- camera $3C-$3F
  m.wr(0x0744, 8);                                               // nametable b3 for BgRowRender's queue hi
  yield* fullScreenDraw(m, 1, RESTORE_LAG);                      // CC70
  m.wr(0x0744, 0);
  switchBank16(m, 9);
  yield* hudWindowSetup(m);                                      // CC7D JSR 19:BA65
  yield* waitFrame(m);
  for (let y = 0x3F; y >= 0; y--) m.wr(0x6060 + y, 0xFF);
  for (let y = 0x2F; y >= 0; y--) m.wr(0x6060 + y, m.rd(0x6100 + y));   // attr_shadow
  yield* ppuQueueBlock(m, 3);
  setR6(m, 0x0B);
  for (let y = 0; y < 0x100; y++) m.wr(0x6000 + y, cpuRd(m, 0x9E00 + y)); // Wram6000InitTable 11:BE00
  m.wr(0x2F, 0x3E);
  let y;
  do {                                                           // blocks $3E-$43 (window pieces)
    y = yield* ppuQueueBlock(m, m.rd(0x2F));
    m.wr(0x2F, (m.rd(0x2F) + 1) & 0xFF);
  } while (m.rd(0x2F) !== 0x44);
  m.wr(R.ppumask_shadow, m.rd(R.ppumask_shadow) | 0x18);
  yield* waitFrame(m);
  m.wr(R.sub_state, 8);
  return y;                                                     // Y (SubstPlay goes on with it after 30:CBB1)
}

/** 30:C8A9: JMP SubstRestoreScreen in area $5F, else RTS (after NpcTalk's pending message 30:D0C5, 30:D877) */
export function* restoreIfArea5F(m) {
  if (m.rd(R.area_id) === 0x5F) yield* substRestoreScreen(m);
}

/** 30:D930 + EquipMenuLoadTail 30:D942: after a menu LOAD (keep level / HP / max HP over ObjInit) */
export function* loadTail(m) {
  m.wr(SAVE_STATE, 0);
  m.wr(R.ppumask_shadow, m.rd(R.ppumask_shadow) & 0xE7);
  yield* waitFrame(m);
  const lvl = m.rd(0x0421), hp = m.rd(0x03C1), hpMax = m.rd(0x03C0);
  m.wr(0x10, 1); m.wr(0x11, 4); objInit(m);
  m.wr(0x10, 0); m.wr(0x11, 6); objInit(m);
  m.wr(0x03C0, hpMax); m.wr(0x03C1, hp); m.wr(0x0421, lvl);
  m.wr(0x07E8, 0);
  yield* statRecalc(m, 0);
  switchBank16(m, 9);
  yield* hudWindowSetup(m);
  m.wr(0x6D, 0xFF);
  m.wr(R.sub_state, 1);
}
