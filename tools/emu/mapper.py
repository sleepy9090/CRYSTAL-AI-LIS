"""MMC3 (iNES mapper 4) for the re-c emulator (Ph5.5 emu 2/6).

Registers (CPU writes, even/odd = A0):
  $8000 even  bank select:  bits 0-2 = target R0-R7, bit 6 = PRG mode, bit 7 = CHR A12 inversion
  $8001 odd   bank data:    value for the selected R
  $A000 even  mirroring:    0 = vertical, 1 = horizontal   (re-c Reset writes 1)
  $A001 odd   PRG-RAM:      bit 7 = enable, bit 6 = write-protect  (re-c Reset writes $80)
  $C000 even  IRQ latch
  $C001 odd   IRQ reload (counter := 0, reloaded from latch on the next clock)
  $E000 even  IRQ disable + acknowledge
  $E001 odd   IRQ enable

PRG windows (8KB banks, mod bank count; -2/-1 = second-last/last = re-c banks 30/31):
  mode 0: $8000=R6  $A000=R7  $C000=-2  $E000=-1
  mode 1: $8000=-2  $A000=R7  $C000=R6  $E000=-1
CHR windows (1KB banks, PPU $0000-$1FFF in 8 x 1KB slots):
  inversion 0: R0&~1,R0|1, R1&~1,R1|1, R2, R3, R4, R5
  inversion 1: R2, R3, R4, R5, R0&~1,R0|1, R1&~1,R1|1
  (re-c uses inversion 0 = "2KB @ $0000, 1KB @ $1000", see CLAUDE.md.)

Scanline IRQ (Sharp/"new" MMC3 behaviour): on each counter clock, if counter == 0 or a reload
is pending -> counter = latch (clear reload), else counter -= 1; then if counter == 0 and IRQs
are enabled -> assert IRQ (cpu.irq_flags bit 0) until $E000 is written.
Clock sources (the PPU decides which one to use; emu 3/6):
  * clock_scanline()       - approximate: called by the PPU at dot 260 of each visible and the
                             pre-render line while rendering is on and BG/sprite pattern tables
                             differ (sprites at $1000 -> A12 rises during sprite fetches).
  * ppu_a12(level, dot)    - exact: the PPU reports the A12 line of every pattern fetch with an
                             absolute PPU-dot timestamp; a rise counts only after A12 was low for
                             >= A12_LOW_DOTS PPU dots (the M2 filter, ~3 CPU cycles).

The bus attaches itself as `mapper.bus` (memory.Bus) so the mapper can reach `bus.cpu`.
"""

A12_LOW_DOTS = 10       # ~3 CPU cycles (M2 falling edges) of A12 low before a rise is counted
IRQ_BIT = 1             # cpu.irq_flags bit for the mapper (see cpu.py)


