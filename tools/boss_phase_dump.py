#!/usr/bin/env python3
"""Decode BossPhaseTable 15:A413 and the per-boss base indices.

Each boss AI handler in bank 15 ends with `LDA #base / JMP BossPhaseDispatch`
(15:A400), which jumps to BossPhaseTable[(base + $0600,X)*2].  This tool finds
every `A9 nn 4C 00 A4` in bank 15, maps the handler start (via ObjAiTable
27:A314 entries pointing into bank 15) and lists each boss's phase handlers.

usage: python3 tools/boss_phase_dump.py [--out file]
"""
import sys
from nesrom import load_rom

TABLE = 0xA413
TABLE_END = 0xA477


def rd(rom, bank, addr, n=1):
    off = 0x10 + bank * 0x2000 + (addr & 0x1FFF)
    return rom[off:off + n]


def main():
    out = None
    if '--out' in sys.argv:
        out = sys.argv[sys.argv.index('--out') + 1]
    rom = load_rom()[0]
    count = (TABLE_END - TABLE) // 2
    ptrs = []
    for i in range(count):
        b = rd(rom, 15, TABLE + 2 * i, 2)
        ptrs.append(b[0] | b[1] << 8)
    # ObjAiTable 27:A314 (128 entries); AI ids $60-$6F/$E0-$EF -> bank 15
    ai = {}
    for i in range(128):
        b = rd(rom, 27, 0xA314 + 2 * i, 2)
        p = b[0] | b[1] << 8
        if 0xA000 <= p < 0xC000 and 0x60 <= i <= 0x6F:
            ai.setdefault(p, []).append(i)
    # callers: A9 nn 4C 00 A4 in bank 15
    bank = rd(rom, 15, 0xA000, 0x2000)
    sites = []
    for o in range(len(bank) - 4):
        if bank[o] == 0xA9 and bank[o + 2:o + 5] == bytes([0x4C, 0x00, 0xA4]):
            sites.append((0xA000 + o, bank[o + 1]))
    handlers = sorted(ai)
    lines = []
    bases = sorted(set(b for _, b in sites))
    for site, base in sites:
        h = max([p for p in handlers if p <= site], default=None)
        ids = ai.get(h, [])
        nxt = [b for b in bases if b > base]
        end = min(nxt) if nxt else count
        idl = ' '.join('$%02X' % (i | 0x80) for i in ids)
        lines.append('handler 15:%04X (AI %s)  call 15:%04X  base $%02X  phases %d'
                     % (h or 0, idl or '?', site, base, end - base))
        for ph in range(end - base):
            p = ptrs[base + ph]
            lines.append('    phase %2d  [%2d] -> 15:%04X%s' %
                         (ph, base + ph, p, '  (null)' if p == 0 else ''))
    unref = [i for i in range(count) if i < min(bases)]
    if unref:
        lines.append('entries below the lowest base: %s' %
                     ' '.join('[%d]=%04X' % (i, ptrs[i]) for i in unref))
    text = '\n'.join(lines) + '\n'
    if out:
        open(out, 'w').write(text)
    print(text, end='')


if __name__ == '__main__':
    main()
