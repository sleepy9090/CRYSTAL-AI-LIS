#!/usr/bin/env python3
"""s210: fit tools.emu pass cycles (`tools.emu.pass_cost` table) against the port's per-frame RAM-access counts
(`web/test/pass_count.mjs` output, `frame rd wr`). The port's row F is the pass that started at tools.emu frame
F-1 (L252) and ends before the F NMI, so it is joined with the pass_cost row F-1. Least squares
tot = a*rd + b*wr + c; prints the coefficients, residual stats and the worst passes.

usage: python3 tools/pass_fit.py COST.txt COUNTS.txt [--worst N]"""
import sys

sys.path.insert(0, __file__.rsplit('/', 1)[0])
from pass_cost_stats import parse  # noqa: E402


def solve3(m, v):
    """Gauss elimination for a 3x3 system"""
    a = [row[:] + [x] for row, x in zip(m, v)]
    for i in range(3):
        p = max(range(i, 3), key=lambda r: abs(a[r][i]))
        a[i], a[p] = a[p], a[i]
        for r in range(3):
            if r != i:
                k = a[r][i] / a[i][i]
                a[r] = [x - k * y for x, y in zip(a[r], a[i])]
    return [a[i][3] / a[i][i] for i in range(3)]


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    worst = int(argv[argv.index('--worst') + 1]) if '--worst' in argv else 8
    cost = {r['f']: r for r in parse(argv[0]) if r['tot'] >= 0}
    pts = []
    for line in open(argv[1]):
        w = line.split()
        if len(w) == 3 and w[0].isdigit():
            f, rd, wr = map(int, w)
            if f - 1 in cost:
                pts.append((f - 1, rd, wr, cost[f - 1]['tot'], cost[f - 1]['lag']))
    if len(pts) < 4:
        raise SystemExit('too few joined passes')
    xs = [(rd, wr, 1) for _, rd, wr, _, _ in pts]
    ys = [t for *_, t, _ in pts]
    ata = [[sum(x[i] * x[j] for x in xs) for j in range(3)] for i in range(3)]
    aty = [sum(x[i] * y for x, y in zip(xs, ys)) for i in range(3)]
    a, b, c = solve3(ata, aty)
    res = [(y - (a * x[0] + b * x[1] + c), p) for x, y, p in zip(xs, ys, pts)]
    rms = (sum(r * r for r, _ in res) / len(res)) ** 0.5
    print(f'{len(pts)} passes: tot = {a:.2f}*rd + {b:.2f}*wr + {c:.0f}; residual rms {rms:.0f}, '
          f'max |res| {max(abs(r) for r, _ in res):.0f} cycles')
    for r, p in sorted(res, key=lambda t: -abs(t[0]))[:worst]:
        print(f'  f{p[0]} rd {p[1]} wr {p[2]} tot {p[3]}{" LAG" if p[4] else ""}: residual {r:+.0f}')


if __name__ == '__main__':
    main(sys.argv[1:])
