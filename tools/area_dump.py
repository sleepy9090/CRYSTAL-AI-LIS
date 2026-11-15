#!/usr/bin/env python3
"""Decode the per-area record table read by AreaLoad (31:E14A).

usage: area_dump.py [list|chr|spawns] [area ...]
  list    (default) one line per area: ptr, $07E6/$07E7, R4/R5, spawn count
  chr     group areas by (R4,R5) sprite CHR pair (for meta_dump.py --chr)
  spawns  full decode of each area's spawn entries
  indirect  every handler-1 param used + its bank 4/5 record (session 8)
  sub     AreaSubTable (30:C7A5) header 10:8300[$6C] sub-tables k0-k5 (session 9)
  sub stats  list areas whose header has a 6th (k5) pointer

Format (session 7, see REVERSE.md "Area records"):
  AreaPtrTable 12:9201 ptr16 indexed by $6C (area id 0-255; banks 12/13 via
  SwitchBank16(6)). Ptr hi bit7 clear -> no record.
  record: 00 e6 e7 R4 R5 {entries} FF
     byte0 != 0 -> record ignored
     entry (4 bytes) per object slot, slots start at X=$0D:
       FE xx xx xx  -> slot skipped (4-byte entry)
       FF           -> end
       b0 = Y cell  : Y = b0*16 + 12
       b1 = X cell  : bit7 -> slot skipped; X = (b1&$7F)*16 + 8 (+8 if b2 b6)
       b2 = flags   : b0-2 handler (SpawnHandlerTable 31:E218), b0-1 also ->$0540,
                      b6 x+8, b7 -> $0380 b5 (tile+$40)
       b3 = param   ($2F) -> $0560
"""
import sys
from nesrom import load_rom

_, PRG, _ = load_rom()
TABLE = 0x9201
BANK = 12

HANDLERS = {
    0: 'type=p+$50',
    1: 'via 4:$80F0 rec',
    2: 'type $0D/$0E (p bit7)',
    3: 'type=p+$D0',
    4: 'type=p (p&F0=40: flag item)',
}


def rd(addr):
    b = BANK + (1 if addr >= 0xA000 else 0)
    return PRG[b * 0x2000 + (addr & 0x1FFF)]


def ptr(area):
    a = TABLE + area * 2
    return rd(a) | rd(a + 1) << 8


