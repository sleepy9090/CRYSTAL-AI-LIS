#!/usr/bin/env python3
"""Dump the NPC talk scripts run by 14:8000 (banks 14/15, SwitchBank16(7)).

Called from 30:D0C8 with $23 = obj_spawn_param of the touched spawn entry.
Script ptr = 14:895D[p] (p < $80) or 14:8A5D[p & $7F].

Flag ref (2 bytes, 14:8148): b0 b1
    flag = ((b0 & 7) << 8 | b1)  -> byte $6480 + (flag >> 3) (low 8 bits), mask 1 << (b1 & 7)
    b0 bit7 = last entry of a list, bit6 = entry has a set-list / last of a set-list,
    b0 bit5 = invert test (true when flag CLEAR). In a set-list bit7 = clear flag, else set.
Result (2 bytes, 14:8078): r0 r1 -> action $22 = r0 >> 3, msg id = (r0 & 7) << 8 | r1
    (group = id >> 5, msg = id & 31; see TextIdUnpack 30:D3C2).

Section A (stateless): entries  cond result [setlist if cond.b6]; first true cond wins,
    result returned, its set-list is NOT applied. Ends at entry with cond.b7.
Area list: (area, state) pairs until FF: if area == $6C then $6580[p] = state.
Section B (stateful, starts after FF): Y = $6580[p] is a byte offset; entries
    cond result next [setlist if cond.b6]; first true -> result, $6580[p] = next, set-list applied.

usage: npc_script_dump.py [param ...] [--json out]
"""
import sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom  # noqa
import text_dump  # noqa

PRG = load_rom()[1]


def rd(addr):  # banks 14/15 mapped at $8000-$BFFF
    bank = 14 if addr < 0xA000 else 15
    return PRG[bank * 0x2000 + (addr & 0x1FFF)]


def w(addr):
    return rd(addr) | rd(addr + 1) << 8


MSGS = {}
for g, bank, _gp, ptrs in text_dump.groups():
    for m, p in enumerate(ptrs):
        MSGS[g * 32 + m] = (bank, p)


def msg_text(mid, n=60):
    if mid not in MSGS:
        return '?'
    s, _ = text_dump.decode(*MSGS[mid])
    s = s.replace('\n', ' ').replace('<END>', '')
    return s[:n]


def flag(b0, b1):
    f = ((b0 & 7) << 8) | b1
    return f, 0x6480 + ((f >> 3) & 0xFF), 1 << (b1 & 7)


def fcond(b0, b1):
    f, a, m = flag(b0, b1)
    return f"{'!' if b0 & 0x20 else ''}F{f:03X}(${a:04X}&{m:02X})"


def result(a):
    r0, r1 = rd(a), rd(a + 1)
    mid = ((r0 & 7) << 8) | r1
    return {'action': r0 >> 3, 'msg': mid, 'group': mid >> 5, 'idx': mid & 31}


def setlist(a):
    out = []
    for _ in range(64):
        b0, b1 = rd(a), rd(a + 1); a += 2
        f, _, _ = flag(b0, b1)
        out.append(('clr' if b0 & 0x80 else 'set') + f' F{f:03X}')
        if b0 & 0x40:
            break
    return out, a


def script(p):
    base = w((0x8A5D if p & 0x80 else 0x895D) + (p & 0x7F) * 2)
    out = {'param': p, 'ptr': base, 'A': [], 'areas': [], 'B': []}
    a = base
    for _ in range(64):  # section A
        b0, b1 = rd(a), rd(a + 1)
        e = {'off': a - base, 'cond': fcond(b0, b1), **result(a + 2)}
        a += 4
        if b0 & 0x40:
            e['sets'], a = setlist(a)
        out['A'].append(e)
        if b0 & 0x80:
            break
    for _ in range(64):  # area list
        ar = rd(a); a += 1
        if ar == 0xFF:
            break
        out['areas'].append((ar, rd(a))); a += 1
    bbase = a
    out['Bbase'] = bbase
    # section B: chains start at state offsets (0, area-list states, 'next' values)
    todo, seen, ends = [0] + [s for _, s in out['areas']], set(), [bbase]
    while todo:
        st = todo.pop(0)
        if st in seen:
            continue
        a = bbase + st
        for _ in range(64):
            if a - bbase in seen:
                break
            seen.add(a - bbase)
            b0, b1 = rd(a), rd(a + 1)
            e = {'off': a - bbase, 'cond': fcond(b0, b1), **result(a + 2), 'next': rd(a + 4)}
            a += 5
            if b0 & 0x40:
                e['sets'], a = setlist(a)
            out['B'].append(e)
            todo.append(e['next'])
            if b0 & 0x80:
                e['last'] = True
                break
        ends.append(a)
    out['B'].sort(key=lambda e: e['off'])
    out['end'] = max(ends)
    return out


def valid(p):
    base = w((0x8A5D if p & 0x80 else 0x895D) + (p & 0x7F) * 2)
    return 0x8000 <= base < 0xC000


def fmt_entry(e, sec):
    s = f"  {sec}+{e['off']:02X} if {e['cond']:<20} -> act {e['action']:2d} msg {e['group']:2d}.{e['idx']:<2d}"
    if 'next' in e:
        s += f" next={e['next']:02X}"
    if e.get('sets'):
        s += ' [' + ', '.join(e['sets']) + ']'
    if e.get('last'):
        s += ' |'
    return s + f'  "{msg_text(e["msg"])}"'


def main():
    args = sys.argv[1:]
    jout = None
    if '--json' in args:
        i = args.index('--json'); jout = args[i + 1]; del args[i:i + 2]
    tout = None
    if '--out' in args:
        i = args.index('--out'); tout = args[i + 1]; del args[i:i + 2]
    params = [int(x, 0) for x in args] or range(256)
    res = []
    lines = []
    print = lines.append  # noqa: A001 - collect, then emit to stdout or --out
    for p in params:
        if not valid(p):
            print(f'param ${p:02X}  (ptr outside $8000-$BFFF, skipped)')
            continue
        s = script(p)
        res.append(s)
        print(f"param ${p:02X}  script 14:{s['ptr']:04X}-{s['end']:04X}  areas {' '.join(f'{x:02X}:{y:02X}' for x, y in s['areas'])}")
        for e in s['A']:
            print(fmt_entry(e, 'A'))
        for e in s['B']:
            print(fmt_entry(e, 'B'))
    if tout:
        with open(tout, 'w') as f:
            f.write('\n'.join(lines) + '\n')
    else:
        sys.stdout.write('\n'.join(lines) + '\n')
    if jout:
        with open(jout, 'w') as f:
            json.dump(res, f, indent=1)


if __name__ == '__main__':
    main()
