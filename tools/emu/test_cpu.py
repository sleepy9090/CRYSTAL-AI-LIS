#!/usr/bin/env python3
"""Hand-written 6502/2A03 unit tests for tools/emu/cpu.py (emu 1/6).

Run: python3 tools/emu/test_cpu.py        (from the repo root)
     python3 tools/emu/test_cpu.py --rom  (also the re-c reset smoke test)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from emu.cpu import CPU, C, Z, I, D, B, U, V, N, IllegalOpcode, base_cycles  # noqa: E402
from emu.memory import FlatBus, Bus  # noqa: E402
from instruction_set import OPCODES  # noqa: E402

ORG = 0x0600
_results = []


def mk(code, a=0, x=0, y=0, p=U, s=0xFD, mem=None):
    bus = FlatBus()
    bus.load(ORG, code)
    for addr, data in (mem or {}).items():
        bus.load(addr, data if isinstance(data, (bytes, list)) else [data])
    cpu = CPU(bus)
    cpu.a, cpu.x, cpu.y, cpu.p, cpu.s, cpu.pc = a, x, y, p, s, ORG
    return cpu, bus


def run1(code, **kw):
    cpu, bus = mk(code, **kw)
    cyc = cpu.step()
    return cpu, bus, cyc


def test(fn):
    try:
        fn()
        _results.append((fn.__name__, None))
    except AssertionError as e:
        _results.append((fn.__name__, str(e) or 'assert'))
    except Exception as e:  # noqa: BLE001
        _results.append((fn.__name__, f'{type(e).__name__}: {e}'))
    return fn


def eq(got, want, what=''):
    assert got == want, f'{what}: got {got!r} want {want!r}' if not isinstance(got, int) else \
        f'{what}: got ${got:02X} want ${want:02X}'


def flags(cpu, want, mask):
    eq(cpu.p & mask, want & mask, 'flags')


# ---------------- ADC / SBC ----------------
@test
def adc_basic_and_overflow():
    cases = [  # a, operand, carry_in -> result, C, V, Z, N
        (0x50, 0x10, 0, 0x60, 0, 0, 0, 0),
        (0x50, 0x50, 0, 0xA0, 0, V, 0, N),     # +80 + +80 = signed overflow
        (0x50, 0x90, 0, 0xE0, 0, 0, 0, N),
        (0x50, 0xD0, 0, 0x20, C, 0, 0, 0),
        (0xD0, 0x90, 0, 0x60, C, V, 0, 0),     # -48 + -112 = overflow
        (0xFF, 0x01, 0, 0x00, C, 0, Z, 0),
        (0xFF, 0x00, 1, 0x00, C, 0, Z, 0),     # carry in
        (0x7F, 0x00, 1, 0x80, 0, V, 0, N),
    ]
    for a, m, cin, r, c, v, z, n in cases:
        cpu, _, cyc = run1([0x69, m], a=a, p=U | cin)
        eq(cpu.a, r, f'ADC {a:02X}+{m:02X}+{cin}')
        flags(cpu, c | v | z | n, C | V | Z | N)
        eq(cyc, 2, 'ADC # cycles')


@test
def sbc_basic_and_overflow():
    cases = [  # a, operand, carry_in(=no borrow) -> result, C, V
        (0x50, 0xF0, 1, 0x60, 0, 0),
        (0x50, 0xB0, 1, 0xA0, 0, V),           # 80 - (-80) overflows
        (0x50, 0x70, 1, 0xE0, 0, 0),
        (0x50, 0x30, 1, 0x20, C, 0),
        (0xD0, 0x70, 1, 0x60, C, V),           # -48 - 112 overflows
        (0x05, 0x05, 1, 0x00, C, 0),
        (0x05, 0x05, 0, 0xFF, 0, 0),           # borrow in
    ]
    for a, m, cin, r, c, v in cases:
        for opc in (0xE9, 0xEB):               # official and illegal SBC #imm
            cpu, _, _ = run1([opc, m], a=a, p=U | cin)
            eq(cpu.a, r, f'SBC({opc:02X}) {a:02X}-{m:02X}')
            flags(cpu, c | v | (Z if r == 0 else 0) | (r & N), C | V | Z | N)


@test
def decimal_mode_ignored():
    cpu, _, _ = run1([0x69, 0x01], a=0x09, p=U | D)
    eq(cpu.a, 0x0A, 'ADC with D set is binary on 2A03')
    assert cpu.p & D, 'D stays set'


# ---------------- compares / BIT ----------------
@test
def compares():
    for opc, reg in ((0xC9, 'a'), (0xE0, 'x'), (0xC0, 'y')):
        for r, m, want in ((0x40, 0x30, C), (0x40, 0x40, C | Z), (0x30, 0x40, N), (0x00, 0x01, N),
                           (0x80, 0x01, C), (0x01, 0xFF, 0)):
            cpu, _, _ = run1([opc, m], **{reg: r})
            flags(cpu, want, C | Z | N)
            eq(getattr(cpu, reg), r, 'compare must not modify register')


@test
def bit_flags():
    cpu, _, cyc = run1([0x24, 0x10], a=0x01, mem={0x10: 0xC0})
    flags(cpu, N | V | Z, N | V | Z)
    eq(cyc, 3, 'BIT zp cycles')
    cpu, _, cyc = run1([0x2C, 0x00, 0x20], a=0x01, p=U | N | V, mem={0x2000: 0x01})
    flags(cpu, 0, N | V | Z)
    eq(cpu.a, 0x01, 'BIT keeps A')
    eq(cyc, 4, 'BIT abs cycles')


# ---------------- shifts / rotates ----------------
@test
def shifts_acc():
    cpu, _, cyc = run1([0x0A], a=0x81)
    eq(cpu.a, 0x02, 'ASL A'); flags(cpu, C, C | Z | N); eq(cyc, 2, 'ASL A cycles')
    cpu, _, _ = run1([0x4A], a=0x01)
    eq(cpu.a, 0x00, 'LSR A'); flags(cpu, C | Z, C | Z | N)
    cpu, _, _ = run1([0x2A], a=0x40, p=U | C)
    eq(cpu.a, 0x81, 'ROL A'); flags(cpu, N, C | Z | N)
    cpu, _, _ = run1([0x6A], a=0x01, p=U | C)
    eq(cpu.a, 0x80, 'ROR A'); flags(cpu, C | N, C | Z | N)
    cpu, _, _ = run1([0x6A], a=0x02, p=U)
    eq(cpu.a, 0x01, 'ROR A no carry'); flags(cpu, 0, C | Z | N)


@test
def shifts_mem_cycles():
    cpu, bus, cyc = run1([0x06, 0x20], mem={0x20: 0x80})
    eq(bus.mem[0x20], 0, 'ASL zp'); flags(cpu, C | Z, C | Z | N); eq(cyc, 5, 'ASL zp cycles')
    cpu, bus, cyc = run1([0x7E, 0xFF, 0x10], x=1, p=U | C, mem={0x1100: 0x00})
    eq(bus.mem[0x1100], 0x80, 'ROR abs,X'); eq(cyc, 7, 'RMW abs,X: no page penalty, always 7')
    cpu, bus, cyc = run1([0x56, 0xFF], x=2, mem={0x01: 0x03})
    eq(bus.mem[0x01], 0x01, 'LSR zp,X wraps to $01'); eq(cyc, 6, 'LSR zp,X cycles')


# ---------------- INC/DEC ----------------
@test
def inc_dec():
    cpu, bus, cyc = run1([0xE6, 0x10], mem={0x10: 0xFF})
    eq(bus.mem[0x10], 0, 'INC zp wrap'); flags(cpu, Z, Z | N); eq(cyc, 5, 'INC zp cycles')
    cpu, bus, cyc = run1([0xCE, 0x34, 0x12], mem={0x1234: 0x00})
    eq(bus.mem[0x1234], 0xFF, 'DEC abs'); flags(cpu, N, Z | N); eq(cyc, 6, 'DEC abs cycles')
    cpu, _, _ = run1([0xCA], x=0)
    eq(cpu.x, 0xFF, 'DEX wrap'); flags(cpu, N, Z | N)
    cpu, _, _ = run1([0xC8], y=0xFF)
    eq(cpu.y, 0, 'INY wrap'); flags(cpu, Z, Z | N)


# ---------------- addressing wraps + penalties ----------------
@test
def jmp_indirect_page_wrap_bug():
    cpu, _, cyc = run1([0x6C, 0xFF, 0x10], mem={0x10FF: 0x34, 0x1000: 0x12, 0x1100: 0x99})
    eq(cpu.pc, 0x1234, 'JMP ($10FF) takes hi byte from $1000'); eq(cyc, 5, 'JMP ind cycles')
    cpu, _, cyc = run1([0x4C, 0x00, 0x80])
    eq(cpu.pc, 0x8000, 'JMP abs'); eq(cyc, 3, 'JMP abs cycles')


@test
def zp_indexed_wrap():
    cpu, _, cyc = run1([0xB5, 0xF0], x=0x20, mem={0x10: 0x5A, 0x110: 0xEE})
    eq(cpu.a, 0x5A, 'LDA $F0,X wraps in zero page'); eq(cyc, 4, 'LDA zp,X cycles')
    cpu, _, _ = run1([0xB6, 0xFF], y=0x02, mem={0x01: 0x77})
    eq(cpu.x, 0x77, 'LDX $FF,Y wraps')
    cpu, bus, _ = run1([0x96, 0x80], x=0x42, y=0x90)
    eq(bus.mem[0x10], 0x42, 'STX $80,Y wraps')


@test
def indirect_x_y():
    # (zp,X): pointer fetched from $FF/$00 wraps
    cpu, _, cyc = run1([0xA1, 0xFE], x=0x01, mem={0xFF: 0x00, 0x00: 0x30, 0x3000: 0xAB})
    eq(cpu.a, 0xAB, 'LDA ($FE,X) pointer at $FF/$00'); eq(cyc, 6, 'izx cycles')
    # (zp),Y: pointer at $FF/$00 wraps, page cross +1
    cpu, _, cyc = run1([0xB1, 0xFF], y=0x10, mem={0xFF: 0xF8, 0x00: 0x20, 0x2108: 0xCD})
    eq(cpu.a, 0xCD, 'LDA ($FF),Y'); eq(cyc, 6, 'izy with page cross')
    cpu, _, cyc = run1([0xB1, 0x40], y=0x01, mem={0x40: 0x00, 0x41: 0x20, 0x2001: 0x11})
    eq(cyc, 5, 'izy no cross')
    cpu, bus, cyc = run1([0x91, 0x40], a=0x99, y=0xFF, mem={0x40: 0x01, 0x41: 0x20})
    eq(bus.mem[0x2100], 0x99, 'STA (zp),Y'); eq(cyc, 6, 'STA izy always 6')


@test
def abs_indexed_penalty():
    cpu, _, cyc = run1([0xBD, 0xFF, 0x20], x=1, mem={0x2100: 0x42})
    eq(cpu.a, 0x42, 'LDA abs,X'); eq(cyc, 5, 'LDA abs,X page cross')
    cpu, _, cyc = run1([0xB9, 0x00, 0x20], y=5)
    eq(cyc, 4, 'LDA abs,Y no cross')
    cpu, bus, cyc = run1([0x9D, 0xFF, 0x20], a=7, x=1)
    eq(bus.mem[0x2100], 7, 'STA abs,X'); eq(cyc, 5, 'STA abs,X always 5')
    cpu, _, cyc = run1([0xBD, 0xFF, 0xFF], x=2, mem={0x0001: 0x66})
    eq(cpu.a, 0x66, 'abs,X wraps at $FFFF'); eq(cyc, 5, 'wrap counts as page cross')


# ---------------- branches ----------------
@test
def branch_cycles():
    cpu, _, cyc = run1([0xD0, 0x05], p=U | Z)
    eq(cpu.pc, ORG + 2, 'BNE not taken'); eq(cyc, 2, 'not taken')
    cpu, _, cyc = run1([0xF0, 0x05], p=U | Z)
    eq(cpu.pc, ORG + 7, 'BEQ taken fwd'); eq(cyc, 3, 'taken same page')
    cpu, _, cyc = run1([0x10, 0x80])         # BPL -128 from $0602 -> $0582
    eq(cpu.pc, 0x0582, 'BPL back across page'); eq(cyc, 4, 'taken + page cross')
    for opc, flag, taken_when_set in ((0x10, N, False), (0x30, N, True), (0x50, V, False), (0x70, V, True),
                                      (0x90, C, False), (0xB0, C, True), (0xD0, Z, False), (0xF0, Z, True)):
        for st in (0, flag):
            cpu, _, _ = run1([opc, 0x10], p=U | st)
            taken = bool(st) == taken_when_set
            eq(cpu.pc, ORG + 2 + (0x10 if taken else 0), f'branch {opc:02X} p={st:02X}')


# ---------------- stack / subroutine ----------------
@test
def jsr_rts():
    cpu, bus = mk([0x20, 0x00, 0x07], mem={0x0700: 0x60})
    eq(cpu.step(), 6, 'JSR cycles'); eq(cpu.pc, 0x0700, 'JSR target')
    eq(bus.mem[0x1FD], 0x06, 'pushed hi'); eq(bus.mem[0x1FC], 0x02, 'pushed lo = last byte of JSR')
    eq(cpu.s, 0xFB, 'S after JSR')
    eq(cpu.step(), 6, 'RTS cycles'); eq(cpu.pc, ORG + 3, 'RTS return'); eq(cpu.s, 0xFD, 'S after RTS')


@test
def php_plp_b_bits():
    cpu, bus = mk([0x08, 0x28], p=U | C)
    eq(cpu.step(), 3, 'PHP cycles')
    eq(bus.mem[0x1FD], U | B | C, 'PHP pushes B and bit5 set')
    bus.mem[0x1FD] = 0xFF
    eq(cpu.step(), 4, 'PLP cycles')
    eq(cpu.p, 0xFF & ~B, 'PLP ignores B, bit5 always 1')


@test
def pha_pla_and_stack_wrap():
    cpu, bus = mk([0x48, 0xA9, 0x00, 0x68], a=0x80, s=0x00)
    cpu.step()
    eq(bus.mem[0x100], 0x80, 'PHA at $0100'); eq(cpu.s, 0xFF, 'S wraps to $FF')
    cpu.step()
    eq(cpu.step(), 4, 'PLA cycles'); eq(cpu.a, 0x80, 'PLA'); flags(cpu, N, N | Z)


@test
def transfers():
    cpu, _, _ = run1([0x9A], x=0x00, p=U)
    eq(cpu.s, 0, 'TXS'); flags(cpu, 0, Z | N)          # TXS sets no flags
    cpu, _, _ = run1([0xBA], s=0x80)
    eq(cpu.x, 0x80, 'TSX'); flags(cpu, N, Z | N)
    cpu, _, _ = run1([0xAA], a=0)
    flags(cpu, Z, Z | N)
    cpu, _, _ = run1([0x98], y=0x90)
    eq(cpu.a, 0x90, 'TYA'); flags(cpu, N, Z | N)


# ---------------- interrupts ----------------
@test
def brk_rti():
    cpu, bus = mk([0x00, 0xEA, 0xEA], p=U | C, mem={0xFFFE: [0x00, 0x09], 0x0900: 0x40})
    eq(cpu.step(), 7, 'BRK cycles')
    eq(cpu.pc, 0x0900, 'BRK vector'); assert cpu.p & I, 'BRK sets I'
    eq(bus.mem[0x1FB], U | B | C, 'BRK pushes B')
    eq(bus.mem[0x1FC], 0x02, 'BRK pushes PC+2 lo'); eq(bus.mem[0x1FD], 0x06, 'hi')
    eq(cpu.step(), 6, 'RTI cycles'); eq(cpu.pc, ORG + 2, 'RTI to PC+2'); eq(cpu.p, U | C, 'RTI restores P')


@test
def nmi_and_irq():
    cpu, bus = mk([0xEA, 0xEA], p=U, mem={0xFFFA: [0x00, 0x0A], 0xFFFE: [0x00, 0x0B]})
    cpu.nmi()
    eq(cpu.step(), 7, 'NMI entry cycles'); eq(cpu.pc, 0x0A00, 'NMI vector')
    eq(bus.mem[0x1FB] & B, 0, 'NMI pushes B clear'); eq(bus.mem[0x1FB] & U, U, 'bit5 set')
    cpu, bus = mk([0xEA, 0x58, 0xEA], p=U | I, mem={0xFFFE: [0x00, 0x0B]})
    cpu.irq_flags = 1
    cpu.step()
    eq(cpu.pc, ORG + 1, 'IRQ masked by I')
    cpu.step()                              # CLI
    cpu.step()
    eq(cpu.pc, 0x0B00, 'IRQ taken after CLI'); assert cpu.p & I, 'IRQ sets I'


# ---------------- illegal opcodes from instruction_set ----------------
@test
def lax_sax():
    cpu, _, cyc = run1([0xA7, 0x10], mem={0x10: 0x83})
    eq(cpu.a, 0x83, 'LAX a'); eq(cpu.x, 0x83, 'LAX x'); flags(cpu, N, N | Z); eq(cyc, 3, 'LAX zp')
    cpu, _, cyc = run1([0xBF, 0xFF, 0x20], y=1, mem={0x2100: 0x01})
    eq(cpu.x, 1, 'LAX aby'); eq(cyc, 5, 'LAX aby page cross')
    cpu, bus, cyc = run1([0x87, 0x10], a=0xF0, x=0x3C)
    eq(bus.mem[0x10], 0x30, 'SAX'); eq(cyc, 3, 'SAX zp')


@test
def illegal_nops():
    for code, size, cyc in (([0x1A], 1, 2), ([0x04, 0x10], 2, 3), ([0x0C, 0x00, 0x20], 3, 4), ([0x80, 0x55], 2, 2)):
        cpu, _, c = run1(code, a=1, p=U)
        eq(cpu.pc, ORG + size, f'NOP {code[0]:02X} size'); eq(c, cyc, f'NOP {code[0]:02X} cycles')
        eq(cpu.a, 1, 'NOP keeps A'); eq(cpu.p, U, 'NOP keeps flags')


@test
def unknown_opcode_raises():
    cpu, _ = mk([0x02])
    try:
        cpu.step()
    except IllegalOpcode:
        return
    raise AssertionError('KIL $02 should raise IllegalOpcode')


@test
def cycle_table_spot_checks():
    # Reference counts from the 6502 datasheet for one opcode per (mnemonic class, mode)
    ref = {0xA9: 2, 0xA5: 3, 0xB5: 4, 0xAD: 4, 0xBD: 4, 0xB9: 4, 0xA1: 6, 0xB1: 5,
           0x85: 3, 0x95: 4, 0x8D: 4, 0x9D: 5, 0x99: 5, 0x81: 6, 0x91: 6,
           0x06: 5, 0x16: 6, 0x0E: 6, 0x1E: 7, 0x0A: 2, 0xE6: 5, 0xFE: 7,
           0x48: 3, 0x08: 3, 0x68: 4, 0x28: 4, 0x20: 6, 0x60: 6, 0x40: 6, 0x00: 7,
           0x4C: 3, 0x6C: 5, 0xEA: 2, 0x18: 2, 0x9A: 2, 0xD0: 2, 0x24: 3, 0x2C: 4,
           0xB6: 4, 0x96: 4, 0xBE: 4, 0xE0: 2, 0xEC: 4, 0xA3: 6, 0xB3: 5, 0x83: 6, 0x97: 4}
    for op, want in ref.items():
        mn, md = OPCODES[op]
        eq(base_cycles(mn, md)[0], want, f'{op:02X} {mn} {md}')
    eq(sum(1 for op in OPCODES), len(OPCODES), 'n')
    assert len([op for op, (mn, _) in OPCODES.items() if not mn.startswith('*')]) == 151, '151 official opcodes'


# ---------------- re-c ROM smoke test ----------------
class _StubPpu:
    """$2002 alternates bit 7 so the two reset vblank-wait loops terminate."""

    def __init__(self):
        self.t = 0

    def read_reg(self, r):
        if r == 2:
            self.t ^= 0x80
            return self.t
        return 0

    def write_reg(self, r, v):
        pass

    def oam_dma(self, data):
        pass


def rom_smoke():
    from emu.loader import load
    cart = load()
    assert cart.mapper == 4 and cart.prg_banks8 == 32 and cart.battery, repr(cart)
    bus = Bus(cart, ppu=_StubPpu())
    cpu = CPU(bus)
    cpu.power_on()
    eq(cpu.pc, 0xF2A4, 'reset vector = Reset 31:F2A4')
    n = cpu.run(max_instr=200000, until_pc={0xC40E})
    eq(cpu.pc, 0xC40E, f'SwitchBank16 reached (after {n} instr)')
    writes = bus.mapper.writes
    assert (0xA001, 0x80) in writes and (0xE000, 0x80) in writes, 'reset MMC3 writes seen'
    print(f'  rom_smoke: Reset -> SwitchBank16 $C40E in {n} instructions / {cpu.cycles} cycles, '
          f'A=${cpu.a:02X} (16KB pair n), {len(writes)} mapper writes')


def main():
    if '--rom' in sys.argv:
        try:
            rom_smoke()
            _results.append(('rom_smoke', None))
        except Exception as e:  # noqa: BLE001
            _results.append(('rom_smoke', f'{type(e).__name__}: {e}'))
    bad = [(n, e) for n, e in _results if e]
    for n, e in _results:
        print(f'  {"FAIL" if e else "ok  "} {n}' + (f' - {e}' if e else ''))
    print(f'{len(_results) - len(bad)}/{len(_results)} passed')
    sys.exit(1 if bad else 0)


if __name__ == '__main__':
    main()
