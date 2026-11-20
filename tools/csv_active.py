#!/usr/bin/env python3
"""Count the active object slots per frame in a tools.emu / run.mjs --compare CSV (session 114).

    python3 tools/csv_active.py FILE.csv [--frames A-B]

Prints `frame  count  slots` where a slot is active when its oNN_ai column is non-zero. Used to look for a
CPU-overrun (lag frame) predictor: the sprite pass + ObjCollideAll cost grows with the active slot count.
"""
import csv
import sys


def main(argv):
    if not argv or argv[0] in ('-h', '--help'):
        print(__doc__)
        return 0
    lo, hi = 0, 1 << 30
    if '--frames' in argv:
        a, b = argv[argv.index('--frames') + 1].split('-')
        lo, hi = int(a), int(b)
    with open(argv[0], newline='') as f:
        rows = list(csv.DictReader(f))
    for r in rows:
        fr = int(r.get('frame_no') or r.get('frame'))
        if not lo <= fr <= hi:
            continue
        slots = sorted(k[1:3] for k, v in r.items() if k.startswith('o') and k.endswith('_ai') and v not in ('', '0'))
        print(f'{fr:6d} {len(slots):3d}  {" ".join(slots)}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
