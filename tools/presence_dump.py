#!/usr/bin/env python3
"""Decode the NpcHidden (14:80A0) presence lists: spawn param -> per-area flag conditions.

usage: presence_dump.py [param ...] [--out f]

NpcHidden (session 17): called by spawn handler 1 (31:E34B) with $23 = spawn param.
List ptr = 14:85E0[p*2] (p < $80) or 14:86E0[(p-$80)*2]. List = records
  area  cond cond ... ,  FF end
where each cond is a 2-byte flag ref (npc_script_dump.fcond: b0 bit5 = invert,
b0 bit7 = LAST cond of the record; 14:80D3 loops while $26 bit7 is clear). For the record whose area == $6C every cond is
tested in order; the first false cond makes the spawn absent ($20 != 0 -> slot cleared).
If all are true, the scan continues with the next record (several records for the
same area are ANDed). Areas not listed -> always present.
"""
import sys
from npc_script_dump import rd, w, fcond


def plist(p):
    base = 0x85E0 if p < 0x80 else 0x86E0
    a = w(base + (p & 0x7F) * 2)
    recs = []
    y = a
    for _ in range(64):
        area = rd(y)
        y += 1
        if area == 0xFF:
            break
        conds = []
        while True:
            b0, b1 = rd(y), rd(y + 1)
            y += 2
            conds.append(fcond(b0, b1))
            if b0 & 0x80:  # bit 7 = last cond of this record
                break
        recs.append((area, conds))
    return a, recs


def main(argv):
    if '--out' in argv:
        i = argv.index('--out')
        sys.stdout = open(argv[i + 1], 'w')
        argv = argv[:i] + argv[i + 2:]
    ps = [int(x, 16) for x in argv] or list(range(256))
    print('; NpcHidden 14:80A0 presence lists (present only while every cond is true)')
    for p in ps:
        a, recs = plist(p)
        if not recs:
            continue
        print(f'p=${p:02X} @{a:04X}: ' + ' | '.join(
            f'area {ar:02X} needs ' + ' & '.join(c) for ar, c in recs))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
