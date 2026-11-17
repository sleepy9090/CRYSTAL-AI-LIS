#!/usr/bin/env python3
"""Diff a tools.emu run against the web port (Ph7 web 3/8): per-frame CSVs and/or screenshots.

usage: compare_frames.py A.csv B.csv [--cols C1,C2,..|GROUP] [--frames LO-HI] [--shift N] [--max N]
       compare_frames.py A.png B.png [render_compare.py options: --crop --offset --search --tol --diff]
       compare_frames.py A.csv B.csv A.png B.png [...]   (both)

CSV (columns of tools/emu/compare.py, written by `python3 -m tools.emu --compare` and
`node web/test/run.mjs --compare`): rows are joined on the `frame` column (B frame = A frame + --shift).
  --cols    only these columns; groups: core (state sub area entry pos cam music),
            snd (snd*_period), pal (pal_crc), obj (o*_ai/x/y), or a column-name prefix such as o01_
  --frames  only A frames LO..HI
  --max N   print at most N differing frames (default 20)
Prints per differing frame the columns that differ (A -> B), then a per-column summary (frames
differing, first differing frame) and the frame ranges present in only one file.
PNG: pixel diff via render_compare.py (mismatch %, bbox, per-tile-row counts, --diff image).
Exit code 0 if everything compared is equal, 1 otherwise.
"""
import csv
import os
import re
import sys

sys.path.insert(0, os.path.dirname(__file__))
import render_compare  # noqa: E402

GROUPS = {
    'core': ['state', 'sub', 'area', 'entry', 'pos_x', 'pos_y', 'cam_x', 'cam_y', 'music'],
    'snd': [f'snd{c}_period' for c in range(4)],
    'pal': ['pal_crc'],
}


def read_csv(path):
    with open(path, newline='') as f:
        rd = csv.reader(f)
        cols = next(rd)
        rows = {}
        for r in rd:
            if r:
                rows[int(r[0])] = [int(v) for v in r]
    return cols, rows


def pick_cols(cols, spec):
    if not spec:
        return [c for c in cols if c != 'frame']
    out = []
    for s in spec.split(','):
        if s in GROUPS:
            out += GROUPS[s]
        elif s == 'obj':
            out += [c for c in cols if re.match(r'o[0-9A-F]{2}_', c)]
        elif s in cols:
            out.append(s)
        else:
            pre = [c for c in cols if c.startswith(s)]
            if not pre:
                raise SystemExit(f'unknown column / group {s}')
            out += pre
    return [c for c in dict.fromkeys(out) if c in cols]


def ranges(nums):
    nums = sorted(nums)
    out, i = [], 0
    while i < len(nums):
        j = i
        while j + 1 < len(nums) and nums[j + 1] == nums[j] + 1:
            j += 1
        out.append(f'{nums[i]}' if i == j else f'{nums[i]}-{nums[j]}')
        i = j + 1
    return ' '.join(out)


def compare_csv(pa, pb, opts):
    ca, ra = read_csv(pa)
    cb, rb = read_csv(pb)
    if ca != cb:
        print(f'column sets differ: only A {sorted(set(ca) - set(cb))} only B {sorted(set(cb) - set(ca))}')
    cols = pick_cols([c for c in ca if c in cb], opts.get('--cols'))
    ia, ib = {c: ca.index(c) for c in cols}, {c: cb.index(c) for c in cols}
    shift = int(opts.get('--shift', 0))
    lo, hi = (int(v) for v in opts['--frames'].split('-')) if '--frames' in opts else (min(ra, default=0), max(ra, default=-1))
    fa = [f for f in ra if lo <= f <= hi]
    both = [f for f in fa if f + shift in rb]
    only_a = [f for f in fa if f + shift not in rb]
    only_b = [f - shift for f in rb if lo <= f - shift <= hi and f - shift not in ra]
    per_col = {c: [] for c in cols}
    bad_frames = 0
    mx = int(opts.get('--max', 20))
    for f in sorted(both):
        a, b = ra[f], rb[f + shift]
        diff = [(c, a[ia[c]], b[ib[c]]) for c in cols if a[ia[c]] != b[ib[c]]]
        if not diff:
            continue
        bad_frames += 1
        for c, _, _ in diff:
            per_col[c].append(f)
        if bad_frames <= mx:
            print(f'frame {f}: ' + ' '.join(f'{c}={x}->{y}' for c, x, y in diff[:12])
                  + (f' (+{len(diff) - 12} more)' if len(diff) > 12 else ''))
    print(f'CSV {pa} vs {pb}: {len(both)} frames compared (A {lo}..{hi}, shift {shift}), {len(cols)} columns, '
          f'{bad_frames} frames differ')
    for c in cols:
        if per_col[c]:
            print(f'  {c:<14} {len(per_col[c]):5d} frames, first {per_col[c][0]}')
    if only_a:
        print(f'  only in A: {ranges(only_a)}')
    if only_b:
        print(f'  only in B: {ranges(only_b)}')
    return bad_frames == 0 and both


def compare_png(pa, pb, opts):
    argv = [pa, pb]
    for k in ('--crop', '--offset', '--search', '--tol', '--diff'):
        if k in opts:
            argv += [k, opts[k]]
    return render_compare.main(argv) == 0


def main(argv):
    files, opts, i = [], {}, 0
    while i < len(argv):
        if argv[i].startswith('--'):
            if i + 1 >= len(argv):
                raise SystemExit(f'{argv[i]} needs a value')
            opts[argv[i]] = argv[i + 1]
            i += 2
        else:
            files.append(argv[i])
            i += 1
    csvs = [f for f in files if f.endswith('.csv')]
    pngs = [f for f in files if f.endswith('.png')]
    if len(csvs) not in (0, 2) or len(pngs) not in (0, 2) or not files:
        raise SystemExit(__doc__)
    ok = True
    if csvs:
        ok = bool(compare_csv(csvs[0], csvs[1], opts)) and ok
    if pngs:
        ok = compare_png(pngs[0], pngs[1], opts) and ok
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
