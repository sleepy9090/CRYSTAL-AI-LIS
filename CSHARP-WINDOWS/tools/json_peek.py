#!/usr/bin/env python3
"""Print a JSON file's structure: top-level keys / one path's value (truncated).

usage: python3 tools/json_peek.py FILE.json [PATH]     PATH = dot-separated keys / list indices, e.g. areas.00.header
"""
import json
import sys


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    with open(sys.argv[1]) as f:
        v = json.load(f)
    if len(sys.argv) > 2:
        for k in sys.argv[2].split('.'):
            v = v[int(k)] if isinstance(v, list) else v[k]
    if isinstance(v, dict):
        print('keys:', list(v.keys())[:40], f'({len(v)})')
    s = json.dumps(v)
    print(s[:3000] + (' ...' if len(s) > 3000 else ''))


if __name__ == '__main__':
    main()
