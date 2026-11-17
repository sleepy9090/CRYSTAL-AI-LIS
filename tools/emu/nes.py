"""Whole-system wiring + frame loop for the re-c emulator (Ph5.5 emu 3/6, hooks emu 4/6).

    from emu.nes import NES
    nes = NES()                  # rom/re-c.nes, power-on
    nes.run_frames(60)
    nes.dump_screen('out.png')

Loop: cpu.step() then ppu.run_to(cpu.cycles * 3); register accesses sync the PPU themselves.
A "frame" ends when the PPU wraps from the pre-render line 261 to line 0 (ppu.frame_no += 1),
so after run_frame() `ppu.fb` holds the complete picture of the frame just finished.

Hook API (emu 4/6). Callbacks may raise `Stop` to end run_frame/run_frames/call early.
  nes.on_exec(bank, addr, fn)  fn(nes) before the instruction at addr executes. bank = 8KB PRG
                               bank that must be mapped there (None = any; ignored below $8000).
  nes.on_write(addr, fn)       fn(addr, value) before a CPU write to exactly addr (no mirroring)
  nes.on_read(addr, fn)        fn(addr, value) after a CPU read of exactly addr
  nes.on_frame(fn)             fn(nes) after every completed frame
  nes.set_tracer(fn)           fn(cpu) before every instruction (debug.Tracer), None = off
  nes.clear_hooks()
Instruction hooks run through cpu.trace, which is only installed while a hook/tracer exists
(no cost otherwise). Interrupt entry is not an instruction: an NMI handler's first instruction
fires its hook on the next step.
"""
from .loader import load
from .memory import Bus
from .cpu import CPU
from .ppu import PPU
from .apu import ApuStub
from .pads import Pads


class Stop(Exception):
    """Raised by a hook to stop the current run (break point, run-func return, ...)."""


class NES:
    def __init__(self, path=None, power=True):
        self.cart = load(path) if path else load()
        self.ppu = PPU()
        self.apu = ApuStub()
        self.pads = Pads()
        self.bus = Bus(self.cart, ppu=self.ppu, apu=self.apu, pads=self.pads)
        self.mapper = self.bus.mapper
        self.cpu = CPU(self.bus)
        self.ppu.attach(self.cpu, self.mapper)
        self.apu.cpu, self.apu.ppu = self.cpu, self.ppu
        self.exec_hooks = {}         # addr -> [(bank|None, fn)]
        self.frame_hooks = []
        self.tracer = None
        self.keys = None             # optional {frame: mask} or callable(frame) -> mask for pad 1
        if power:
            self.cpu.power_on()

    # ------------------------------------------------------------ hooks
    def on_exec(self, bank, addr, fn):
        self.exec_hooks.setdefault(addr, []).append((bank, fn))
        self._install()

    def on_write(self, addr, fn):
        self._watch(self.bus.write_watch, addr, fn)

    def on_read(self, addr, fn):
        self._watch(self.bus.read_watch, addr, fn)

    def on_frame(self, fn):
        self.frame_hooks.append(fn)

    def set_tracer(self, fn):
        self.tracer = fn
        self._install()

    def clear_hooks(self):
        self.exec_hooks.clear()
        self.frame_hooks.clear()
        self.bus.write_watch.clear()
        self.bus.read_watch.clear()
        self.tracer = None
        self._install()

    @staticmethod
    def _watch(table, addr, fn):
        old = table.get(addr)
        if old is None:
            table[addr] = fn
        else:
            def both(a, v, old=old, fn=fn):
                old(a, v)
                fn(a, v)
            table[addr] = both

    def _install(self):
        if not self.exec_hooks and not self.tracer:
            self.cpu.trace = None
            return
        hooks, tracer, bank_at = self.exec_hooks, self.tracer, self.mapper.prg_bank_at

        def dispatch(cpu):
            if tracer:
                tracer(cpu)
            h = hooks.get(cpu.pc)
            if h:
                pc = cpu.pc
                for bank, fn in h:
                    if bank is None or pc < 0x8000 or bank_at(pc) == bank:
                        fn(self)
        self.cpu.trace = dispatch

    # ------------------------------------------------------------ running
    def step(self):
        c = self.cpu.step()
        self.ppu.run_to(self.cpu.cycles * 3)
        return c

    def run_frame(self):
        cpu, ppu = self.cpu, self.ppu
        f = ppu.frame_no
        if self.keys is not None:
            k = self.keys
            self.pads.set(0, k(f) if callable(k) else k.get(f, 0))
        step = cpu.step
        run_to = ppu.run_to
        while ppu.frame_no == f:
            step()
            run_to(cpu.cycles * 3)
        for fn in self.frame_hooks:
            fn(self)

    def run_frames(self, n):
        for _ in range(n):
            self.run_frame()

    def call(self, addr, bank=None, a=None, x=None, y=None, max_instr=2_000_000):
        """JSR-style call: map `bank` (its 16KB pair if addr is in $8000-$BFFF), push a sentinel
        return address, run until the matching RTS (PC == sentinel with S back at its start).
        NMI/IRQ keep running normally. Returns instructions executed; raises Stop on limit."""
        cpu, m = self.cpu, self.mapper
        if bank is not None and 0x8000 <= addr < 0xC000:
            m.regs[6], m.regs[7] = bank & ~1, bank | 1
            m._update_prg()
        for r, v in (('a', a), ('x', x), ('y', y)):
            if v is not None:
                setattr(cpu, r, v & 0xFF)
        s0 = cpu.s
        ret = 0xFFFF                     # stop PC; the pushed value is ret-1 like JSR
        cpu._push((ret - 1) >> 8)
        cpu._push((ret - 1) & 0xFF)
        cpu.pc = addr
        n = 0
        while not (cpu.pc == ret and cpu.s == s0):
            if n >= max_instr:
                raise Stop(f'call ${addr:04X}: no return after {max_instr} instructions')
            self.step()
            n += 1
        return n

    def dump_screen(self, path, scale=1):
        from .png import write_rgb
        write_rgb(path, 256, 240, self.ppu.to_rgb(), scale)
