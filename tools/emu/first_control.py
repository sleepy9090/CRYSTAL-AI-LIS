#!/usr/bin/env python3
"""Find the first frame where a held direction moves the player (emu 5/6).

usage: python3 tools/emu/first_control.py STATE FROM TO [--dir DOWN] [--hold 16] [--slot 1] [--settle 0]
Loads STATE, runs to FROM with no input, then for each frame F in FROM..TO compares
"hold DIR for --hold frames starting at F" against "no input" and reports the player
slot's x/y (obj_x_lo $70+s, obj_y_lo $B0+s) after the hold.  Prints the first F where
they differ (= first controllable frame), plus $40/$41/$6C at that frame.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from emu.nes import NES  # noqa: E402
from emu.pads import parse_buttons  # noqa: E402
from emu import state  # noqa: E402


def pos(nes, s):
    r = nes.bus.ram
    return r[0x70 + s], r[0x90 + s], r[0xB0 + s], r[0xD0 + s]


def main(argv):
    path, lo, hi = argv[0], int(argv[1]), int(argv[2])
    opts = dict(zip(argv[3::2], argv[4::2]))
    mask = parse_buttons(opts.get('--dir', 'DOWN'))
    hold = int(opts.get('--hold', 16))
    settle = int(opts.get('--settle', 0))   # extra no-input frames before comparing
    slot = int(opts.get('--slot', 1))
    nes = NES()
    state.load(nes, path)
    nes.keys = {}
    while nes.ppu.frame_no < lo:
        nes.run_frame()
    for f in range(lo, hi + 1):
        snap = state.snapshot(nes)
        nes.keys = {}
        nes.run_frames(hold + settle)
        base = pos(nes, slot)
        state.restore(nes, snap)
        nes.keys = {g: mask for g in range(f, f + hold)}
        nes.run_frames(hold + settle)
        moved = pos(nes, slot)
        state.restore(nes, snap)
        if moved != base:
            r = nes.bus.ram
            print(f'first controllable frame {f}: base={base} held={moved} '
                  f'$40={r[0x40]:02X} $41={r[0x41]:02X} $6C={r[0x6C]:02X}')
            return
        nes.keys = {}
        nes.run_frame()
    print(f'no response in {lo}..{hi}')


if __name__ == '__main__':
    main(sys.argv[1:])
