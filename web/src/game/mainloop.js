// re-c web port — MainLoop 30:C900, MainStateTable 30:C91F, NMI 31:F3B6, NmiModeDispatch 31:F6AD.
// Ph7 web 2/8: the frame skeleton is ported line by line; the six main states are stubs that later
// tasks replace (states/*.js). See docs/architecture_web.md §2.1 / §4.
//
// Multi-frame routines: a state handler may be a generator (function*). It `yield`s where the ROM waits
// for the next NMI; step() resumes it on the next frame. A plain function = one frame of work.
import { R } from './ram.js';
import { paletteUpload } from './palette.js';
import { cpuRd } from './prg.js';
import { mainState2, mainState3 } from './states/title.js';
import { mainState1 } from './states/play.js';
import { SPIN, NO_NMI } from './states/death.js';
import { mainState4, mainState5 } from './states/ending.js';
import { mainStateNewGame, mainStateContinue } from './states/areaenter.js';

// ---------------------------------------------------------------- main states
// Each entry: ROM address, name, handler(m) (MainStateTable 30:C91F, 9 entries; s108: all ported).

export const MAIN_STATES = [
  // state 0: MainStateNewGame -> main_state 1 / sub_state 0 (states/areaenter.js, s108)
  { addr: '30:C957', name: 'state0_newgame', fn: mainStateNewGame },
  // state 1: MainStateGameplay -> SubstateHandlerTable[$41] (states/play.js, s88: only sub_state 3 PlayerDeath)
  { addr: '30:CAB6', name: 'state1_gameplay', fn: mainState1 },
  // state 2: MainState2Init (18:9FCC -> 19:A000) ends with INC main_state (states/title.js, s82)
  { addr: '30:C931', name: 'state2_init', fn: mainState2 },
  // state 3: ReadPadsMenu, then JMP MainState3 19:A015 (title / menus / name entry; states/title.js, s82)
  { addr: '30:C939', name: 'state3_title', fn: mainState3 },
  // states 4 / 5: EndingInit 17:A000 / EndingTick 17:A100 (states/ending.js, s90)
  { addr: '30:C944', name: 'state4_ending_init', fn: mainState4 },
  { addr: '30:C94C', name: 'state5_ending', fn: mainState5 },
  null, null,                                       // 6 / 7: $0000 in MainStateTable (30:C92B / C92D)
  // state 8: MainStateContinue (title CONTINUE) -> main_state 1 / sub_state 1 (states/areaenter.js, s108)
  { addr: '30:C9DA', name: 'state8_continue', fn: mainStateContinue },
];

/** MainLoop body for one frame: runs only when the NMI has set frame_ready ($09 != 0). */
export function mainStep(m) {
  if (m.noNmiResume) m.noNmiResume = false;         // s107: the last frame's NMI was lost (NO_NMI)
  else if (m.rd(R.frame_ready) === 0) return false;
  let r = null;
  if (m.pending) {                                  // resume a multi-frame routine
    r = m.pending.next();
  } else {
    const st = MAIN_STATES[m.rd(R.main_state)];
    if (!st) throw new Error(`main_state ${m.rd(R.main_state)} out of range`);
    const g = st.fn(m);
    if (g && typeof g.next === 'function') { m.pending = g; r = g.next(); }
  }
  if (r) {
    if (r.done) m.pending = null;
    else if (r.value === SPIN) return true;         // ROM spin loop (queue wait): frame_ready stays set
    else if (r.value === NO_NMI) { m.skipNmi = true; m.noNmiResume = true; return true; }
  }
  if (m.overrun) {                                  // s114: CPU overrun (lag frame), see Machine.runFrame
    m.overrun = false;
    m.overrunNmi = true;                            // the NMI comes while frame_ready is still set
    return true;
  }
  m.wr(R.frame_ready, 0);                           // 30:C919 (wait loops inside routines clear it too)
  return true;
}

