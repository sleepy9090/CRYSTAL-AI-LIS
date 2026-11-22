#!/usr/bin/env python3
"""tools.emu `--watch 0043` output -> web-port --keys items (session 191).

usage: python3 tools/pad_watch_keys.py WATCH.txt|- [--last F]   (- = stdin: tools.emu ... --watch 0043 | ...)

Each `write $0043=VV frame=F line=L` is a pad read of the main-loop pass of frame F (F+1 when L >= 241,
the read sits in the next frame's pass). The port's button byte for a pass is the held value until the
next write, so every non-zero value becomes FRAME:BUTTONS:LEN with LEN = next pass frame - this one
(the last runs to --last, inclusive). Values are ROM-order bytes (A=b7 .. RIGHT=b0).
"""
import re
import sys

NAMES = ['RIGHT', 'LEFT', 'DOWN', 'UP', 'START', 'SELECT', 'B', 'A']   # b0..b7


def buttons(v):
    return '+'.join(NAMES[b] for b in range(7, -1, -1) if v >> b & 1)


def main(argv):
    path, last = argv[0], None
    if '--last' in argv:
        last = int(argv[argv.index('--last') + 1])
    ev = []
    text = sys.stdin.read() if path == '-' else open(path).read()
    for m in re.finditer(r'write \$0043=([0-9A-F]{2})\s+frame=(\d+) line=(\d+)', text):
        v, f, line = int(m[1], 16), int(m[2]), int(m[3])
        ev.append((f + (1 if line >= 241 else 0), v))
    out = []
    for i, (f, v) in enumerate(ev):
        if not v:
            continue
        end = ev[i + 1][0] if i + 1 < len(ev) else (last + 1 if last is not None else f + 1)
        out.append(f'{f}:{buttons(v)}:{end - f}')
    print(' '.join(out))


if __name__ == '__main__':
    main(sys.argv[1:])
