#!/usr/bin/env python3
"""Player movement probe (session 44, "gameplay 1/3").

usage: python3 tools/emu/player_probe.py [STATE] [--seg BUTTONS:FRAMES ...] [--every N]

From a save state (default tools/emu/states/gameplay_first.state) play the button segments
in order (e.g. RIGHT:40 NONE:10 B:110 NONE:30) and print per frame: slot 0 / slot 1 x,y,
slot 0 speed nibble ($0340&$0F), obj_timer $0480, jump timer $0620, obj_phase $0600,
orbit_level $06C0, slot 1 meta id $0301, MP $0708, and the order in which the two AIs ran.
Ends with per-segment displacement of slot 0.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from emu.nes import NES  # noqa: E402
from emu import state  # noqa: E402
from emu.pads import parse_buttons  # noqa: E402

DEF_STATE = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'states', 'gameplay_first.state')


def main(argv):
    st, segs, every = DEF_STATE, [], 1
    i = 0
    while i < len(argv):
        if argv[i] == '--seg':
            i += 1
            while i < len(argv) and not argv[i].startswith('--'):
                b, n = argv[i].rsplit(':', 1)
                segs.append((b, 0 if b == 'NONE' else parse_buttons(b), int(n)))
                i += 1
            continue
        if argv[i] == '--every':
            every = int(argv[i + 1])
            i += 2
            continue
        st = argv[i]
        i += 1
    nes = NES()
    state.load(nes, st)
    order = []
    nes.on_exec(26, 0x9D9A, lambda *a: order.append(f'0:X{nes.cpu.x}'))
    nes.on_exec(26, 0x9B45, lambda *a: order.append(f'1:X{nes.cpu.x}'))
    r = nes.bus.ram
    print('seg       f  s0x  s0y  s1x  s1y sp tmr jmp ph orb m1  mp  ai-order')
    for name, mask, n in segs:
        x0 = r[0x90] * 256 + r[0x70]
        y0 = r[0xD0] * 240 + r[0xB0]
        for f in range(n):
            order.clear()
            nes.keys = (lambda m: (lambda _f: m))(mask)
            nes.run_frame()
            if f % every == 0 or f == n - 1:
                print(f'{name:8s}{f:3d} {r[0x70]:4d} {r[0xB0]:4d} {r[0x71]:4d} {r[0xB1]:4d} '
                      f'{r[0x340] & 15:2d} {r[0x480]:3d} {r[0x620]:3d} {r[0x600]:2X} {r[0x6C0]:3d} '
                      f'{r[0x301]:02X} {r[0x708]:3d}  {" ".join(order)}')
        x1 = r[0x90] * 256 + r[0x70]
        y1 = r[0xD0] * 240 + r[0xB0]
        print(f'== {name} {n}f: slot0 dx={x1 - x0:+d} dy={y1 - y0:+d}')


if __name__ == '__main__':
    main(sys.argv[1:])
