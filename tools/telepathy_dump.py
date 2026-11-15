#!/usr/bin/env python3
"""Dump the Telepathy spell tables (session 14). See REVERSE.md "Telepathy reply".

Telepathy 14:816F, $23 = sage 0-3 (Tornel/Zebu/Asina/Kensu):
  fixed rows   14:9A2C[(row*4+sage)*2] -> linear msg id (hi, lo)   rows 0..3
  hint class   c = 14:98F4[area_id]  (256 bytes)
  level gate   14:8213[c] >= $0421 (player level) -> fixed row 3
  hint list    14:99F4[(c*4+sage)*2] -> entries: flagref(2) msg(2) [alt msg(2) if b0 bit6]
               first entry whose flag test is FALSE gives the message (alt if $29 = r&1 set);
               an entry with b0 bit7 that tests TRUE ends the list -> fixed row 3.
usage: telepathy_dump.py [--out f]
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
import npc_script_dump as N  # noqa

SAGES = ['Tornel', 'Zebu', 'Asina', 'Kensu']


def mid_at(a):
    return N.rd(a) << 8 | N.rd(a + 1)


def m(mid):
    return '%d.%d "%s"' % (mid >> 5, mid & 31, N.msg_text(mid, 70))


def main():
    L = []
    L.append('Fixed rows 14:9A2C')
    for row in range(4):
        for s in range(4):
            L.append('  row %d %-6s %s' % (row, SAGES[s], m(mid_at(0x9A2C + (row * 4 + s) * 2))))
    cls = [N.rd(0x98F4 + a) for a in range(256)]
    nclass = max(cls) + 1
    L.append('')
    L.append('Hint class per area 14:98F4 (%d classes)' % nclass)
    for c in range(nclass):
        areas = [a for a in range(256) if cls[a] == c]
        L.append('  class %d (level gate > %d): %d areas: %s' % (
            c, N.rd(0x8213 + c), len(areas), ' '.join('%02X' % a for a in areas)))
    L.append('')
    for c in range(nclass):
        for s in range(4):
            p = N.w(0x99F4 + (c * 4 + s) * 2)
            L.append('class %d %-6s list %04X' % (c, SAGES[s], p))
            for _ in range(32):
                b0, b1 = N.rd(p), N.rd(p + 1)
                alt = b0 & 0x40
                txt = '    unless %s: %s' % (N.fcond(b0, b1), m(mid_at(p + 2)))
                if alt:
                    txt += ' | alt %s' % m(mid_at(p + 4))
                L.append(txt)
                p += 6 if alt else 4
                if b0 & 0x80:
                    L.append('    (end -> fixed row 3)')
                    break
    txt = '\n'.join(L)
    if '--out' in sys.argv:
        with open(sys.argv[sys.argv.index('--out') + 1], 'w') as f:
            f.write(txt + '\n')
    print(txt)


if __name__ == '__main__':
    main()
