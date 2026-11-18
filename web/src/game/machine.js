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
import { mainStep, nmiWork } from './mainloop.js';
import { Apu } from '../nes/apu.js';
import { cpuRd } from './prg.js';
import { sndInit } from './sound/driver.js';
import { soundUpdate } from './sound/soundupdate.js';

export const FRAME_HZ = 60.0988;       // NTSC: 39375000/11/341/262*3 ≈ 60.0988 (odd-frame skip ignored)
const FRAME_CYCLES3 = 341 * 262;       // CPU cycles per frame × 3 (= 29780.67 cycles)

// IRQ handlers (IrqHandlerTable 31:F433). Bodies arrive with the HUD / world tasks (4/8, 7/8);
// until then a split only marks where it would fire.
const IRQ_HANDLERS = ['31:F455', '31:F488', '31:F4E5', '31:F54B', '31:F5E1', '31:F60E', '31:F691', '31:F61A'];

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
    // hooks filled by later tasks (Ph7 web 4/8: ppuqueue.js)
    this.ppuQueueFlush = () => {};
    // sound (7/8, s56-57): SoundUpdate 31:F7FE + the APU. Register writes take effect at once; the APU is
    // advanced one frame of CPU cycles at the end of runFrame (the driver writes once or twice per frame, so
    // only the phase within the frame differs from hardware).
    this.soundUpdate = soundUpdate;
    this.apu = new Apu((a) => cpuRd(this, a));
    this.apuWrite = (a, v) => this.apu.write(a, v);
    this.apuRead = (a) => this.apu.read(a);
    this._cyc3 = 0;
    // world/area.js hooks (s53): PlaySound 30:C125 (7/8), AreaSpawnEntry 31:E1B6 + bank 26 $98A8 (5/8)
    this.playSound = () => {};
    this.spawnEntry = () => {};
    this.entryFF = () => {};
    this.irqHandlers = IRQ_HANDLERS.map((addr) => (m) => {
      if (!m.unported.has(addr)) { m.unported.add(addr); m.log(`IRQ handler ${addr} not ported yet`); }
    });
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
    this._cyc3 = 0;
  }

  /** Map the split list from the last NMI-mode routine onto ppu.splits (an MMC3 IRQ with latch L
   *  fires at the end of line L, so its register changes show from line L+1). */
  _installSplits() {
    const s = this.ppu.splits;
    s.clear();
    for (const { handler, latch } of this.irqSplits) {
      const line = latch + 1;
      if (line < 240) s.set(line, () => this.irqHandlers[handler](this));
    }
  }

  runFrame(buttons = 0, buttons2 = 0) {
    this.buttons[0] = buttons & 0xFF;
    this.buttons[1] = buttons2 & 0xFF;
    mainStep(this);
    this._installSplits();
    this.ppu.renderFrame();
    nmiWork(this);
    this._cyc3 += FRAME_CYCLES3;
    const n = Math.floor(this._cyc3 / 3);
    this._cyc3 -= n * 3;
    this.apu.run(n);
    this.frame++;
    return this.ppu.fb;
  }
}
