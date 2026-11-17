"""Ricoh 2A03 CPU core (6502 without decimal mode).

Opcode set = tools/instruction_set.OPCODES (151 official + the illegal ones listed there:
LAX, SAX, SBC #imm ($EB), the 1-byte/zp/abs/imm NOPs). Any other opcode raises IllegalOpcode
(re-c never executes one on the traced paths; KIL/JAM opcodes would hang a real 2A03).

Timing: exact per-instruction cycle counts incl. +1 page-cross for read ops on abs,X / abs,Y /
(zp),Y, +1 taken branch, +1 more if the branch crosses a page. Interrupt entry = 7 cycles.
OAM DMA stall comes from the bus (cpu.stall). Not modelled: dummy reads/writes (RMW double
write, abs,X dummy read), the 1-instruction IRQ latency after CLI/SEI/PLP, NMI hijacking BRK.
Decimal flag D is stored/pushed but ADC/SBC are always binary (2A03).

Interrupts: cpu.nmi() = edge (taken before the next instruction); cpu.irq_flags = bitmask of
asserted level-IRQ sources (mapper=1, APU frame=2, DMC=4); taken when non-zero and I clear.
"""
from . import _TOOLS  # noqa: F401
from instruction_set import OPCODES

C, Z, I, D, B, U, V, N = 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80

READ_OPS = {'ORA', 'AND', 'EOR', 'ADC', 'SBC', 'CMP', 'CPX', 'CPY', 'BIT', 'LDA', 'LDX', 'LDY',
            '*LAX', '*SBC', '*NOP'}
STORE_OPS = {'STA', 'STX', 'STY', '*SAX'}
RMW_OPS = {'ASL', 'LSR', 'ROL', 'ROR', 'INC', 'DEC'}

_READ_CYC = {'imm': 2, 'zp': 3, 'zpx': 4, 'zpy': 4, 'abs': 4, 'abx': 4, 'aby': 4, 'izx': 6, 'izy': 5,
             'imp': 2}
_STORE_CYC = {'zp': 3, 'zpx': 4, 'zpy': 4, 'abs': 4, 'abx': 5, 'aby': 5, 'izx': 6, 'izy': 6}
_RMW_CYC = {'acc': 2, 'zp': 5, 'zpx': 6, 'abs': 6, 'abx': 7}
_IMP_CYC = {'PHA': 3, 'PHP': 3, 'PLA': 4, 'PLP': 4, 'RTS': 6, 'RTI': 6, 'BRK': 7}
_PENALTY_MODES = {'abx', 'aby', 'izy'}


class IllegalOpcode(Exception):
    pass


def base_cycles(mn, md):
    """(cycles, page_cross_penalty) for an opcode from the instruction_set DB."""
    if mn == 'JMP':
        return (3 if md == 'abs' else 5), False
    if mn == 'JSR':
        return 6, False
    if md == 'rel':
        return 2, False
    if mn in RMW_OPS:
        return _RMW_CYC[md], False
    if mn in STORE_OPS:
        return _STORE_CYC[md], False
    if mn in READ_OPS:
        return _READ_CYC[md], md in _PENALTY_MODES
    return _IMP_CYC.get(mn, 2), False


