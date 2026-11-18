#!/usr/bin/env python3
"""Decode ChestPresencePtrs 14:9C82 (was ItemAuxPtrs): chest presence lists (session 72).

SpawnH2 31:E375, p < $80 (chest, type $0D): SwitchBank16(7), $23 = p, JSR ChestPresence 14:85BF:
    ptr = 14:9C82[p*2]; Y = 0; repeat NpcFlagTest (2-byte flag ref, b0 b5 = invert) ...
    first true cond -> $20 = 1 (chest stays); list ends at a ref with b0 b7.
    $20 = 0 -> obj_active = 0 (chest not spawned). So the list is an OR of conditions.

usage: chest_presence_dump.py [--out f]
"""
import sys
import os
sys.path.insert(0, os.path.dirname(__file__))
import npc_script_dump as N  # noqa: E402
import area_dump as A  # noqa: E402

TBL = 0x9C82


def plist(p):
    ptr = N.w(TBL + p * 2)
    conds, a = [], ptr
    for _ in range(32):
        b0, b1 = N.rd(a), N.rd(a + 1)
        a += 2
        conds.append((b0, b1))
        if b0 & 0x80:
            break
    return ptr, conds, a


def main(argv):
    idx = A.param_index()
    placed = {p: lst for (h, p), lst in idx.items() if h == 2 and p < 0x80}
    L = ['; chest_presence_dump.py (session 72): ChestPresencePtrs 14:9C82 -> 14:85BF OR-lists',
         '; chest spawned if ANY cond true; place = area.slot', '']
    lo, hi, used_end = 0xFFFF, 0, 0
    for p in range(0x80):
        ptr, conds, end = plist(p)
        ok = 0xA106 <= ptr <= 0xA178
        if p in placed or ok:
            lo, hi = min(lo, ptr), max(hi, ptr)
            used_end = max(used_end, end)
        pl = ' '.join(f'{a:02X}.{s:02X}' for a, s, *_ in placed.get(p, []))
        cs = ' | '.join(N.fcond(*c).split('(')[0] for c in conds)
        L.append(f'p=${p:02X} ${ptr:04X} {"" if ok else "(out of range) "}{cs}'
                 + (f'  place {pl}' if pl else ''))
    L.append(f'; pointer range ${lo:04X}-${hi:04X}, records end ${used_end:04X}')
    txt = '\n'.join(L)
    if '--out' in argv:
        with open(argv[argv.index('--out') + 1], 'w') as fh:
            fh.write(txt + '\n')
    print(txt)


if __name__ == '__main__':
    main(sys.argv[1:])
