#!/usr/bin/env python3
"""s191: main-loop pass timing from a save state with keys: per frame the line of the main-loop top 30:C904,
ObjUpdateAll 30:CCCC entry / ObjCollideAll 26:8F6E entry, the NMI's frame_ready $09, and the active-slot count.
A frame with no main-loop top is a lag frame (the pass before it ran past the NMI).

usage: python3 -m tools.emu.pass_probe STATE FRAMES [--from F] [--to F] [--slots] --keys SPEC ...
  --slots also lists the line each active slot's AI dispatch 30:CCFA starts (X = slot) and its AI id."""
import sys

from tools.emu.nes import NES
from tools.emu import state, debug


def active(ram):
    return sum(1 for s in range(0x1F) if ram[0x04A0 + s])


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    nes = NES()
    state.load(nes, argv[0])
    frames = int(argv[1])
    lo = int(argv[argv.index('--from') + 1]) if '--from' in argv else 0
    hi = int(argv[argv.index('--to') + 1]) if '--to' in argv else 1 << 30
    if '--keys' in argv:
        nes.keys = debug.parse_keys(argv[argv.index('--keys') + 1:])
    log = []

    def at(tag):
        def f(n):
            r = n.bus.ram
            fr, ln = n.ppu.frame_no, n.ppu.line
            if not lo <= fr <= hi:
                return
            if tag == 'top':
                log.append(f'f{fr} L{ln:3}: main-loop top  active {active(r)}')
            elif tag == 'nmi':
                log.append(f'f{fr} L{ln:3}:   NMI frame_ready {r[0x09]:02X}')
            else:
                log.append(f'f{fr} L{ln:3}:   {tag}')
        return f
    if '--slots' in argv:
        def slot(n):
            fr, x = n.ppu.frame_no, n.cpu.x
            if lo <= fr <= hi and n.bus.ram[0x04A0 + x]:
                log.append(f'f{fr} L{n.ppu.line:3}:     slot {x:02X} AI {n.bus.ram[0x04A0 + x] & 0x7F:02X}')
        nes.on_exec(30, 0xCCFA, slot)   # ObjUpdateAll's per-slot AI dispatch (X = slot)
    nes.on_exec(30, 0xC904, at('top'))
    nes.on_exec(30, 0xCCCC, at('ObjUpdateAll'))
    nes.on_exec(31, 0xE8F6, at('CameraTick'))
    nes.on_exec(28, 0x800E, at('ObjSpriteDraw'))
    nes.on_exec(26, 0x8F6E, at('ObjCollideAll'))
    nes.on_exec(30, 0xC169, at('WaitFrame'))
    nes.on_exec(31, 0xF3B6, at('nmi'))
    nes.run_frames(frames)
    print('\n'.join(log))


if __name__ == '__main__':
    main(sys.argv[1:])
