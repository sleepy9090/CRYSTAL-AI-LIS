#!/usr/bin/env python3
"""s161: print an evenly spaced key list for --keys (e.g. `keyseq.py 2535 30 40 A` -> 2535:A 2565:A ...).

usage: python3 tools/keyseq.py START STEP COUNT BTN[:N]"""
import sys


def main():
    if len(sys.argv) != 5:
        raise SystemExit(__doc__)
    start, step, count = (int(v) for v in sys.argv[1:4])
    print(' '.join(f'{start + step * i}:{sys.argv[4]}' for i in range(count)))


if __name__ == '__main__':
    main()
