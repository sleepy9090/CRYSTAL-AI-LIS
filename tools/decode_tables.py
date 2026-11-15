#!/usr/bin/env python3
"""Decode tables from banked PRG.

usage: decode_tables.py BB:AAAA <count> <fmt> [--per-line N]
  fmt: u8 | s8 | u16 | ptr16 | hex<N> (N-byte records, raw hex) | split:<hi_addr>
       split:<AAAA>  -> lo bytes at BB:AAAA, hi bytes at BB:<hi_addr> (count entries)
Addressing: BB is an 8KB bank; addresses in $8000-$BFFF resolve within the 16KB pair
(2n at $8000, 2n+1 at $A000), $C000 -> bank 30, $E000 -> bank 31.
"""
import sys
from nesrom import load_rom, parse_bank_addr, lookup, load_labels

_, PRG, _ = load_rom()


def resolve(bank, addr):
    if addr >= 0xE000:
        return 31
    if addr >= 0xC000:
        return 30
    base = bank & ~1
    return base + (1 if addr >= 0xA000 else 0)


def rd(bank, addr):
    b = resolve(bank, addr)
    return PRG[b * 0x2000 + (addr & 0x1FFF)]


def main(argv):
    if len(argv) < 3:
        print(__doc__)
        return 1
    bank, addr = parse_bank_addr(argv[0])
    if bank is None:
        bank = 30 if addr >= 0xC000 else 0
    count = int(argv[1], 0)
    fmt = argv[2]
    per = 8
    if '--per-line' in argv:
        per = int(argv[argv.index('--per-line') + 1])
    labels = load_labels()
    if fmt in ('u8', 's8'):
        vals = [rd(bank, addr + i) for i in range(count)]
        if fmt == 's8':
            vals = [v - 256 if v >= 128 else v for v in vals]
        for i in range(0, count, per):
            chunk = vals[i:i + per]
            print(f"{bank:02d}:{addr + i:04X} [{i:3d}] " +
                  ' '.join(f"{v:4d}" if fmt == 's8' else f"{v:02X}" for v in chunk))
    elif fmt in ('u16', 'ptr16'):
        for i in range(count):
            a = addr + i * 2
            v = rd(bank, a) | (rd(bank, a + 1) << 8)
            name = ''
            if fmt == 'ptr16':
                lab = lookup(labels, bank, v)
                name = f"  {lab[0]}" if lab else ''
            print(f"{bank:02d}:{a:04X} [{i:3d}] ${v:04X}{name}")
    elif fmt.startswith('hex'):
        n = int(fmt[3:] or 1)
        for i in range(count):
            a = addr + i * n
            print(f"{bank:02d}:{a:04X} [{i:3d}] " +
                  ' '.join(f"{rd(bank, a + j):02X}" for j in range(n)))
    elif fmt.startswith('split:'):
        hi = int(fmt[6:], 16)
        for i in range(count):
            v = rd(bank, addr + i) | (rd(bank, hi + i) << 8)
            print(f"[{i:3d}] ${v:04X}")
    else:
        print(f"unknown fmt {fmt}")
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
