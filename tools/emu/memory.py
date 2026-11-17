"""CPU bus.

$0000-$1FFF  2KB internal RAM, mirrored x4
$2000-$3FFF  PPU registers (8, mirrored)       -> ppu.read_reg(r) / ppu.write_reg(r, v)
$4000-$4013, $4015, $4017  APU                  -> apu.read_reg(a) / apu.write_reg(a, v)
$4014        OAM DMA                            -> 256 bytes to ppu.oam_dma(page_bytes); CPU stall 513/514
$4016/$4017  controllers (read)                 -> pads.read(port) / pads.write_strobe(v)
$4020-$5FFF  expansion: open bus
$6000-$7FFF  8KB WRAM (battery)                 -> mapper.wram_enabled / wram_writable gate it if a mapper is set
$8000-$FFFF  PRG ROM                            -> mapper.read(a) / mapper.write(a, v)

Devices are optional; missing ones read as open bus (last value on the bus) and ignore writes.
With a cart and no explicit mapper, the bus builds `mapper.make_mapper(cart)` (MMC3 for re-c,
emu 2/6) and sets `mapper.bus = self`. `FixedPrg` (static $8000=bank 0, $A000=bank 1,
$C000=second-last, $E000=last) stays available as an explicit stand-in for CPU-only tests.

`FlatBus` is a 64KB RAM bus for CPU unit tests.
"""


class FixedPrg:
    """Read-only PRG with a static 4x8KB layout; records writes (for tests before MMC3 exists)."""

    def __init__(self, cart, banks=(0, 1, -2, -1)):
        self.cart = cart
        self.windows = [cart.prg_bank(b) for b in banks]
        self.writes = []
        self.wram_enabled = True
        self.wram_writable = True

    def read(self, a):
        return self.windows[(a >> 13) & 3][a & 0x1FFF]

    def write(self, a, v):
        self.writes.append((a, v))


class Bus:
    def __init__(self, cart=None, mapper=None, ppu=None, apu=None, pads=None):
        self.ram = bytearray(0x800)
        self.wram = bytearray(0x2000)
        self.cart = cart
        if mapper is None and cart is not None:
            from .mapper import make_mapper
            mapper = make_mapper(cart)
        self.mapper = mapper
        if mapper is not None and hasattr(mapper, 'bus'):
            mapper.bus = self            # MMC3 reaches bus.cpu to assert its IRQ
        self.ppu = ppu
        self.apu = apu
        self.pads = pads
        self.open_bus = 0
        self.cpu = None          # set by CPU.__init__ (for DMA stall)
        self.read_watch = {}     # addr -> callable(addr, value)   (debug hooks, emu 4/6)
        self.write_watch = {}

    # --- read ---
    def read(self, a):
        if a < 0x2000:
            v = self.ram[a & 0x7FF]
        elif a < 0x4000:
            v = self.ppu.read_reg(a & 7) if self.ppu else self.open_bus
        elif a < 0x4020:
            if a == 0x4016 or a == 0x4017:
                v = (self.pads.read(a & 1) & 0x1F) | (self.open_bus & 0xE0) if self.pads else self.open_bus
            elif a == 0x4015 and self.apu:
                v = self.apu.read_reg(a)
            else:
                v = self.open_bus
        elif a < 0x6000:
            v = self.open_bus
        elif a < 0x8000:
            m = self.mapper
            v = self.wram[a & 0x1FFF] if (m is None or m.wram_enabled) else self.open_bus
        else:
            v = self.mapper.read(a) if self.mapper else self.open_bus
        self.open_bus = v
        if self.read_watch:
            f = self.read_watch.get(a)
            if f:
                f(a, v)
        return v

    # --- write ---
    def write(self, a, v):
        self.open_bus = v
        if self.write_watch:
            f = self.write_watch.get(a)
            if f:
                f(a, v)
        if a < 0x2000:
            self.ram[a & 0x7FF] = v
        elif a < 0x4000:
            if self.ppu:
                self.ppu.write_reg(a & 7, v)
        elif a < 0x4020:
            if a == 0x4014:
                self._oam_dma(v)
            elif a == 0x4016:
                if self.pads:
                    self.pads.write_strobe(v)
            elif self.apu:
                self.apu.write_reg(a, v)
        elif a < 0x6000:
            pass
        elif a < 0x8000:
            m = self.mapper
            if m is None or (m.wram_enabled and m.wram_writable):
                self.wram[a & 0x1FFF] = v
        elif self.mapper:
            self.mapper.write(a, v)

    def _oam_dma(self, page):
        base = page << 8
        data = bytes(self.read(base + i) for i in range(256))
        if self.ppu:
            self.ppu.oam_dma(data)
        if self.cpu is not None:
            # 513 cycles, +1 if the DMA starts on an odd CPU cycle
            self.cpu.stall += 513 + (self.cpu.cycles & 1)

    def read16(self, a):
        return self.read(a) | self.read((a + 1) & 0xFFFF) << 8


class FlatBus:
    """64KB plain RAM (CPU unit tests)."""

    def __init__(self):
        self.mem = bytearray(0x10000)
        self.cpu = None

    def read(self, a):
        return self.mem[a]

    def write(self, a, v):
        self.mem[a] = v

    def load(self, addr, data):
        self.mem[addr:addr + len(data)] = bytes(data)
