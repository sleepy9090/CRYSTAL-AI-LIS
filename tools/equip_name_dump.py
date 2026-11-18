#!/usr/bin/env python3
"""Dump the status-screen equipment rows read by EquipNameRow 15:BE7A (session 72).

EquipNameRow: SetR6(6) (bank 6 at $8000), Y = $18 = equip slot 0-3 (sword/armor/shield/accessory),
ptr = EquipNameBase 15:BEB7[slot] ($80F0 $85F0 $8EF0 $97F0) + equip_sword[slot] * $100;
an empty slot (0) uses $80F0. 16 bytes -> $6010-$601F, then SetR6($1A).
Rows that land at >= $A000 would read the R7 bank (not bank 6), flagged below.

usage: equip_name_dump.py [max_id]
"""
import sys
import os
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom  # noqa: E402

PRG = load_rom()[1]
BASES = (0x80F0, 0x85F0, 0x8EF0, 0x97F0)
NAMES = ('sword', 'armor', 'shield', 'accessory')


def row(addr):
    return PRG[6 * 0x2000 + (addr & 0x1FFF): 6 * 0x2000 + (addr & 0x1FFF) + 16]


def txt(b):
    return ''.join(chr(c) if 0x20 <= c < 0x7F else '.' for c in b)


def rows7():
    """15:B6C1: A=n -> 15:B709[n] ($80F0 + n*$300), SetR6(7), copy $C0 bytes of the $xxF0-$xxFF rows of
    consecutive pages into $6000 (X backs off 2 at each page change -> 14 bytes kept per page)."""
    for n in range(10):
        o = 15 * 0x2000 + 0x1709 + 2 * n
        a = PRG[o] | PRG[o + 1] << 8
        buf = []
        src = a
        for _ in range(0xC0):
            buf.append(PRG[7 * 0x2000 + (src & 0x1FFF)])
            src += 1
            if src & 0xFF == 0:
                src = (src & 0xFF00) | 0xF0
                buf = buf[:-2]
        print(f'n={n} ${a:04X}: ' + ' / '.join(txt(bytes(buf[i:i + 14])) for i in range(0, 42, 14))
              + f'   (+{len(buf) - 42} more bytes)')


def main(argv):
    if '--bank7' in argv:
        return rows7()
    mx = int(argv[0], 0) if argv else 10
    print('empty    $80F0', row(0x80F0).hex(' '), '|' + txt(row(0x80F0)) + '|')
    for s, base in enumerate(BASES):
        for i in range(1, mx + 1):
            a = base + i * 0x100
            flag = '' if a < 0xA000 else '  (>= $A000: R7 bank, not 6)'
            print(f'{NAMES[s]:9} {i:2} ${a:04X}', row(a).hex(' '), '|' + txt(row(a)) + '|' + flag)


if __name__ == '__main__':
    main(sys.argv[1:])
