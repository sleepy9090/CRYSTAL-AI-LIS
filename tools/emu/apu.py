"""APU stub for the re-c emulator (Ph5.5 emu 3/6).

Register writes $4000-$4013, $4015, $4017 are stored in `regs` and, when `log` is a list,
appended as (frame, cpu_cycle, addr, value). Reads of $4015 return 0: no length counters,
no frame-counter IRQ and no DMC IRQ are generated (the IRQ line, cpu.irq_flags bit 1/2, is never
asserted). Sound synthesis is the web port's job (Ph7 web 7/8).
"""


class ApuStub:
    def __init__(self):
        self.regs = bytearray(0x18)
        self.log = None
        self.cpu = None
        self.ppu = None

    def write_reg(self, a, v):
        self.regs[a - 0x4000] = v
        if self.log is not None:
            self.log.append((self.ppu.frame_no if self.ppu else 0,
                             self.cpu.cycles if self.cpu else 0, a, v))

    def read_reg(self, a):
        return 0

    def state(self):
        return {'regs': bytes(self.regs)}

    def load_state(self, d):
        self.regs[:] = d['regs']
