#!/usr/bin/env python3
"""s214 (gap (c), lag model): fit the interrupt costs of every frame, not only the ones that land in a pass.
`tools.emu.pass_cost --split-int` rows list `ints I142=202,I189=1742,N241=1296` (all outermost interrupts from that
pass top to the next: the H0 split IRQ, the HUD IRQ IrqHandler1Hud 31:F488 + its SoundUpdate, the NMI). Pass row F
joins the port's row F+1 (`web/test/pass_count.mjs --stages`: the pass that started at tools.emu F, then frame
F+1's render, HUD IRQ and NMI). Fits
  hud = I18x cycles   = a*rd + b*wr + c*rom + d on the port `snd` bucket (SoundUpdate)
  nmi = N241 cycles   = a*rd + b*wr + c*rom + d on the port `nmi` bucket (render + NMI / IRQ work minus snd)
and shows how the NMI length sets the next pass's start line (L252 / L255 / ..).
Several runs: comma lists. --coef F applies a saved fit (--save F writes one).

s218: --im LIST (hex irq_mode values) keeps only the frames whose port row has `im=` in LIST.

usage: python3 tools/int_fit.py COST.txt[,..] COUNTS.txt[,..] [--save F | --coef F] [--worst N] [--im 0,7]"""
import json
import re
import sys

sys.path.insert(0, __file__.rsplit('/', 1)[0])
from pass_fit import lstsq, rms  # noqa: E402


def load(cpath, npath):
    cost = {}
    for line in open(cpath):
        m = re.match(r'f(\d+) L\s*(\d+) ', line)
        it = re.search(r'ints (\S*)', line)
        if m and it:
            cost[int(m.group(1))] = dict(l=int(m.group(2)), lag=' LAG' in line,
                                         ints=[(t[0], int(t[1:].split('=')[0]), int(t.split('=')[1]))
                                               for t in it.group(1).split(',') if t])
    port = {}
    for line in open(npath):
        w = line.split()
        if w and w[0].isdigit():
            port[int(w[0])] = {k: tuple(map(int, v.split('/'))) for k, v in (x.split('=') for x in w[1:])
                               if '/' in v and not re.match(r'[0-9A-F]{2}:', k)}
            im = [x[3:] for x in w[1:] if x.startswith('im=')]
            if im:
                port[int(w[0])]['im'] = int(im[0], 16)
    return cost, port


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    worst = int(argv[argv.index('--worst') + 1]) if '--worst' in argv else 5
    coef = json.load(open(argv[argv.index('--coef') + 1])) if '--coef' in argv else {}
    # s218: --im 0,7 = only frames whose port row ends with irq_mode $51 in the list (pass_count.mjs `im=`)
    ims = {int(x, 16) for x in argv[argv.index('--im') + 1].split(',')} if '--im' in argv else None
    hud, nmi, starts = [], [], {}
    for cpath, npath in zip(argv[0].split(','), argv[1].split(',')):
        cost, port = load(cpath, npath)
        for f, c in sorted(cost.items()):
            if c['lag'] or f + 1 not in port or f + 1 not in cost:
                continue
            b = port[f + 1]
            if ims is not None and b.get('im') not in ims:
                continue
            for k, line, cyc in c['ints']:
                if k == 'I' and 180 <= line <= 200 and 'snd' in b:
                    hud.append((f, (*b['snd'], 1), cyc))
                if k == 'N' and line == 241:
                    nmi.append((f, (*b['nmi'], 1), cyc))
                    starts.setdefault(cost[f + 1]['l'], []).append(cyc)
    for name, v in (('hud', hud), ('nmi', nmi)):
        if len(v) < 5:
            print(f'{name}: {len(v)} samples, too few')
            continue
        if name not in coef:
            coef[name] = lstsq([s[1] for s in v], [s[2] for s in v])
        c = coef[name]
        res = [s[2] - sum(a * b for a, b in zip(c, s[1])) for s in v]
        print(f'{name}: {len(v)} frames, cyc ={c[0]:.2f}*rd + {c[1]:.2f}*wr + {c[2]:.2f}*rom +{c[3]:.0f}; '
              f'rms {rms(res):.0f}, max |res| {max(map(abs, res)):.0f} (mean {sum(s[2] for s in v) / len(v):.0f}, '
              f'{min(s[2] for s in v)}..{max(s[2] for s in v)})')
        for s, r in sorted(zip(v, res), key=lambda t: -abs(t[1]))[:worst]:
            print(f'  f{s[0]} {name} {s[2]} port {"/".join(map(str, s[1][:-1]))} residual {r:+.0f}')
    for l, v in sorted(starts.items()):
        print(f'  next pass at L{l}: {len(v)} NMIs of {min(v)}..{max(v)} cycles')
    if '--save' in argv:
        json.dump(coef, open(argv[argv.index('--save') + 1], 'w'))


if __name__ == '__main__':
    main(sys.argv[1:])
