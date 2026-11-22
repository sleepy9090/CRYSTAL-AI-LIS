// re-c web port — the teleport menu (session 143). REVERSE.md "ItemUseMenu" (the `$08` Warp Boots item, s135),
// the substate table rows 20/29 and "TeleportAreaTable".
//
//   substTeleportMenu(m, y)  SubstTeleportMenu 30:DB28 (sub_state $14 = magic 4, $1D = never stored; also JSR'd by
//                            ItemUseMenu 30:D4DC for the Warp Boots with sub_state 6):
//     refusals 30:DB28-DB4E  HP 0 (silent), boss_cam_lock $07D7 != 0, area $58-$5F, status 4 -> SFX $4E; sub_state 8
//     open 30:DB4F           orbit_level 0, PaletteBuild(0), msg 33.0 window (TextWindowOpen/TextPrintBox),
//                            TeleportMenuLocks 30:DD36 (blank the towns whose flag F2F5+i is clear), cursor = OAM 0
//     loop 30:DBAC           blink tile $3F/$3C ($07FF b3), ReadPadsMenu, sprite pass, OamClipTop, WaitFrame,
//                            PadNewButtonIndex -> JmpTable30_DBDC (A pick, B/SEL/START exit, U/D/L/R move + SFX $4B)
//     pick 30:DC70           index = row*3 + col (11 = cancel cell), TestFlagF2F0, TeleportAreaTable 30:DC58;
//                            same area -> exit; else area_id/entry $40, SpendMp($14) for sub_state $14, the warp-out
//                            (64 frames), SubstAreaEnter, the warp-in (64 frames), StatRecalc, sprite pass
import { R } from '../ram.js';
import { cpuRd, setR6 } from '../prg.js';
import { playSound } from '../playsound.js';
import { readPadsMenu } from '../pads.js';
import { SPIN, waitFrame, paletteBuildRom, statRecalc } from './death.js';
import { textWindowOpen, textPrintBox, oamClipTop } from './talk.js';
import { farSpriteDraw, substAreaEnter } from './areaenter.js';
import { ppuQueueBlock } from './pause.js';
import { spendMp } from '../obj/player.js';

const OAM = 0x0200, AREA_TABLE = 0xDC58;
const META = 0x0300, META_S01 = 0x0301, FLAGS_S01 = 0x0381, ACTIVE = 0x04A0, ANIM_S01 = 0x04E1, OVERLAY_S01 = 0x0581;

/** 30:C19F: OAM entry 0 = $F0 x 4 */
function hideOam0(m) { for (let i = 0; i < 4; i++) m.wr(OAM + i, 0xF0); }

/** TestFlagF2F0 30:DD58: flag F2F0 + a + 5 ($64DE + (a+5)>>3, BitMask[(a+5)&7]); returns the AND result */
export function testFlagF2F0(m, a) {
  const n = (a + 5) & 0xFF;
  return m.rd(0x64DE + (n >> 3)) & cpuRd(m, 0xC000 + (n & 7));
}

/** TeleportMenuLocks 30:DD36: $6000-$6012 = $10, then PpuQueueBlock($20 + i) for every locked town i = 10..0 */
function* teleportMenuLocks(m) {
  for (let x = 0x12; x >= 0; x--) m.wr(0x6000 + x, 0x10);
  m.wr(0x11, 0x0A);
  do {
    const i = m.rd(0x11);
    if (!testFlagF2F0(m, i)) yield* ppuQueueBlock(m, (i + 0x20) & 0xFF);
    m.wr(0x11, (m.rd(0x11) - 1) & 0xFF);
  } while (!(m.rd(0x11) & 0x80));
}

/** TeleportMenuExit 30:DC64 */
function menuExit(m) {
  hideOam0(m);
  m.wr(R.irq_mode, 0);
  m.wr(R.sub_state, 8);
}

/** cursor moves 30:DBF4 / DC0C / DC26 / DC3E: [OAM byte, temp, step, count, wrap] */
const MOVES = {
  4: [0, 0x21, -0x10, 4, 0x40],                         // UP
  5: [0, 0x21, 0x10, 4, 0x40],                          // DOWN
  6: [3, 0x20, -0x50, 3, 0xF0],                         // LEFT
  7: [3, 0x20, 0x50, 3, 0xF0],                          // RIGHT
};

/** PadNewButtonIndex 30:C210: index 0-7 (A B SEL START U D L R) of the single button in pad_new, else -1 */
export function padNewButtonIndex(m) {
  const p = m.rd(R.pad_new);
  for (let y = 7; y >= 0; y--) if (p === cpuRd(m, 0xC222 + y)) return y;
  return -1;
}

