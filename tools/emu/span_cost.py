#!/usr/bin/env python3
"""s216: CPU cycles of a ROM code span per occurrence (gap (c3), exact-cycle models of hit handlers etc.).

Starts a span when the CPU executes ENTER and ends it at the first following execution of EXIT (e.g. a
routine's first instruction and the instruction after its JSR). Several ENTER EXIT pairs are measured
independently. The cycles include any interrupt that lands inside the span: the line:dot at entry and exit
are printed so such spans can be told apart (the H0 / HUD IRQs at L142 / L189 split lines, NMI at L241).

usage: python3 -m tools.emu.span_cost STATE FRAMES ENTER EXIT [ENTER EXIT ..] [--from F] --keys SPEC ...
  ENTER / EXIT = BB:AAAA (8KB bank : address). --keys must be last.
"""
import sys

from tools.emu.nes import NES
from tools.emu import state, debug


def addr(s):
    b, a = s.split(':')
    return int(b), int(a, 16)


def main(argv):
    keys = []
    if '--keys' in argv:
        i = argv.index('--keys')
        keys, argv = argv[i + 1:], argv[:i]
    lo = 0
    if '--from' in argv:
        i = argv.index('--from')
        lo = int(argv[i + 1])
        argv = argv[:i] + argv[i + 2:]
    if len(argv) < 4 or len(argv) % 2:
        raise SystemExit(__doc__)
    nes = NES()
    state.load(nes, argv[0])
    if keys:
        nes.keys = debug.parse_keys(keys)
    frames = int(argv[1])
    names = [(argv[k], argv[k + 1]) for k in range(2, len(argv), 2)]
    cpu = nes.cpu
    open_ = {}
    out = []

    def enter(k):
        def f(n):
            open_[k] = (cpu.cycles, n.ppu.frame_no, n.ppu.line, n.ppu.dot)
        return f

    def leave(k):
        def f(n):
            if k not in open_:
                return
            c0, fr, ln, dt = open_.pop(k)
            if fr >= lo:
                out.append(f'f{fr} L{ln}.{dt} -> L{n.ppu.line}.{n.ppu.dot}  {names[k][0]}-{names[k][1]}'
                           f'  {cpu.cycles - c0}')
        return f

    for k, (e, x) in enumerate(names):
        nes.on_exec(*addr(e), enter(k))
        nes.on_exec(*addr(x), leave(k))
    nes.run_frames(frames)
    print('\n'.join(out))


if __name__ == '__main__':
    main(sys.argv[1:])
