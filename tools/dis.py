#!/usr/bin/env python3
"""Banked 6502 disassembler for the MMC3 ROM.

usage: dis.py <bank> <addr> [nbytes] [--stop]
  bank   8KB PRG bank (0-31); 30/31 are fixed at $C000/$E000
  addr   CPU address (hex, e.g. F2A4 or 0xF2A4)
  nbytes number of bytes to disassemble (default 0x80)
  --stop stop at first RTS/RTI/JMP
Labels from labels.csv are shown inline; hardware registers are named automatically.
"""
import sys
from instruction_set import decode, fmt_operand, HW_REGS, TERMINATORS
from nesrom import load_rom, load_labels, lookup, bank_file_off


def main(argv):
    args = [a for a in argv if not a.startswith('--')]
    stop = '--stop' in argv
    if len(args) < 2:
        print(__doc__); return 1
    bank = int(args[0], 0)
    addr = int(args[1], 16)
    n = int(args[2], 0) if len(args) > 2 else 0x80
    rom, prg, _ = load_rom()
    labels = load_labels()
    win_end = (addr & 0xE000) + 0x2000
    pc = addr
    while pc < min(addr + n, win_end):
        off = bank_file_off(bank, pc)
        lab = lookup(labels, bank, pc)
        if lab and lab[0]:
            print(f'\n{lab[0]}:' + (f'   ; {lab[1]}' if lab[1] else ''))
        sz, mn, md, val, tgt = decode(rom, off, pc)
        if (pc & 0x1FFF) + sz > 0x2000:
            sz, mn, md = 1, '.db', 'imp'
            val = rom[off]
        raw = rom[off:off + sz].hex(' ')
        name = None
        cmt = ''
        if tgt is not None:
            if tgt in HW_REGS and md in ('abs', 'abx', 'aby'):
                name = HW_REGS[tgt]
            tl = lookup(labels, bank, tgt)
            if tl and tl[0]:
                name = tl[0]
        if mn == '.db':
            text = f'.db ${val:02X}'
        else:
            text = f'{mn} {fmt_operand(md, val, name)}'.rstrip()
        if lab and lab[1] and not lab[0]:
            cmt = '; ' + lab[1]
        print(f'  {bank:02d}:{pc:04X}  {off:05X}  {raw:<9} {text:<28}{cmt}')
        pc += sz
        if stop and mn in TERMINATORS:
            break
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
