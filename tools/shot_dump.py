#!/usr/bin/env python3
"""Decode ShotTable (bank 20) used by SpawnShot 26:972D (session 47).

usage: shot_dump.py [--out f]

SpawnShot(A=shot id, Y=dir): record = 20:BC00 + id*4 (id bit 6 -> page $BD00,
so ids $00-$3F at BC00, $40-$5F at BD00). Record:
  +0 first slot (bit 7 set -> nothing spawned, C=0)
  +1 end slot (exclusive)       free slot search 26:98B9 (obj_active = 0)
  +2 object type                ObjInit via FarCall_C25D
  +3 count                      objects spawned per call
Each spawned object: obj_meta_dir = Y; inherits shooter's obj_flags b7
(offscreen); placed at shooter pos + offset 20:BD80[(shooter $06E0 << 4) |
((Y*2) & $0F)] (dx, dy; dy += 2 if shooter obj_flags b6).
Also lists the enemy types whose obj_shot_id ($0440) points at each record.
"""
import sys
from nesrom import load_rom
from objtype_dump import decode

_, PRG, _ = load_rom()


def rd20(a):
    return PRG[20 * 0x2000 + (a & 0x1FFF)]


def rec(i):
    base = 0xBD00 + (i & 0x3F) * 4 if i & 0x40 else 0xBC00 + (i & 0x3F) * 4
    return base, [rd20(base + k) for k in range(4)]


def s8(v):
    return v - 256 if v & 0x80 else v


def main(argv):
    if '--out' in argv:
        sys.stdout = open(argv[argv.index('--out') + 1], 'w')
    users = {}
    for t in range(256):
        if t == 1:
            continue
        _, _, c, _ = decode(t)
        if 0x440 in c and c[0x440] and 0x4A0 in c:
            users.setdefault(c[0x440], []).append(f'{t:02X}(ai{c[0x4A0] & 0x7F:02X})')
    print('; ShotTable 20:BC00/BD00: id addr  slots  type count  <- types with obj_shot_id = id')
    for i in range(0x60):
        a, (s0, s1, typ, n) = rec(i)
        u = ' '.join(users.get(i, []))
        if s0 & 0x80:
            print(f'{i:02X} {a:04X}  none ({s0:02X} {s1:02X} {typ:02X} {n:02X})  {u}')
        else:
            print(f'{i:02X} {a:04X}  {s0:02X}-{s1 - 1:02X}  type {typ:02X} x{n}  {u}')
    print('\n; spawn offsets 20:BD80[behav*16 + dir*2] (dx,dy) for dirs 0-7')
    for b in range(8):
        row = []
        for d in range(8):
            a = 0xBD80 + b * 16 + d * 2
            row.append(f'{s8(rd20(a)):+d},{s8(rd20(a + 1)):+d}')
        print(f'behav {b}: ' + '  '.join(row))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
