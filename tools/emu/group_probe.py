#!/usr/bin/env python3
"""Enemy-group probe (session 181): plan the keys of a group_AA scenario.

usage: python3 tools/emu/group_probe.py STATE --slots 0D,13 [--seg BUTTONS:FRAMES ...] [--every N]

From a save state (e.g. scenario_state group_05 12 --save-state ...) play the button segments in order
(NONE:10 LEFT:40 B:4 ...) and print every N frames (and on any HP / active change): player x,y (16-bit world,
y = page*240 + lo) and HP, then per listed slot: active AI id $04A0, x,y, HP $03C0, phase $0600.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from emu.nes import NES  # noqa: E402
from emu import state  # noqa: E402
from emu.pads import parse_buttons  # noqa: E402


def pos(r, s):
    return r[0x90 + s] * 256 + r[0x70 + s], r[0xD0 + s] * 240 + r[0xB0 + s]


def snap(r, slots):
    return tuple([r[0x03C1]] + [(r[0x04A0 + s], r[0x03C0 + s]) for s in slots])


def main(argv):
    st, segs, every, slots = None, [], 8, [0x0D]
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--seg':
            i += 1
            while i < len(argv) and not argv[i].startswith('--'):
                b, n = argv[i].rsplit(':', 1)
                segs.append((b, 0 if b == 'NONE' else parse_buttons(b), int(n)))
                i += 1
            continue
        if a == '--every':
            every = int(argv[i + 1])
        elif a == '--slots':
            slots = [int(x, 16) for x in argv[i + 1].split(',')]
        else:
            st = a
            i += 1
            continue
        i += 2
    nes = NES()
    state.load(nes, st)
    r = nes.bus.ram
    print('  frame seg        f   p.x  p.y hp | ' + ' | '.join(f's{s:02X} ai   x    y  hp ph' for s in slots))
    last = snap(r, slots)
    for name, mask, n in segs:
        for f in range(n):
            nes.keys = (lambda m: (lambda _f: m))(mask)
            nes.run_frame()
            now = snap(r, slots)
            if f % every == 0 or f == n - 1 or now != last:
                px, py = pos(r, 1)
                cols = []
                for s in slots:
                    x, y = pos(r, s)
                    cols.append(f'{r[0x04A0 + s]:02X} {x:4d} {y:4d} {r[0x03C0 + s]:3d} {r[0x0600 + s]:02X}')
                mark = ' *' if now != last else ''
                print(f'{nes.ppu.frame_no:7d} {name:9s}{f:3d} {px:5d} {py:4d} {r[0x03C1]:2X} | '
                      + ' | '.join(cols) + mark)
            last = now


if __name__ == '__main__':
    main(sys.argv[1:])
