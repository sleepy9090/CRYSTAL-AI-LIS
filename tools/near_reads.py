#!/usr/bin/env python3
"""Find absolute,X / absolute,Y instructions whose base address lies within
`span` bytes below (or equal to) a target, i.e. instructions that can reach
the target via an index.  Catches table-style reads that xref.py misses.

usage: near_reads.py <addr> [span=16] [--all-ops]
  default: only loads/compares/arith (reads); --all-ops includes stores/RMW.
Reports BB:AAAA, base, implied index (target-base), and the instruction.
Not sync-checked: verify hits with dis.py.
"""
import sys
from instruction_set import decode, fmt_operand
from nesrom import load_rom, bank_cpu_addr, HDR, BANK_SIZE

# abs,X / abs,Y opcodes that read memory
READ_OPS = {0xBD, 0xB9, 0xBE, 0xBC, 0x7D, 0x79, 0xFD, 0xF9, 0xDD, 0xD9,
            0x3D, 0x39, 0x1D, 0x19, 0x5D, 0x59}
WRITE_OPS = {0x9D, 0x99, 0xFE, 0xDE, 0x1E, 0x3E, 0x5E, 0x7E}


def main(argv):
    if not argv:
        print(__doc__); return 1
    tgt = int(argv[0].replace('$', ''), 16)
    span = int(argv[1], 0) if len(argv) > 1 and not argv[1].startswith('--') else 16
    ops = READ_OPS | (WRITE_OPS if '--all-ops' in argv else set())
    rom, prg, _ = load_rom()
    hits = 0
    for p in range(len(prg) - 2):
        op = prg[p]
        if op not in ops:
            continue
        bank = p // BANK_SIZE
        if (p + 2) // BANK_SIZE != bank:
            continue
        base = prg[p + 1] | (prg[p + 2] << 8)
        if not (tgt - span < base <= tgt):
            continue
        addr = bank_cpu_addr(bank, p)
        sz, mn, md, val, _t = decode(rom, HDR + p, addr)
        print(f'{bank:02d}:{addr:04X}  base ${base:04X} idx+{tgt-base:<3d} {mn} {fmt_operand(md, val, None)}')
        hits += 1
    print(f'-- {hits} hit(s)')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
