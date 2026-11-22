#!/usr/bin/env python3
"""s180: classify every cell that differs at shift 0 between a tools.emu CSV (A) and a web-port CSV (B).

usage: python3 tools/cell_class.py A.csv B.csv [COLS] [LO-HI]
  LATE    = the port's NEXT row has tools.emu's value (tools.emu one value ahead: its row is sampled after the
            next pass has started in vblank)
  EARLY   = the port's PREVIOUS row has it
  PARTIAL = neither (e.g. a half-built buffer in tools.emu's row); 'emu-odd' is added when tools.emu's value
            also differs from both of its own neighbours
COLS = comma list of column names / prefixes (default: all). One line per cell, then counts.
(No argparse, see tools/csv_cols.py.)"""
import sys

sys.path.insert(0, __file__.rsplit('/', 1)[0])
from csv_cols import load  # noqa: E402


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    ha, ra = load(argv[0])
    _, rb = load(argv[1])
    cols = [h for h in ha if h != 'frame']
    if len(argv) > 2 and argv[2]:
        want = argv[2].split(',')
        cols = [h for h in cols if any(h == w or h.startswith(w) for w in want)]
    lo, hi = (map(int, argv[3].split('-')) if len(argv) > 3 else (min(ra), max(ra)))
    counts = {}
    for f in range(lo, hi + 1):
        a, b = ra.get(f), rb.get(f)
        if not a or not b:
            continue
        for c in cols:
            if a[c] == b.get(c):
                continue
            nb, pb = rb.get(f + 1, {}), rb.get(f - 1, {})
            kind = 'LATE' if nb.get(c) == a[c] else 'EARLY' if pb.get(c) == a[c] else 'PARTIAL'
            if kind == 'PARTIAL' and ra.get(f - 1, {}).get(c) != a[c] and ra.get(f + 1, {}).get(c) != a[c]:
                kind += ' emu-odd'
            counts[kind] = counts.get(kind, 0) + 1
            print(f'{f:5d} {c:12s} {kind:16s} emu={a[c]} web={b[c]}')
    print('counts:', ', '.join(f'{k} {v}' for k, v in sorted(counts.items())) or 'none')


if __name__ == '__main__':
    main(sys.argv[1:])