// ---------------------------------------------------------------- NMI
/** NMI 31:F3B6 */
export function nmiWork(m) {
  m.ppu.readStatus();                               // F3B7 LDA PPUSTATUS: resets w (s108: IrqHandler1Hud leaves w = 1)
  if (m.rd(R.nmi_disable)) return;                  // F3BA
  if (m.rd(R.nmi_busy_flag)) { m.wr(R.nmi_missed_flag, 1); return; }   // F3BE
  if (m.rd(R.frame_ready) === 0) {                  // F3CC: main loop finished -> DMA + scroll copy
    m.ppu.oamAddr = 0;                              // F3D0 STA OAMADDR (A = 0)
    m.ppu.oamDma(m.ram.page(2));                    // F3D5 $4014 <- 2
    const src = (m.rd(R.irq_mode) === 9 || m.rd(R.irq_mode) === 7)
      ? [0x83, 0xA3, 0xC3, 0xE3]                    // F3F6: object slot $13 x lo/hi, y lo/hi
      : [R.scroll_shadow0, R.cam_x_hi, R.cam_y_lo, R.cam_y_hi];
    for (let i = 0; i < 4; i++) m.wr(R.scroll_x_irq + i, m.rd(src[i]));
  }
  m.ppuQueueFlush(m);                               // F40A PpuQueueFlush 30:C67D (world/ppuqueue.js, 4/8)
  paletteUpload(m, 0);                              // F40F
  m.ppu.writeReg(1, m.rd(R.ppumask_shadow));        // F412
  nmiModeDispatch(m);                               // F417
  m.wr(R.frame_ready, m.rd(R.frame_ready) + 1);     // F41A
}

// ---------------------------------------------------------------- NMI-mode routines + IRQ split schedule
/** SetIrqHandler 31:F424: $56 = index (IrqHandlerTable 31:F433). The port keeps a split list instead of $54/$55. */
export function setIrqHandler(m, i, latch) {
  irqIndex(m, i);
  m.irqSplits.push({ handler: i, latch });
}

/** SetIrqHandler 31:F424 RAM side: $56 = index, irq_vector $54/$55 = IrqHandlerTable 31:F433[index]
 *  (s208: the port wrote only $56 before; the IRQ entry 31:F443 does JMP ($0054)) */
export function irqIndex(m, i) {
  m.wr(0x56, i);
  m.wr(0x54, cpuRd(m, 0xF433 + i * 2));
  m.wr(0x55, cpuRd(m, 0xF434 + i * 2));
}

/** 31:EB6D: PPUSCROLL <- $07D8, $07DA; ppuctrl_shadow nametable bits = ($07DB b0) << 1 | ($07D9 b0) */
export function ppuScrollWrite(m) {
  const ppu = m.ppu;
  ppu.writeReg(5, m.rd(R.scroll_x_irq));
  ppu.writeReg(5, m.rd(R.scroll_x_irq + 2));
  const nt = (((m.rd(R.scroll_x_irq + 3) << 1) | (m.rd(R.scroll_x_irq + 1) & 1)) & 3);
  m.wr(0x52, nt);                                   // EB83 STA $52 (temp; s208: was not written)
  const ctrl = (m.rd(R.ppuctrl_shadow) & 0xFC) | nt;
  m.wr(R.ppuctrl_shadow, ctrl);
  ppu.writeReg(0, ctrl);
}

/** 31:F71B: ppuScrollWrite with $07DB temporarily = ($07DA >= $E8) */
export function nmiScrollSet(m) {
  const yHi = m.rd(R.scroll_x_irq + 3);
  m.wr(R.scroll_x_irq + 3, m.rd(R.scroll_x_irq + 2) >= 0xE8 ? 1 : 0);
  ppuScrollWrite(m);
  m.wr(R.scroll_x_irq + 3, yHi);
}

/** ApplyChrPlay 31:F883: MMC3 R0-R5 <- $07F0-$07F5 */
export function applyChrPlay(m) {
  for (let r = 0; r < 6; r++) m.ppu.setChrReg(r, m.rd(R.chr_r0_play + r));
}

