#!/usr/bin/env python3
"""Hex-dump a range of a reference capture file (ref/frame_NNNN.ram|.wram) side by side.

usage (repo root):
  python3 tools/ref/ref_peek.py ADDR LEN FILE [FILE ...]
ADDR is a CPU address ($0000-$07FF for .ram, $6000-$7FFF for .wram).
"""
import sys


def main(argv):
    if len(argv) < 3:
        print(__doc__); return 2
    addr, n = int(argv[0], 16), int(argv[1], 0)
    for p in argv[2:]:
        b = open(p, 'rb').read()
        base = 0x6000 if p.endswith('.wram') else 0
        for row in range(addr, addr + n, 16):
            chunk = b[row - base:min(row + 16, addr + n) - base]
            print(f'{p:28s} ${row:04X}: ' + ' '.join(f'{v:02X}' for v in chunk))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
