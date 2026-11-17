#!/usr/bin/env python3
"""Debug: log non-zero writes to pad vars around a press (emu 3/6 input check).

usage: python3 tools/emu/padprobe.py FROM TO --press FRAME:BUTTONS[:LEN] ...
Prints frame, PC and value for every non-zero write to pad_raw $4F / pad_new $4B during
frames FROM..TO, plus the $40/$41 state at each frame start.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from emu.nes import NES  # noqa: E402
from emu.pads import parse_buttons  # noqa: E402


def main(argv):
    lo, hi = int(argv[0]), int(argv[1])
    presses = []
    i = 2
    while i < len(argv):
        p = argv[i + 1].split(':')
        presses.append((int(p[0]), parse_buttons(p[1]), int(p[2]) if len(p) > 2 else 2))
        i += 2
    nes = NES()
    cur = {'f': 0}

    def watch(name):
        def f(a, v):
            if v and lo <= cur['f'] <= hi:
                print(f'  f{cur["f"]} {name}={v:02X} pc=${nes.cpu.pc:04X}')
        return f
    nes.bus.write_watch[0x4F] = watch('pad_raw')
    nes.bus.write_watch[0x4B] = watch('pad_new')
    for f in range(hi + 1):
        cur['f'] = f
        m = 0
        for s, mask, n in presses:
            if s <= f < s + n:
                m |= mask
        nes.pads.set(0, m)
        if lo <= f <= hi:
            r = nes.bus.ram
            print(f'f{f} buttons={m:02X} $40={r[0x40]:02X} $41={r[0x41]:02X} $06F3={r[0x6F3]:02X}')
        nes.run_frame()


if __name__ == '__main__':
    main(sys.argv[1:])
