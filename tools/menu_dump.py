#!/usr/bin/env python3
"""Dump the bank 16 menu string table (session 14). See REVERSE.md "Menu strings".

MenuPrint 16:8933 (A = string id -> $17):
  record = 16:8C1B + id*4 : PPU addr lo, PPU addr hi, string ptr lo, ptr hi
  string bytes are BG tile ids (= ASCII with the text font) written to $6000[$28]:
    FF       end (queue upload of $24 bytes to the PPU addr, 16:8AB0)
    FE s     6-char player name of save slot s&1 ($7118 / $7718)
    FD s     level of save slot s ($71F7 if s==0 else $77F7), two digits via 16:89E0 ("00".."16")
    FC       item name 16:9086[$19] ($FF -> $49), FF-terminated
usage: menu_dump.py [--items] [--out f]
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom  # noqa

PRG = load_rom()[1]
BANK = 16
TABLE = 0x8C1B
ITEMS = 0x9086


def rd(addr):
    # bank 16/17 pair at $8000-$BFFF
    bank = BANK + ((addr - 0x8000) >> 13)
    return PRG[bank * 0x2000 + (addr & 0x1FFF)]


def w(addr):
    return rd(addr) | rd(addr + 1) << 8


def ch(b):
    return chr(b) if 0x20 <= b < 0x7F else '{%02X}' % b


def decode(p, ff_only=False):
    out, n = [], 0
    while n < 200:
        b = rd(p); p += 1; n += 1
        if b == 0xFF:
            return ''.join(out)
        if not ff_only and b == 0xFE:
            out.append('<NAME%d>' % (rd(p) & 1)); p += 1
        elif not ff_only and b == 0xFD:
            out.append('<LV%d>' % (0 if rd(p) == 0 else 1)); p += 1
        elif not ff_only and b == 0xFC:
            out.append('<ITEM[$19]>')
        else:
            out.append(ch(b))
    return ''.join(out) + '...(no FF)'


def main():
    lines = []
    first_ptr = w(TABLE + 2)
    n = (first_ptr - TABLE) // 4
    lines.append('MenuStrings 16:%04X: %d records (table ends at first string %04X)' % (TABLE, n, first_ptr))
    for i in range(n):
        a = TABLE + i * 4
        ppu = w(a); ptr = w(a + 2)
        lines.append('%02X  PPU %04X  str %04X  "%s"' % (i, ppu, ptr, decode(ptr)))
    if '--items' in sys.argv:
        first = w(ITEMS)
        cnt = (first - ITEMS) // 2
        lines.append('')
        lines.append('MenuItemNames 16:%04X: %d ptrs' % (ITEMS, cnt))
        for i in range(cnt):
            p = w(ITEMS + i * 2)
            lines.append('%02X  %04X  "%s"' % (i, p, decode(p, ff_only=True)))
    txt = '\n'.join(lines)
    if '--out' in sys.argv:
        with open(sys.argv[sys.argv.index('--out') + 1], 'w') as f:
            f.write(txt + '\n')
    print(txt)


if __name__ == '__main__':
    main()
