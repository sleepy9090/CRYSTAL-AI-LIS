#!/usr/bin/env python3
"""s175: print chosen columns of two frame CSVs (tools.emu / web port) side by side, one line per frame.

usage: python3 tools/csv_cols.py A.csv B.csv COLS [LO-HI[:STEP]]   (s180: STEP samples every STEP-th frame)
COLS = comma list; a column given as a prefix (e.g. o0E) expands to every column starting with it.
Cells that differ print as A|B*. (No argparse: on Python 3.14 it imports inspect -> dis, which tools/dis.py
shadows when run as a script.)"""
import csv
import sys


def load(p):
    with open(p) as f:
        rows = list(csv.reader(f))
    head = rows[0]
    return head, {int(r[0]): dict(zip(head, r)) for r in rows[1:]}


def main(argv):
    if len(argv) < 3:
        raise SystemExit(__doc__)
    ha, ra = load(argv[0])
    _, rb = load(argv[1])
    cols = []
    for c in argv[2].split(','):
        cols += [h for h in ha if h == c] or [h for h in ha if h.startswith(c)]
    rng, _, step = (argv[3] if len(argv) > 3 else '').partition(':')
    lo, hi = (map(int, rng.split('-')) if rng else (min(ra), max(ra)))
    print('frame  ' + '  '.join(cols))
    for f in range(lo, hi + 1, int(step or 1)):
        x, y = ra.get(f), rb.get(f)
        if not x or not y:
            continue
        cells = []
        for c in cols:
            u, v = x.get(c, '?'), y.get(c, '?')
            cells.append(u if u == v else f'{u}|{v}*')
        print(f'{f:5d}  ' + '  '.join(cells))


if __name__ == '__main__':
    main(sys.argv[1:])
