#!/usr/bin/env python3
"""Area connection graph from the AreaSubTable data (session 75).

usage: area_graph.py [area ...]
  For each given area (default: all), list every k3 exit (ExitLookup 31:EB09)
  and k5 edge link (AreaEdgeLink 31:E573) in OTHER areas that leads to it,
  plus the code-side area_id sources (warp table 30:DC58, debug cycler
  30:CBEC, event act 30:D5C9 = $F2, new game 30:C96D = $00).
  --group LO-HI   report only links that enter [LO,HI] from outside it
"""
import sys
from area_dump import rd10, sub_header, table_len
from nesrom import load_rom

_, PRG, _ = load_rom()


def rd30(addr):
    return PRG[30 * 0x2000 + (addr & 0x1FFF)]


def k3_exits(a):
    h, ps = sub_header(a)
    p, out = ps[3], []
    while rd10(p) != 0xFF and len(out) < 64:
        out.append((rd10(p), rd10(p + 1), rd10(p + 2), rd10(p + 3)))
        p += 4
    return out


def k5_links(a):
    h, ps = sub_header(a)
    p, out = ps[5], []
    if not (0x8000 <= p < 0xC000 and p > h + 10):
        return out
    while len(out) < 16:
        out.append(tuple(rd10(p + i) for i in range(4)))
        p += 4
        if rd10(p) == 0xFF or p >= ps[0]:
            break
    return out


def code_sources():
    src = {}
    for i in range(16):
        src.setdefault(rd30(0xCBEC + i), []).append(f'debug cycler 30:CBEC[{i}]')
    i = 0
    while rd30(0xDC58 + i) != 0xFF:
        src.setdefault(rd30(0xDC58 + i), []).append(f'warp table 30:DC58[{i}]')
        i += 1
    src.setdefault(0xF2, []).append('event act 30:D5C9')
    src.setdefault(0x00, []).append('new game 30:C96D')
    return src


def main(argv):
    grp = None
    if '--group' in argv:
        i = argv.index('--group')
        lo, hi = (int(x, 16) for x in argv[i + 1].split('-'))
        grp = range(lo, hi + 1)
        argv = argv[:i] + argv[i + 2:]
    n = table_len()
    inbound = {}
    for a in range(n):
        h, _ = sub_header(a)
        if not h & 0x8000:
            continue
        for (cx, cy, d, e) in k3_exits(a):
            inbound.setdefault(d, []).append(f'k3 {a:02X} cell({cx:X},{cy:X}) entry {e:02X}')
        for r in k5_links(a):
            inbound.setdefault(r[0], []).append(f'k5 {a:02X} {" ".join(f"{b:02X}" for b in r)}')
    for d, l in code_sources().items():
        inbound.setdefault(d, []).extend(l)
    targets = [int(x, 16) for x in argv] if argv else (list(grp) if grp else range(256))
    for t in targets:
        srcs = inbound.get(t, [])
        if grp:
            srcs = [s for s in srcs if not (s.startswith('k') and int(s.split()[1], 16) in grp)]
        print(f'${t:02X}: ' + ('; '.join(srcs) if srcs else '(no inbound link)'))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
