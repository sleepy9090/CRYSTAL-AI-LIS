#!/usr/bin/env python3
"""Pixel-compare one reference frame against every tools.emu frame in a window (session 84).

usage (repo root):
  python3 tools/ref/blank_probe.py [--ref DIR] [--frame 2409] [--window 4] [--watch 6009,6120,6204]

For ref frame N, tools.emu frames N-W..N+W are snapshotted and each is run through compare_ref's
pixel_diff (incl. the colour-fold flag).  Also prints the number of distinct colours per emu frame,
PPUMASK-relevant RAM ($0001 shadow) and the WRAM bytes given by --watch, ref vs emu.  Used for the
s74 item (b): which frame FCEUX shows blank at ref f2409 and whether tools.emu blanks the same frame.
"""
import argparse
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if ROOT not in sys.path:
    sys.path.append(ROOT)

from tools.ref.compare_ref import (CANON_KEYS, load_ref, pixel_diff, ram_diffs,  # noqa: E402
                                   run_emu)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--ref', default=os.path.join(ROOT, 'ref'))
    ap.add_argument('--frame', type=int, default=2409)
    ap.add_argument('--window', type=int, default=4)
    ap.add_argument('--watch', default='6009,600A,6017,601C,601D,6120,6204')
    a = ap.parse_args(argv)
    n = a.frame
    ref = load_ref(os.path.join(a.ref, f'frame_{n:04d}'))
    if 'img' not in ref:
        print(f'no screenshot for ref frame {n}')
        return 2
    rcols = len(set(ref['img'][2]))
    print(f'ref frame {n}: {rcols} distinct colours')
    frames = set(range(n - a.window, n + a.window + 1))
    snaps = run_emu([CANON_KEYS], frames)
    watch = [int(x, 16) for x in a.watch.split(',') if x]
    hdr = ' '.join(f'${w:04X}' for w in watch)
    print(f'ref  wram  {hdr}')
    print('          ' + ' '.join(f'{ref["wram"][w - 0x6000]:02X}   ' for w in watch))
    for f in sorted(frames):
        s = snaps[f]
        bad, msg, folds = pixel_diff(ref['img'], s['px'])
        ncol = len(set(s['px']))
        rd = len(ram_diffs(ref['ram'], s['ram']))
        ww = ' '.join(f'{s["wram"][w - 0x6000]:02X}   ' for w in watch)
        print(f'emu f{f} (d={f - n:+d}): {ncol:2d} colours, RAM diffs {rd:4d}, '
              f'{"FOLD " if folds else ""}{msg.splitlines()[0]}')
        print(f'          {ww}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
