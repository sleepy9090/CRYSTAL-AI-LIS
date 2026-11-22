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

s212: --ai fits ObjUpdateAll per AI dispatch (needs pass_cost --slots + pass_count.mjs slot tokens), see ai_mode;
the top stage is fitted without the passes where pass_cost saw a ReadPadStable re-read (`padretry`), whose
cost is a separate 'padretry' constant (tools.emu key-timing artifact); --sort orders --show by |residual|.
s213: when COUNTS has `colc` tokens (pass_count.mjs: exact ROM cycles of ObjCollideAll's record loop +
ObjOverlapScan + SubstPlayTail's respawn scan, from collide.js / play.js m.cyc), col = a*colc + the `colh` bucket's
rd/wr/rom (hit handlers, AreaSpawnEntry) + d; a fits to 0.999, d = 77.
All steps: sh tools/passfit_runs.sh.

usage: python3 tools/pass_fit.py COST.txt COUNTS.txt [--worst N]
       [--stages [--ai] [--save F | --coef F] [--skip-lag] [--show S [--sort]]]"""
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


def ai_mode(pts, coef, pred, argv):
    """s212 --ai: the upd stage as per-dispatch fits. pass_cost --slots gives cycles per active-slot dispatch
    (30:CCFA to the next one, `XX:YY` slot:AI), pass_count.mjs the port's rd/wr/rom for the same span. Each AI id
    with >= 6 joined dispatches gets cyc = a*rd + b*wr + c*rom + d, the rest share one 'ai*' fit; the loop
    overhead (upd minus the dispatch spans: inactive slots, knockback / freeze paths) is fitted as 'updrest'."""
    by_ai = {}
    for i, (_, b, c) in enumerate(pts):
        for k, cyc in c['sl'].items():
            if k in b:
                by_ai.setdefault(k[3:], []).append((i, (*b[k], 1), cyc))
    allx = [s for v in by_ai.values() for s in v]
    if 'ai*' not in coef:
        coef['ai*'] = lstsq([s[1] for s in allx], [s[2] for s in allx])
    for ai, v in sorted(by_ai.items()):
        key = 'ai' + ai
        if key not in coef and len(v) >= 6 and '--coef' not in argv:   # out of sample: unseen ids use ai*
            coef[key] = lstsq([s[1] for s in v], [s[2] for s in v])
        c = coef.get(key, coef['ai*'])
        res = [s[2] - sum(a * b for a, b in zip(c, s[1])) for s in v]
        for s, r in zip(v, res):
            pred[s[0]] += s[2] - r
        print(f'  AI ${ai}{"" if key in coef else " (ai*)"}: {len(v)} dispatches, cyc ={c[0]:.2f}*rd + {c[1]:.2f}*wr + '
              f'{c[2]:.2f}*rom +{c[3]:.0f}; rms {rms(res):.0f}, max |res| {max(map(abs, res)):.0f}'
              f' (mean {sum(s[2] for s in v) / len(v):.0f})')
    xs, ys = [], []
    for _, b, c in pts:
        sb = [b[k] for k in c['sl'] if k in b]
        xs.append((b['upd'][0] - sum(s[0] for s in sb), b['upd'][1] - sum(s[1] for s in sb),
                   b['upd'][2] - sum(s[2] for s in sb), 1))
        ys.append(c['st']['upd'] - sum(v for k, v in c['sl'].items() if k in b))
    if 'updrest' not in coef:
        coef['updrest'] = lstsq(xs, ys)
    c = coef['updrest']
    res = [y - sum(a * b for a, b in zip(c, x)) for x, y in zip(xs, ys)]
    for i, (x, y) in enumerate(zip(xs, ys)):
        pred[i] += y - res[i]
    print(f'updrest: cyc ={c[0]:.2f}*rd + {c[1]:.2f}*wr + {c[2]:.2f}*rom +{c[3]:.0f}; rms {rms(res):.0f}, '
          f'max |res| {max(map(abs, res)):.0f} (mean {sum(ys) / len(ys):.0f})')
    miss = sum(1 for _, b, c in pts for k in c['sl'] if k not in b)
    if miss:
        print(f'  ({miss} tools.emu dispatches without a port bucket, left in updrest)')


INT_BASE = 202   # s211: the fixed interrupt time every pass holds


def hud_fit(pts, coef, argv):
    """s214: the HUD IRQ (IrqHandler1Hud 31:F488 + its SoundUpdate) in a pass that runs past ~L191: int - 202 =
    a*rd + b*wr + c*rom + d on the port's `snd` bucket (pass_count.mjs, the SoundUpdate of the same port row)."""
    v = [(p, (*p[1]['snd'], 1), p[2]['int'] - INT_BASE) for p in pts if p[2]['int'] > 400]
    if len(v) < 5:
        print(f'hud: {len(v)} HUD-interrupted passes, too few to fit')
        return
    if 'hud' not in coef:
        coef['hud'] = lstsq([s[1] for s in v], [s[2] for s in v])
    c = coef['hud']
    res = [s[2] - sum(a * b for a, b in zip(c, s[1])) for s in v]
    print(f'hud: {len(v)} passes, int-{INT_BASE} ={c[0]:.2f}*rd + {c[1]:.2f}*wr + {c[2]:.2f}*rom +{c[3]:.0f}; '
          f'rms {rms(res):.0f}, max |res| {max(map(abs, res)):.0f} (mean {sum(s[2] for s in v) / len(v):.0f})')
    if '--show' in argv and argv[argv.index('--show') + 1] == 'hud':
        for s, r in zip(v, res):
            print(f'  f{s[0][0]} hud {s[2]:5} snd {"/".join(map(str, s[1][:-1]))} residual {r:+.0f}'
                  f'  end f{s[0][2]["ef"]}L{s[0][2]["el"]}')


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
    if '--ai' in argv:
        ai_mode(pts, coef, pred, argv)
    for s in STAGES:
        if s == 'upd' and '--ai' in argv:
            continue
        colc = s == 'col' and all('colc' in p[1] for p in pts)
        if colc:   # s213: exact record-loop cycles (collide.js m.cyc) + the hit handlers' rd/wr/rom
            xs = [(p[1]['colc'][0], *p[1].get('colh', (0, 0, 0)), 1) for p in pts]
        else:
            xs = [(*p[1][s], 1) for p in pts]
        ys = [p[2]['st'][s] for p in pts]
        rt = [p[2]['retry'] if s == 'top' else 0 for p in pts]   # s212: ReadPadStable re-reads (pass_cost)
        if s not in coef:
            ok = [(x, y) for x, y, r in zip(xs, ys, rt) if not r]
            coef[s] = lstsq([t[0] for t in ok], [t[1] for t in ok])
        c = coef[s]
        res = [y - sum(a * b for a, b in zip(c, x)) for x, y in zip(xs, ys)]
        if s == 'top':
            if 'padretry' not in coef:
                rr = [r / n for r, n in zip(res, rt) if n]
                coef['padretry'] = sum(rr) / len(rr) if rr else 0.0
            print(f'top: {sum(1 for n in rt if n)} passes with a pad re-read, {coef["padretry"]:.0f} cycles each '
                  f'(a tools.emu key-timing artifact, not in the port model)')
            res = [r - n * coef['padretry'] for r, n in zip(res, rt)]
        for i, (x, y) in enumerate(zip(xs, ys)):
            pred[i] += y - res[i]
        if '--show' in argv and argv[argv.index('--show') + 1] == s:
            rows = list(zip(pts, xs, ys, res))
            if '--sort' in argv:   # s212: largest |residual| first
                rows.sort(key=lambda t: -abs(t[3]))
            for p, x, y, r in rows:
                print(f'  f{p[0]} {s} {y:6} {"colc/h rd/wr/rom" if colc else "rd/wr/rom"} '
                      f'{"/".join(map(str, x[:-1]))} residual {r:+.0f}  end f{p[2]["ef"]}L{p[2]["el"]}')
        if colc:
            print(f'{s}: cyc ={c[0]:.3f}*colc + handlers {c[1]:.2f}*rd + {c[2]:.2f}*wr + {c[3]:.2f}*rom +{c[4]:.0f}; '
                  f'rms {rms(res):.0f}, max |res| {max(map(abs, res)):.0f} (stage mean {sum(ys) / len(ys):.0f})')
            continue
        print(f'{s}: cyc ={c[0]:.2f}*rd + {c[1]:.2f}*wr + {c[2]:.2f}*rom +{c[3]:.0f}; '
              f'rms {rms(res):.0f}, max |res| {max(map(abs, res)):.0f} (stage mean {sum(ys) / len(ys):.0f})')
    if '--show' in argv and argv[argv.index('--show') + 1] == 'int':
        for p, q in zip(pts, pred):
            print(f'  f{p[0]} int {p[2]["int"]:5} main-loop {p[2]["tot"] - p[2]["int"]:6} (fit {q:6.0f}) '
                  f'end f{p[2]["ef"]}L{p[2]["el"]}')
    if all('snd' in p[1] for p in pts):
        hud_fit(pts, coef, argv)
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