class CPU:
    def __init__(self, bus):
        self.bus = bus
        bus.cpu = self
        self.a = self.x = self.y = 0
        self.s = 0xFD
        self.p = I | U
        self.pc = 0
        self.cycles = 0
        self.stall = 0
        self.nmi_pending = False
        self.irq_flags = 0
        self.trace = None            # optional callable(cpu) before each instruction
        self.table = [None] * 256
        for op, (mn, md) in OPCODES.items():
            fn = getattr(self, '_op_' + mn.lstrip('*'))
            cyc, pen = base_cycles(mn, md)
            self.table[op] = (fn, md, cyc, pen, mn)

    # ---------- setup ----------
    def reset(self):
        self.s = (self.s - 3) & 0xFF
        self.p |= I
        self.pc = self.bus.read(0xFFFC) | self.bus.read(0xFFFD) << 8
        self.cycles += 7

    def power_on(self):
        self.a = self.x = self.y = 0
        self.s = 0x00
        self.p = I | U
        self.reset()            # S ends at $FD like real hardware

    def nmi(self):
        self.nmi_pending = True

    # ---------- helpers ----------
    def _push(self, v):
        self.bus.write(0x100 | self.s, v)
        self.s = (self.s - 1) & 0xFF

    def _pull(self):
        self.s = (self.s + 1) & 0xFF
        return self.bus.read(0x100 | self.s)

    def _nz(self, v):
        self.p = (self.p & 0x7D) | (v & N) | (0 if v else Z)
        return v

    def _interrupt(self, vector, brk=False):
        pc = self.pc
        self._push(pc >> 8)
        self._push(pc & 0xFF)
        self._push((self.p | U | B) if brk else ((self.p | U) & ~B & 0xFF))
        self.p |= I
        self.pc = self.bus.read(vector) | self.bus.read(vector + 1) << 8
        self.cycles += 7
        return 7

    # ---------- main loop ----------
    def step(self):
        """Execute one instruction (or take a pending interrupt / DMA stall). Returns cycles used."""
        if self.stall:
            c = self.stall
            self.stall = 0
            self.cycles += c
            return c
        if self.nmi_pending:
            self.nmi_pending = False
            return self._interrupt(0xFFFA)
        if self.irq_flags and not (self.p & I):
            return self._interrupt(0xFFFE)
        if self.trace:
            self.trace(self)
        bus = self.bus
        pc = self.pc
        op = bus.read(pc)
        ent = self.table[op]
        if ent is None:
            raise IllegalOpcode(f'opcode ${op:02X} at ${pc:04X}')
        fn, md, cyc, pen, _ = ent
        self._extra = 0
        if md == 'imp' or md == 'acc':
            self.pc = (pc + 1) & 0xFFFF
            addr = None
        elif md == 'imm':
            addr = (pc + 1) & 0xFFFF
            self.pc = (pc + 2) & 0xFFFF
        elif md == 'zp':
            addr = bus.read((pc + 1) & 0xFFFF)
            self.pc = (pc + 2) & 0xFFFF
        elif md == 'zpx':
            addr = (bus.read((pc + 1) & 0xFFFF) + self.x) & 0xFF
            self.pc = (pc + 2) & 0xFFFF
        elif md == 'zpy':
            addr = (bus.read((pc + 1) & 0xFFFF) + self.y) & 0xFF
            self.pc = (pc + 2) & 0xFFFF
        elif md == 'rel':
            off = bus.read((pc + 1) & 0xFFFF)
            self.pc = (pc + 2) & 0xFFFF
            addr = (self.pc + (off - 256 if off & 0x80 else off)) & 0xFFFF
        elif md == 'izx':
            zp = (bus.read((pc + 1) & 0xFFFF) + self.x) & 0xFF
            addr = bus.read(zp) | bus.read((zp + 1) & 0xFF) << 8
            self.pc = (pc + 2) & 0xFFFF
        elif md == 'izy':
            zp = bus.read((pc + 1) & 0xFFFF)
            base = bus.read(zp) | bus.read((zp + 1) & 0xFF) << 8
            addr = (base + self.y) & 0xFFFF
            if pen and (base ^ addr) & 0xFF00:
                self._extra = 1
            self.pc = (pc + 2) & 0xFFFF
        else:
            base = bus.read((pc + 1) & 0xFFFF) | bus.read((pc + 2) & 0xFFFF) << 8
            self.pc = (pc + 3) & 0xFFFF
            if md == 'abs':
                addr = base
            elif md == 'abx':
                addr = (base + self.x) & 0xFFFF
                if pen and (base ^ addr) & 0xFF00:
                    self._extra = 1
            elif md == 'aby':
                addr = (base + self.y) & 0xFFFF
                if pen and (base ^ addr) & 0xFF00:
                    self._extra = 1
            else:  # ind (JMP only): 6502 bug - high byte read wraps within the page
                addr = bus.read(base) | bus.read((base & 0xFF00) | ((base + 1) & 0xFF)) << 8
        fn(addr, md)
        c = cyc + self._extra
        self.cycles += c
        return c

    def run(self, max_instr=None, until_pc=None, max_cycles=None):
        """Step until a PC in `until_pc` (set) is reached, or a limit. Returns instructions run."""
        n = 0
        end = self.cycles + max_cycles if max_cycles is not None else None
        while True:
            if until_pc is not None and self.pc in until_pc:
                return n
            if max_instr is not None and n >= max_instr:
                return n
            if end is not None and self.cycles >= end:
                return n
            self.step()
            n += 1

    # ---------- operand helpers ----------
    def _rd(self, addr):
        return self.bus.read(addr)

    def _rmw(self, addr, md, f):
        if md == 'acc':
            self.a = f(self.a)
        else:
            self.bus.write(addr, f(self.bus.read(addr)))

    # ---------- loads/stores ----------
    def _op_LDA(self, a, md): self.a = self._nz(self._rd(a))
    def _op_LDX(self, a, md): self.x = self._nz(self._rd(a))
    def _op_LDY(self, a, md): self.y = self._nz(self._rd(a))
    def _op_LAX(self, a, md): self.a = self.x = self._nz(self._rd(a))
    def _op_STA(self, a, md): self.bus.write(a, self.a)
    def _op_STX(self, a, md): self.bus.write(a, self.x)
    def _op_STY(self, a, md): self.bus.write(a, self.y)
    def _op_SAX(self, a, md): self.bus.write(a, self.a & self.x)

    # ---------- transfers ----------
    def _op_TAX(self, a, md): self.x = self._nz(self.a)
    def _op_TAY(self, a, md): self.y = self._nz(self.a)
    def _op_TXA(self, a, md): self.a = self._nz(self.x)
    def _op_TYA(self, a, md): self.a = self._nz(self.y)
    def _op_TSX(self, a, md): self.x = self._nz(self.s)
    def _op_TXS(self, a, md): self.s = self.x

    # ---------- stack ----------
    def _op_PHA(self, a, md): self._push(self.a)
    def _op_PHP(self, a, md): self._push(self.p | B | U)
    def _op_PLA(self, a, md): self.a = self._nz(self._pull())
    def _op_PLP(self, a, md): self.p = (self._pull() & ~B & 0xFF) | U

    # ---------- logic / arithmetic ----------
    def _op_AND(self, a, md): self.a = self._nz(self.a & self._rd(a))
    def _op_ORA(self, a, md): self.a = self._nz(self.a | self._rd(a))
    def _op_EOR(self, a, md): self.a = self._nz(self.a ^ self._rd(a))

    def _adc(self, v):
        acc = self.a
        r = acc + v + (self.p & C)
        p = self.p & ~(C | V | Z | N) & 0xFF
        if r > 0xFF:
            p |= C
        r &= 0xFF
        if (~(acc ^ v) & (acc ^ r)) & 0x80:
            p |= V
        self.p = p | (r & N) | (0 if r else Z)
        self.a = r

    def _op_ADC(self, a, md): self._adc(self._rd(a))
    def _op_SBC(self, a, md): self._adc(self._rd(a) ^ 0xFF)

    def _cmp(self, reg, v):
        r = (reg - v) & 0xFF
        self.p = (self.p & ~(C | Z | N) & 0xFF) | (C if reg >= v else 0) | (r & N) | (0 if r else Z)

    def _op_CMP(self, a, md): self._cmp(self.a, self._rd(a))
    def _op_CPX(self, a, md): self._cmp(self.x, self._rd(a))
    def _op_CPY(self, a, md): self._cmp(self.y, self._rd(a))

    def _op_BIT(self, a, md):
        v = self._rd(a)
        self.p = (self.p & 0x3D) | (v & 0xC0) | (0 if v & self.a else Z)

    # ---------- inc/dec ----------
    def _op_INC(self, a, md): self._rmw(a, md, lambda v: self._nz((v + 1) & 0xFF))
    def _op_DEC(self, a, md): self._rmw(a, md, lambda v: self._nz((v - 1) & 0xFF))
    def _op_INX(self, a, md): self.x = self._nz((self.x + 1) & 0xFF)
    def _op_INY(self, a, md): self.y = self._nz((self.y + 1) & 0xFF)
    def _op_DEX(self, a, md): self.x = self._nz((self.x - 1) & 0xFF)
    def _op_DEY(self, a, md): self.y = self._nz((self.y - 1) & 0xFF)

    # ---------- shifts ----------
    def _setc(self, bit):
        self.p = (self.p & ~C & 0xFF) | (C if bit else 0)

    def _asl(self, v):
        self._setc(v & 0x80)
        return self._nz((v << 1) & 0xFF)

    def _lsr(self, v):
        self._setc(v & 1)
        return self._nz(v >> 1)

    def _rol(self, v):
        c = self.p & C
        self._setc(v & 0x80)
        return self._nz(((v << 1) | c) & 0xFF)

    def _ror(self, v):
        c = self.p & C
        self._setc(v & 1)
        return self._nz((v >> 1) | (c << 7))

    def _op_ASL(self, a, md): self._rmw(a, md, self._asl)
    def _op_LSR(self, a, md): self._rmw(a, md, self._lsr)
    def _op_ROL(self, a, md): self._rmw(a, md, self._rol)
    def _op_ROR(self, a, md): self._rmw(a, md, self._ror)

    # ---------- flags ----------
    def _op_CLC(self, a, md): self.p &= ~C & 0xFF
    def _op_SEC(self, a, md): self.p |= C
    def _op_CLI(self, a, md): self.p &= ~I & 0xFF
    def _op_SEI(self, a, md): self.p |= I
    def _op_CLD(self, a, md): self.p &= ~D & 0xFF
    def _op_SED(self, a, md): self.p |= D
    def _op_CLV(self, a, md): self.p &= ~V & 0xFF

    # ---------- branches ----------
    def _branch(self, cond, target):
        if cond:
            self._extra = 2 if (self.pc ^ target) & 0xFF00 else 1
            self.pc = target

    def _op_BPL(self, a, md): self._branch(not self.p & N, a)
    def _op_BMI(self, a, md): self._branch(self.p & N, a)
    def _op_BVC(self, a, md): self._branch(not self.p & V, a)
    def _op_BVS(self, a, md): self._branch(self.p & V, a)
    def _op_BCC(self, a, md): self._branch(not self.p & C, a)
    def _op_BCS(self, a, md): self._branch(self.p & C, a)
    def _op_BNE(self, a, md): self._branch(not self.p & Z, a)
    def _op_BEQ(self, a, md): self._branch(self.p & Z, a)

    # ---------- jumps ----------
    def _op_JMP(self, a, md): self.pc = a

    def _op_JSR(self, a, md):
        ret = (self.pc - 1) & 0xFFFF       # address of the JSR's last byte
        self._push(ret >> 8)
        self._push(ret & 0xFF)
        self.pc = a

    def _op_RTS(self, a, md):
        lo = self._pull()
        self.pc = ((self._pull() << 8 | lo) + 1) & 0xFFFF

    def _op_RTI(self, a, md):
        self.p = (self._pull() & ~B & 0xFF) | U
        lo = self._pull()
        self.pc = self._pull() << 8 | lo

    def _op_BRK(self, a, md):
        self.pc = (self.pc + 1) & 0xFFFF   # BRK skips a padding byte
        self._interrupt(0xFFFE, brk=True)
        self.cycles -= 7                   # counted by the table entry instead

    def _op_NOP(self, a, md):
        if a is not None and md != 'imm':
            self._rd(a)                    # illegal zp/abs NOPs do a read
