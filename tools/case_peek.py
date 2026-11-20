#!/usr/bin/env python3
"""Print bytes of one routine_capture case: input RAM and the emulator's after-value (session 111).

usage: case_peek.py FILE.json CASE_NAME ADDR[,ADDR..]   (hex CPU addresses < $800)
"""
import json
import sys


def main(argv):
    if len(argv) != 3:
        sys.exit(__doc__)
    j = json.load(open(argv[0]))
    case = next((c for c in j['cases'] if c['name'] == argv[1]), None)
    if case is None:
        sys.exit(f'no case {argv[1]}')
    ram = bytes.fromhex(case['ram'])
    after = dict(case['after'])
    print(f"{case['name']} regs {case['regs']} ret {case['ret']}")
    for a in argv[2].split(','):
        ad = int(a, 16)
        v = ram[ad]
        out = f'  ${ad:04X}: in {v:02X}'
        if ad in after:
            out += f'  emu after {after[ad]:02X}'
        print(out)


if __name__ == '__main__':
    main(sys.argv[1:])
