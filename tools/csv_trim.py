#!/usr/bin/env python3
"""Cut a tools/emu/compare.py CSV down to some columns and frames (session 138).

usage: csv_trim.py IN.csv OUT.csv [--cols c1,c2,..] [--frames LO-HI]
Columns accept compare_frames.py groups / prefixes (core, snd, pal, obj, oam_crc, ...); `frame` is always kept.
Used for small reference CSVs under web/test/ (e.g. web/test/wake_ref.csv for wake_check.mjs).
(No argparse: tools/dis.py shadows the stdlib `dis` that argparse imports on Python 3.14.)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import compare_frames  # noqa: E402


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    src, dst, rest = argv[0], argv[1], argv[2:]
    spec, lo, hi = None, None, None
    i = 0
    while i < len(rest):
        if rest[i] == '--cols':
            spec = rest[i + 1]
            i += 2
        elif rest[i] == '--frames':
            a, b = rest[i + 1].split('-')
            lo, hi = int(a), int(b)
            i += 2
        else:
            raise SystemExit(f'unknown argument {rest[i]}')
    cols, rows = compare_frames.read_csv(src)
    keep = ['frame'] + compare_frames.pick_cols(cols, spec)
    idx = [cols.index(c) for c in keep]
    n = 0
    with open(dst, 'w') as f:
        f.write(','.join(keep) + '\n')
        for fr in sorted(rows):
            if lo is not None and not (lo <= fr <= hi):
                continue
            f.write(','.join(str(rows[fr][j]) for j in idx) + '\n')
            n += 1
    print(f'{dst}: {n} rows, {len(keep)} columns')


if __name__ == '__main__':
    main(sys.argv[1:])
