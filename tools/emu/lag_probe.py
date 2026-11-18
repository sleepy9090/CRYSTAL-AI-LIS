#!/usr/bin/env python3
"""List lag frames in a tools.emu run: frames in which a per-pass counter was NOT written (session 74).

The main loop's per-frame handlers decrement a counter once per pass (title: `$06FF` in MainState3
19:A015).  A frame without that write is a lag frame (the previous pass overran the NMI, e.g. the
PPU-queue drain wait at 19:A092).  Prints the lag frames in [--from, --to] and the running count, so
two emulators' frame offsets can be explained.

usage (repo root):
  python3 -m tools.emu.lag_probe [--addr 06FF] [--from 0] [--to 2300] [--keys SPEC ...] [--trace FILE]
--trace writes the same columns as ref/trace.txt from tools/ref/fceux_capture.lua (session 74), so the
two runs' lag frames can be diffed line by line once FCEUX is rerun.
"""
import argparse
import sys

from tools.emu.nes import NES
from tools.emu import debug
from tools.ref.compare_ref import CANON_KEYS


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--addr', default='06FF', type=lambda s: int(s, 16))
    ap.add_argument('--from', dest='lo', type=int, default=0)
    ap.add_argument('--to', dest='hi', type=int, default=2300)
    ap.add_argument('--keys', nargs='+', default=[CANON_KEYS])
    ap.add_argument('--fill', default='fceux')
    ap.add_argument('--trace', metavar='FILE', help='per-frame trace in the fceux_capture.lua trace.txt format')
    a = ap.parse_args(argv)
    nes = NES(ram_fill=a.fill, wram_fill='zero')
    nes.keys = debug.parse_keys(a.keys)
    writes = {}

    def w(addr, val):
        f = nes.ppu.frame_no
        writes[f] = writes.get(f, 0) + 1
    nes.on_write(a.addr, w)
    rows = []
    if a.trace:
        def fr(n):
            r, wr = n.bus.ram, n.bus.wram
            vals = [r[x] for x in (0x40, 0x41, 0x6F0, 0x6F1, 0x6F3, 0x6FF, 0x617, 0x08)] + [wr[0x423]]
            rows.append(f'{n.ppu.frame_no - 1} ' + ' '.join(f'{v:02X}' for v in vals))
        nes.on_frame(fr)
    nes.run_frames(a.hi + 1)
    if a.trace:
        with open(a.trace, 'w') as fh:
            fh.write('# frame $40 $41 $06F0 $06F1 $06F3 $06FF $0617 $0008 $6423  (tools.emu, frame end)\n')
            fh.write('\n'.join(rows) + '\n')
    lag = [f for f in range(a.lo, a.hi + 1) if f not in writes]
    multi = [f for f in range(a.lo, a.hi + 1) if writes.get(f, 0) > 1]
    print(f'${a.addr:04X} writes: frames {a.lo}-{a.hi}: {len(lag)} frames without a write, '
          f'{len(multi)} with >1')
    runs, start = [], None
    for i, f in enumerate(lag):
        if start is None:
            start = f
        if i + 1 == len(lag) or lag[i + 1] != f + 1:
            runs.append((start, f))
            start = None
    print('  no-write runs: ' + ' '.join(f'{s}' if s == e else f'{s}-{e}' for s, e in runs))
    if multi:
        print('  multi-write frames: ' + ' '.join(str(f) for f in multi[:60]))
    return 0


if __name__ == '__main__':
    sys.exit(main())