def table_len():
    """Table ends where the lowest valid record begins."""
    lo = min(ptr(i) for i in range(256) if ptr(i) & 0x8000 and ptr(i) > TABLE)
    return min(256, (lo - TABLE) // 2)


def decode(area):
    p = ptr(area)
    if not p & 0x8000:
        return None
    if rd(p) != 0:
        return {'ptr': p, 'ignored': rd(p)}
    e6, e7, r4, r5 = (rd(p + i) for i in range(1, 5))
    ents = []
    y = p + 5
    x = 0x0D
    while y < 0xC000:
        b0 = rd(y)
        if b0 == 0xFF:
            break
        if b0 == 0xFE:
            ents.append((x, None))
        else:
            b1, b2, b3 = rd(y + 1), rd(y + 2), rd(y + 3)
            if b1 & 0x80:
                ents.append((x, 'skip'))
            else:
                ents.append((x, (b0 * 16 + 12, (b1 & 0x7F) * 16 + 8 + (8 if b2 & 0x40 else 0),
                                 b2 & 7, b3, b2)))
        y += 4
        x = (x + 1) & 0xFF
    return {'ptr': p, 'e6': e6, 'e7': e7, 'r4': r4, 'r5': r5, 'ents': ents, 'end': y}


def indirect_rec(p):
    """Handler-1 record: banks 4/5 (SwitchBank16(2)), $8000+(p>>2)*$100+$F0+(p&3)*4."""
    a = 0x8000 + (p >> 2) * 0x100 + 0xF0 + (p & 3) * 4
    b = 4 + (1 if a >= 0xA000 else 0)
    o = b * 0x2000 + (a & 0x1FFF)
    return a, bytes(PRG[o:o + 4])


def describe_indirect(r):
    r20, r21, r22, r23 = r
    if r20 == 0xFF:
        return f'type=${r21:02X}'
    s = f'type=$30 $0680={r20:02X} $06A0={r21:02X} meta/$06C0={r23:02X} '
    if r22 & 0x80:
        s += f'$04A0/$06E0={r22 & 0x7F:02X}'
    else:
        s += f'$06E0={r22:02X} tile+={r22 & 0x70:02X}'
    if r22 & 0x08:
        s += ' y|=0E'
    return s


def rd10(addr):
    """Banks 10/11 at $8000-$BFFF (SwitchBank16(5))."""
    b = 10 + (1 if addr >= 0xA000 else 0)
    return PRG[b * 0x2000 + (addr & 0x1FFF)]


def sub_header(area):
    """AreaSubTable 30:C7A5: header ptr from 10:8300[$6C*2] ($8400 half for >=$80)."""
    a = 0x8300 + area * 2  # area*2 overflows into $8400 naturally
    h = rd10(a) | rd10(a + 1) << 8
    return h, [rd10(h + 2 * k) | rd10(h + 2 * k + 1) << 8 for k in range(6)]


def decode_sub(area):
    h, ps = sub_header(area)
    out = [f'{area:3d} ${area:02X}  header {h:04X}  ptrs ' + ' '.join(f'{p:04X}' for p in ps)]
    p = ps[0]
    snd, w, hh, fe, ff = (rd10(p + i) for i in range(5))
    grid = []
    y = p + 5
    for _ in range(hh + 1):
        grid.append(' '.join(f'{rd10(y + i):02X}' for i in range(w + 1)))
        y += w + 1
    out.append(f'   k0 snd=${snd:02X} W={w + 1} H={hh + 1} 62FE={fe:02X} 62FF={ff:02X}')
    out += [f'      row {r:2d}: {g}' for r, g in enumerate(grid)]
    p = ps[1]
    b = [rd10(p + i) for i in range(7)]
    out.append(f'   k1 bgpal={b[0]:02X} {b[1]:02X} {b[2]:02X} $67={b[3]:02X} $69={b[4]:02X} '
               f'R0={b[5]:02X} R1={b[6]:02X}')
    # k2: entry table, length unknown -> up to k3 ptr if it follows, else 8 entries
    p = ps[2]
    n = (ps[3] - p) // 4 if ps[2] < ps[3] <= ps[2] + 0x80 else 8
    out.append('   k2 entries: ' + ' | '.join(
        f'{e}:x={rd10(p + 4 * e + 1):02X}{rd10(p + 4 * e):02X} y={rd10(p + 4 * e + 3):02X}{rd10(p + 4 * e + 2):02X}'
        for e in range(n)))
    p = ps[3]
    ex = []
    while rd10(p) != 0xFF and len(ex) < 64:
        ex.append(f'({rd10(p):X},{rd10(p + 1):X})->{rd10(p + 2):02X}.{rd10(p + 3):02X}')
        p += 4
    out.append('   k3 exits: ' + ' '.join(ex))
    p = ps[4]
    fl = []
    while rd10(p) != 0xFF and len(fl) < 64:
        fl.append(f'f{rd10(p):02X}@scr{rd10(p + 1):02X}')
        p += 2
    out.append('   k4 screen flags: ' + ' '.join(fl))
    p = ps[5]
    if 0x8000 <= p < 0xC000 and p > h + 10:
        lk = []
        while len(lk) < 16:
            r = [rd10(p + i) for i in range(4)]
            lk.append(f'area {r[0]:02X} if x_hi={r[1] >> 4:X} y_hi={(r[2] & 0xF0) | (r[3] >> 4):02X} '
                      f'-> x_hi={r[1] & 15:X} y_hi={((r[2] << 1) & 0xF0) | (r[3] & 15):02X}')
            p += 4
            if rd10(p) == 0xFF or p >= ps[0]:
                break
        out.append('   k5 links (heuristic end): ' + '; '.join(lk))
    else:
        out.append(f'   k5 none (ptr {p:04X})')
    return out


MODES = ('list', 'chr', 'spawns', 'indirect', 'sub')


def main(argv):
    mode = argv[0] if argv and argv[0] in MODES else 'list'
    rest = [a for a in argv if a not in MODES]
    if mode == 'sub':
        if rest == ['stats']:
            # header size check: is sub-table 0 always right after a 5-ptr header?
            k5 = []
            for a in range(256):
                h, ps = sub_header(a)
                if ps[0] != h + 10:
                    k5.append(f'{a:02X}(h={h:04X} k0={ps[0]:04X} k5={ps[5]:04X})')
            print(f'areas whose k0 is not at header+10 ({len(k5)}): ' + ' '.join(k5))
            return 0
        for a in (int(x, 0) for x in rest) if rest else range(table_len()):
            if decode(a) is None or not sub_header(a)[0] & 0x8000:
                print(f'{a:3d} ${a:02X}  (null area)')
                continue
            print('\n'.join(decode_sub(a)))
        return 0
    if mode == 'indirect':
        # every handler-1 spawn across all areas, with its bank 4/5 record
        n = table_len()
        seen = {}
        for a in (int(x, 0) for x in rest) if rest else range(n):
            d = decode(a)
            if not d or 'ents' not in d:
                continue
            for x, e in d['ents']:
                if isinstance(e, tuple) and e[2] == 1:
                    seen.setdefault(e[3], []).append(a)
        for p in sorted(seen):
            addr, r = indirect_rec(p)
            print(f'p=${p:02X} rec {4 + (addr >= 0xA000)}:{addr:04X} {r.hex(" ")}  '
                  f'{describe_indirect(r):<52} areas ' + ' '.join(f'{a:02X}' for a in seen[p]))
        print(f'-- {len(seen)} distinct params')
        return 0
    n = table_len()
    areas = [int(a, 0) for a in rest] or list(range(n))
    print(f'; AreaPtrTable 12:{TABLE:04X}, {n} entries (table end ${TABLE + 2 * n:04X})')
    groups = {}
    for a in areas:
        d = decode(a)
        if d is None:
            if mode == 'list':
                print(f'{a:3d} ${a:02X}  ----  (null ${ptr(a):04X})')
            continue
        if 'ignored' in d:
            print(f'{a:3d} ${a:02X}  {d["ptr"]:04X} byte0=${d["ignored"]:02X} (ignored)')
            continue
        nsp = sum(1 for _, e in d['ents'] if isinstance(e, tuple))
        groups.setdefault((d['r4'], d['r5']), []).append(a)
        if mode in ('list', 'spawns'):
            print(f'{a:3d} ${a:02X}  {d["ptr"]:04X} e6={d["e6"]:02X} e7={d["e7"]:02X} '
                  f'R4={d["r4"]:02X} R5={d["r5"]:02X} spawns={nsp} slots={len(d["ents"])}')
        if mode == 'spawns':
            for x, e in d['ents']:
                if e is None:
                    print(f'      slot {x:02X}: FE (skip)')
                elif e == 'skip':
                    print(f'      slot {x:02X}: b1 bit7 (skip)')
                else:
                    yy, xx, h, prm, b2 = e
                    print(f'      slot {x:02X}: x={xx:4d} y={yy:4d} h{h} p=${prm:02X} '
                          f'b2=${b2:02X}  {HANDLERS.get(h, "INVALID")}')
    if mode == 'chr':
        for (r4, r5), al in sorted(groups.items()):
            print(f'R4={r4:02X} R5={r5:02X}  {len(al):3d} areas: '
                  + ' '.join(f'{a:02X}' for a in al))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
