#!/usr/bin/env python3
"""s210: fit tools.emu pass cycles (`tools.emu.pass_cost` table) against the port's per-frame RAM-access counts
(`web/test/pass_count.mjs` output, `frame rd wr`). The port's row F is the pass that started at tools.emu frame
F-1 (L252) and ends before the F NMI, so it is joined with the pass_cost row F-1. Least squares
tot = a*rd + b*wr + c; prints the coefficients, residual stats and the worst passes.

s211 --stages: COUNTS is `pass_count.mjs --stages` output; each pass_cost stage column (top upd cam spr col) is
fitted on its own port bucket (cyc = a*rd + b*wr + c*rom + d, rom = cpuRd calls), then the five fitted stages are summed and
compared with `tot`. --coef FILE.txt applies the coefficients saved by an earlier --save FILE.txt run
(cross-check on another area) instead of fitting.

COST / COUNTS may be comma lists (runs fitted together); --skip-lag leaves LAG passes out (hit-stop frames the
port already models); --show STAGE|int lists every pass of one stage / the interrupt time with the pass end.

usage: python3 tools/pass_fit.py COST.txt COUNTS.txt [--worst N]
       [--stages [--save F | --coef F] [--skip-lag] [--show S]]"""
import json
import re
import sys

sys.path.insert(0, __file__.rsplit('/', 1)[0])
from pass_cost_stats import parse  # noqa: E402

STAGES = ['top', 'upd', 'cam', 'spr', 'col']


def solve(m, v):
    """Gauss elimination for an n x n system (a singular column gets coefficient 0)"""
    n = len(v)
    a = [row[:] + [x] for row, x in zip(m, v)]
    for i in range(n):
        p = max(range(i, n), key=lambda r: abs(a[r][i]))
        a[i], a[p] = a[p], a[i]
        if abs(a[i][i]) < 1e-9:
            continue
        for r in range(n):
            if r != i:
                k = a[r][i] / a[i][i]
                a[r] = [x - k * y for x, y in zip(a[r], a[i])]
    return [a[i][n] / a[i][i] if abs(a[i][i]) >= 1e-9 else 0.0 for i in range(n)]


def lstsq(xs, ys):
    n = len(xs[0])
    ata = [[sum(x[i] * x[j] for x in xs) for j in range(n)] for i in range(n)]
    aty = [sum(x[i] * y for x, y in zip(xs, ys)) for i in range(n)]
    return solve(ata, aty)


def rms(res):
    return (sum(r * r for r in res) / len(res)) ** 0.5


def stages_mode(argv, cost, worst):
    pts = []
    # comma lists COST1,COST2 COUNTS1,COUNTS2: several runs fitted together (each joined on its own)
    for cpath, npath in zip(argv[0].split(','), argv[1].split(',')):
        cost = {r['f']: r for r in parse(cpath) if r['tot'] >= 0 and len(r['st']) == len(STAGES)}
        for line in open(npath):
            w = line.split()
            if not w or not w[0].isdigit() or int(w[0]) - 1 not in cost:
                continue
            if '--skip-lag' in argv and cost[int(w[0]) - 1]['lag']:
                continue
            b = {k: tuple(map(int, v.split('/'))) for k, v in (x.split('=') for x in w[1:])}
            pts.append((int(w[0]) - 1, b, cost[int(w[0]) - 1]))
    if len(pts) < 5:
        raise SystemExit('too few joined passes')
    coef = json.load(open(argv[argv.index('--coef') + 1])) if '--coef' in argv else {}
    pred = [0.0] * len(pts)
    for s in STAGES:
        xs = [(*p[1][s], 1) for p in pts]
        ys = [p[2]['st'][s] for p in pts]
        if s not in coef:
            coef[s] = lstsq(xs, ys)
        c = coef[s]
        res = [y - sum(a * b for a, b in zip(c, x)) for x, y in zip(xs, ys)]
        for i, (x, y) in enumerate(zip(xs, ys)):
            pred[i] += y - res[i]
        if '--show' in argv and argv[argv.index('--show') + 1] == s:
            for p, x, y, r in zip(pts, xs, ys, res):
                print(f'  f{p[0]} {s} {y:6} rd/wr/rom {x[0]}/{x[1]}/{x[2]} residual {r:+.0f}  '
                      f'end f{p[2]["ef"]}L{p[2]["el"]}')
        print(f'{s}: cyc ={c[0]:.2f}*rd + {c[1]:.2f}*wr + {c[2]:.2f}*rom +{c[3]:.0f}; '
              f'rms {rms(res):.0f}, max |res| {max(map(abs, res)):.0f} (stage mean {sum(ys) / len(ys):.0f})')
    if '--show' in argv and argv[argv.index('--show') + 1] == 'int':
        for p, q in zip(pts, pred):
            print(f'  f{p[0]} int {p[2]["int"]:5} main-loop {p[2]["tot"] - p[2]["int"]:6} (fit {q:6.0f}) '
                  f'end f{p[2]["ef"]}L{p[2]["el"]}')
    res = [p[2]['tot'] - p[2]['int'] - q for p, q in zip(pts, pred)]
    print(f'{len(pts)} passes, sum of stages vs tot - int: rms {rms(res):.0f}, max |res| {max(map(abs, res)):.0f} cycles')
    for r, p in sorted(zip(res, pts), key=lambda t: -abs(t[0]))[:worst]:
        print(f'  f{p[0]} tot {p[2]["tot"]}{" LAG" if p[2]["lag"] else ""}: residual {r:+.0f}  '
              + ' '.join(f'{s}={p[2]["st"][s]}' for s in STAGES))
    if '--save' in argv:
        json.dump(coef, open(argv[argv.index('--save') + 1], 'w'))


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    worst = int(argv[argv.index('--worst') + 1]) if '--worst' in argv else 8
    if '--stages' in argv:
        return stages_mode(argv, None, worst)
    cost = {r['f']: r for r in parse(argv[0]) if r['tot'] >= 0 and len(r['st']) == len(STAGES)}
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
    a, b, c = lstsq(xs, ys)
    res = [(y - (a * x[0] + b * x[1] + c), p) for x, y, p in zip(xs, ys, pts)]
    print(f'{len(pts)} passes: tot = {a:.2f}*rd + {b:.2f}*wr + {c:.0f}; residual rms {rms([r for r, _ in res]):.0f}, '
          f'max |res| {max(abs(r) for r, _ in res):.0f} cycles')
    for r, p in sorted(res, key=lambda t: -abs(t[0]))[:worst]:
        print(f'  f{p[0]} rd {p[1]} wr {p[2]} tot {p[3]}{" LAG" if p[4] else ""}: residual {r:+.0f}')


if __name__ == '__main__':
    main(sys.argv[1:])
