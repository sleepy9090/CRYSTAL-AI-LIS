#!/usr/bin/env python3
"""s214 (gap (c2)): does a pass lag exactly when its wall-clock cycles pass the next NMI? For each complete pass of
`tools.emu.pass_cost` tables: start line S, wall cycles `tot` (main loop + interrupts), LAG flag. The next NMI
(L241 of the next frame) is (262 - S + 241) lines away = budget(S) = (503 - S) * 341/3 cycles. Prints, per table,
the margin tot - budget(S) of the largest non-LAG and the smallest LAG pass, and whether one constant K
(lag iff tot - budget(S) > K) separates them; also the start-line histogram and the int (interrupt) cycles
by pass end line (HUD IRQ window).

usage: python3 tools/lag_threshold.py COST.txt [COST2.txt ..] [--list N]"""
import re
import sys

sys.path.insert(0, __file__.rsplit('/', 1)[0])
from pass_cost_stats import parse, STAGES  # noqa: E402

CPL = 341 / 3


def budget(s):
    return (503 - s) * CPL


def nmi_in_pass(path):
    """pass_cost --split-int `ints` log: cycles of the first NMI (N241) after each pass top; on a LAG pass that is
    the NMI inside the pass"""
    out = {}
    for line in open(path):
        m = re.match(r'f(\d+) ', line)
        it = re.search(r'ints (\S*)', line)
        if m and it:
            n = [int(t.split('=')[1]) for t in it.group(1).split(',') if t.startswith('N')]
            if n:
                out[int(m.group(1))] = n[0]
    return out


def main(argv):
    if not argv:
        raise SystemExit(__doc__)
    nlist = int(argv[argv.index('--list') + 1]) if '--list' in argv else 0
    paths = [a for a in argv if a.endswith('.txt')]
    for path in paths:
        rows = [r for r in parse(path) if r['tot'] >= 0 and len(r['st']) == len(STAGES)]
        hist = {}
        for r in rows:
            hist[r['l']] = hist.get(r['l'], 0) + 1
        nmi = nmi_in_pass(path)
        # s214: a LAG pass also holds the NMI it overran (~1300 cycles); the margin is taken without it
        ok = [(r['tot'] - budget(r['l']), r) for r in rows if not r['lag']]
        lag = [(r['tot'] - nmi.get(r['f'], 0) - budget(r['l']), r) for r in rows if r['lag']]
        print(f'{path}: {len(rows)} passes, {len(lag)} LAG; start lines ' +
              ' '.join(f'L{k}:{v}' for k, v in sorted(hist.items())))
        hi = max(ok, key=lambda t: t[0])
        print(f'  largest non-LAG margin {hi[0]:+.0f} (f{hi[1]["f"]} L{hi[1]["l"]} tot {hi[1]["tot"]} '
              f'end f{hi[1]["ef"]}L{hi[1]["el"]})')
        # hit-stop LAG passes (ObjCollideAll huge) are modelled separately in the port; list all LAG margins
        for mg, r in sorted(lag, key=lambda t: t[0]):
            print(f'  LAG f{r["f"]} L{r["l"]} tot {r["tot"]} int {r["int"]} margin {mg:+.0f} col {r["st"]["col"]}'
                  f' end f{r["ef"]}L{r["el"]}')
        if lag:
            lo = min(lag, key=lambda t: t[0])
            print(f'  separates: {"yes" if lo[0] > hi[0] else "NO"} (window {hi[0]:+.0f} .. {lo[0]:+.0f})')
            k = (lo[0] + hi[0]) / 2
            near = {d: sum(1 for mg, _ in ok + lag if abs(mg - k) < d) for d in (100, 200, 400)}
            print(f'  passes within 100 / 200 / 400 cycles of the boundary {k:+.0f}: '
                  f'{near[100]} / {near[200]} / {near[400]} of {len(rows)}')
        # HUD IRQ: int by end line
        ints = {}
        for r in rows:
            if r['lag']:
                continue
            b = 'hud' if r['int'] > 400 else 'none'
            ints.setdefault(b, []).append(r['el'])
        for b, v in sorted(ints.items()):
            print(f'  int {b}: {len(v)} passes, end lines {min(v)}..{max(v)}')
        for mg, r in sorted(ok, key=lambda t: -t[0])[:nlist]:
            print(f'  ok f{r["f"]} L{r["l"]} tot {r["tot"]} int {r["int"]} margin {mg:+.0f} end f{r["ef"]}L{r["el"]}')


if __name__ == '__main__':
    main(sys.argv[1:])
