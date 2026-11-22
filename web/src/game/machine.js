// re-c web port — Machine: RAM/WRAM + PPU + frame sequencing (docs/architecture_web.md §2.1). DOM-free.
//
//   const m = new Machine(data); m.reset();
//   m.runFrame(buttons)  -> m.ppu.fb (256x240 NES colour indices) for the frame shown between two NMIs
//
// Frame order (one NES frame): MainLoop work (runs if the previous NMI set frame_ready) -> the PPU draws
// the frame from the state the previous NMI uploaded (IRQ splits scheduled by that NMI's mode routine)
// -> NMI work. The game only talks to the PPU through its registers (ppu.writeReg / oamDma / setChrReg).
import { Ram, R } from './ram.js';
import { Ppu } from '../nes/ppu.js';
import { mainStep, nmiWork, nmiLostToHud, irqMenuChrSplit, irqIntroSplit, irqWrapSplit, irqHud, irqMsgWindowTop,
  irqMsgWindowEnd, irqRowStretch, irqOffSound } from './mainloop.js';
import { ENTRY_POINTS } from './states/play.js';
import { Apu } from '../nes/apu.js';
import { cpuRd } from './prg.js';
import { sndInit } from './sound/driver.js';
import { soundUpdate } from './sound/soundupdate.js';
import { ppuQueueFlush } from './world/ppuqueue.js';
import { playSound } from './playsound.js';
import { objPosToTemp } from './obj/util.js';
import { areaSpawnEntry } from './world/spawn.js';
import { introLeftovers, coldSaveInit, bootSaveCheck, copySramBlock } from './boot.js';

export const FRAME_HZ = 60.0988;       // NTSC: 39375000/11/341/262*3 ≈ 60.0988 (odd-frame skip ignored)
const FRAME_CYCLES3 = 341 * 262;       // CPU cycles per frame × 3 (= 29780.67 cycles)

// IRQ handlers (IrqHandlerTable 31:F433). All eight have bodies since s208 (4 / 5 = the dead NmiMode4 path);
// the logging fallback below is only a guard.
const IRQ_HANDLERS = ['31:F455', '31:F488', '31:F4E5', '31:F54B', '31:F5E1', '31:F60E', '31:F691', '31:F61A'];

/**
 * s176: the IRQ split the NMI just before a loaded state scheduled (a state taken in vblank, line > 240).
 * Without it the first frame ran no IRQ handler, so in gameplay (handler 1 IrqHandler1Hud 31:F488, whose
 * SoundUpdate 31:F4CF is the gameplay sound tick) the port lost one sound tick for good: the open 1-frame
 * `snd*_period` offset after every scenario_state / restore5f load (s173-175). Every NMI mode leaves its
 * SetIrqHandler index in $56 and the latch in $57 (NmiModeDispatch 31:F6AD clears $57 first), except
 * mode 5 (latch $58, $57 not written); modes 0/2/7 with rendering off and mode 3 schedule none.
 */
function loadSplits(m, s) {
  if (!(s.line > 240)) return [];                    // mid-frame state: this frame's split is already past
  const mode = m.rd(R.irq_mode) & 0x7F;
  if (mode === 3 || mode === 8) return [];
  if ((mode === 0 || mode === 2 || mode === 7) && (m.rd(R.ppumask_shadow) & 0x18) === 0) return [];
  if (mode === 5) return [{ handler: 6, latch: m.rd(R.chr_r0_alt) }];
  if (mode === 4) return m.rd(R.chr_r0_alt) ? [{ handler: 4, latch: 4 }] : [];   // s208: NmiMode4 31:F782
  return [{ handler: m.rd(0x56), latch: m.rd(R.zp_57) }];
}

