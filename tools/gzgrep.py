#!/usr/bin/env python3
"""Regex search inside a (optionally gzipped) text file.

usage: gzgrep.py FILE REGEX [--max N]
Prints 'lineno: text' for matching lines (case-insensitive). Stdlib only.
"""
import gzip
import re
import sys


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    path, pat = argv[0], re.compile(argv[1], re.I)
    mx = int(argv[argv.index('--max') + 1]) if '--max' in argv else 200
    op = gzip.open if path.endswith('.gz') else open
    n = 0
    with op(path, 'rt', errors='replace') as f:
        for i, ln in enumerate(f, 1):
            if pat.search(ln):
                print(f'{i}: {ln.rstrip()}')
                n += 1
                if n >= mx:
                    break
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
