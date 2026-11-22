#!/usr/bin/env python3
"""s163: print columns of two frame CSVs (tools.emu / web port) side by side for a frame range; '*' marks rows
that differ.

usage: python3 tools/csv_cols.py A.csv B.csv COL[,COL..] LO-HI"""
import csv
import sys


def load(path):
    with open(path) as f:
        rows = list(csv.DictReader(f))
    key = next(iter(rows[0]))
    return {int(r[key]): r for r in rows}


def main():
    if len(sys.argv) < 5:
        raise SystemExit(__doc__)
    a, b = load(sys.argv[1]), load(sys.argv[2])
    cols = sys.argv[3].split(',')
    lo, hi = (int(v) for v in sys.argv[4].split('-'))
    print('frame  ' + '  '.join(f'{c:>23}' for c in cols))
    for f in range(lo, hi + 1):
        ra, rb = a.get(f, {}), b.get(f, {})
        cells = [f'{ra.get(c, "-"):>11}|{rb.get(c, "-"):<11}' for c in cols]
        mark = '*' if any(ra.get(c) != rb.get(c) for c in cols) else ' '
        print(f'{f:5}{mark} ' + '  '.join(cells))


if __name__ == '__main__':
    main()
