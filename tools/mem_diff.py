#!/usr/bin/env python3
"""Diff two memory images over a CPU address range (session 98).

usage: mem_diff.py A B LO-HI [--key K]
  A / B: a raw dump (run.mjs / tools.emu --dump-ram = $0000, --dump-wram = $6000) or a
         tools/emu/web_state.py JSON (its `ram` / `wram` hex), or `zero`. LO-HI: hex CPU addresses.
  --key K: take JSON field K instead (e.g. `ciram` vs run.mjs --dump-ciram, offsets $0000-$07FF).
Prints `$ADDR: aa -> bb` for every differing byte in the range.
"""
import json
import sys


def image(path, base):
    if path == 'zero':                       # all-zero image: lists the non-zero bytes of the other side
        return bytes(0x2000)
    if path.endswith('.json'):
        js = json.load(open(path))
        return bytes.fromhex(js[KEY or ('wram' if base == 0x6000 else 'ram')])
    return open(path, 'rb').read()


KEY = None


def main():
    global KEY
    if '--key' in sys.argv:                   # s108: other JSON images, e.g. --key ciram (CIRAM = $0000-$07FF)
        i = sys.argv.index('--key')
        KEY = sys.argv[i + 1]
        del sys.argv[i:i + 2]
    if len(sys.argv) != 4:
        print(__doc__); sys.exit(1)
    lo, _, hi = sys.argv[3].partition('-')
    lo, hi = int(lo, 16), int(hi or lo, 16)
    base = 0x6000 if lo >= 0x6000 else 0
    a, b = image(sys.argv[1], base), image(sys.argv[2], base)
    n = 0
    for ad in range(lo, hi + 1):
        x, y = a[ad - base], b[ad - base]
        if x != y:
            print(f'${ad:04X}: {x:02X} -> {y:02X}')
            n += 1
    print(f'{n} bytes differ in ${lo:04X}-${hi:04X}')


if __name__ == '__main__':
    main()
