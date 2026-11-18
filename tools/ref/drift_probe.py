#!/usr/bin/env python3
"""Measure the true frame offset between the FCEUX reference capture and tools.emu (session 74).

compare_ref.py picks the offset d (ref frame N <-> emu frame N+d) with the fewest RAM diffs inside
+-3 frames. On near-static screens that choice is noisy, and the real offset can lie outside the window.
This tool runs tools.emu once over the canonical keys (RAM fill 'fceux', WRAM zero, so unwritten bytes
agree) and snapshots RAM + WRAM at every frame end within +-WIN of each ref frame.  For each ref frame:

  * a wide scan: RAM+WRAM diffs per d in [-WIN, WIN], excluding the stack page $0100-$01FF, the
    $0200 OAM page and the vblank-vs-frame-end sample-point bytes ($09 $10-$12 $6E $6F, session 73 (d));
  * counter matches: for each COUNTER byte, the d values where emu[N+d] == ref (counters advance once per
    frame in some phase, so they pin the offset exactly when they are moving).

Event log: frames where main_state $40 / title_step-ish bytes change in tools.emu, to place the drift
between ref frames.  Output -> docs/ref_drift.txt (and stdout).

usage (repo root):  python3 tools/ref/drift_probe.py [--win 12] [--out docs/ref_drift.txt]
"""
import argparse
import glob
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if ROOT not in sys.path:
    sys.path.append(ROOT)

from tools.emu.nes import NES          # noqa: E402
from tools.emu import debug            # noqa: E402
from tools.ref.compare_ref import CANON_KEYS  # noqa: E402

SAMPLE_PT = {0x09, 0x10, 0x11, 0x12, 0x6E, 0x6F}
# (addr, name); addr >= $6000 = WRAM
COUNTERS = [(0x06FF, 'ram_06FF'), (0x06B4, '$06B4'), (0x0008, 'tmp_08'), (0x060C, '$060C'),
            (0x0614, '$0614'), (0x0617, 'ram_0617'), (0x0627, 'ram_0627'), (0x0628, 'ram_0628'),
            (0x0046, '$0046'), (0x6423, 'name_frames $6423')]
EVENTS = [(0x40, 'main_state'), (0x41, 'sub_state'), (0x06F3, '$06F3'), (0x06F0, '$06F0'),
          (0x6C, 'area')]


def get(snap, addr):
    ram, wram = snap
    return wram[addr - 0x6000] if addr >= 0x6000 else ram[addr]


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--ref', default=os.path.join(ROOT, 'ref'))
    ap.add_argument('--win', type=int, default=12)
    ap.add_argument('--out', default=os.path.join(ROOT, 'docs', 'ref_drift.txt'))
    a = ap.parse_args(argv)
    refs = {int(re.search(r'frame_(\d+)\.ram$', p).group(1)): p[:-4]
            for p in sorted(glob.glob(os.path.join(a.ref, 'frame_*.ram')))}
    want = set()
    for n in refs:
        want.update(range(n - a.win, n + a.win + 1))

    nes = NES(ram_fill='fceux', wram_fill='zero')
    nes.keys = debug.parse_keys([CANON_KEYS])
    snaps, events, last = {}, [], {}

    def hook(n):
        f = n.ppu.frame_no - 1
        ram = n.bus.ram
        for ad, nm in EVENTS:
            v = ram[ad]
            if last.get(ad) != v:
                events.append((f, nm, last.get(ad), v))
                last[ad] = v
        if f in want:
            snaps[f] = (bytes(ram), bytes(n.bus.wram))
    nes.on_frame(hook)
    nes.run_frames(max(want) + 1)

    out = [f'drift_probe.py: ref frame N <-> emu frame N+d, d in +-{a.win}; diffs exclude stack page, '
           'OAM page and sample-point bytes; RAM fill fceux, WRAM zero', '']
    for n, base in sorted(refs.items()):
        rram = open(base + '.ram', 'rb').read()
        rw = open(base + '.wram', 'rb').read() if os.path.exists(base + '.wram') else bytes(0x2000)
        ref = (rram, rw)
        scores = []
        for d in range(-a.win, a.win + 1):
            s = snaps.get(n + d)
            if s is None:
                continue
            c = sum(1 for i in range(0x800) if not (0x100 <= i < 0x300) and i not in SAMPLE_PT
                    and rram[i] != s[0][i])
            c += sum(1 for i in range(0x2000) if rw[i] != s[1][i])
            scores.append((c, d))
        scores.sort()
        best = scores[0]
        top = ' '.join(f'{d:+d}:{c}' for c, d in scores[:5])
        out.append(f'== ref {n}: best d={best[1]:+d} ({best[0]} diffs); top5 {top}')
        for ad, nm in COUNTERS:
            seq = [get(snaps[n + d], ad) for d in range(-a.win, a.win + 1) if n + d in snaps]
            moving = len(set(seq)) > 3
            hits = [d for d in range(-a.win, a.win + 1) if n + d in snaps and get(snaps[n + d], ad) == get(ref, ad)]
            if moving:
                out.append(f'   {nm:20s} ref={get(ref, ad):02X} moving; match d={hits}')
    out.append('')
    out.append('tools.emu event log (frame end values):')
    for f, nm, o, v in events:
        out.append(f'   f{f:5d} {nm:10s} {"--" if o is None else f"{o:02X}"} -> {v:02X}')
    txt = '\n'.join(out) + '\n'
    os.makedirs(os.path.dirname(a.out), exist_ok=True)
    with open(a.out, 'w') as fh:
        fh.write(txt)
    print(txt)
    return 0


if __name__ == '__main__':
    sys.exit(main())
