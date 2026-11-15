#!/usr/bin/env python3
"""Profile 8KB PRG banks per 256-byte page: top bytes, ASCII ratio, zero/FF ratio,
and printable-ASCII runs (>= --min chars). Used to classify flat-entropy banks.

usage: bank_profile.py <bank> [<bank> ...] [--min N] [--pages] [--strings]
"""
import sys, os
from collections import Counter
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom, bank_window  # noqa


def prg_bank(rom, b):
    return rom[1][b * 0x2000:(b + 1) * 0x2000]


def main():
    args = sys.argv[1:]
    mn = 6
    if '--min' in args:
        i = args.index('--min'); mn = int(args[i + 1]); del args[i:i + 2]
    pages = '--pages' in args
    strings = '--strings' in args
    banks = [int(a, 0) for a in args if not a.startswith('--')]
    rom = load_rom()
    for b in banks:
        data = prg_bank(rom, b)
        c = Counter(data)
        asc = sum(1 for x in data if 0x20 <= x < 0x7F)
        print(f"== bank {b}: ascii {asc/len(data):.2f}  00 {c[0]/len(data):.2f}  FF {c[0xFF]/len(data):.2f}"
              f"  distinct {len(c)}  top {' '.join(f'{k:02X}:{v}' for k, v in c.most_common(10))}")
        if pages:
            for p in range(32):
                pg = data[p * 256:(p + 1) * 256]
                pc = Counter(pg)
                pa = sum(1 for x in pg if 0x20 <= x < 0x7F)
                print(f"  page {p:2d} (${bank_window(b) + p*256:04X}) ascii {pa/256:.2f} distinct {len(pc):3d}"
                      f" top {' '.join(f'{k:02X}:{v}' for k, v in pc.most_common(6))}")
        if strings:
            base = bank_window(b)
            run = bytearray(); start = 0
            for i, x in enumerate(data + b'\x00'):
                if 0x20 <= x < 0x7F:
                    if not run: start = i
                    run.append(x)
                else:
                    if len(run) >= mn:
                        print(f"  {b}:{base+start:04X} [{len(run)}] {run.decode('ascii')!r}")
                    run = bytearray()


if __name__ == '__main__':
    main()
