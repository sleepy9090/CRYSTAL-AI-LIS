#!/usr/bin/env python3
"""Count ObjStepVector calls by (effective speed, dir, AI) (session 76, Ph3 player leftovers (c)).

usage: python3 tools/emu/step_probe.py [STATE] [--canon] [--seg BUTTONS:FRAMES ...] [--poke ADDR=VAL ...]
       [--speeds 4,9]

Hooks 26:8497 (inside ObjStepVector 26:8480, after the sink speed-2 adjust): Y = effective speed
index, tmp_12 >> 3 = dir 0-7 (0 up, 1 up-right ... 7 up-left), X = slot, AI = obj_active $04A0,X & $7F.
--canon runs tools.emu from power-on over compare_ref.CANON_KEYS instead of a state.
Prints every (speed, dir) seen with call counts per AI, and flags the diagonal calls at the speeds
given by --speeds (default 4,9: the step-table overlap quirk).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from emu.nes import NES  # noqa: E402
from emu import state  # noqa: E402
from emu.pads import parse_buttons  # noqa: E402

# same as tools/emu/sweep.py / tools/ref/compare_ref.py (importing sweep here would shadow stdlib dis)
CANON_KEYS = '1250:START 1400:START 1600:START 1700:START 2300:START 2340:DOWN:80'
DEF_STATE = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'states', 'gameplay_first.state')


def main(argv):
    st, segs, pokes, canon, speeds = DEF_STATE, [], [], False, {4, 9}
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--seg':
            i += 1
            while i < len(argv) and not argv[i].startswith('--'):
                b, n = argv[i].rsplit(':', 1)
                segs.append((0 if b == 'NONE' else parse_buttons(b), int(n)))
                i += 1
            continue
        if a == '--poke':
            i += 1
            while i < len(argv) and not argv[i].startswith('--'):
                ad, v = argv[i].split('=')
                pokes.append((int(ad, 16), int(v, 16)))
                i += 1
            continue
        if a == '--speeds':
            speeds = {int(s) for s in argv[i + 1].split(',')}
            i += 2
            continue
        if a == '--canon':
            canon = True
            i += 1
            continue
        st = a
        i += 1
    nes = NES()
    r = nes.bus.ram
    cnt = {}
    first = {}
    fr = [0]

    def hook(*_):
        cpu = nes.cpu
        k = (cpu.y, r[0x12] >> 3 & 7, r[0x4A0 + cpu.x] & 0x7F)
        cnt[k] = cnt.get(k, 0) + 1
        first.setdefault(k, (fr[0], cpu.x))

    nes.on_exec(26, 0x8497, hook)
    if canon:
        from emu import debug
        nes.keys = debug.parse_keys([CANON_KEYS])
        last = max(int(t.split(':')[0]) for t in CANON_KEYS.split()) + 200
        for f in range(last):
            fr[0] = f
            nes.run_frame()
    else:
        state.load(nes, st)
        for ad, v in pokes:
            r[ad] = v
        for mask, n in segs:
            nes.keys = (lambda m: (lambda _f: m))(mask)
            for _ in range(n):
                nes.run_frame()
                fr[0] += 1
    print('speed dir  ai  calls  first(frame,slot)')
    for k in sorted(cnt):
        s, d, ai = k
        flag = '  <== diagonal at quirk speed' if s in speeds and d & 1 else ''
        print(f'{s:5d} {d:3d}  {ai:02X} {cnt[k]:6d}  {first[k]}{flag}')


if __name__ == '__main__':
    main(sys.argv[1:])
