#!/usr/bin/env python3
"""s161: AI $08 (AiScreenRedraw08 27:B7F6) CPU-overrun probe from a saved state: per AI-$08 call the frame /
scanline, its act_timer, the active-slot count (slots 0-$1E, as ai_effect.js countActive) before and after the
call (a rise = explodeStep spawned), the line where ObjCollideAll 26:8F6E returns, and the main-loop top 30:C904
hits (a frame without one = lag frame).

usage: python3 -m tools.emu.lag08_probe STATE FRAMES [--from F]"""
import sys

from tools.emu.nes import NES
from tools.emu import state


def load_nes(path):
    nes = NES()
    state.load(nes, path)
    return nes


def active(ram):
    return sum(1 for s in range(0x1F) if ram[0x04A0 + s])


def main():
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)
    nes = load_nes(sys.argv[1])
    frames = int(sys.argv[2])
    lo = int(sys.argv[sys.argv.index('--from') + 1]) if '--from' in sys.argv else 0
    log = []
    cur = {}

    def at(tag):
        def f(n):
            r = n.bus.ram
            fr, ln = n.ppu.frame_no, n.ppu.line
            if fr < lo:
                return
            if tag == 'top':
                log.append(f'f{fr} L{ln:3}: main-loop top  active {active(r)}')
            elif tag == 'ai08':
                cur['n'] = active(r)
                log.append(f'f{fr} L{ln:3}: AI08 slot {n.cpu.x:02X} act_timer {r[0x0640 + n.cpu.x]:02X} active {active(r)}')
            elif tag == 'ai08x':
                log.append(f'f{fr} L{ln:3}:   after explodeStep active {active(r)}'
                           f'{"  (spawned)" if active(r) > cur.get("n", 99) else ""}')
            elif tag == 'nmi':
                log.append(f'f{fr} L{ln:3}:   NMI frame_ready {r[0x09]:02X}')
        return f
    nes.on_exec(30, 0xC904, at('top'))
    nes.on_exec(27, 0xB7F6, at('ai08'))
    nes.on_exec(27, 0xB7F9, at('ai08x'))
    nes.on_exec(31, 0xF3B6, at('nmi'))
    nes.run_frames(frames)
    print('\n'.join(log))


if __name__ == '__main__':
    main()
