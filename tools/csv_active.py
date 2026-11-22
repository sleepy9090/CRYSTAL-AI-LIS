#!/usr/bin/env python3
"""s161: per-frame count of active object slots (non-zero oXX_ai columns) in a tools/emu/compare.py CSV,
plus pos_x/pos_y, to line up CPU-overrun lag frames with the slot count (s114 heuristic in obj/ai_effect.js).

usage: python3 tools/csv_active.py A.csv [B.csv] --frames LO-HI"""
import csv
import sys


def load(path, lo, hi):
    out = {}
    with open(path) as f:
        for row in csv.DictReader(f):
            fr = int(row['frame'])
            if lo <= fr <= hi:
                n = sum(1 for k, v in row.items() if k.startswith('o') and k.endswith('_ai') and v not in ('0', ''))
                out[fr] = (n, row.get('pos_x'), row.get('pos_y'))
    return out


def main():
    args = sys.argv[1:]
    lo, hi = 0, 1 << 30
    if '--frames' in args:
        i = args.index('--frames')
        lo, hi = (int(v) for v in args[i + 1].split('-'))
        del args[i:i + 2]
    if not args:
        raise SystemExit(__doc__)
    runs = [load(p, lo, hi) for p in args]
    for fr in sorted(set().union(*runs)):
        cells = []
        for r in runs:
            n, px, py = r.get(fr, ('-', '-', '-'))
            cells.append(f'{n:>3} ({px},{py})')
        print(f'f{fr}: ' + '   |   '.join(cells))


if __name__ == '__main__':
    main()
