#!/usr/bin/env python3
"""json_peek.py <file.json> [path] [--depth N] [--items N]

Print the shape of a JSON file (keys, types, list lengths, short sample values)
so data formats can be checked without dumping megabytes.  `path` is a dotted
path into the document (list indices are numbers), e.g. `music.0.channels.0`.
"""
import json
import sys


WIDTH = 70


def short(v, n=None):
    n = n or WIDTH
    s = json.dumps(v)
    return s if len(s) <= n else s[:n] + '...'


def shape(v, depth, items, ind=''):
    if isinstance(v, dict):
        print(f'{ind}{{dict {len(v)} keys}}')
        if depth <= 0:
            return
        for i, (k, x) in enumerate(v.items()):
            if i >= items:
                print(f'{ind}  ... {len(v) - items} more keys')
                break
            if isinstance(x, (dict, list)):
                print(f'{ind}  {k}:')
                shape(x, depth - 1, items, ind + '    ')
            else:
                print(f'{ind}  {k}: {short(x)}')
    elif isinstance(v, list):
        print(f'{ind}[list len {len(v)}]')
        if depth <= 0 or not v:
            return
        if all(not isinstance(x, (dict, list)) for x in v):
            print(f'{ind}  {short(v)}')
            return
        for i, x in enumerate(v[:min(items, 2)]):
            print(f'{ind}  [{i}]:')
            shape(x, depth - 1, items, ind + '    ')
    else:
        print(f'{ind}{short(v)}')


def main():
    args = sys.argv[1:]
    global WIDTH
    depth, items = 3, 12
    if '--width' in args:
        i = args.index('--width'); WIDTH = int(args[i + 1]); del args[i:i + 2]
    if '--depth' in args:
        i = args.index('--depth'); depth = int(args[i + 1]); del args[i:i + 2]
    if '--items' in args:
        i = args.index('--items'); items = int(args[i + 1]); del args[i:i + 2]
    with open(args[0]) as f:
        doc = json.load(f)
    if len(args) > 1:
        for p in args[1].split('.'):
            doc = doc[int(p)] if isinstance(doc, list) else doc[p]
    shape(doc, depth, items)


if __name__ == '__main__':
    main()
