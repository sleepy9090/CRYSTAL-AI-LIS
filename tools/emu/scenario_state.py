#!/usr/bin/env python3
"""s160: export a web-port state (web_state.py JSON + tools.emu .state) from inside a tools.emu.scenario run:
the first hit of BB:AAAA at or after scenario frame offset OFF (default 30:C904, the main-loop top, which the
port resumes from with no ENTRY_POINTS entry).

usage: python3 -m tools.emu.scenario_state SCENARIO OFF --out J.json --save-state S.state [--until BB:AAAA]"""
import argparse

from tools.emu import scenario, web_state


class _Stop(Exception):
    pass


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('scenario')
    ap.add_argument('off', type=int)
    ap.add_argument('--until', default='30:C904')
    ap.add_argument('--out', required=True)
    ap.add_argument('--save-state')
    ap.add_argument('-v', '--verbose', action='store_true')
    a = ap.parse_args(argv)
    sc = dict(scenario.SCENARIOS[a.scenario])
    bank, addr = a.until.split(':')
    f0 = []
    # scenario.run spawns in the pad read of the frame (off counted from its own f0, one pass later than ours)
    late = [s for s in sc.get('spawn', []) if s[0] >= a.off + 1]
    sc['spawn'] = [s for s in sc.get('spawn', []) if s[0] < a.off + 1]

    def hit(nes):
        if not f0:                        # the first main-loop pass after the setup = scenario f0
            f0.append(nes.ppu.frame_no)
        if a.verbose:
            print(f'  hit f{nes.ppu.frame_no} (+{nes.ppu.frame_no - f0[0]})')
        if nes.ppu.frame_no - f0[0] >= a.off:
            nes.clear_hooks()
            nes.keys = None
            for off, slot, typ, dx, dy in late:   # spawns due this frame: done here, before the export
                scenario.spawn_obj(nes, slot, typ, dx, dy)
            web_state.export(nes, a.until, f'scenario {a.scenario} +{a.off}', a.out, a.save_state)
            raise _Stop()
    # one hook per pc (on_exec keeps the last one), so f0 is taken by the stop hook itself; --until must
    # then be reached every frame (the default main-loop top is)
    sc['exec'] = list(sc.get('exec', [])) + [(int(bank), int(addr, 16), hit)]
    try:
        scenario.run(sc)
    except _Stop:
        return
    raise SystemExit(f'{a.until} not reached after +{a.off}')


if __name__ == '__main__':
    main()
