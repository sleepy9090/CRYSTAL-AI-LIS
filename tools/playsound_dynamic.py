#!/usr/bin/env python3
"""List PlaySound (30:C125) call sites whose id is NOT an `LDA #imm` right before the JSR/JMP (s127).

usage: playsound_dynamic.py [target_hex]   (default C125)
Prints each site with the 6 bytes before it, so the id's source (table load, register, RAM) can be read off.
Complements sound_ids.py (which lists only the immediate ids).
"""
import sys
from nesrom import load_rom, bank_cpu_addr

_, PRG, _ = load_rom()
tgt = int(sys.argv[1], 16) if len(sys.argv) > 1 else 0xC125
lo, hi = tgt & 0xFF, tgt >> 8

n = 0
for i in range(2, len(PRG) - 2):
    if PRG[i] in (0x20, 0x4C) and PRG[i + 1] == lo and PRG[i + 2] == hi:
        if PRG[i - 2] == 0xA9:
            continue
        bank, off = divmod(i, 0x2000)
        pre = ' '.join(f'{b:02X}' for b in PRG[max(0, i - 6):i])
        op = 'JSR' if PRG[i] == 0x20 else 'JMP'
        print(f'{bank:02d}:{bank_cpu_addr(bank, off):04X} {op}  before: {pre}')
        n += 1
print(f'-- {n} non-immediate site(s)')
