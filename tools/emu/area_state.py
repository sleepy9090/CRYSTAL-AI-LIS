#!/usr/bin/env python3
"""Save a tools.emu state standing in play in a poked area (session 116, pit-fall / edge-link scenarios).

Loads --state (default area1_enter.state), pokes area_id/area_entry + sub_state 1 and taps A (like
poke_probe.py / tour.py) until sub_state 8 in that area, runs --settle more frames, saves --out.
Then e.g. `python3 -m tools.emu.web_state --load-state OUT --poke 0070=78 ... --until 30:CA2E` exports
the frame the port starts from.

usage (repo root):
  python3 -m tools.emu.area_state 6A [--entry 00] [--settle 20] --out /tmp/a6a.state
"""
import argparse
import os
import sys

from tools.emu.nes import NES
from tools.emu import state

STATES = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'states')


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('area')
    ap.add_argument('--entry', default='00')
    ap.add_argument('--state', default=os.path.join(STATES, 'area1_enter.state'))
    ap.add_argument('--settle', type=int, default=20)
    ap.add_argument('--out', required=True)
    a = ap.parse_args(argv)
    nes = NES()
    state.load(nes, a.state)
    r = nes.bus.ram
    area = int(a.area, 16)
    r[0x6C], r[0x6D], r[0x41] = area, int(a.entry, 16), 1
    left = False
    for i in range(900):
        nes.keys = {nes.ppu.frame_no: 0x01} if i > 150 and i % 8 == 4 else {}
        nes.run_frame()
        left |= r[0x41] != 8
        if left and r[0x41] == 8 and r[0x6C] == area:
            break
    else:
        sys.exit(f'area {area:02X} not reached (sub_state {r[0x41]:02X}, area {r[0x6C]:02X})')
    nes.keys = {}
    nes.run_frames(a.settle)
    state.save(nes, a.out)
    print(f'area {area:02X} in play at frame {nes.ppu.frame_no}, sub_state {r[0x41]:02X} -> {a.out}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