/** NmiMode0 31:F6D6 (irq_mode 0 and 7) */
function nmiMode0(m) {
  if ((m.rd(R.ppumask_shadow) & 0x18) === 0) { m.soundUpdate(m); return; }   // F6DC JMP SoundUpdate
  nmiScrollSet(m);                                  // F6DF
  applyChrPlay(m);                                  // F6E2
  let a;
  if (!(m.rd(R.ppuctrl_shadow) & 2)) {              // F6E5 LSR LSR -> C = b1 (lower nametable)
    a = (0xEF - m.rd(R.scroll_x_irq + 2)) & 0xFF;   // lines until the 240-row wrap
    if (a < 0xBC) {                                 // F6F1: wrap above the HUD -> handler 0 first
      m.wr(R.zp_57, a);
      setIrqHandler(m, 0, a);                       // F6F5-F702: latch a, IRQ on, SetIrqHandler(0)
      return;                                       // handler 0 (31:F455) re-arms handler 1 with $BE - a
    }
  }
  setIrqHandler(m, 1, 0xBE);                        // F705: HUD split, latch $BE - $57 with $57 = 0
  m.wr(R.zp_57, 0xBE);
}

/** ApplyChrAlt 31:F64F: MMC3 R0-R5 <- $58-$5D */
export function applyChrAlt(m) {
  for (let r = 0; r < 6; r++) m.ppu.setChrReg(r, m.rd(R.chr_r0_alt + r));
}

/** NmiMode2 31:F751 (irq_mode 2, pause / status screen): PPUCTRL nametable 2 ($2800, the shadow is not
 *  changed), scroll 0/0, ApplyChrAlt; rendering on -> NmiMode0HudIrq 31:F705 (handler 1 at $BE: the HUD
 *  below the window), else SoundUpdate. Like mode 0 with rendering on, no SoundUpdate this frame. */
function nmiMode2(m) {
  m.ppu.writeReg(0, (m.rd(R.ppuctrl_shadow) & 0xFC) | 2);
  m.ppu.writeReg(5, 0);
  m.ppu.writeReg(5, 0);
  applyChrAlt(m);
  if ((m.rd(R.ppumask_shadow) & 0x18) === 0) { m.soundUpdate(m); return; }
  setIrqHandler(m, 1, 0xBE);                        // F705: latch $BE - $57 ($57 = 0 here)
  m.wr(R.zp_57, 0xBE);
}

/** NmiMode3 31:F771 (title / menus, no splits): PPUMASK <- $01, scroll, CHR R0-R5, MMC3 IRQ off, SoundUpdate */
function nmiMode3(m) {
  m.ppu.writeReg(1, m.rd(R.ppumask_shadow));        // F771
  ppuScrollWrite(m);                                // F776
  applyChrPlay(m);                                  // F779
  m.irqSplits = [];                                 // F77C STA MMC3_IRQOFF
  m.soundUpdate(m);                                 // F77F JMP SoundUpdate
}

/** NmiMode6 31:F7DB (irq_mode 6, the bank 16 equipment menu): PPUCTRL <- shadow, scroll 0/0, ApplyChrAlt,
 *  IRQ handler 7 (IrqMenuChrSplit) at latch $77, $57 = $77 (s98) */
function nmiMode6(m) {
  m.ppu.writeReg(0, m.rd(R.ppuctrl_shadow));
  m.ppu.writeReg(5, 0);
  m.ppu.writeReg(5, 0);
  applyChrAlt(m);
  setIrqHandler(m, 7, 0x77);
  m.wr(R.zp_57, 0x77);
}

/** IrqMenuChrSplit 31:F61A (IRQ handler 7, end of line $77): CHR R0/R1 <- $5E/$5F (the lower part of the
 *  menu uses the font set), then handler 1 (HUD, unported: marker only) at $BC - $57 lines on (s98) */
export function irqMenuChrSplit(m) {
  m.ppu.setChrReg(0, m.rd(0x5E));
  m.ppu.setChrReg(1, m.rd(0x5F));
  irqIndex(m, 1);                                   // SetIrqHandler(1)
  const line = 0x78 + ((0xBC - m.rd(R.zp_57)) & 0xFF) + 1;
  if (line < 240 && !m.ppu.splits.has(line)) m.ppu.splits.set(line, () => m.irqHandlers[1](m));
}

/** NmiMode1 31:F731 (irq_mode 1 and 9, the message window / bank 16 shop text screens, s105): NmiScrollSet,
 *  ApplyChrPlay, IRQ handler 2 at latch $57 = ((07DA & 7) ^ 7) + 7 (the first tile-row boundary >= line 7) */
