"""Whole-system wiring + frame loop for the re-c emulator (Ph5.5 emu 3/6).

    from emu.nes import NES
    nes = NES()                  # rom/re-c.nes, power-on
    nes.run_frames(60)
    nes.dump_screen('out.png')

Loop: cpu.step() then ppu.run_to(cpu.cycles * 3); register accesses sync the PPU themselves.
A "frame" ends when the PPU wraps from the pre-render line 261 to line 0 (ppu.frame_no += 1),
so after run_frame() `ppu.fb` holds the complete picture of the frame just finished.
"""
from .loader import load
from .memory import Bus
from .cpu import CPU
from .ppu import PPU
from .apu import ApuStub
from .pads import Pads


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
        if power:
            self.cpu.power_on()

    def step(self):
        c = self.cpu.step()
        self.ppu.run_to(self.cpu.cycles * 3)
        return c

    def run_frame(self):
        cpu, ppu = self.cpu, self.ppu
        f = ppu.frame_no
        step = cpu.step
        run_to = ppu.run_to
        while ppu.frame_no == f:
            step()
            run_to(cpu.cycles * 3)

    def run_frames(self, n):
        for _ in range(n):
            self.run_frame()

    def dump_screen(self, path, scale=1):
        from .png import write_rgb
        write_rgb(path, 256, 240, self.ppu.to_rgb(), scale)
