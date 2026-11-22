#!/usr/bin/env python3
"""s210: summary of a `tools.emu.pass_cost` table: per stage min / mean / max cycles, the largest non-LAG and the
smallest LAG pass total (the overrun threshold window), and for each LAG pass which stages were above their mean.

usage: python3 tools/pass_cost_stats.py TABLE.txt [TABLE2.txt ..]"""
import re
import sys

STAGES = ['top', 'upd', 'cam', 'spr', 'col']


def parse(path):
    rows = []
    for line in open(path):
        m = re.match(r'f(\d+) L\s*(\d+) act\s+(\d+) (.*?) tot\s+(-?\d+) end (f\d+L\s*\d+|-)( LAG)?', line)
        if not m:
            continue
        st = dict((k, int(v)) for k, v in re.findall(r'(top|upd|cam|spr|col)\s+(\d+)', m.group(4)))
        it = re.search(r'int\s+(\d+)', m.group(4))
        e = re.match(r'f(\d+)L\s*(\d+)', m.group(6))
        rows.append(dict(int=int(it.group(1)) if it else 0,          # s211 pass_cost --split-int
                         retry=int((re.search(r'padretry (\d+)', line) or [0, 0])[1]),  # s212
                         sl={k: int(v) for k, v in re.findall(r'\b([0-9A-F]{2}:[0-9A-F]{2})=(\d+)', line)},  # s212 --slots
f=int(m.group(1)), l=int(m.group(2)), act=int(m.group(3)), st=st,
                         tot=int(m.group(5)), lag=bool(m.group(7)),
                         ef=int(e.group(1)) if e else -1, el=int(e.group(2)) if e else -1))
    return rows


def main(argv):
    if not argv:
        raise SystemExit(__doc__)
    for path in argv:
        rows = [r for r in parse(path) if r['tot'] >= 0 and len(r['st']) == len(STAGES)]
        if not rows:
            print(f'{path}: no complete passes')
            continue
        print(f'{path}: {len(rows)} passes, {sum(r["lag"] for r in rows)} LAG')
        mean = {}
        for s in STAGES:
            v = [r['st'][s] for r in rows]
            mean[s] = sum(v) / len(v)
            print(f'  {s}: min {min(v):6} mean {mean[s]:8.0f} max {max(v):6}')
        ok = [r for r in rows if not r['lag']]
        lag = [r for r in rows if r['lag']]
        if ok:
            r = max(ok, key=lambda r: r['tot'])
            print(f'  largest non-LAG total {r["tot"]} (f{r["f"]} start L{r["l"]})')
        if lag:
            r = min(lag, key=lambda r: r['tot'])
            print(f'  smallest LAG total    {r["tot"]} (f{r["f"]} start L{r["l"]})')
        for r in lag:
            hot = ' '.join(f'{s}+{r["st"][s] - mean[s]:.0f}' for s in STAGES if r['st'][s] > mean[s] * 1.15)
            print(f'  LAG f{r["f"]} tot {r["tot"]}: {hot}')


if __name__ == '__main__':
    main(sys.argv[1:])