function nmiMode1(m) {
  nmiScrollSet(m);
  applyChrPlay(m);
  const latch = (((m.rd(R.scroll_x_irq + 2) & 7) ^ 7) + 7) & 0xFF;
  m.wr(R.zp_57, latch);
  setIrqHandler(m, 2, latch);
}

/** s115: handlers 2 / 3 write their MMC3 reload 2 lines later than handler 0 does, relative to their split line
 *  (tools/emu/irq_split_probe.py at f2600: H2's split shows from line 11 but it writes $C000/$C001 at line 11
 *  dot 273-285, after that line's counter clock, so the count starts at line 12; H3's split shows from line 86
 *  (2nd PPUADDR write at line 85 dot 266) and it writes the reload at line 87 dot 4). So their chained split
 *  lands N + 3 lines after their own split line, not N + 1. */
const MSG_LATE = 2;

/** s209: an IRQ handler's ApplyChrAlt / ApplyChrPlay (R0-R5 from RAM at `base`) on the line its hook precedes:
 *  register r switches at dot0 + r * step (tools/emu/split_probe.py --mapper, the dot where tools.emu's
 *  unsynced PPU stands = the first fetch the new bank serves). Only R0/R1 (BG $0000) change this line's
 *  pixels; R2-R5 are queued too so the writes stay in order. */
function chrTimed(m, base, dot0, step) {
  const p = m.ppu;
  for (let r = 0; r < 6; r++) {
    const v = m.rd(base + r);
    p.timed(dot0 + r * step, () => p.setChrReg(r, v));
  }
}

/** A chained MMC3 IRQ: reload N written inside the handler of line `line` fires N + 1 lines later */
function chainSplit(m, line, n, handler) {
  const at = line + n + 1;
  if (at < 240 && !m.ppu.splits.has(at)) m.ppu.splits.set(at, () => m.irqHandlers[handler](m, at));
}

/** IrqMsgWindowTop 31:F4E5 (IRQ handler 2, s105): PPUADDR $2800 + X 0 (nametable 2 = the window), ApplyChrAlt;
 *  H3 after $48 lines, $57 += $46; resume row = (($07DA + 8 + $57) wrapped past 240 with +$11) >> 3 ->
 *  $5E/$5F = NtRowAddrTable 31:F5A1[row] hi / lo | ($07D8 >> 3); then $57 += 8 */
export function irqMsgWindowTop(m, line) {
  const p = m.ppu;
  p.readStatus();
  p.writeReg(6, 0x28); p.writeReg(6, 0x00); p.writeReg(5, 0x00);   // previous line's hblank (dots 272-315)
  // ApplyChrAlt: R0 at dot 10-22 of this line (2nd PPUADDR at the line before, dot 290-303, - 281), the
  // middle 13; then every 39 dots (talk16.state, 200 frames)
  chrTimed(m, R.chr_r0_alt, 13, 39);
  chainSplit(m, line, 0x48 + MSG_LATE, 3);
  irqIndex(m, 3);                                   // F515 SetIrqHandler(3) (s208: $56 / $54-$55)
  let z = (m.rd(R.zp_57) + 0x46) & 0xFF;
  m.wr(R.zp_57, z);
  let a = m.rd(R.scroll_x_irq + 2) + 8;
  a = (a & 0xFF) + z;                               // CLC ADC #8 / CLC ADC $57 (carry of the 2nd add)
  if (a > 0xFF || (a & 0xFF) >= 0xF0) a = (a & 0xFF) + 0x11;   // BCS / CMP #$F0: ADC #$10 with C = 1
  const y = ((a & 0xF8) >> 2) & 0xFF;
  m.wr(R.zp_57, (z + 8) & 0xFF);
  m.wr(0x5E, cpuRd(m, 0xF5A2 + y));
  m.wr(0x5F, (m.rd(R.scroll_x_irq) >> 3) | cpuRd(m, 0xF5A1 + y));
}

/** IrqMsgWindowEnd 31:F54B (IRQ handler 3, s105): PPUADDR $5E/$5F + X = $07D8 (back to the playfield row),
 *  ApplyChrPlay; then H0 (wrap) at ($EF - $07DA) - $57 if that row is above the HUD and below $57, else H1
 *  (IrqMsgWindowEndHud 31:F58B, the HUD) at $BE - $57. H0 / H1 are markers (unported). */
