#!/usr/bin/env python3
"""Look for consumers of the session-32 orphan pointer runs (session 72).

For each run start (and each address inside the run, step 2) search the whole PRG for:
  abs   - any 3-byte instruction with that 16-bit operand (LDA abs / abs,X / abs,Y, ...)
  imm   - LDA/LDX/LDY #lo and #hi of the address within 16 bytes of each other (pointer set-up)
  word  - the address stored as a little-endian word anywhere (a pointer to the run)
Also prints the run itself (words) and the label/bank context.

Hits are kept only in the run's own 16 KB bank pair and the fixed banks 30/31 (--all keeps every bank).
Caveat: odd-window mappings (SetR6/SetR7 of another bank) are not modelled.

usage: orphan_check.py [BB:AAAAxN ...] [--all]   (default: the session-32 list)
"""
import sys
import os
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom  # noqa: E402

PRG = load_rom()[1]
DEFAULT = ['17:ABDCx32', '12:8006x10', '12:8032x8', '12:804Ex8', '12:8060x9', '12:8074x22',
           '24:9EB8x11', '24:9EFBx11', '25:B87Fx8', '25:B941x8', '26:8015x9', '26:8038x7',
           '26:83B7x10', '26:83D4x22', '19:BAFBx11', '17:AC99x8', '17:B1A1x8', '18:9664x7',
           '15:B708x10']
ABS_OPS = {0xAD, 0xBD, 0xB9, 0xAE, 0xBE, 0xAC, 0xBC, 0x6D, 0x7D, 0x79, 0xED, 0xFD, 0xF9, 0x2D, 0x3D, 0x39,
           0x0D, 0x1D, 0x19, 0x4D, 0x5D, 0x59, 0xCD, 0xDD, 0xD9, 0x20, 0x4C, 0x6C}
IMM = {0xA9, 0xA2, 0xA0}


def bank_off(bank, addr):
    return bank * 0x2000 + (addr & 0x1FFF)


def where(off):
    b = off // 0x2000
    base = 0xC000 if b == 30 else 0xE000 if b == 31 else 0xA000 if b % 2 else 0x8000
    return f'{b:02d}:{base + off % 0x2000:04X}'


def scan(addr, span):
    hits = []
    lo, hi = addr & 0xFF, addr >> 8
    for o in range(len(PRG) - 2):
        b = PRG[o]
        if b in ABS_OPS:
            v = PRG[o + 1] | PRG[o + 2] << 8
            if addr <= v < addr + span:
                hits.append(('abs', o, v))
        if b in IMM and PRG[o + 1] == lo:
            for d in range(2, 17):
                p = o + d
                if p + 1 < len(PRG) and PRG[p] in IMM and PRG[p + 1] == hi:
                    hits.append(('imm', o, addr))
                    break
        if PRG[o] == lo and PRG[o + 1] == hi:
            hits.append(('word', o, addr))
    return hits


def main(argv):
    runs = [a for a in argv if not a.startswith('--')] or DEFAULT
    for r in runs:
        spec, _, n = r.replace(' ', '').partition('x')
        bank, addr = spec.split(':')
        bank, addr, n = int(bank), int(addr, 16), int(n or 8)
        o = bank_off(bank, addr)
        words = [PRG[o + 2 * i] | PRG[o + 2 * i + 1] << 8 for i in range(n)]
        print(f'== {bank:02d}:{addr:04X} x{n}: ' + ' '.join(f'{w:04X}' for w in words[:12]) + (' ...' if n > 12 else ''))
        hs = scan(addr, 2 * n)
        # word hits inside the run itself / self matches are noise
        hs = [h for h in hs if not (h[0] == 'word' and o <= h[1] < o + 2 * n)]
        if '--all' not in sys.argv:  # only banks that can see the run: its 16 KB pair + fixed 30/31
            hs = [h for h in hs if h[1] // 0x2000 in (bank & ~1, bank | 1, 30, 31)]
        for k, off, v in hs[:20]:
            print(f'   {k:4} at {where(off)} (file {off + 0x10:05X}) -> ${v:04X}')
        if not hs:
            print('   no abs / imm-pair / word reference')


if __name__ == '__main__':
    main(sys.argv[1:])
