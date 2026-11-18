#!/usr/bin/env python3
"""Magic / charged-shot probe (session 76, Ph3 player leftovers (a)).

usage: python3 tools/emu/magic_probe.py [STATE] [--poke ADDR=VAL ...] [--seg BUTTONS:FRAMES ...]
       [--every N] [--quiet]

Like player_probe.py, but prints per frame: MP $0708, HP $03C1, sub_state $41, jump timer $0620,
slot-1 y offset (slot1.y - slot0.y, signed), orbit_level $06C0, obj_phase $0600, and every
SpawnShot 26:972D (A=id, Y=dir) and SpendMp 26:9AE8 (A=cost) call made during that frame.
Ends each segment with totals (MP spent, shot ids spawned).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from emu.nes import NES  # noqa: E402
from emu import state  # noqa: E402
from emu.pads import parse_buttons  # noqa: E402

DEF_STATE = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'states', 'gameplay_first.state')


def main(argv):
    st, segs, every, pokes, quiet = DEF_STATE, [], 1, [], False
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
        if a == '--poke':
            i += 1
            while i < len(argv) and not argv[i].startswith('--'):
                ad, v = argv[i].split('=')
                pokes.append((int(ad, 16), int(v, 16)))
                i += 1
            continue
        if a == '--every':
            every = int(argv[i + 1])
            i += 2
            continue
        if a == '--quiet':
            quiet = True
            i += 1
            continue
        st = a
        i += 1
    nes = NES()
    state.load(nes, st)
    ev = []
    cpu = nes.cpu
    nes.on_exec(26, 0x972D, lambda *a: ev.append(('shot', cpu.a, cpu.y)))
    nes.on_exec(26, 0x9AE8, lambda *a: ev.append(('mp', cpu.a, None)))
    r = nes.bus.ram
    for ad, v in pokes:
        if ad < 0x800:
            r[ad] = v
        else:
            nes.bus.write(ad, v)
    print('seg       f  mp  hp ss jmp yofs orb ph  events')
    for name, mask, n in segs:
        mp0 = r[0x708]
        spent, shots = 0, {}
        for f in range(n):
            ev.clear()
            nes.keys = (lambda m: (lambda _f: m))(mask)
            nes.run_frame()
            yofs = ((r[0xB1] - r[0xB0]) + 128) % 256 - 128
            for k, a, y in ev:
                if k == 'mp':
                    spent += a
                else:
                    shots[a] = shots.get(a, 0) + 1
            evs = ' '.join(f'shot{a:02X}/{y}' if k == 'shot' else f'mp-{a}' for k, a, y in ev)
            if not quiet and (f % every == 0 or f == n - 1 or ev):
                print(f'{name:8s}{f:3d} {r[0x708]:3d} {r[0x3C1]:3d} {r[0x41]:02X} {r[0x620]:3d} {yofs:4d} '
                      f'{r[0x6C0]:3d} {r[0x600]:02X}  {evs}')
        sh = ' '.join(f'{k:02X}x{v}' for k, v in sorted(shots.items()))
        print(f'== {name} {n}f: MP {mp0}->{r[0x708]} (SpendMp sum {spent}), shots {sh or "-"}, '
              f'sub_state {r[0x41]:02X}')


if __name__ == '__main__':
    main(sys.argv[1:])