export function irqMsgWindowEnd(m, line) {
  const p = m.ppu;
  p.readStatus();
  p.writeReg(6, m.rd(0x5E)); p.writeReg(6, m.rd(0x5F)); p.writeReg(5, m.rd(R.scroll_x_irq));   // hblank
  // ApplyChrPlay: R0 at dot 0-13 (2nd PPUADDR at the line before, dot 266-279, - 266), the middle 4;
  // then every 42 dots
  chrTimed(m, R.chr_r0_play, 4, 42);
  const a = (0xEF - m.rd(R.scroll_x_irq + 2)) & 0xFF;
  const z = m.rd(R.zp_57);
  if (a < 0xBC && a >= z) {
    chainSplit(m, line, ((a - z) & 0xFF) + MSG_LATE, 0);
    m.wr(R.zp_57, a);
    irqIndex(m, 0);
  } else {
    chainSplit(m, line, ((0xBE - z) & 0xFF) + MSG_LATE, 1);
    irqIndex(m, 1);
  }
}

/** IrqHandler0 31:F455 (s108, the playfield's 240-line wrap): PPUADDR $00:(07D8 >> 3) = nametable 0 row 0,
 *  fine X = $07D8; then handler 1 (HUD) after $BC - $57 lines */
export function irqWrapSplit(m, line) {
  const p = m.ppu;
  p.readStatus();
  p.writeReg(6, 0x00);
  p.writeReg(6, m.rd(R.scroll_x_irq) >> 3);
  p.writeReg(5, m.rd(R.scroll_x_irq));
  chainSplit(m, line, (0xBC - m.rd(R.zp_57)) & 0xFF, 1);
  irqIndex(m, 1);                                   // SetIrqHandler(1)
}

/** IrqHandler1Hud 31:F488 (s108): IRQ off, PPUMASK sprites off (BG kept), PPUADDR $2B00 + X 0 (the HUD rows of
 *  nametable 2), CHR R0/R1 = $3C/$38, then the gameplay SoundUpdate under nmi_busy_flag (an NMI meanwhile only
 *  sets nmi_missed_flag -> NmiModeDispatch here) */
export function irqHud(m) {
  const p = m.ppu;
  p.readStatus();
  p.writeReg(1, (m.rd(R.ppumask_shadow) & 0x0E) | 0x08);   // line 190 dot 331-338 (hblank): whole next line
  // s209: the rest lands inside the next line (tools/emu/split_probe.py --hist, 299 frames: 2nd PPUADDR at
  // dot 38/39/41/42/44/45, one per CPU cycle of IRQ latency, about evenly; the port takes the middle, 41).
  // Until R0/R1 switch, the PPU fetches the HUD nametable row through the playfield CHR: the line-191 stripe
  // hardware shows too (triage (g)). The first PPUADDR write (dot 23) only sets t, so it goes with the second.
  p.timed(HUD_DOT_ADDR, () => { p.readStatus(); p.writeReg(6, 0x2B); p.writeReg(6, 0x00); });
  p.timed(HUD_DOT_ADDR + 18, () => p.writeReg(5, 0x00));       // STA PPUSCROLL 31:F4AF (+6 cycles)
  // STA MMC3_BANKDATA 31:F4B9 / F4C2 write at +54 / +90 dots, but tools.emu's mapper writes do not sync the
  // PPU (only $2000-$2007 / DMA do), so a CHR switch already applies from the dot its STA starts (-9)
  p.timed(HUD_DOT_ADDR + 45, () => p.setChrReg(0, 0x3C));
  p.timed(HUD_DOT_ADDR + 81, () => p.setChrReg(1, 0x38));
  const start = musicStartId(m);
  m.wr(R.nmi_busy_flag, 1);
  m.soundUpdate(m);
  // s174: a SoundUpdate that starts a track runs its first tick on all 4 channels. When that is longer than
  // the time to line 241, the NMI comes while nmi_busy_flag is set: it only sets nmi_missed_flag (no DMA, no
  // PpuQueueFlush / PaletteUpload, no frame_ready increment) and this handler then runs NmiModeDispatch
  // alone. Machine.runFrame does that in place of nmiWork (hudNmiLost).
  if (start >= 0 && m.rd(0x0111) === start && MUSIC_START_CYCLES[start] > HUD_SOUND_BUDGET) {
    m.hudNmiLost = true;
    return;
  }
  m.wr(R.nmi_busy_flag, 0);
  if (m.rd(R.nmi_missed_flag)) nmiModeDispatch(m);
  m.wr(R.nmi_missed_flag, 0);
}

