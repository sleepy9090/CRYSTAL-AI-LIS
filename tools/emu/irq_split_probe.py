#!/usr/bin/env python3
"""(line, dot) of every IRQ handler entry and of its PPU / MMC3 IRQ writes (session 115, message window splits).

usage (repo root):
  python3 -m tools.emu.irq_split_probe STATE [--frames N]

Logs entries of IrqHandlerTable 31:F433 handlers 0-3 (F455 F488 F4E5 F54B) and every write to PPUADDR,
PPUSCROLL, MMC3 $C000 (latch) / $C001 (reload) / $E000 / $E001 while N frames run, plus the PPU v register
right after each second PPUADDR write. Compare with the port's split lines (web/src/game/mainloop.js chainSplit).
"""
import argparse
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if ROOT not in sys.path:
    sys.path.append(ROOT)

from tools.emu.nes import NES      # noqa: E402
from tools.emu import state        # noqa: E402

HANDLERS = {0xF455: 'H0 wrap', 0xF488: 'H1 hud', 0xF4E5: 'H2 msgtop', 0xF54B: 'H3 msgend'}
WRITES = {0x2005: 'PPUSCROLL', 0x2006: 'PPUADDR', 0xC000: 'IRQLATCH', 0xC001: 'IRQRELOAD',
          0xE000: 'IRQOFF', 0xE001: 'IRQON'}


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('state')
    ap.add_argument('--frames', type=int, default=2)
    a = ap.parse_args(argv)
    nes = NES()
    state.load(nes, a.state)
    log = []

    def where():
        nes.ppu.sync()
        return nes.ppu.frame_no, nes.ppu.line, nes.ppu.dot

    for addr, name in HANDLERS.items():
        nes.on_exec(31, addr, lambda n, name=name: log.append((*where(), 'ENTER ' + name, None)))
    for addr, name in WRITES.items():
        nes.on_write(addr, lambda ad, v, name=name: log.append((*where(), name, v)))
    nes.run_frames(a.frames)
    for f, line, dot, what, v in log:
        print(f'f{f} line {line:3d} dot {dot:3d} {what:14s}' + ('' if v is None else f' ${v:02X}'))


if __name__ == '__main__':
    main()
