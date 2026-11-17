#!/usr/bin/env python3
"""MMC3 tests for tools/emu/mapper.py (emu 2/6).

Run: python3 tools/emu/test_mapper.py        (from the repo root; uses rom/re-c.nes)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from emu.loader import load  # noqa: E402
from emu.mapper import MMC3, A12_LOW_DOTS  # noqa: E402
from emu.memory import Bus  # noqa: E402
from emu.cpu import CPU  # noqa: E402
from test_cpu import eq, _StubPpu  # noqa: E402

CART = load()
_results = []


def test(fn):
    try:
        fn()
        _results.append((fn.__name__, None))
    except AssertionError as e:
        _results.append((fn.__name__, str(e) or 'assert'))
    except Exception as e:  # noqa: BLE001
        _results.append((fn.__name__, f'{type(e).__name__}: {e}'))
    return fn


def prg_byte(bank, addr):
    return CART.prg[bank * 0x2000 + (addr & 0x1FFF)]


def mk():
    bus = Bus(CART, ppu=_StubPpu())
    cpu = CPU(bus)
    return bus, bus.mapper, cpu


@test
def bus_builds_mmc3():
    bus, m, _ = mk()
    assert isinstance(m, MMC3) and m.bus is bus, 'Bus attaches MMC3'
    eq(m.prg_banks, (0, 1, 30, 31), 'power-on PRG mode 0 layout')


@test
def prg_mode0_and_1():
    _, m, _ = mk()
    m.write(0x8000, 6); m.write(0x8001, 24)
    m.write(0x8000, 7); m.write(0x8001, 25)
    eq(m.prg_banks, (24, 25, 30, 31), 'mode 0')
    for a in (0x8000, 0x9FFF, 0xA123, 0xC40E, 0xFFFC):
        eq(m.read(a), prg_byte(m.prg_bank_at(a), a), f'read ${a:04X}')
    m.write(0x8000, 0x46)                      # PRG mode 1
    eq(m.prg_banks, (30, 25, 24, 31), 'mode 1 swaps $8000/$C000')
    m.write(0x8001, 40)                        # R6 = 40 -> wraps mod 32 (bits 6-7 ignored too)
    eq(m.prg_banks[2], 40 % 32, 'R6 wraps to bank count')


@test
def chr_modes():
    _, m, _ = mk()
    for r, v in enumerate((0x11, 0x20, 0x30, 0x31, 0x32, 0x33)):
        m.write(0x8000, r); m.write(0x8001, v)
    eq(m.chr_banks, (0x10, 0x11, 0x20, 0x21, 0x30, 0x31, 0x32, 0x33), 'inversion 0 (R0/R1 low bit dropped)')
    m.write(0x8000, 0x80)
    eq(m.chr_banks, (0x30, 0x31, 0x32, 0x33, 0x10, 0x11, 0x20, 0x21), 'inversion 1')
    eq(m.chr_read(0x0005), CART.chr[0x30 * 0x400 + 5], 'chr_read slot 0')
    eq(m.chr_read(0x1C07), CART.chr[0x21 * 0x400 + 7], 'chr_read slot 7')
    m.write(0x8000, 0); m.write(0x8001, 0xFF)  # inversion back to 0; 255 -> 127 mod 128 1KB banks
    eq(m.chr_banks[0:2], (0x7E, 0x7F), 'R0 wraps mod 128')


@test
def mirroring_and_wram():
    bus, m, _ = mk()
    m.write(0xA000, 0)
    eq(m.nt_index(0x2400), 0x400, 'vertical $2400'); eq(m.nt_index(0x2800), 0x000, 'vertical $2800')
    m.write(0xA000, 1)
    eq(m.nt_index(0x2400), 0x000, 'horizontal $2400'); eq(m.nt_index(0x2800), 0x400, 'horizontal $2800')
    eq(m.nt_index(0x2C3F), 0x43F, 'horizontal $2C3F')
    bus.write(0xA001, 0x80); bus.write(0x6000, 0x5A)
    eq(bus.read(0x6000), 0x5A, 'WRAM enabled+writable')
    bus.write(0xA001, 0xC0); bus.write(0x6000, 0x11)
    eq(bus.read(0x6000), 0x5A, 'write-protect blocks writes')
    bus.write(0xA001, 0x00); bus.read(0x0000)
    eq(bus.read(0x6000), bus.open_bus, 'disabled -> open bus')


@test
def irq_counter():
    bus, m, cpu = mk()
    m.write(0xC000, 3); m.write(0xC001, 0); m.write(0xE001, 0)
    fired = []
    for line in range(10):
        m.clock_scanline()
        fired.append(bool(cpu.irq_flags & 1))
        if cpu.irq_flags & 1:
            m.write(0xE000, 0); m.write(0xE001, 0)   # ack + re-enable (like an IRQ handler)
    # clock 0 reloads 3, then 2,1,0 -> fire on clock 3; counter 0 reloads 3 on clock 4 ... fire on 7
    eq(fired, [False, False, False, True, False, False, False, True, False, False], 'IRQ every latch+1 lines')
    m.write(0xE000, 0)
    m.write(0xC000, 0); m.write(0xC001, 0); m.write(0xE001, 0)
    m.clock_scanline()
    assert cpu.irq_flags & 1, 'latch 0 + reload -> IRQ every clock (new-MMC3 behaviour)'
    m.write(0xE000, 0)
    assert not cpu.irq_flags & 1 and not m.irq_enabled, '$E000 acks + disables'
    m.clock_scanline()
    assert not cpu.irq_flags & 1, 'disabled -> no IRQ'


@test
def irq_taken_by_cpu():
    bus, m, cpu = mk()
    cpu.power_on()
    m.write(0xC000, 0); m.write(0xC001, 0); m.write(0xE001, 0)
    m.clock_scanline()
    cpu.p &= ~0x04                               # clear I
    cpu.step()
    eq(cpu.pc, bus.read16(0xFFFE), 'IRQ vector taken')


@test
def a12_filter():
    _, m, _ = mk()
    m.write(0xC000, 5); m.write(0xC001, 0)
    dot = 0
    m.ppu_a12(0, dot)
    for _ in range(8):                           # 8 BG-style fetches 2 dots apart: only 1st counts
        dot += A12_LOW_DOTS
        m.ppu_a12(1, dot); m.ppu_a12(0, dot + 1)
        dot += 1
    for _ in range(7):                           # quick toggles (< filter) do not clock
        dot += 2
        m.ppu_a12(1, dot); dot += 1; m.ppu_a12(0, dot)
    eq(m.irq_clocks, 8, 'only rises after >= A12_LOW_DOTS low are counted')


@test
def state_roundtrip():
    _, m, _ = mk()
    m.write(0x8000, 0x47); m.write(0x8001, 9); m.write(0xC000, 7); m.write(0xC001, 0)
    s = m.state()
    _, m2, _ = mk()
    m2.load_state(s)
    eq(m2.prg_banks, m.prg_banks, 'PRG'); eq(m2.chr_banks, m.chr_banks, 'CHR')
    eq(m2.irq_latch, 7, 'latch'); assert m2.irq_reload, 'reload flag'


@test
def rom_reset_to_8020():
    """Reset 31:F2A4 -> SwitchBank16(A=$0C) -> JSR $8020 with banks 24/25 mapped."""
    bus, m, cpu = mk()
    cpu.power_on()
    eq(cpu.pc, 0xF2A4, 'reset vector')
    n1 = cpu.run(max_instr=200000, until_pc={0xC40E})
    eq(cpu.pc, 0xC40E, 'SwitchBank16 reached'); eq(cpu.a, 0x0C, 'A = 16KB pair $0C')
    eq(m.mirroring, 1, 'Reset wrote $A000=1 (horizontal)')
    assert m.wram_enabled and m.wram_writable, 'Reset wrote $A001=$80'
    assert not m.irq_enabled, 'Reset wrote $E000'
    n2 = cpu.run(max_instr=1000, until_pc={0x8020})
    eq(cpu.pc, 0x8020, '$8020 reached')
    eq(m.prg_banks, (24, 25, 30, 31), 'banks 24/25 at $8000/$A000')
    eq(bus.ram[0x6E], 0x18, '$6E = R6 shadow (2n = $18)')
    eq(bus.ram[0x6F], 0x19, '$6F = R7 shadow (2n+1)')
    eq(bus.ram[0x50], 0x06, '$50 = last bank-select value (6)')
    eq(m.regs[6], 0x18, 'R6'); eq(m.regs[7], 0x19, 'R7')
    eq(bus.read(0x8020), prg_byte(24, 0x8020), 'opcode at $8020 comes from bank 24')
    eq(m.prg_mode, 0, 'PRG mode 0'); eq(m.chr_inv, 0, 'CHR inversion 0')
    print(f'  rom: Reset -> $C40E in {n1} instr, -> $8020 in {n2} more; {len(m.writes)} mapper writes: '
          + ' '.join(f'{a:04X}={v:02X}' for a, v in m.writes))


def main():
    bad = [(n, e) for n, e in _results if e]
    for n, e in _results:
        print(f'  {"FAIL" if e else "ok  "} {n}' + (f' - {e}' if e else ''))
    print(f'{len(_results) - len(bad)}/{len(_results)} passed')
    sys.exit(1 if bad else 0)


if __name__ == '__main__':
    main()
