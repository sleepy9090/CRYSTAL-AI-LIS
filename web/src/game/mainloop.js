// re-c web port — MainLoop 30:C900, MainStateTable 30:C91F, NMI 31:F3B6, NmiModeDispatch 31:F6AD.
// Ph7 web 2/8: the frame skeleton is ported line by line; the six main states are stubs that later
// tasks replace (states/*.js). See docs/architecture_web.md §2.1 / §4.
//
// Multi-frame routines: a state handler may be a generator (function*). It `yield`s where the ROM waits
// for the next NMI; step() resumes it on the next frame. A plain function = one frame of work.
import { R } from './ram.js';
import { paletteUpload } from './palette.js';
import { mainState2, mainState3 } from './states/title.js';
import { mainState1 } from './states/play.js';
import { SPIN } from './states/death.js';
import { mainState4, mainState5 } from './states/ending.js';

// ---------------------------------------------------------------- main states (stubs)
// Each entry: ROM address, name, handler(m). Stub bodies only do what REVERSE.md documents for the
// state's first instructions; everything else is TODO for Ph7 web 4/8-7/8.
const stub = (name) => (m) => { m.stubHits[name] = (m.stubHits[name] || 0) + 1; };

export const MAIN_STATES = [
  { addr: '30:C957', name: 'state0_newgame', fn: stub('state0_newgame') },
  // state 1: MainStateGameplay -> SubstateHandlerTable[$41] (states/play.js, s88: only sub_state 3 PlayerDeath)
  { addr: '30:CAB6', name: 'state1_gameplay', fn: mainState1 },
  // state 2: MainState2Init (18:9FCC -> 19:A000) ends with INC main_state (states/title.js, s82)
  { addr: '30:C931', name: 'state2_init', fn: mainState2 },
  // state 3: ReadPadsMenu, then JMP MainState3 19:A015 (title / menus / name entry; states/title.js, s82)
  { addr: '30:C939', name: 'state3_title', fn: mainState3 },
  // states 4 / 5: EndingInit 17:A000 / EndingTick 17:A100 (states/ending.js, s90)
  { addr: '30:C944', name: 'state4_ending_init', fn: mainState4 },
  { addr: '30:C94C', name: 'state5_ending', fn: mainState5 },
];

/** MainLoop body for one frame: runs only when the NMI has set frame_ready ($09 != 0). */
export function mainStep(m) {
  if (m.rd(R.frame_ready) === 0) return false;
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
  }
  m.wr(R.frame_ready, 0);                           // 30:C919 (wait loops inside routines clear it too)
  return true;
}

// ---------------------------------------------------------------- NMI
/** NMI 31:F3B6 */
export function nmiWork(m) {
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
  m.wr(0x56, i);
  m.irqSplits.push({ handler: i, latch });
}

/** 31:EB6D: PPUSCROLL <- $07D8, $07DA; ppuctrl_shadow nametable bits = ($07DB b0) << 1 | ($07D9 b0) */
export function ppuScrollWrite(m) {
  const ppu = m.ppu;
  ppu.writeReg(5, m.rd(R.scroll_x_irq));
  ppu.writeReg(5, m.rd(R.scroll_x_irq + 2));
  const nt = (((m.rd(R.scroll_x_irq + 3) << 1) | (m.rd(R.scroll_x_irq + 1) & 1)) & 3);
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
  m.wr(0x56, 1);                                    // SetIrqHandler(1)
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
  p.writeReg(6, 0x28); p.writeReg(6, 0x00); p.writeReg(5, 0x00);
  applyChrAlt(m);
  chainSplit(m, line, 0x48, 3);
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
  p.writeReg(6, m.rd(0x5E)); p.writeReg(6, m.rd(0x5F)); p.writeReg(5, m.rd(R.scroll_x_irq));
  applyChrPlay(m);
  const a = (0xEF - m.rd(R.scroll_x_irq + 2)) & 0xFF;
  const z = m.rd(R.zp_57);
  if (a < 0xBC && a >= z) {
    chainSplit(m, line, (a - z) & 0xFF, 0);
    m.wr(R.zp_57, a);
    m.wr(0x56, 0);
  } else {
    chainSplit(m, line, (0xBE - z) & 0xFF, 1);
    m.wr(0x56, 1);
  }
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

/** JmpTable31_F6C2: irq_mode -> NMI routine. Unported modes log once and do nothing. */
const unportedMode = (addr) => (m) => {
  if (!m.unported.has(addr)) { m.unported.add(addr); m.log(`NMI mode routine ${addr} not ported yet`); }
};
export const NMI_MODES = [
  nmiMode0, nmiMode1, nmiMode2, nmiMode3,
  unportedMode('31:F782'), nmiMode5, nmiMode6, nmiMode0,
  null, nmiMode1,
];

/** NmiModeDispatch 31:F6AD: $57 = 0, JMP (JmpTable31_F6C2[irq_mode]); ASL drops b7 (the "skip
 *  PaletteUpload" flag set by PauseScreen / the bank 16 menus), so $82 runs mode 2 */
export function nmiModeDispatch(m) {
  m.wr(R.zp_57, 0);
  m.irqSplits = [];
  const fn = NMI_MODES[m.rd(R.irq_mode) & 0x7F];
  if (!fn) throw new Error(`irq_mode ${m.rd(R.irq_mode)}: null NMI-mode entry`);
  fn(m);
}