/** s209: PPU dot of IrqHandler1Hud's 2nd PPUADDR write (31:F4AA) on the line after the IRQ line */
const HUD_DOT_ADDR = 41;

/** s174 (tools/emu/sound_cost_probe.py): CPU cycles of the SoundUpdate call that starts music id $00-$1F
 *  (SndCommand + the first SndTick, from SndInit, NMI / IRQ blocked). */
const MUSIC_START_CYCLES = [
  2677, 6010, 414, 4512, 5297, 414, 5614, 5764, 5822, 5016, 4305, 5683, 4959, 4171, 4555, 4715,
  414, 5097, 6228, 4443, 5668, 6197, 3941, 5012, 4981, 5487, 5910, 4579, 6091, 5033, 5341, 4869,
];
/** IrqHandler1Hud sets nmi_busy_flag at about L191 dot 172 (31:F4CF, area $5F f2664-2673); the NMI is at
 *  L241 dot 1: (50 * 341 - 171) / 3 = 5626 CPU cycles. Music $14 (5668 alone, 5718 in the scene) loses it. */
const HUD_SOUND_BUDGET = 5626;

/** The music id the next SoundUpdate starts (its F832 resume or F843 queue path, id < $20), else -1 */
function musicStartId(m) {
  const pend = m.rd(0x0102);
  if (pend & 0x80) {
    const resume = (m.rd(0xFE) & 0x80)
      ? ((m.rd(0x0168) | m.rd(0x0169) | m.rd(0x016A) | m.rd(0x016B)) & 0x0F) === 0
      : m.rd(0x0111) === 0xFF;
    if (resume) return (pend & 0x7F) < 0x20 ? pend & 0x7F : -1;
  }
  if (m.rd(0x0101) === 0 && m.rd(0x0100) !== 0 && m.rd(0x0103) < 0x20) return m.rd(0x0103);
  return -1;
}

/** s174: the NMI that found nmi_busy_flag set (31:F3BE): PPUSTATUS read, nmi_missed_flag; then the end of
 *  IrqHandler1Hud: busy off, NmiModeDispatch, missed flag off */
export function nmiLostToHud(m) {
  m.ppu.readStatus();
  if (m.rd(R.nmi_disable)) { m.wr(R.nmi_busy_flag, 0); return; }
  m.wr(R.nmi_busy_flag, 0);
  nmiModeDispatch(m);
  m.wr(R.nmi_missed_flag, 0);
}

/** NmiMode5 31:F7C5 (irq_mode 5, the cold-boot intro; in the port only the f214 NMI, s106): PpuScrollWrite,
 *  ApplyChrPlay, IRQ handler 6 at latch $58 */
function nmiMode5(m) {
  ppuScrollWrite(m);
  applyChrPlay(m);
  setIrqHandler(m, 6, m.rd(R.chr_r0_alt));
}

/** IrqHandler6 31:F691 (s106): (DelayX $07FE) IRQ off, PPUADDR <- $59:$5A (the intro: $2800 at line 137) */
export function irqIntroSplit(m) {
  const p = m.ppu;
  p.readStatus();
  p.writeReg(6, m.rd(R.chr_r0_alt + 1));
  p.writeReg(6, m.rd(R.chr_r0_alt + 2));
}

/** NmiMode4 31:F782 (irq_mode 4, s208; dead in the ROM: only the RTS-stubbed shop-door routines 31:FE01/FE18
 *  set it, see "IRQ handlers in full"): PpuScrollWrite, ApplyChrPlay; $58 = 0 -> ppumask_shadow BG bit 3 off
 *  + IRQ off; else $59 = NmiMode4R1Table 31:F7B5[$58] (row count), $5A = 0 (first NT row), handler 4 at latch 4.
 *  The STA $F451 at F79C is an MMC3 $E001 write (IRQ enable), which the following STA $E001 repeats. */
