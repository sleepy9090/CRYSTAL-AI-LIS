#!/usr/bin/env python3
"""Filter web_state_compare.py / web_compare-style `shot F: N px differ, tile rows {..}` lines (session 89).

Reads the lines on stdin and prints only the shots whose differing tile rows are not all in --ignore
(tile-row list / ranges, e.g. 23-29 = the HUD under the unported IRQ split) and whose ignored-row-free
pixel count is above --min (default 0). Ends with a one-line summary.

usage: python3 tools/web_state_compare.py ... | python3 tools/shot_filter.py --ignore 23-29 [--small-row0 8]
  --small-row0 N   also ignore tile row 0 when it has <= N px (palette upload spilling into line 0 in tools.emu)
"""
import ast
import re
import sys


def parse_rows(spec):
    out = set()
    for s in filter(None, spec.split(',')):
        lo, _, hi = s.partition('-')
        out.update(range(int(lo), int(hi or lo) + 1))
    return out


def main(argv):
    ignore, small0 = set(), -1
    i = 0
    while i < len(argv):
        if argv[i] == '--ignore':
            ignore = parse_rows(argv[i + 1]); i += 2
        elif argv[i] == '--small-row0':
            small0 = int(argv[i + 1]); i += 2
        else:
            sys.exit(__doc__)
    pat = re.compile(r'^shot (\d+): (\d+) px differ(?:, tile rows (\{.*\}))?')
    n = kept = 0
    for line in sys.stdin:
        mt = pat.match(line)
        if not mt:
            continue
        n += 1
        rows = ast.literal_eval(mt.group(3)) if mt.group(3) else {}
        rest = {r: c for r, c in rows.items() if r not in ignore and not (r == 0 and c <= small0)}
        if rest:
            kept += 1
            print(f'shot {mt.group(1)}: {sum(rest.values())} px outside ignored rows: {rest}')
    print(f'{kept}/{n} shots differ outside the ignored rows')
    return 1 if kept else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
