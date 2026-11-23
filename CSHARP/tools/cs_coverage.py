"""C# port coverage against the REA obligation inventory.

docs/obligations.csv = every named callable of the web port (../web/src), from REA's static analysis
(`rea analyze-javascript-application ../web/src --artifact-format directory`, see docs/architecture_cs.md §6).
Each must be either
  * ported: a tag comment `// js: <module> <name>` in src/**/*.cs right above the C# member
    (several tags may sit above one member), or
  * waived: a row `module,name,reason` in docs/waivers.csv (JS-only plumbing: getters, browser glue the
    SDL frontend replaces, class constructors whose fields became a record).
Tags naming a callable that is not in the inventory are reported as stale; waivers with an empty reason,
naming no inventory callable, or for a callable that is also tagged are reported as bad.

usage: python3 tools/cs_coverage.py [--module SUBSTR] [--missing] [--quiet]
Exit 1 when anything is missing or stale (in the selected modules).
"""
import csv
import glob
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TAG = re.compile(r'js:\s*([\w./-]+\.js)\s+([\w$]+)')


def load_csv(path):
    if not os.path.exists(path):
        return []
    with open(path) as fh:
        return list(csv.DictReader(line for line in fh if not line.startswith('#')))


def main():
    args = sys.argv[1:]
    sub = args[args.index('--module') + 1] if '--module' in args else None
    show_all, quiet = '--missing' in args, '--quiet' in args
    obl = load_csv(os.path.join(ROOT, 'docs/obligations.csv'))
    if not obl:
        sys.exit('docs/obligations.csv missing (see docs/architecture_cs.md §6)')
    wrows = load_csv(os.path.join(ROOT, 'docs/waivers.csv'))
    waived = {(w['module'], w['name']) for w in wrows}
    # C# s7: a waiver needs a reason and must name an inventory callable
    bad_waivers = [f"{w['module']} {w['name']}" for w in wrows if not (w.get('reason') or '').strip()]
    tags = {}
    for p in glob.glob(os.path.join(ROOT, 'src/**/*.cs'), recursive=True):
        for i, line in enumerate(open(p, errors='replace'), 1):
            for m in TAG.finditer(line):
                tags.setdefault((m.group(1), m.group(2)), f'{os.path.relpath(p, ROOT)}:{i}')
    known = {(o['module'], o['name']) for o in obl}
    mods = {}
    for o in obl:
        if sub and sub not in o['module']:
            continue
        k = (o['module'], o['name'])
        st = 'ported' if k in tags else 'waived' if k in waived else 'missing'
        mods.setdefault(o['module'], []).append((o['name'], st, o['start']))
    tot = {'ported': 0, 'waived': 0, 'missing': 0}
    for mod in sorted(mods):
        items = mods[mod]
        c = {s: sum(1 for _, x, _ in items if x == s) for s in tot}
        for s in tot:
            tot[s] += c[s]
        miss = [f'{n}@{ln}' for n, s, ln in items if s == 'missing']
        if quiet and not miss:
            continue
        line = f"{mod:34s} {c['ported'] + c['waived']:4d}/{len(items):<4d}"
        if miss:
            shown = miss if show_all else miss[:3]
            line += '  missing: ' + ' '.join(shown) + ('' if len(shown) == len(miss) else f' (+{len(miss) - 3})')
        print(line)
    stale = sorted(k for k in tags if k not in known and (not sub or sub in k[0]))
    for k in stale:
        print(f'STALE tag js: {k[0]} {k[1]} at {tags[k]}')
    for k in sorted(waived):
        if k not in known and (not sub or sub in k[0]):
            bad_waivers.append(f'{k[0]} {k[1]} (not in the inventory)')
        elif k in tags and (not sub or sub in k[0]):
            bad_waivers.append(f'{k[0]} {k[1]} (also tagged at {tags[k]})')
    for b in bad_waivers:
        print(f'BAD waiver {b}' + ('' if '(' in b else ' (no reason)'))
    n = sum(tot.values())
    print(f"coverage: {tot['ported']} ported + {tot['waived']} waived of {n} "
          f"({100.0 * (tot['ported'] + tot['waived']) / max(n, 1):.1f}%), {tot['missing']} missing, {len(stale)} stale, "
          f"{len(bad_waivers)} bad waivers")
    sys.exit(1 if tot['missing'] or stale or bad_waivers else 0)


if __name__ == '__main__':
    main()
