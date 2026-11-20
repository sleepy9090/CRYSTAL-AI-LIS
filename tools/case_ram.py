#!/usr/bin/env python3
"""Print bytes of one routine_capture case before / after the call (session 115).

usage: case_ram.py FILE.json CASE ADDR[-ADDR] [ADDR ...]
  ADDR hex: < $800 = RAM (case `ram`), $6000-$7FFF = WRAM (wram_base + case `wram`). `after` = the value the
  emulator left (same as before when the routine did not write it).
"""
import json
import sys


def main(argv):
    if len(argv) < 3:
        sys.exit(__doc__)
    d = json.load(open(argv[0]))
    case = next(c for c in d['cases'] if c['name'] == argv[1])
    ram = bytes.fromhex(case['ram'])
    wram = bytearray(bytes.fromhex(d['wram_base']))
    for a, v in case['wram']:
        wram[a - 0x6000] = v
    after = dict(case['after'])
    print(f"{case['name']} regs {case['regs']} ret {case['ret']}")
    for spec in argv[2:]:
        lo, _, hi = spec.partition('-')
        lo = int(lo, 16)
        hi = int(hi, 16) if hi else lo
        for a in range(lo, hi + 1):
            b = ram[a] if a < 0x800 else wram[a - 0x6000]
            print(f'  ${a:04X}: {b:02X} -> {after.get(a, b):02X}')


if __name__ == '__main__':
    main(sys.argv[1:])
