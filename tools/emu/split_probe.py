#!/usr/bin/env python3
"""PPU register writes during the visible lines (line:dot) per frame, from a saved state (session 209).

usage: python3 -m tools.emu.split_probe S.state N [--from F] [--to F] [--all]
Prints, for each frame F (ppu frame_no) in range, the $2000/$2001/$2005/$2006 writes made on
lines 0-239 as `line:dot REG=vv` (--all: also vblank / pre-render writes), plus the CPU PC of each
write's instruction when the CPU exposes it. Same log as tools/emu/shots.py --splits, but from a
.state (scenario_state / web_state exports).
"""
import argparse

from tools.emu.nes import NES
from tools.emu import state

REG = {0: 'CTRL', 1: 'MASK', 5: 'SCROLL', 6: 'ADDR'}


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('state')
    ap.add_argument('frames', type=int)
    ap.add_argument('--from', dest='lo', type=int, default=0)
    ap.add_argument('--to', dest='hi', type=int, default=1 << 30)
    ap.add_argument('--all', action='store_true')
    ap.add_argument('--hist', action='store_true',
                    help='instead of per-frame lines: histogram of the dot per (line, k-th write on it, reg)')
    ap.add_argument('--mapper', action='store_true',
                    help='also log MMC3 bank-data writes ($8001) as R<sel>; their line:dot is where the PPU '
                         'stood unsynced, = the first dot the new bank applies to in tools.emu (mapper writes '
                         'do not sync the PPU, so the switch counts from the start of the STA)')
    a = ap.parse_args(argv)
    nes = NES()
    state.load(nes, a.state)
    ppu = nes.ppu
    ppu.split_log = []
    ppu.split_all = a.all
    hist = {}
    if a.mapper:
        sel = [0]
        REG[8] = 'R'

        def w8000(ad, v):
            sel[0] = v & 7

        def w8001(ad, v):
            if sel[0] < 6 and (a.all or ppu.line < 240):
                ppu.split_log.append((ppu.frame_no, ppu.line, ppu.dot, 8, (sel[0] << 8) | v))
        nes.on_write(0x8000, w8000)
        nes.on_write(0x8001, w8001)

    def fmt(r, v):
        return f'R{v >> 8}={v & 0xFF:02X}' if r == 8 else f'{REG[r]}={v:02X}'

    def hook(n):
        log = [e for e in ppu.split_log]
        ppu.split_log.clear()
        by = {}
        for fr, ln, d, r, v in log:
            by.setdefault(fr, []).append((ln, d, r, v))
        for fr in sorted(by):
            if not (a.lo <= fr <= a.hi):
                continue
            if a.hist:
                k = {}
                for ln, d, r, v in by[fr]:
                    i = k[ln] = k.get(ln, -1) + 1
                    h = hist.setdefault((ln, i, fmt(r, v) if r == 8 else REG[r]), {})
                    h[d] = h.get(d, 0) + 1
            else:
                print(f'f{fr}: ' + ' '.join(f'{ln}:{d} {fmt(r, v)}' for ln, d, r, v in by[fr]))
    nes.on_frame(hook)
    nes.run_frames(a.frames)
    for key in sorted(hist):
        h = hist[key]
        print(f'line {key[0]} write {key[1]} {key[2]}: ' + ' '.join(f'{d}x{c}' for d, c in sorted(h.items())))


if __name__ == '__main__':
    main()
