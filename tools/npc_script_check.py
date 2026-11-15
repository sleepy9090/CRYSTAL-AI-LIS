#!/usr/bin/env python3
"""Sanity-check NPC talk scripts (14:8000 VM) decoded by npc_script_dump.

For each param, report section-B entries whose start offset lies *inside* another
decoded entry (overlapping byte ranges). Such a chain can only be reached through a
misaligned `next` byte, so its conds/results/set-lists are junk. Also lists the
entry whose `next` points at the misaligned offset, and the areas that place the
param (from gfx/spawn_params.txt) so unplaced scripts are visible.

usage: npc_script_check.py [params hex...] [--flag F100]
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(__file__))
import npc_script_dump as nsd  # noqa: E402

ROOT = os.path.join(os.path.dirname(__file__), '..')


def entry_len(bbase, e):
    a = bbase + e['off'] + 5
    if 'sets' in e:
        _, a = nsd.setlist(a)
    return a - (bbase + e['off'])


def placed_params():
    path = os.path.join(ROOT, 'gfx', 'spawn_params.txt')
    out = {}
    if not os.path.exists(path):
        return out
    for line in open(path):
        m = re.match(r'h1 p=\$([0-9A-F]{2}) type=\$30\b.*?\d+x: (.*)$', line)
        if m:
            out.setdefault(int(m.group(1), 16), []).extend(m.group(2).split())
    return out


def linear(p, limit=0x200):
    """Decode section B back-to-back from Bbase (entries are packed contiguously).
    Returns (entries, starts, bad) where bad = entries whose `next` is not an entry start."""
    s = nsd.script(p)
    bbase, a, ents = s['Bbase'], s['Bbase'], []
    while a - bbase < limit:
        b0, b1 = nsd.rd(a), nsd.rd(a + 1)
        e = {'off': a - bbase, 'cond': nsd.fcond(b0, b1), **nsd.result(a + 2), 'next': nsd.rd(a + 4),
             'last': bool(b0 & 0x80)}
        a += 5
        if b0 & 0x40:
            e['sets'], a = nsd.setlist(a)
        ents.append(e)
        if a >= s['end'] and e['last']:
            break
    starts = {e['off'] for e in ents}
    bad = [e for e in ents if e['next'] not in starts]
    return s, ents, bad


def check(p):
    s = nsd.script(p)
    bbase = s['Bbase']
    spans = [(e['off'], e['off'] + entry_len(bbase, e), e) for e in s['B']]
    bad = []
    for off, end, e in spans:
        for o2, e2, other in spans:
            if other is not e and o2 < off < e2:
                srcs = [x['off'] for x in s['B'] if x['next'] == off]
                bad.append((e, other, srcs))
                break
    return s, bad


def main():
    args = sys.argv[1:]
    if args and args[0] == 'linear':
        for p in [int(a, 16) for a in args[1:]]:
            s, ents, bad = linear(p)
            print(f"param ${p:02X} script 14:{s['ptr']:04X} Bbase {bs(s)}: {len(ents)} linear entries, "
                  f"{len(bad)} with next not on an entry start")
            for e in ents:
                mark = ' <-- next misaligned' if e in bad else ''
                print(f"   B+{e['off']:02X} {'L' if e['last'] else ' '} cond {e['cond']:<22} act {e['action']:2} "
                      f"msg {e['msg'] >> 5}.{e['msg'] & 31} next={e['next']:02X} {e.get('sets', '')}{mark}")
        return
    flag = None
    if '--flag' in args:
        i = args.index('--flag')
        flag = args[i + 1].upper()
        del args[i:i + 2]
    params = [int(a, 16) for a in args] or [p for p in range(256) if nsd.valid(p)]
    placed = placed_params()
    for p in params:
        if not nsd.valid(p):
            print(f'param ${p:02X}: no valid script pointer')
            continue
        s, bad = check(p)
        if not bad and not args:
            continue
        bad_offs = {id(b[0]) for b in bad}
        print(f"param ${p:02X} script 14:{s['ptr']:04X} Bbase {bs(s)} placed in "
              f"{' '.join(placed.get(p, [])) or '-'}: {len(bad)}/{len(s['B'])} misaligned B entries")
        for e, other, srcs in bad:
            line = (f"   B+{e['off']:02X} inside B+{other['off']:02X}; reached via next from "
                    f"{' '.join('B+%02X' % x for x in srcs) or 'chain fallthrough/area state'}  cond {e['cond']}")
            if flag:
                hit = [x for x in e.get('sets', []) if flag in x] or ([e['cond']] if flag in e['cond'] else [])
                if not hit:
                    continue
                line += f"  [{', '.join(hit)}]"
            print(line)
        if flag:
            for e in s['B']:
                if id(e) in bad_offs:
                    continue
                if flag in e['cond'] or any(flag in x for x in e.get('sets', [])):
                    print(f"   aligned B+{e['off']:02X} cond {e['cond']} sets {e.get('sets', [])}")


def bs(s):
    return f"{s['Bbase']:04X}"


if __name__ == '__main__':
    main()
