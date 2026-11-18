#!/usr/bin/env python3
"""Dump PpuQueueBlock 30:C482 block records (5 bytes at 30:C518 + id*5) (session 72).

record: hi lo len src rows   -> per row (rows & $7F)+1 times: queue (ppu=hi:lo, len, src = $6000+src);
    ppu += max(len, $20), src += len; rows b7 = do not WaitPpuQueue after the block.
Lists the $6000-page bytes each block reads (src..src+len*rows-1).

usage: ppu_block_dump.py [id ...] [--count N]
"""
import sys
import os
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom  # noqa: E402

PRG = load_rom()[1]
BASE = 0xC518


def rec(i):
    o = 30 * 0x2000 + (BASE + i * 5 - 0xC000)
    return PRG[o:o + 5]


def main(argv):
    n = int(argv[argv.index('--count') + 1], 0) if '--count' in argv else 0x50
    ids = [int(a, 16) for a in argv if not a.startswith('--') and (argv.index(a) == 0 or argv[argv.index(a) - 1] != '--count')]
    for i in ids or range(n):
        hi, lo, ln, src, rows = rec(i)
        r = (rows & 0x7F) + 1
        end = src + ln * r - 1
        print(f'blk ${i:02X} @30:{BASE + i * 5:04X}  ppu ${hi:02X}{lo:02X} len {ln:3} x {r:2} rows'
              f'  src $60{src:02X}-${0x6000 + end:04X}{"" if end < 0x100 else " (wraps past $60FF!)"}'
              f'{"  nowait" if rows & 0x80 else ""}')


if __name__ == '__main__':
    main(sys.argv[1:])
