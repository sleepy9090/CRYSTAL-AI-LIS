#!/usr/bin/env python3
"""Group compare_frames.py per-frame lines into runs of consecutive differing frames per column (session 90).

usage: python3 tools/compare_frames.py A.csv B.csv [--cols ...] | python3 tools/diff_runs.py [--min N]
Prints, per column, the number of runs and their length histogram; with --min N also lists every run of
length >= N. A 1-frame run at a value change = a CSV sampling-phase artifact (see REVERSE.md Verification Log,
"Title side" row (1)); longer runs are real divergences.
"""
import re
import sys
from collections import defaultdict

LINE = re.compile(r'^frame (\d+): (.*)$')
CELL = re.compile(r'(\w+)=(\S+)->(\S+)')


def main():
    # no argparse: run as tools/diff_runs.py, tools/dis.py shadows the stdlib `dis` that argparse imports
    min_run = int(sys.argv[sys.argv.index('--min') + 1]) if '--min' in sys.argv else 0
    frames = defaultdict(list)
    for line in sys.stdin:
        mt = LINE.match(line.strip())
        if not mt:
            continue
        f = int(mt.group(1))
        for col, _, _ in CELL.findall(mt.group(2)):
            frames[col].append(f)
    for col in sorted(frames):
        fs = sorted(set(frames[col]))
        runs, start, prev = [], fs[0], fs[0]
        for f in fs[1:]:
            if f != prev + 1:
                runs.append((start, prev))
                start = f
            prev = f
        runs.append((start, prev))
        hist = defaultdict(int)
        for s, e in runs:
            hist[e - s + 1] += 1
        h = ' '.join(f'{k}f:{v}' for k, v in sorted(hist.items()))
        print(f'{col:16s} {len(fs):5d} frames in {len(runs):4d} runs  ({h})')
        if min_run:
            for s, e in runs:
                if e - s + 1 >= min_run:
                    print(f'    f{s}-f{e} ({e - s + 1})')


if __name__ == '__main__':
    main()
