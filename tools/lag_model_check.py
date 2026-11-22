#!/usr/bin/env python3
"""s215 (gap (c2)): the port's lag model (web/src/game/lagmodel.js) against tools.emu, pass by pass.
PROBE = `node web/test/lag_probe.mjs STATE.json LAST keys.. > PROBE` (port row F: start line, estimate, budget);
COST = `tools.emu.pass_cost --split-int` table (tools.emu pass F-1). For every joined pass: emu wall cycles =
main-loop stages + the interrupts inside the pass, minus the overrun NMI on a LAG pass (s214 rule), vs the
port estimate; start line port vs emu; LAG port vs emu. Prints bias / rms of est - emu, the start-line misses,
the LAG disagreements and the passes nearest the boundary. Stops the join at the first LAG disagreement unless
--all (after it the runs are one pass apart).

usage: python3 tools/lag_model_check.py PROBE.txt COST.txt [--all] [--worst N]"""
import re
import sys

sys.path.insert(0, __file__.rsplit('/', 1)[0])
from pass_cost_stats import parse, STAGES  # noqa: E402
from lag_threshold import nmi_in_pass, budget  # noqa: E402


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    worst = int(argv[argv.index('--worst') + 1]) if '--worst' in argv else 6
    port = {}
    for line in open(argv[0]):
        m = re.match(r'f(\d+) L(\d+) est (\d+) \(hud (\d+)\) budget (\d+)', line)
        if m:
            port[int(m.group(1))] = dict(l=int(m.group(2)), est=int(m.group(3)), hud=int(m.group(4)),
                                         lag=' LAG' in line)
    rows = {r['f']: r for r in parse(argv[1]) if r['tot'] >= 0 and len(r['st']) == len(STAGES)}
    nmi = nmi_in_pass(argv[1])
    out, lmiss, dis = [], [], []
    for f in sorted(port):
        r = rows.get(f - 1)
        if not r:
            continue
        p = port[f]
        emu = r['tot'] - (nmi.get(r['f'], 0) if r['lag'] else 0)
        if p['l'] != r['l']:
            lmiss.append((f, p['l'], r['l']))
        out.append((f, p['est'] - emu, p['est'] - budget(p['l']), emu - budget(r['l']), p, r))
        if p['lag'] != r['lag']:
            dis.append(out[-1])
            if '--all' not in argv:
                break
    d = [o[1] for o in out]
    print(f'{len(out)} passes joined: est - emu bias {sum(d) / len(d):+.0f}, '
          f'rms {(sum(x * x for x in d) / len(d)) ** 0.5:.0f}, max |d| {max(map(abs, d))}')
    print(f'start-line misses: {len(lmiss)} ' + ' '.join(f'f{f}:L{a}/L{b}' for f, a, b in lmiss[:10]))
    print(f'LAG port / emu: {sum(o[4]["lag"] for o in out)} / {sum(o[5]["lag"] for o in out)}; disagreements:')
    for f, dd, pm, em, p, r in dis:
        print(f'  f{f} port {"LAG" if p["lag"] else "ok"} margin {pm:+.0f} (hud {p["hud"]}), emu '
              f'{"LAG" if r["lag"] else "ok"} margin {em:+.0f} (int {r["int"]}), est - emu {dd:+d}')
    print('nearest the boundary (port margin):')
    for f, dd, pm, em, p, r in sorted(out, key=lambda o: abs(o[2]))[:worst]:
        print(f'  f{f} port margin {pm:+.0f} emu margin {em:+.0f} est - emu {dd:+d}{" LAG" if r["lag"] else ""}')
    print('largest est - emu:')
    for f, dd, pm, em, p, r in sorted(out, key=lambda o: -abs(o[1]))[:worst]:
        print(f'  f{f} est - emu {dd:+d} (port hud {p["hud"]}, emu int {r["int"]}) end f{r["ef"]}L{r["el"]}'
              f'{" LAG" if r["lag"] else ""}')


if __name__ == '__main__':
    main(sys.argv[1:])
