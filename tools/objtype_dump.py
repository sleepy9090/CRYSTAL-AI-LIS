#!/usr/bin/env python3
"""Decode ObjTypeTable (13:AC00) ObjInit records and map each type to its AI handler.

usage: objtype_dump.py [type ...] [--ai] [--out f]
  (default)  one line per object type 0-255: sound, every initialised column,
             AI id ($04A0) and its handler from ObjAiTable 27:A314
  --ai       instead list ObjAiTable: AI id -> handler, and the types that use it

ObjInit 30:C25D (session 8/17): record = snd, then 4 groups. Each group is a
mask byte (MSB first) + one byte per set bit; group g, bit b -> column
$0300 + g*$100 + b*$20 (,X). snd != 0 -> PlaySound. Type 1 = no-op, $FF = clear only.

ObjUpdateAll 30:CCCC (session 17): banks 26/27 mapped (SwitchBank16($0D));
for slots 0-30: handler = ObjAiTable 27:A314[(obj_active & $7F)*2]; 0 = none;
AI ids $60-$6F also map bank 15 at $A000 (SetR7($0F)); id $1A is special (30:CD7F).
"""
import sys
from nesrom import load_rom

_, PRG, _ = load_rom()
AI_TABLE = 0xA314  # bank 27


def rd(bank, addr):
    return PRG[bank * 0x2000 + (addr & 0x1FFF)]


def rd13(addr):
    """Banks 12/13 window: ObjInit maps bank 13 at $A000 only."""
    return rd(13 if addr >= 0xA000 else 12, addr)


def type_ptr(t):
    a = 0xAC00 + (t * 2 & 0xFF) + (0x100 if t >= 0x80 else 0)
    return rd13(a) | rd13(a + 1) << 8


def decode(t):
    p = type_ptr(t)
    snd = rd13(p)
    cols = {}
    if t == 0xFF:
        return p, snd, cols, p + 1
    y = p + 1
    for g in range(4):
        m = rd13(y)
        y += 1
        for b in range(8):
            if m & (0x80 >> b):
                cols[0x300 + g * 0x100 + b * 0x20] = rd13(y)
                y += 1
    return p, snd, cols, y


def ai_handler(ai):
    i = ai & 0x7F
    a = AI_TABLE + i * 2
    return rd(27, a) | rd(27, a + 1) << 8


def main(argv):
    if '--out' in argv:
        i = argv.index('--out')
        sys.stdout = open(argv[i + 1], 'w')
        argv = argv[:i] + argv[i + 2:]
    ai_mode = '--ai' in argv
    types = [int(a, 16) for a in argv if not a.startswith('--')] or list(range(256))
    if ai_mode:
        users = {}
        for t in range(256):
            if t == 1:
                continue
            _, _, cols, _ = decode(t)
            if 0x4A0 in cols:
                users.setdefault(cols[0x4A0] & 0x7F, []).append(t)
        print('; ObjAiTable 27:A314, AI id = obj_active & $7F')
        for i in range(0x80):
            h = ai_handler(i)
            bank = '15' if 0x60 <= i <= 0x6F and h >= 0xA000 else ('26' if h < 0xA000 else '27')
            if h == 0 and i not in users:
                continue
            print(f'ai ${i:02X} -> {bank}:{h:04X}  types ' +
                  ' '.join(f'{t:02X}' for t in users.get(i, [])))
        return 0
    print('; ObjTypeTable 13:AC00 (types >= $80 at $AD00); cols = $0300-$06E0 column: value')
    for t in types:
        if t == 1:
            print('type $01  (no-op)')
            continue
        p, snd, cols, end = decode(t)
        ai = cols.get(0x4A0)
        aitxt = f'ai ${ai:02X}->{ai_handler(ai):04X}' if ai is not None else 'ai --'
        print(f'type ${t:02X} @{p:04X} snd={snd:02X} {aitxt:<14} ' +
              ' '.join(f'{c:04X}={v:02X}' for c, v in sorted(cols.items())))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
