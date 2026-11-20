#!/usr/bin/env python3
"""Collapse a tools.emu --trace file (usually filtered with --trace-range to a few routine entries) into
one line per run of the same PC: `frame line  BB:AAAA label  xN` (session 108).

Use it to see where the NMIs (31:F3B6) fall inside a multi-frame routine, i.e. to find lag frames:
    python3 -m tools.emu ... --trace T.txt --trace-range F3B6-F3B6 --trace-range C831-C831 ...
    python3 tools/trace_summary.py T.txt [--frames A-B]
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nesrom import load_labels  # noqa: E402

LINE = re.compile(r'^\s*(\d+)\s+(\d+),\s*(\d+)\s+(\d\d):([0-9A-F]{4})\s')


def main(argv):
    if not argv or argv[0] in ('-h', '--help'):
        print(__doc__)
        return 0
    path, lo, hi = argv[0], 0, 1 << 30
    if '--frames' in argv:
        a, b = argv[argv.index('--frames') + 1].split('-')
        lo, hi = int(a), int(b)
    try:
        labels = load_labels()
    except Exception:  # labels are only cosmetic here
        labels = {}
    prev, count, first = None, 0, None
    out = []
    with open(path) as f:
        for ln in f:
            m = LINE.match(ln)
            if not m:
                continue
            fr, line = int(m.group(1)), int(m.group(2))
            if not lo <= fr <= hi:
                continue
            key = (m.group(4), m.group(5))
            if key == prev:
                count += 1
                continue
            if prev:
                out.append((first, prev, count))
            prev, count, first = key, 1, (fr, line)
    if prev:
        out.append((first, prev, count))
    for (fr, line), (bank, addr), n in out:
        name = ''
        for k in ((int(bank), int(addr, 16)), ('*', int(addr, 16))):
            if k in labels:
                v = labels[k]
                name = v[0] if isinstance(v, (tuple, list)) else str(v)
                break
        print(f'{fr:6d} L{line:3d}  {bank}:{addr} {name:<24s}' + (f' x{n}' if n > 1 else ''))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
