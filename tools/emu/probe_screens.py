#!/usr/bin/env python3
"""Probe which buttons / pokes reach the menu, death and save screens (Ph6 validate 4/4 helper).

usage: python3 tools/emu/probe_screens.py [--state F] OUTDIR SCEN [SCEN ...]
SCEN = name=STEP;STEP;...   STEP = 'p:BUTTONS' (press 2 frames) | 'w:N' (run N frames)
                                   | 's:ADDR=VAL' (poke RAM hex) | 'shot' (dump OUTDIR/name_K.png)
Prints (main_state $40, sub_state $41, irq_mode $51, area $6C) after every step.
"""
import os
import sys

TOOLS = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.append(TOOLS)
from emu.nes import NES  # noqa: E402
from emu import state  # noqa: E402
from emu.pads import parse_buttons  # noqa: E402

DEF_STATE = os.path.join(TOOLS, 'emu', 'states', 'area1_enter.state')


def run(nes, out, name, steps):
    r = nes.bus.ram
    k = 0
    for st in steps:
        if st == 'shot':
            p = os.path.join(out, f'{name}_{k}.png')
            nes.dump_screen(p)
            k += 1
        elif st.startswith('p:'):
            m = parse_buttons(st[2:])
            for _ in range(2):
                nes.keys = {nes.ppu.frame_no: m}
                nes.run_frame()
            nes.keys = {}
        elif st.startswith('w:'):
            for _ in range(int(st[2:])):
                nes.keys = {}
                nes.run_frame()
        elif st.startswith('s:'):
            a, v = st[2:].split('=')
            r[int(a, 16)] = int(v, 16)
        print(f'  {name:10s} {st:12s} f{nes.ppu.frame_no:5d} main {r[0x40]:02X} sub {r[0x41]:02X} '
              f'irq {r[0x51]:02X} area {r[0x6C]:02X}')


def main(argv):
    st = DEF_STATE
    if argv and argv[0] == '--state':
        st, argv = argv[1], argv[2:]
    out = argv[0]
    os.makedirs(out, exist_ok=True)
    for spec in argv[1:]:
        name, steps = spec.split('=', 1)
        nes = NES()
        state.load(nes, st)
        run(nes, out, name, [s for s in steps.split(';') if s])


if __name__ == '__main__':
    main(sys.argv[1:])
