#!/usr/bin/env python3
"""Count how many routine_capture cases changed each given byte (path coverage check, s158).

    python3 tools/case_paths.py web/test/obj_frames/fuzz_boss.json AiReward6C 0041 07E7 0102 col:04A0 col:0620

ADDR = absolute hex address; col:COL = slot column COL + regs.x. A case counts for ADDR when the byte differs
after the call. Also prints how many cases changed none of them.
"""
import json
import sys


def main():
    if len(sys.argv) < 4:
        sys.exit(__doc__)
    j = json.load(open(sys.argv[1]))
    cases = [c for c in j['cases'] if c['name'].split('#')[0] == sys.argv[2]]
    counts = {a: 0 for a in sys.argv[3:]}
    none = 0
    for c in cases:
        ram, after, x = bytes.fromhex(c['ram']), dict(c['after']), c['regs'].get('x', 0)
        hit = False
        for a in counts:
            ad = int(a[4:], 16) + x if a.startswith('col:') else int(a, 16)
            v = after.get(ad)
            if v is not None and (ad >= len(ram) or v != ram[ad]):    # s159: WRAM ($6000+) = listed in 'after'
                counts[a] += 1
                hit = True
        none += not hit
    print(f'{len(cases)} case(s) of {sys.argv[2]}')
    for a, n in counts.items():
        print(f'  {a:10} changed in {n}')
    print(f'  (none)     {none}')


if __name__ == '__main__':
    main()