export class Machine {
  constructor(data, { log = () => {} } = {}) {
    this.data = data;
    this.log = log;
    this.mem = new Ram();
    this.ram = this.mem;                 // ram.page(), ram.ram, ram.wram
    this.ppu = new Ppu(data.chr);
    this.buttons = [0, 0];               // pad 1 / pad 2 button bytes (A B Sel St U D L R)
    this.unported = new Set();
    this.stubHits = {};
    this.irqSplits = [];
    this.pending = null;
    this.frame = 0;
    this.romRd = 0;                                  // s211: prg.js cpuRd calls (web/test/pass_count.mjs)
    this.passStage = 'top';                          // s211: main-loop stage (states/play.js, pass_count.mjs)
    // PpuQueueFlush 30:C67D (world/ppuqueue.js, s79), called by the NMI at F40A
    this.ppuQueueFlush = ppuQueueFlush;
    // sound (7/8, s56-57): SoundUpdate 31:F7FE + the APU. Register writes take effect at once; the APU is
    // advanced one frame of CPU cycles at the end of runFrame (the driver writes once or twice per frame, so
    // only the phase within the frame differs from hardware).
    this.soundUpdate = soundUpdate;
    this.apu = new Apu((a) => cpuRd(this, a));
    this.apuWrite = (a, v) => this.apu.write(a, v);
    this.apuRead = (a) => this.apu.read(a);
    this._cyc3 = 0;
    // world/area.js hooks (s53): PlaySound 30:C125 (7/8), AreaSpawnEntry 31:E1B6 + bank 26 $98A8 (5/8)
    // s108: PlaySound + the $6D = $FF entry are wired (AreaEnter runs in the port now); spawn handlers = (e1)
    this.playSound = (id) => playSound(this, id, 0);   // AreaLoadMap 31:E634 (Y = 0)
    this.spawnEntry = (x, y) => areaSpawnEntry(this, x, y);   // s112: world/spawn.js (entry at ($10),Y)
    this.entryFF = (x) => objPosToTemp(this, x);       // 26:98A8 ObjPosToTemp
    this.irqHandlers = IRQ_HANDLERS.map((addr) => (m) => {
      if (!m.unported.has(addr)) { m.unported.add(addr); m.log(`IRQ handler ${addr} not ported yet`); }
    });
    this.irqHandlers[0] = irqWrapSplit;                // 31:F455 (s108)
    this.irqHandlers[1] = irqHud;                      // 31:F488 (s108)
    this.irqHandlers[2] = irqMsgWindowTop;             // 31:F4E5 (s105 body, wired s115: message window)
    this.irqHandlers[3] = irqMsgWindowEnd;             // 31:F54B (s105 body, wired s115)
    this.irqHandlers[4] = irqRowStretch;               // 31:F5E1 (s208, dead NmiMode4 path)
    this.irqHandlers[5] = irqOffSound;                 // 31:F60E (s208, only reached from F5FE)
    this.irqHandlers[6] = irqIntroSplit;               // 31:F691 (s106)
    this.irqHandlers[7] = irqMenuChrSplit;             // 31:F61A (s98)
  }

  rd(a) { return this.mem.rd(a); }
  wr(a, v) { this.mem.wr(a, v); }

  /**
   * Reset 31:F2A4, the parts that set state the frame skeleton depends on (REVERSE.md "Boot" +
   * Verification Log row "Boot", s41). The cold-boot intro (bank 18, 212 frames) is not run: the
   * machine starts at the first MainLoop pass with main_state = 2.
   */
  reset() {
    this.mem.ram.fill(0);
    this.ppu.reset();
    this.ppu.mirroring = 1;                          // $A000 = 1 (horizontal)
    for (let a = 0x6140; a < 0x6160; a++) this.wr(a, 0xFF);   // pal_buf (31:F315)
    for (let a = 0x6200; a < 0x6220; a++) this.wr(a, 0xFF);   // PPU queue ring
    this.wr(0x0182, 0x55); this.wr(0x0183, 0x53); this.wr(0x0184, 0x4F);   // "USO" warm-boot signature
    this.wr(0x07FE, 9);                              // IRQ delay
    this.wr(R.irq_mode, 3);                          // first $51 write
    this.wr(R.ppuctrl_shadow, 0x88);
    this.ppu.writeReg(0, 0x88);
    this.wr(R.main_state, 2);
    this.pending = null;
    this.irqSplits = [];
    this.frame = 0;
    this.prgR6 = 0x18;                               // MMC3 R6/R7 as left by reset's SwitchBank16($0C) (prg.js)
    this.prgR7 = 0x19;
    const out = this.apu.out, rate = this.apu.sampleRate;
    this.apu = new Apu((a) => cpuRd(this, a));
    if (rate) this.apu.setOutput(rate, out);
    this.apu.write(0x4010, 0);                       // 31:F2C9 DMC IRQ off
    this.apu.write(0x4017, 0x40);                    // 31:F2CE APU frame IRQ off
    sndInit(this);                                   // 31:F325 JSR $8020 with banks 24/25
    // s106: cold path (RAM is zeroed, so no "USO"): what the skipped intro leaves behind, then the save side of
    // Reset (ColdSaveInit, BootSaveCheck on the battery WRAM loaded before reset(), CopySramBlock) -> MainLoop
    introLeftovers(this);
    coldSaveInit(this);
    bootSaveCheck(this);
    copySramBlock(this);
    this._cyc3 = 0;
  }

