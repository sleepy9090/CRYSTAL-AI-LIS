#!/usr/bin/env python3
"""Path coverage of routine_capture cases (s81): per routine, how many cases changed a given slot column to a value.

    python3 tools/obj_case_stats.py web/test/obj_frames/fuzz_walk.json [--col 0620=60] [--col 0640] ...

--col COL[=VAL]: count cases whose `after` writes slot column COL (+ regs.x) [with value VAL] (hex).
--abs ADDR[=VAL]: the same for an absolute RAM / WRAM address (s93).
Always prints: cases, popped, cases that spawned an object (obj_active of another slot $0D-$1E went 0 -> != 0).
"""
import json
import sys

# no argparse: it imports inspect -> stdlib dis, which tools/dis.py shadows when run as tools/obj_case_stats.py


def main():
    argv = sys.argv[1:]
    if not argv:
        sys.exit(__doc__)
    file, cols = argv[0], []
    for i, v in enumerate(argv[:-1]):
        if v in ('--col', '--abs'):             # s93: --abs ADDR[=VAL] = an absolute address (no + regs.x)
            k, _, val = argv[i + 1].partition('=')
            cols.append((argv[i + 1], int(k, 16), int(val, 16) if val else None, v == '--abs'))
    j = json.load(open(file))
    per = {}
    for c in j['cases']:
        p = per.setdefault(c['routine'], {'cases': 0, 'popped': 0, 'spawned': 0, **{n: 0 for n, *_ in cols}})
        p['cases'] += 1
        p['popped'] += c['ret'] == 'popped'
        ram = bytes.fromhex(c['ram'])
        after = dict((ad, v) for ad, v in c['after'])
        x = c['regs'].get('x', 0)
        if any(ram[0x04A0 + s] == 0 and after.get(0x04A0 + s) for s in range(0x0D, 0x1F) if s != x):
            p['spawned'] += 1
        for n, col, val, absolute in cols:
            ad = col if absolute else col + x
            if ad in after and (val is None or after[ad] == val):
                p[n] += 1
    for r, p in per.items():
        print(f'{r:20} ' + ' '.join(f'{k}={v}' for k, v in p.items()))


if __name__ == '__main__':
    main()
