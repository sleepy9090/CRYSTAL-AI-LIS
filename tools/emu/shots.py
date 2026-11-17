#!/usr/bin/env python3
"""Dump emulator screens at given frames (emu 3/6 check tool).

usage: python3 tools/emu/shots.py OUTDIR FRAME[,FRAME...] [--press FRAME:BUTTONS[:LEN] ...]
                                  [--scale S] [--splits] [--vblank]
  --press 300:START:4  holds START on pad 1 for frames 300-303 (default length 2)
  --splits             print PPUCTRL/MASK/SCROLL/ADDR writes made during lines 0-239 of each
                       dumped frame (the IRQ split writes), as line:dot reg=value
  --vblank             like --splits but also logs writes made in vblank/pre-render lines
Writes OUTDIR/frame_NNNN.png.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from emu.nes import NES  # noqa: E402
from emu.pads import parse_buttons  # noqa: E402

REG = {0: 'CTRL', 1: 'MASK', 5: 'SCROLL', 6: 'ADDR'}


def main(argv):
    out, frames = argv[0], sorted(int(f) for f in argv[1].split(','))
    presses, scale, splits, vblank = [], 1, False, False
    i = 2
    while i < len(argv):
        if argv[i] == '--press':
            p = argv[i + 1].split(':')
            presses.append((int(p[0]), parse_buttons(p[1]), int(p[2]) if len(p) > 2 else 2))
            i += 2
        elif argv[i] == '--scale':
            scale = int(argv[i + 1])
            i += 2
        elif argv[i] == '--splits':
            splits = True
            i += 1
        elif argv[i] == '--vblank':
            splits = vblank = True
            i += 1
        else:
            raise SystemExit(f'unknown arg {argv[i]}')
    os.makedirs(out, exist_ok=True)
    nes = NES()
    if splits:
        nes.ppu.split_log = []
        nes.ppu.split_all = vblank
    for f in range(frames[-1] + 1):
        m = 0
        for start, mask, n in presses:
            if start <= f < start + n:
                m |= mask
        nes.pads.set(0, m)
        if splits:
            nes.ppu.split_log.clear()
        nes.run_frame()
        if f in frames:
            path = os.path.join(out, f'frame_{f:04d}.png')
            nes.dump_screen(path, scale)
            print(f'{path}: pc=${nes.cpu.pc:04X} state=${nes.bus.ram[0x40]:02X} '
                  f'mmc3_clocks={nes.mapper.irq_clocks} nmi={nes.ppu.nmi_count} banks={nes.mapper.prg_banks} '
                  f'chr={nes.mapper.chr_banks} ctrl=${nes.ppu.ctrl:02X} mask=${nes.ppu.mask:02X}')
            if splits:
                print('  ' + ' '.join(f'{ln}:{d} {REG[r]}={v:02X}' for _, ln, d, r, v in nes.ppu.split_log))


if __name__ == '__main__':
    main(sys.argv[1:])
