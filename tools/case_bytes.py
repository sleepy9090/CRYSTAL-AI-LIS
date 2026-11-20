#!/usr/bin/env python3
"""Print RAM bytes of one routine_capture case before / after (s104).

    python3 tools/case_bytes.py web/test/obj_frames/fuzz_boss.json 'BossAiE6#195' 0012 0112 col:0360 col:0620

ADDR = absolute hex address; col:COL = slot column COL + regs.x. Prints before -> after ('-' = unchanged).
"""
import json
import sys


def main():
    if len(sys.argv) < 4:
        sys.exit(__doc__)
    j = json.load(open(sys.argv[1]))
    c = next(c for c in j['cases'] if c['name'] == sys.argv[2])
    ram, after, x = bytes.fromhex(c['ram']), dict(c['after']), c['regs'].get('x', 0)
    print(f"regs {c['regs']} ret {c['ret']}")
    for a in sys.argv[3:]:
        ad = int(a[4:], 16) + x if a.startswith('col:') else int(a, 16)
        v = after.get(ad)
        print(f'${ad:04X} {ram[ad]:02X} -> ' + ('-' if v is None else f'{v:02X}'))


if __name__ == '__main__':
    main()
