#!/usr/bin/env python3
"""Which object types start with a given move speed (session 76, Ph3 player leftovers (c)).

usage: python3 tools/speed_users.py [SPEED ...]   (default: 4 9)

Speed = obj_flags2 ($0340) low nibble, the ObjStepVector 26:8480 speed index. For each speed,
lists the ObjTypeTable types whose ObjInit record sets $0340 with that low nibble, with their
AI id + handler, plus code sites in banks 26/27/30/31 that OR / load that literal speed into
obj_flags2 (patterns `09 0s 9D 40 03`, `A9 0s 20 F6 9B` = PlayerSetSpeed, `A9 xs 9D 40 03`).
"""
import sys
import os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from objtype_dump import decode, ai_handler, PRG  # noqa: E402


def code_sites(s):
    pats = [bytes([0x09, s, 0x9D, 0x40, 0x03]), bytes([0xA9, s, 0x20, 0xF6, 0x9B])]
    pats += [bytes([0xA9, hi << 4 | s, 0x9D, 0x40, 0x03]) for hi in range(16)]
    out = []
    for bank in range(32):
        base = bank * 0x2000
        blk = PRG[base:base + 0x2000]
        for p in pats:
            i = blk.find(p)
            while i >= 0:
                cpu = (0xC000 if bank == 30 else 0xE000 if bank == 31 else 0x8000 + (bank & 1) * 0x2000) + i
                out.append(f'{bank}:{cpu:04X} {p.hex(" ")}')
                i = blk.find(p, i + 1)
    return out


def main(argv):
    speeds = [int(a) for a in argv] or [4, 9]
    for s in speeds:
        print(f'== speed {s}')
        for t in range(256):
            if t == 1:
                continue
            _, _, cols, _ = decode(t)
            v = cols.get(0x340)
            if v is not None and v & 0x0F == s:
                ai = cols.get(0x4A0)
                ait = f'ai ${ai:02X}->{ai_handler(ai):04X}' if ai is not None else 'ai --'
                print(f'  type ${t:02X} 0340={v:02X} {ait}')
        for c in code_sites(s):
            print(f'  code {c}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
