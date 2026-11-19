#!/usr/bin/env python3
"""Dump the player movement constants (session 44, "gameplay 1/3").

usage: python3 tools/player_dump.py [> docs/player_tables.txt]

Sections:
  speed  - ObjStepVector 26:8480 speed tables: for each speed nibble (obj_flags2 & $0F) and
           direction 0-7, the 8-frame (dx, dy) step pattern indexed by obj_timer & 7, plus px/8f.
  jump   - JumpHeightTable 26:9D4D: slot-1 body y offset per slot-0 obj_timer2 value.
  misc   - small player tables (SwordChargeCap, ShotLevelMask, PlayerFacingKeep, ...).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nesrom  # noqa: E402

_, PRG, _ = nesrom.load_rom()
PAIR = 26  # bank pair 26/27 at $8000-$BFFF


def rd(addr, bank_pair=PAIR):
    if addr >= 0xC000:
        bank = 30 if addr < 0xE000 else 31
    else:
        bank = bank_pair + (1 if addr >= 0xA000 else 0)
    return PRG[bank * 0x2000 + (addr & 0x1FFF)]


def s8(v):
    return v - 256 if v >= 128 else v


DIRS = ['U', 'UR', 'R', 'DR', 'D', 'DL', 'L', 'UL']


def speed_rows(speed):
    lo, hi, dxo = rd(0x84BC + speed), rd(0x84CC + speed), rd(0x84DC + speed)
    ptr = lo | hi << 8
    rows = []
    # s86: dx offset $10 = 2 dirs ahead (8-dir table, speeds 0-10); $20 = 4 dirs ahead
    # (16-dir table, speeds 11-15, used by AiHomingFlyer / bosses with 16-dir headings).
    for d in range(16 if dxo == 0x20 else 8):
        pat = []
        for f in range(8):
            y = d * 8 + f
            pat.append((s8(rd(ptr + y + dxo)), s8(rd(ptr + y))))
        rows.append(pat)
    return ptr, dxo, rows


def dump_speed():
    print('== speed: ObjStepVector 26:8480 (ptr lo 84BC / hi 84CC / dx offset 84DC) ==')
    print('step = table[dir*8 + (obj_timer & 7)] (dy), dx at +offset. obj_flags b6 -> speed-2 (min 0)')
    for sp in range(16):
        ptr, dxo, rows = speed_rows(sp)
        print(f'speed {sp:2d}: table ${ptr:04X} dx+{dxo:02X}')
        for d, pat in enumerate(rows):
            sx = sum(p[0] for p in pat)
            sy = sum(p[1] for p in pat)
            steps = ' '.join(f'{x:+d},{y:+d}' for x, y in pat)
            name = DIRS[d] if len(rows) == 8 else f'{d:X}'
            print(f'   {name:2s} sum/8f=({sx:+3d},{sy:+3d})  {steps}')


def dump_jump():
    print('== jump: JumpHeightTable 26:9D4D (index = slot 0 obj_timer2 $0620) ==')
    vals = [s8(rd(0x9D4D + i)) for i in range(0x31)]
    for i in range(0, len(vals), 8):
        print(f'  [{i:2d}] ' + ' '.join(f'{v:+4d}' for v in vals[i:i + 8]))


def dump_misc():
    print('== misc ==')
    tabs = [
        ('SwordChargeCap 26:9D7E (max orbit_level per sword_power)', 0x9D7E, 3),
        ('SwordShotLevelBit 26:9C66 (by equip_sword 0-5)', 0x9C66, 6),
        ('9BF2 status flash meta add (obj1_flash b5-4)', 0x9BF2, 4),
        ('9B41 meta ids while status 9 pose', 0x9B41, 4),
        ('FacingKeep 27:A211 ((2*facing - dir)&7 -> 1 keep)', 0xA211, 8),
        ('BodyNudge 27:A242 (dx,dy by facing)', 0xA242, 8),
        ('DiagToCardinal 27:A017 (pad_dir -> forced dir while obj_param1 b7)', 0xA017, 8),
        ('WallSlideDir 27:A00F (dir -> slide dir on blocked auto-move)', 0xA00F, 8),
        ('27:A092 pad var index table for A022', 0xA092, 12),
        ('26:8BD2 by equip_sword', 0x8BD2, 6),
        ('26:8BD8 MP cost by equip_sword', 0x8BD8, 6),
    ]
    for name, a, n in tabs:
        print(f'  {name}: ' + ' '.join(f'{rd(a + i):02X}' for i in range(n)))


if __name__ == '__main__':
    dump_speed()
    print()
    dump_jump()
    print()
    dump_misc()
