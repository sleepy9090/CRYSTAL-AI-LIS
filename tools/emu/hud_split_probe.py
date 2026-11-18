#!/usr/bin/env python3
"""Ph6 triage (g): exact PPU line/dot of every write IRQ handler 1 (`31:F488` HUD split) makes.

usage (repo root):
  python3 -m tools.emu.hud_split_probe [--state FILE] [--frames N]

Loads a gameplay save state, runs N frames and prints, per frame, the (line, dot) at which the
handler starts and of its PPUMASK / PPUSTATUS / PPUADDR / PPUSCROLL / MMC3 R0 / R1 writes, plus
the screen x the write lands on (dot - 1 on lines 0-239; dot > 256 = in hblank).
"""
import argparse
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if ROOT not in sys.path:
    sys.path.append(ROOT)

from tools.emu.nes import NES      # noqa: E402
from tools.emu import state        # noqa: E402

NAMES = {0x2001: 'PPUMASK', 0x2005: 'PPUSCROLL', 0x2006: 'PPUADDR', 0x8000: 'BANKSEL',
         0x8001: 'BANKDATA', 0xE000: 'IRQOFF'}


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--state', default=os.path.join(ROOT, 'tools', 'emu', 'states', 'area1_enter.state'))
    ap.add_argument('--frames', type=int, default=3)
    ap.add_argument('--ref', help='FCEUX PNG of a gameplay frame: diff lines 190-192 against the last frame')
    a = ap.parse_args(argv)
    nes = NES()
    state.load(nes, a.state)
    log = []
    inside = [False]

    def where():
        nes.ppu.sync()
        return nes.ppu.frame_no, nes.ppu.line, nes.ppu.dot

    def enter(n):
        inside[0] = True
        log.append((*where(), 'ENTER F488', None))

    def leave(n):
        inside[0] = False
    nes.on_exec(31, 0xF488, enter)
    nes.on_exec(31, 0xF4CF, leave)          # JSR SoundUpdate: the split itself is finished

    def mk(addr):
        def w(ad, v):
            if inside[0]:
                log.append((*where(), NAMES[addr], v))
        return w
    for ad in NAMES:
        nes.on_write(ad, mk(ad))
    nes.run_frames(a.frames)
    print(f'{a.state}: {a.frames} frames, writes inside IrqHandler1Hud (31:F488-F4CF)')
    for f, line, dot, what, v in log:
        x = f'x={dot - 1}' if 1 <= dot <= 256 else 'hblank'
        print(f'  f{f} line {line:3d} dot {dot:3d} ({x:7s}) {what:10s}' + ('' if v is None else f' ${v:02X}'))
    fb = nes.ppu.fb
    for line in (190, 191, 192):
        row = fb[line * 256:(line + 1) * 256]
        runs, s = [], 0
        for x in range(1, 257):
            if x == 256 or row[x] != row[s]:
                runs.append(f'{s}-{x - 1}:{row[s]:02X}')
                s = x
        print(f'  fb line {line}: ' + ' '.join(runs[:40]) + (' ...' if len(runs) > 40 else ''))
    if a.ref:
        from tools.render_compare import read_png
        w, h, px = read_png(a.ref)
        top = 8 if h == 224 else 0
        rgb = nes.ppu.to_rgb()
        emu = [tuple(rgb[i:i + 3]) for i in range(0, len(rgb), 3)]
        cmap = {}
        for y in range(top, top + h):          # colour map from every row except 191
            if y == 191:
                continue
            for x in range(256):
                d = cmap.setdefault(emu[y * 256 + x], {})
                r = px[(y - top) * 256 + x]
                d[r] = d.get(r, 0) + 1
        cmap = {e: max(d, key=d.get) for e, d in cmap.items()}
        for line in (190, 191, 192):
            bad = [x for x in range(256) if cmap.get(emu[line * 256 + x]) != px[(line - top) * 256 + x]]
            print(f'  vs {os.path.basename(a.ref)} line {line}: {len(bad)} px differ'
                  + (f' (x {bad[0]}-{bad[-1]})' if bad else ''))
            if bad and line == 191:
                r = lambda yy, xx: px[(yy - top) * 256 + xx]   # noqa: E731
                above = sum(r(191, x) == r(190, x) for x in bad)
                below = sum(r(191, x) == r(192, x) for x in bad)
                print(f'    ref line 191 at those x: = ref line 190 (pre-split) {above}, '
                      f'= ref line 192 (HUD) {below}; ref line 191 x 40-160 colours: '
                      + ''.join('%X' % (sorted(set(px)).index(r(191, x)) % 16) for x in range(40, 160)))
                print('    emu line 191 x 40-160 colours: ' + ''.join(
                    '%X' % (sorted(set(px)).index(cmap[emu[191 * 256 + x]]) % 16) for x in range(40, 160)))
    return 0


if __name__ == '__main__':
    sys.exit(main())