function nmiMode4(m) {
  ppuScrollWrite(m);                                // F782
  applyChrPlay(m);                                  // F785
  const n = m.rd(R.chr_r0_alt);                     // F788 LDA $58
  if (n === 0) {
    m.wr(R.ppumask_shadow, m.rd(R.ppumask_shadow) & 0xF7);   // F7AA-F7AE (STA $0001 absolute)
    m.irqSplits = [];                               // F7B1 STA MMC3_IRQOFF
    return;
  }
  m.wr(R.chr_r0_alt + 1, cpuRd(m, 0xF7B5 + n));     // F78E $59
  m.wr(R.chr_r0_alt + 2, 0);                        // F795 $5A
  setIrqHandler(m, 4, 4);                           // F797-F7A7: latch 4, IRQ on, SetIrqHandler(4)
}

/** IrqRowStretch 31:F5E1 (IRQ handler 4, s208, dead like NmiMode4): (DelayX $07FE) PPUADDR <-
 *  NtRowAddrTable 31:F5A1[$5A] (hi at F5A2, lo at F5A1: nametable-0 row $5A, X 0), $5A++, $59--; $59 < 0 ->
 *  falls into handler 5, else the same handler again (reload $58, IRQ stays on). s208 (irq_split_probe on a
 *  state forced into mode 4 with $58 = 8): the IRQ comes at line 3 dot ~335, DelayX takes the rest of line 3,
 *  the 2nd PPUADDR write lands at line 4 dot ~297 (new row from line 5) and the reload at line 5 dot ~25; the
 *  next 2nd write is at line 14, so the rows change every $58 + 2 lines = chainSplit with N + 1 (ROW_LATE). */
export function irqRowStretch(m, line) {
  const p = m.ppu;
  p.readStatus();                                   // F5E7
  const row = m.rd(R.chr_r0_alt + 2);
  const y = (row << 1) & 0xFF;                      // F5EA ASL TAY
  p.writeReg(6, cpuRd(m, 0xF5A2 + y));
  p.writeReg(6, cpuRd(m, 0xF5A1 + y));
  m.wr(R.chr_r0_alt + 2, (row + 1) & 0xFF);         // F5FA INC $5A
  const left = (m.rd(R.chr_r0_alt + 1) - 1) & 0xFF;
  m.wr(R.chr_r0_alt + 1, left);                     // F5FC DEC $59
  if (left & 0x80) { irqOffSound(m); return; }      // F5FE BMI IrqOffSound
  chainSplit(m, line, m.rd(R.chr_r0_alt) + ROW_LATE, 4);   // F600-F605 latch $58 + reload
}
const ROW_LATE = 1;

/** IrqOffSound 31:F60E (IRQ handler 5, s208; only reached from F5FE): MMC3 IRQ off, SoundUpdate. A split
 *  already scheduled further down could only come from handler 4 itself, which does not chain on this path. */
export function irqOffSound(m) {
  m.soundUpdate(m);                                 // F611
}

/** JmpTable31_F6C2: irq_mode -> NMI routine (every non-null entry ported since s208) */
export const NMI_MODES = [
  nmiMode0, nmiMode1, nmiMode2, nmiMode3,
  nmiMode4, nmiMode5, nmiMode6, nmiMode0,
  null, nmiMode1,
];

/** NmiModeDispatch 31:F6AD: $57 = 0, JMP (JmpTable31_F6C2[irq_mode]); ASL drops b7 (the "skip
 *  PaletteUpload" flag set by PauseScreen / the bank 16 menus), so $82 runs mode 2 */
export function nmiModeDispatch(m) {
  m.wr(R.zp_57, 0);
  m.irqSplits = [];
  const mode = m.rd(R.irq_mode) & 0x7F;
  m.wr(0x52, cpuRd(m, 0xF6C2 + mode * 2));          // F6B5-F6BD: $52/$53 = the JMP ($0052) vector (s208)
  m.wr(0x53, cpuRd(m, 0xF6C3 + mode * 2));
  const fn = NMI_MODES[mode];
  if (!fn) throw new Error(`irq_mode ${m.rd(R.irq_mode)}: null NMI-mode entry`);
  fn(m);
}
