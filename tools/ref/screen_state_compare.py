#!/usr/bin/env python3
"""Compare the FCEUX menu/death/save screens (ref/screen_NAME.*) by machine state, not pixels (s91).

usage (repo root):
  python3 tools/ref/screen_state_compare.py [--ref DIR] [--search N] [--out FILE] [NAME ...]

The s84 fceux_capture.lua run (speedmode "maximum") saved the SAME stale screenshot for every
screen_*.png (identical to frame_2450.png / frame_2460.png: FCEUX skips rendering at max speed), but
its .ram/.wram/.nt/.pal dumps are live.  So this replays each SCREENS scenario of the Lua script in
tools.emu from tools/emu/states/area1_enter.state (= after frame 2450 of the canonical script, where
the Lua takes its savestate) and diffs RAM / OAM shadow / WRAM / nametables / palette with
compare_ref.compare_frame.  The last wait is shortened by N (--search, default 3) and the 2N+1 frames
around the nominal end are snapshotted, so a +-N frame phase offset is absorbed (fewest RAM diffs wins).
Exit 0 = every screen identical, 1 = differences.
"""
import argparse   # noqa: F401  (stdlib first: tools/ on sys.path shadows dis)
import dis        # noqa: F401
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if ROOT not in sys.path:
    sys.path.append(ROOT)

from tools.emu.nes import NES                      # noqa: E402
from tools.emu import state                        # noqa: E402
from tools.emu.pads import parse_buttons           # noqa: E402
from tools.emu import debug                        # noqa: E402
from tools.ref.compare_ref import (load_ref, compare_frame, snapshot, load_labels,  # noqa: E402
                                   CANON_KEYS, ram_diffs, pal_fold)

STATE = os.path.join(ROOT, 'tools', 'emu', 'states', 'area1_enter.state')
SCREEN_STATE_FRAME = 2450

# copy of fceux_capture.lua SCREENS (session 84)
SCREENS = [
    ('status',   'w:10 p:START w:40'),
    ('equip',    'w:10 p:SELECT w:40'),
    ('items',    'w:10 p:SELECT w:40 p:UP w:20'),
    ('save',     'w:10 p:SELECT w:40 p:START w:40'),
    ('save_ask', 'w:10 p:SELECT w:40 p:START w:40 p:A w:40'),
    ('load_ask', 'w:10 p:SELECT w:40 p:START w:40 p:RIGHT w:20 p:A w:40'),
    ('death',    'w:10 s:3C1=0 w:200'),
    ('gameover', 'w:10 s:3C1=0 w:500'),
]


def start_state(src, delay=0):
    """'canon' = canonical --keys run to frame 2450 with the FCEUX RAM fill (REVERSE.md s74 (b));
    else a tools.emu .state path (area1_enter.state was saved from a zero-fill run)."""
    nes = NES(ram_fill='fceux', wram_fill='zero')
    if src == 'canon':
        nes.keys = debug.parse_keys([CANON_KEYS])
        nes.run_frames(SCREEN_STATE_FRAME + 1 + delay)
    else:
        state.load(nes, src)
    return state.snapshot(nes)


def run_scenario(snap, steps, search):
    nes = NES()
    state.restore(nes, snap)
    toks = steps.split()
    last = toks[-1]
    assert last.startswith('w:') and int(last[2:]) > search
    toks[-1] = f'w:{int(last[2:]) - search}'
    for st in toks:
        kind, arg = st.split(':', 1)
        if kind == 'p':
            m = parse_buttons(arg)
            for _ in range(2):
                nes.keys = {nes.ppu.frame_no: m}
                nes.run_frame()
            nes.keys = {}
        elif kind == 'w':
            for _ in range(int(arg)):
                nes.run_frame()
        elif kind == 's':
            a, v = arg.split('=')
            nes.bus.ram[int(a, 16)] = int(v, 16)
    snaps = {}
    for d in range(-search, search + 1):
        snaps[d] = snapshot(nes)
        nes.run_frame()
    return snaps


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--ref', default=os.path.join(ROOT, 'ref'))
    ap.add_argument('--search', type=int, default=3)
    ap.add_argument('--out')
    ap.add_argument('--json', default=os.path.join(ROOT, 'gfx', 'compare', 'ref_screens.json'),
                    help='per-screen counts for tools/comparison.py report (section 4 Reference column)')
    ap.add_argument('--start', default='canon', help="'canon' (default) or a .state path")
    ap.add_argument('--delay', type=int, default=1,
                    help='canon start: run D more frames before the savestate point (default 1 = '
                         'the s74 gameplay offset; with 0 the death screens keep 36 OAM diffs)')
    ap.add_argument('names', nargs='*')
    a = ap.parse_args(argv)
    lab = load_labels()
    snap0 = start_state(a.start, a.delay)
    res = {}
    out, clean = [f'# screen_state_compare: {a.ref}/screen_* vs tools.emu (state, not pixels)'], 0
    todo = [s for s in SCREENS if not a.names or s[0] in a.names]
    for name, steps in todo:
        ref = load_ref(os.path.join(a.ref, 'screen_' + name))
        if 'ram' not in ref:
            out.append(f'== screen {name}: no ref capture')
            continue
        ref.pop('img', None)        # stale screenshot, see docstring
        snaps = run_scenario(snap0, steps, a.search)
        sub = []
        ok = compare_frame(0, ref, snaps, a.search, lab, sub)
        d = int(sub[0].split('offset ')[-1].rstrip(')'))
        nonstack = [i for i in ram_diffs(ref['ram'], snaps[d]['ram']) if not 0x100 <= i < 0x200]
        sub[0] = (f'== screen {name} ({steps}): offset {d:+d}, {len(nonstack)} RAM diffs outside the stack '
                  f'page: ' + ' '.join(f'${i:04X}={ref["ram"][i]:02X}/{snaps[d]["ram"][i]:02X}'
                                       for i in nonstack[:24]) + (' ...' if len(nonstack) > 24 else ''))
        out += sub
        clean += ok
        s = snaps[d]
        res[name] = {
            'offset': d, 'ram_nonstack': [f'{i:04X}' for i in nonstack],
            'oam': sum(ref['ram'][0x200 + 4 * k:0x204 + 4 * k] != s['ram'][0x200 + 4 * k:0x204 + 4 * k]
                       for k in range(64)),
            'wram': sum(ref['wram'][i] != s['wram'][i] for i in range(0x2000)),
            'nt': sum(ref['nt'][i] != s['nt'][i] for i in range(0x1000)),
            'pal': len({pal_fold(i) for i in range(32)
                        if (ref['pal'][pal_fold(i)] & 0x3F) != (s['pal'][pal_fold(i)] & 0x3F)})}
    out.append(f'# screens: {clean}/{len(todo)} state-identical')
    txt = '\n'.join(out)
    print(txt)
    if a.out:
        open(a.out, 'w').write(txt + '\n')
    if a.json:
        with open(a.json, 'w') as fh:
            json.dump({'delay': a.delay, 'search': a.search, 'screens': res}, fh, indent=1)
    return 0 if clean == len(todo) else 1


if __name__ == '__main__':
    sys.exit(main())
