#!/usr/bin/env python3
"""s217 (gap (c4)): the port lag model's per-stage estimates against tools.emu's stage cycles, pass by pass.
PROBE = `node web/test/lag_probe.mjs STATE.json LAST --out PROBE keys..` (port row F: `top N cam N spr N upd N col N`),
COST = `tools.emu.pass_cost --split-int --slots` table (tools.emu pass F-1; interrupt time taken out of the stages).
Prints per stage bias / rms / max of port - emu over the joined passes, the worst passes of each stage, and
(with --slots) the emu AI dispatch cycles of the worst `upd` passes, to see which AI the fit misses.

usage: python3 tools/lag_stage_diff.py PROBE.txt COST.txt [--worst N] [--slots]"""
import re
import sys

sys.path.insert(0, __file__.rsplit('/', 1)[0])
from pass_cost_stats import parse, STAGES  # noqa: E402


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    worst = int(argv[argv.index('--worst') + 1]) if '--worst' in argv else 4
    port = {}
    for line in open(argv[0]):
        m = re.match(r'f(\d+) L', line)
        if m:
            st = dict((k, int(v)) for k, v in re.findall(r'\b(top|upd|cam|spr|col) (-?\d+)', line))
            if len(st) == len(STAGES):
                port[int(m.group(1))] = st
    rows = {r['f']: r for r in parse(argv[1]) if r['tot'] >= 0 and len(r['st']) == len(STAGES)}
    joined = [(f, port[f], rows[f - 1]) for f in sorted(port) if f - 1 in rows]
    print(f'{len(joined)} passes joined')
    for s in STAGES:
        d = [(p[s] - r['st'][s], f, r) for f, p, r in joined]
        v = [x[0] for x in d]
        print(f'{s:4s} bias {sum(v) / len(v):+7.0f}  rms {(sum(x * x for x in v) / len(v)) ** 0.5:6.0f}  '
              f'max |d| {max(map(abs, v)):5d}')
        for dd, f, r in sorted(d, key=lambda x: -abs(x[0]))[:worst]:
            extra = ''
            if '--slots' in argv and s == 'upd':
                extra = '  ' + ' '.join(f'{k}={c}' for k, c in r['sl'].items())
            print(f'     f{f} port - emu {dd:+6d}  emu {r["st"][s]:6d}{extra}')


if __name__ == '__main__':
    main(sys.argv[1:])
