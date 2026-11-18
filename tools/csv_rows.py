#!/usr/bin/env python3
"""Print selected columns of tools/emu/compare.py CSVs side by side (session 82).

usage: csv_rows.py A.csv [B.csv ...] --cols c1,c2,.. --frames LO-HI[,LO-HI..]
Columns accept compare_frames.py groups / prefixes. One line per frame: frame, then for each file its values.
(No argparse: tools/dis.py shadows the stdlib `dis` that argparse imports on Python 3.14.)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import compare_frames  # noqa: E402


def main(argv):
    files, cols_spec, frames_spec = [], None, None
    i = 0
    while i < len(argv):
        if argv[i] == '--cols':
            cols_spec = argv[i + 1]; i += 2
        elif argv[i] == '--frames':
            frames_spec = argv[i + 1]; i += 2
        else:
            files.append(argv[i]); i += 1
    if not files or not cols_spec or not frames_spec:
        raise SystemExit(__doc__)
    data = [compare_frames.read_csv(p) for p in files]
    cols = compare_frames.pick_cols(data[0][0], cols_spec)
    frames = []
    for part in frames_spec.split(','):
        lo, _, hi = part.partition('-')
        frames += range(int(lo), int(hi or lo) + 1)
    print('frame ' + ' | '.join(' '.join(cols) for _ in data))
    for f in frames:
        out = []
        for hdr, rows in data:
            r = rows.get(f)
            out.append(' '.join(str(r[hdr.index(c)]) if r else '-' for c in cols))
        print(f'{f:5d} ' + ' | '.join(out))


if __name__ == '__main__':
    main(sys.argv[1:])
