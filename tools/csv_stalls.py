#!/usr/bin/env python3
"""List frames where a CSV column does not change from the previous row (session 107).

Used to find lag frames: MainState3 decrements `ttl_ctr` ($06FF) on every pass, so a frame whose
row has the same value as the previous one had no MainState3 pass (a lag frame, or a WaitFrameReady
inside a step). With two CSVs (tools.emu and the web port) it prints the stalls only one side has.

  python3 tools/csv_stalls.py A.csv [B.csv] [--col ttl_ctr] [--frames LO-HI]
"""
import csv
import sys


def stalls(path, col, lo, hi):
    out, prev = [], None
    with open(path, newline='') as f:
        for row in csv.DictReader(f):
            fr = int(row['frame_no'] if 'frame_no' in row else row['frame'])
            v = row.get(col)
            if prev is not None and v == prev and lo <= fr <= hi:
                out.append(fr)
            prev = v
    return out


def main():
    # no argparse: tools/dis.py shadows the stdlib `dis` that argparse -> inspect imports
    argv, col, frames, files = sys.argv[1:], 'ttl_ctr', '0-999999', []
    while argv:
        a = argv.pop(0)
        if a == '--col':
            col = argv.pop(0)
        elif a == '--frames':
            frames = argv.pop(0)
        else:
            files.append(a)
    if not files:
        sys.exit(__doc__)
    lo, hi = (int(x) for x in frames.split('-'))
    sa = stalls(files[0], col, lo, hi)
    if len(files) < 2:
        print(' '.join(map(str, sa)))
        return
    sb = stalls(files[1], col, lo, hi)
    print(f'A ({files[0]}): {len(sa)} stalls')
    print(f'B ({files[1]}): {len(sb)} stalls')
    print('only A:', ' '.join(map(str, sorted(set(sa) - set(sb)))))
    print('only B:', ' '.join(map(str, sorted(set(sb) - set(sa)))))


if __name__ == '__main__':
    main()