  /**
   * Start from a tools.emu state taken at the first instruction of a routine the port runs from its start
   * (tools/emu/web_state.py, s88): RAM, WRAM, CIRAM, palette, OAM, PPU registers, MMC3 CHR/PRG registers and
   * mirroring. The next runFrame() is tools.emu frame `s.frame`. The split the NMI that ran just
   * before scheduled is rebuilt from $56/$57 (loadSplits, s176).
   */
  loadState(s) {
    const hex = (h) => Uint8Array.from(h.match(/../g), (b) => parseInt(b, 16));
    this.mem.ram.set(hex(s.ram));
    this.mem.wram.set(hex(s.wram));
    const p = this.ppu;
    p.vram.set(hex(s.ciram)); p.pal.set(hex(s.pal)); p.oam.set(hex(s.oam));
    Object.assign(p, { ctrl: s.ppu.ctrl, mask: s.ppu.mask, v: s.ppu.v, t: s.ppu.t, x: s.ppu.x, w: s.ppu.w });
    p.latchVert();
    for (let r = 0; r < 6; r++) p.setChrReg(r, s.mapper.regs[r]);
    p.mirroring = s.mapper.mirroring;
    p.chrInv = s.mapper.chr_inv;
    this.prgR6 = s.mapper.regs[6] & 0x3F;
    this.prgR7 = s.mapper.regs[7] & 0x3F;
    this.pending = null;
    this.irqSplits = loadSplits(this, s);
    const entry = ENTRY_POINTS[s.until];             // s94: a routine reached by JSR from unported code
    if (entry) this.pending = entry(this);
  }

  /** Map the split list from the last NMI-mode routine onto ppu.splits (an MMC3 IRQ with latch L
   *  fires at the end of line L, so its register changes show from line L+1). */
  _installSplits() {
    const s = this.ppu.splits;
    s.clear();
    for (const { handler, latch } of this.irqSplits) {
      const line = latch + 1;
      if (line < 240) s.set(line, () => this.irqHandlers[handler](this, line));
    }
  }

  runFrame(buttons = 0, buttons2 = 0) {
    this.buttons[0] = buttons & 0xFF;
    this.buttons[1] = buttons2 & 0xFF;
    mainStep(this);
    this._installSplits();
    this.ppu.renderFrame();
    if (this.skipNmi) this.skipNmi = false;          // s107: NO_NMI (lost NMI, see states/death.js)
    else if (this.hudNmiLost) { this.hudNmiLost = false; nmiLostToHud(this); }   // s174: see irqHud
    else nmiWork(this);
    // s114: CPU overrun. The ROM's frame ran past line 241, so its NMI found frame_ready != 0 (no OAM DMA, no
    // scroll copy, frame_ready 2). Then MainLoop's 30:C919 clears it and waits a whole frame for the next NMI.
    if (this.overrunNmi) { this.overrunNmi = false; this.wr(0x09, 0); }
    this.ppu.latchVert();                            // s125: pre-render vertical copy (see Ppu.latchVert)
    this._cyc3 += FRAME_CYCLES3;
    const n = Math.floor(this._cyc3 / 3);
    this._cyc3 -= n * 3;
    this.apu.run(n);
    this.frame++;
    return this.ppu.fb;
  }
}
