#!/usr/bin/env python3
"""Print bytes of a RAM dump (run.mjs / tools.emu --dump-ram) at hex addresses (session 198).

usage: python3 tools/ram_byte.py DUMP.bin ADDR[,ADDR..] [DUMP2.bin ..]
"""
import sys


def main(argv):
    if len(argv) < 2:
        print(__doc__.strip())
        return 2
    addrs = [int(a, 16) for a in argv[1].split(',')]
    for path in [argv[0]] + argv[2:]:
        data = open(path, 'rb').read()
        vals = ' '.join(f'${a:04X}={data[a]:02X}' if a < len(data) else f'${a:04X}=--' for a in addrs)
        print(f'{path}: {vals}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