class MMC3:
    def __init__(self, cart):
        self.cart = cart
        self.bus = None
        self.n_prg = cart.prg_banks8
        self.chr_ram = len(cart.chr) == 0
        self.chr = bytearray(0x2000) if self.chr_ram else cart.chr
        self.n_chr = max(1, len(self.chr) // 0x400)
        self.writes = []            # (addr, value) log of register writes (tests/tracing)
        self.log_writes = True
        self.power_on()

    # ------------------------------------------------------------ state
    def power_on(self):
        # Register contents are undefined on hardware; these are the common emulator defaults.
        self.regs = [0, 2, 4, 5, 6, 7, 0, 1]
        self.bank_select = 0
        self.prg_mode = 0
        self.chr_inv = 0
        self.mirroring = 0          # 0 vertical, 1 horizontal
        self.wram_enabled = True    # many boards ignore $A001; default open so WRAM works pre-init
        self.wram_writable = True
        self.irq_latch = 0
        self.irq_counter = 0
        self.irq_reload = False
        self.irq_enabled = False
        self.irq_pending = False
        self.irq_clocks = 0         # total counter clocks (debug)
        self._a12 = 0
        self._a12_low_since = 0
        self._update_prg()
        self._update_chr()

    def state(self):
        keys = ('regs', 'bank_select', 'prg_mode', 'chr_inv', 'mirroring', 'wram_enabled',
                'wram_writable', 'irq_latch', 'irq_counter', 'irq_reload', 'irq_enabled',
                'irq_pending', 'irq_clocks', '_a12', '_a12_low_since')
        d = {k: getattr(self, k) for k in keys}
        d['regs'] = list(self.regs)
        if self.chr_ram:
            d['chr'] = bytes(self.chr)
        return d

    def load_state(self, d):
        for k, v in d.items():
            if k == 'chr':
                self.chr[:] = v
            else:
                setattr(self, k, list(v) if k == 'regs' else v)
        self._update_prg()
        self._update_chr()
        self._set_irq_line(self.irq_pending)

    # ------------------------------------------------------------ banking
    def _update_prg(self):
        r6, r7 = self.regs[6] & 0x3F, self.regs[7] & 0x3F
        if self.prg_mode == 0:
            banks = (r6, r7, -2, -1)
        else:
            banks = (-2, r7, r6, -1)
        self.prg_banks = tuple(b % self.n_prg for b in banks)
        self.prg_windows = [self.cart.prg_bank(b) for b in self.prg_banks]

    def _update_chr(self):
        r = self.regs
        lo = (r[0] & 0xFE, r[0] | 1, r[1] & 0xFE, r[1] | 1)
        hi = (r[2], r[3], r[4], r[5])
        banks = (hi + lo) if self.chr_inv else (lo + hi)
        self.chr_banks = tuple(b % self.n_chr for b in banks)
        self.chr_offsets = [b * 0x400 for b in self.chr_banks]

    def prg_bank_at(self, addr):
        """8KB PRG bank currently mapped at CPU addr ($8000-$FFFF)."""
        return self.prg_banks[(addr >> 13) & 3]

    # ------------------------------------------------------------ CPU side
    def read(self, a):
        return self.prg_windows[(a >> 13) & 3][a & 0x1FFF]

    def write(self, a, v):
        if self.log_writes:
            self.writes.append((a, v))
        odd = a & 1
        reg = (a >> 13) & 3          # 0:$8000 1:$A000 2:$C000 3:$E000
        if reg == 0:
            if not odd:
                self.bank_select = v & 7
                pm, ci = (v >> 6) & 1, (v >> 7) & 1
                if pm != self.prg_mode:
                    self.prg_mode = pm
                    self._update_prg()
                if ci != self.chr_inv:
                    self.chr_inv = ci
                    self._update_chr()
            else:
                self.regs[self.bank_select] = v
                if self.bank_select >= 6:
                    self._update_prg()
                else:
                    self._update_chr()
        elif reg == 1:
            if not odd:
                self.mirroring = v & 1
            else:
                self.wram_enabled = bool(v & 0x80)
                self.wram_writable = not (v & 0x40)
        elif reg == 2:
            if not odd:
                self.irq_latch = v
            else:
                self.irq_counter = 0
                self.irq_reload = True
        else:
            if not odd:
                self.irq_enabled = False
                self.irq_pending = False
                self._set_irq_line(False)
            else:
                self.irq_enabled = True

    # ------------------------------------------------------------ PPU side
    def chr_read(self, a):
        a &= 0x1FFF
        return self.chr[self.chr_offsets[a >> 10] + (a & 0x3FF)]

    def chr_write(self, a, v):
        if self.chr_ram:
            a &= 0x1FFF
            self.chr[self.chr_offsets[a >> 10] + (a & 0x3FF)] = v

    def chr_bank_at(self, a):
        """1KB CHR bank mapped at PPU addr $0000-$1FFF."""
        return self.chr_banks[(a >> 10) & 7]

    def nt_index(self, a):
        """PPU $2000-$3EFF -> offset into the 2KB CIRAM according to the current mirroring."""
        a &= 0x0FFF
        if self.mirroring == 0:          # vertical: $2000=$2800, $2400=$2C00
            return a & 0x7FF
        return ((a >> 1) & 0x400) | (a & 0x3FF)   # horizontal: $2000=$2400, $2800=$2C00

    # ------------------------------------------------------------ IRQ
    def clock_scanline(self):
        self.irq_clocks += 1
        if self.irq_counter == 0 or self.irq_reload:
            self.irq_counter = self.irq_latch
            self.irq_reload = False
        else:
            self.irq_counter -= 1
        if self.irq_counter == 0 and self.irq_enabled:
            self.irq_pending = True
            self._set_irq_line(True)

    def ppu_a12(self, level, dot):
        """Report the PPU A12 line at absolute PPU dot `dot`; counts filtered rising edges."""
        if level and not self._a12:
            if dot - self._a12_low_since >= A12_LOW_DOTS:
                self.clock_scanline()
        elif not level and self._a12:
            self._a12_low_since = dot
        self._a12 = 1 if level else 0

    def _set_irq_line(self, on):
        cpu = getattr(self.bus, 'cpu', None) if self.bus is not None else None
        if cpu is None:
            return
        if on:
            cpu.irq_flags |= IRQ_BIT
        else:
            cpu.irq_flags &= ~IRQ_BIT


def make_mapper(cart):
    if cart.mapper == 4:
        return MMC3(cart)
    raise NotImplementedError(f'mapper {cart.mapper} (re-c is MMC3 = 4)')
