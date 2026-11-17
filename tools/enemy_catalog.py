#!/usr/bin/env python3
"""Enemy catalogue (session 47): ObjTypeTable stats per type, grouped by AI id,
with the areas whose spawn lists place each type directly.

usage: enemy_catalog.py [--all] [--out f]
  default: types that are collision targets with HP or attack (enemies/bosses)
  --all:   every type with an AI

Columns (see REVERSE.md "Object slot struct"):
  hp $03C0, atk $03E0, def $0400, hitf $0420, shot $0440 (per-AI meaning),
  bg $0460, spd = $0340 lo nibble, coll $03A0, die $04C0 (death type),
  ed $0500 (lo elem / hi drop class), xp $0520 (b7 -> (v&7F)*16),
  tmr $0480, ph $0600, beh $06E0.
Spawn handlers (31:E218): h0 type=p+$50, h3 type=p+$D0, h4 type=p.
"""
import sys
from objtype_dump import decode, ai_handler
import area_dump


def spawn_index():
    idx = {}
    for (h, p), uses in area_dump.param_index().items():
        t = {0: (p + 0x50) & 0xFF, 3: (p + 0xD0) & 0xFF, 4: p}.get(h)
        if t is None:
            continue
        for a, *_ in uses:
            idx.setdefault(t, set()).add(a)
    return idx


def xp(v):
    if v is None:
        return '-'
    return str((v & 0x7F) * 16) if v & 0x80 else str(v)


def main(argv):
    if '--out' in argv:
        i = argv.index('--out')
        sys.stdout = open(argv[i + 1], 'w')
        argv = argv[:i] + argv[i + 2:]
    allt = '--all' in argv
    sp = spawn_index()
    groups = {}
    for t in range(256):
        if t == 1:
            continue
        _, snd, c, _ = decode(t)
        ai = c.get(0x4A0)
        if ai is None:
            continue
        enemy = (c.get(0x3A0, 0) & 0xF0) and (c.get(0x3C0, 0) or c.get(0x3E0, 0))
        if not (allt or enemy):
            continue
        groups.setdefault(ai & 0x7F, []).append((t, snd, c))

    def f(c, k):
        return f'{c[k]:02X}' if k in c else '--'

    print(__doc__.split('\n')[0])
    print('type snd  hp atk def hitf shot bg spd coll die  ed   xp  tmr ph beh  areas')
    for ai in sorted(groups):
        h = ai_handler(ai)
        bank = '15' if 0x60 <= ai <= 0x6F else ('26' if h < 0xA000 else '27')
        print(f'\n; AI ${ai:02X} -> {bank}:{h:04X}')
        for t, snd, c in groups[ai]:
            areas = ' '.join(f'{a:02X}' for a in sorted(sp.get(t, ())))
            print(f'  {t:02X}  {snd:02X}  {f(c,0x3C0)}  {f(c,0x3E0)}  {f(c,0x400)}  {f(c,0x420)}  '
                  f' {f(c,0x440)}  {f(c,0x460)}  {c.get(0x340, 0) & 15:X}  {f(c,0x3A0)}  {f(c,0x4C0)}  '
                  f'{f(c,0x500)} {xp(c.get(0x520)):>4}  {f(c,0x480)}  {f(c,0x600)} {f(c,0x6E0)}  {areas}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
