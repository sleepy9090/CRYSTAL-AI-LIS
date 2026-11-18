#!/usr/bin/env python3
"""List every `STA zp` (85 zz) / `STA abs` (8D lo hi) write to an address with the value source.

Looks back up to 3 instructions-worth of bytes for LDA #imm / ORA #imm / AND #imm / EOR #imm
immediately preceding the store (byte patterns only, so it is a heuristic).

usage: python3 tools/zp_writers.py <addr hex> [--abs]
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nesrom import load_rom, nbanks, bank_cpu_addr, load_labels, lookup

IMM = {0xA9: 'LDA', 0x09: 'ORA', 0x29: 'AND', 0x49: 'EOR'}


def main():
    a = int(sys.argv[1], 16)
    pat = bytes([0x85, a]) if a < 0x100 and '--abs' not in sys.argv else bytes([0x8D, a & 0xFF, a >> 8])
    _, prg, _ = load_rom()
    labels = load_labels()
    for b in range(nbanks(prg)):
        blk = prg[b * 0x2000:(b + 1) * 0x2000]
        i = blk.find(pat)
        while i >= 0:
            src = []
            j = i
            while j >= 2 and blk[j - 2] in IMM and len(src) < 2:
                src.insert(0, f'{IMM[blk[j-2]]} #${blk[j-1]:02X}')
                j -= 2
            if not src:
                src = [f'prev bytes {blk[max(0,i-3):i].hex(" ")}']
            ca = bank_cpu_addr(b, i)
            try:
                name = lookup(labels, b, ca)
            except Exception:
                name = None
            print(f'{b:02d}:{ca:04X}  {"; ".join(src)}' + (f'  [{name}]' if name else ''))
            i = blk.find(pat, i + 1)


if __name__ == '__main__':
    main()
