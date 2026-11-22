#!/usr/bin/env python3
"""Expand a repeated key pattern into tools.emu --keys items (session 191).

usage: python3 tools/keyseq.py [--shift N] ITEM ...
  ITEM = FRAME:BUTTONS:LEN                  one press
       | FRAME:BUTTONS:LEN,+OFF:BUTTONS:LEN..*COUNT/STEP
         a group of presses at FRAME (+OFF relative to FRAME) repeated COUNT times every STEP frames
--shift N adds N to every frame (e.g. -1: group_probe.py rows -> tools.emu --keys at L261 pad reads).
Prints the items space-separated, e.g. $(python3 tools/keyseq.py 2505:DOWN:2 '2569:DOWN:6,+6:B:4*26/16').
"""
import sys


def expand(item, shift=0):
    grp, _, rep = item.partition('*')
    count, step = (rep.split('/') + ['0'])[:2] if rep else ('1', '0')
    parts = grp.split(',')
    f0, rest = parts[0].split(':', 1)
    base = int(f0)
    presses = [(0, rest)]
    for p in parts[1:]:
        off, rest = p.split(':', 1)
        presses.append((int(off.lstrip('+')), rest))
    out = []
    for i in range(int(count)):
        for off, rest in presses:
            out.append(f'{base + shift + off + int(step) * i}:{rest}')
    return out


def main(argv):
    shift, items = 0, []
    i = 0
    while i < len(argv):
        if argv[i] == '--shift':
            shift = int(argv[i + 1])
            i += 2
            continue
        items.append(argv[i])
        i += 1
    print(' '.join(k for it in items for k in expand(it, shift)))


if __name__ == '__main__':
    main(sys.argv[1:])
