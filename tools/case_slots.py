#!/usr/bin/env python3
"""Show one routine_capture case (s100): sp, regs and the active object slots with their phase.

    python3 tools/case_slots.py web/test/obj_frames/fuzz_update.json ObjUpdateAll#184
"""
import json
import sys


def main():
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    j = json.load(open(sys.argv[1]))
    c = next(c for c in j['cases'] if c['name'] == sys.argv[2])
    ram = bytes.fromhex(c['ram'])
    print(f"sp ${c['sp']:02X} regs {c['regs']} ret {c['ret']}")
    for s in range(0x20):
        if ram[0x04A0 + s]:
            print(f"slot ${s:02X} active ${ram[0x04A0 + s]:02X} phase ${ram[0x0600 + s]:02X} "
                  f"freeze ${ram[0x05A0 + s]:02X} flags2 ${ram[0x0340 + s]:02X}")
    stack = [f'{a:04X}={v:02X}' for a, v in c['after'] if 0x100 <= a < 0x200]
    print('stack writes:', ' '.join(stack))


if __name__ == '__main__':
    main()