/** SubstTeleportMenu 30:DB28. yIn = Y at the dispatch (PlaySound($4E) saves it). */
export function* substTeleportMenu(m, yIn = m.rd(0x010F)) {
  if (m.rd(0x03C1) === 0) { m.wr(R.sub_state, 8); return; }
  if (m.rd(0x07D7) !== 0 || (m.rd(R.area_id) & 0xF8) === 0x58 || (m.rd(0x0710) & 0x0F) === 4) {
    playSound(m, 0x4E, yIn);
    m.wr(R.sub_state, 8);
    return;
  }
  m.wr(0x06C0, 0);                                      // DB4F orbit_level
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);                                // JSR 26:8C0E
  m.wr(R.irq_mode, 0);
  m.wr(0x07DE, 0x00);                                   // msg 33.0 (town list)
  m.wr(0x07DF, 0x21);
  setR6(m, 0x14);
  yield* textWindowOpen(m);                             // 20:8500
  yield* textPrintBox(m);                               // 20:8528
  yield* teleportMenuLocks(m);                          // 30:DD36
  yield* waitFrame(m);
  m.wr(R.chr_r0_alt, 0x3C);
  m.wr(R.chr_r0_alt + 1, 0x38);
  m.wr(R.chr_r0_alt + 2, 0x5C);
  m.wr(OAM + 3, 0x10);
  m.wr(0x10, m.rd(R.cam_y_lo) & 7);
  m.wr(OAM, (0x1C - m.rd(0x10)) & 0xFF);
  m.wr(OAM + 2, 0);
  m.wr(0x20, 0);
  m.wr(0x21, 0);
  farSpriteDraw(m);
  m.wr(R.irq_mode, 1);
  for (;;) {                                            // DBAC
    m.wr(OAM + 1, (m.rd(0x07FF) & 0x08) ? 0x3C : 0x3F);
    readPadsMenu(m);
    farSpriteDraw(m);
    oamClipTop(m);
    yield* waitFrame(m);
    const k = padNewButtonIndex(m);
    if (k < 0) continue;
    if (k >= 1 && k <= 3) { menuExit(m); return; }      // B / SELECT / START
    if (k >= 4) {
      const [o, t, step, n, wrap] = MOVES[k];
      let a = m.rd(OAM + o) + step;
      let y = m.rd(t) + (step < 0 ? -1 : 1);
      if (y < 0) { y = n - 1; a += wrap; }
      else if (y === n) { y = 0; a -= wrap; }
      m.wr(OAM + o, a & 0xFF);
      m.wr(t, y);
      playSound(m, 0x4B, y);                            // DBEC
      continue;
    }
    // TeleportMenuPick 30:DC70
    const idx = (m.rd(0x21) * 3 + m.rd(0x20)) & 0xFF;
    m.wr(0x22, idx);
    if (idx === 0x0B) continue;                         // the cancel cell
    if (!testFlagF2F0(m, idx)) continue;
    const area = cpuRd(m, AREA_TABLE + idx);
    if (area === m.rd(R.area_id)) { menuExit(m); return; }
    m.wr(R.area_id, area);
    m.wr(0x6D, 0x40);                                   // area_entry $40 (SubstAreaEnter then runs SubstPlay at once)
    if (m.rd(R.sub_state) === 0x14) {                   // magic 4 (not the Warp Boots' sub_state 6)
      setR6(m, 0x1A);
      yield* spendMp(m, 0x14, 0);                       // X = 0 (index 0 * 2)
    }
    // DCA9: LDY $56 / DEY / BNE = spin until the IRQ chain has reached handler 1 (the HUD split). The pick runs
    // in vblank, so on hardware the wait ends in the next frame (tools.emu clears the objects one frame after
    // the A press). The port runs its splits only between main-loop steps and $56 is never 1 here, so this is
    // one SPIN (frame_ready kept) instead of a loop on $56.
    yield SPIN;
    yield* warp(m);
    return;
  }
}

/** 30:DCAE-DD33: warp-out, SubstAreaEnter, warp-in */
function* warp(m) {
  hideOam0(m);
  const meta = m.rd(META_S01), ovl = m.rd(OVERLAY_S01);
  m.wr(META_S01, 0xCE);
  m.wr(OVERLAY_S01, 0x4C);
  m.wr(META, 0x4C);
  m.wr(ANIM_S01, 0x3F);
  m.wr(0x0BB1, 0);
  for (let x = 4; x < 0x20; x++) m.wr(ACTIVE + x, 0);
  playSound(m, 0x23, 0);                                // Y = 0 after the DCA9 loop
  m.wr(0x07F2, 0x73);                                   // chr_r2_play
  m.wr(R.irq_mode, 0);
  do {                                                  // DCE8: 64 frames
    farSpriteDraw(m);
    yield* waitFrame(m);
    m.wr(ANIM_S01, (m.rd(ANIM_S01) - 1) & 0xFF);
  } while (!(m.rd(ANIM_S01) & 0x80));
  m.wr(META_S01, 0x4C);
  m.wr(META, 0x4C);
  farSpriteDraw(m);
  yield* substAreaEnter(m);                             // 30:CA2E (entry $40: runs one SubstPlay pass too)
  m.wr(FLAGS_S01, m.rd(FLAGS_S01) & 0xFB);
  m.wr(OVERLAY_S01, 0x4C);
  m.wr(META_S01, 0xCE);
  m.wr(ANIM_S01, 0xC0);
  playSound(m, 0x66, m.rd(0x010F));                     // Y after SubstAreaEnter: not tracked (approximation)
  do {                                                  // DD1D: 64 frames
    farSpriteDraw(m);
    yield* waitFrame(m);
    m.wr(ANIM_S01, (m.rd(ANIM_S01) + 1) & 0xFF);
  } while (m.rd(ANIM_S01) !== 0);
  m.wr(OVERLAY_S01, ovl);
  m.wr(META_S01, meta);
  yield* statRecalc(m);
  farSpriteDraw(m);                                     // JMP FarCall_B14_800E
}
