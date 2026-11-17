#!/usr/bin/env python3
"""Hex dump part of a binary file (RAM/WRAM dumps from tools.emu).

usage: python3 tools/hexpeek.py FILE START [LEN]   (START/LEN hex, LEN default 0x40)
"""
import sys


def main(argv):
    data = open(argv[0], 'rb').read()
    start = int(argv[1], 16)
    n = int(argv[2], 16) if len(argv) > 2 else 0x40
    for a in range(start, min(start + n, len(data)), 16):
        row = data[a:min(a + 16, start + n)]
        print(f'{a:04X}: ' + ' '.join(f'{b:02X}' for b in row))


if __name__ == '__main__':
    main(sys.argv[1:])
