"""Per-scanline PPU snapshot of one tools.emu frame, for checking web/src/nes/ppu.js (Ph7 web 2/8).

    python3 -m tools.emu.ppu_lines STATE [--frames N] [-o OUT.json]
    python3 -m tools.emu.ppu_lines --all          # every tools/emu/states/*.state -> web/test/ppu_frames/

Loads a save state, runs N frames (default 1) so the PPU sits at the start of line 0, then runs one
more frame and records:
  frame start : ciram (2KB), palette RAM (32), OAM (256), mirroring
  each line 0-239 at dot 0: v, fine x, PPUCTRL, PPUMASK, the eight 1KB CHR banks
  frame end   : the 256x240 framebuffer of NES colour indices
Binary fields are hex strings. At dot 0 of a line the PPU has already prefetched the line's first
two tiles (dots 328/336 of the previous line), so v's coarse X is the line's start column + 2.
web/test/ppu_check.mjs renders each line from these values with ppu.js and diffs the framebuffer.
"""
import argparse
import glob
import json
import os
import sys

from tools.emu.nes import NES
from tools.emu import state

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def snapshot(state_path, frames=1):
    nes = NES()
    state.load(nes, state_path)
    ppu, m = nes.ppu, nes.mapper
    # mid-line writes: PPU registers / MMC3 bank registers written while a visible line (or the
    # pre-render fetches for line 0) is being drawn. ppu.js applies changes between lines only,
    # so ppu_check.mjs reports such lines separately instead of failing them.
    writes = []
    orig_wr, orig_mw = ppu.write_reg, m.write

    def wr(r, v):
        ppu.sync()
        writes.append((ppu.frame_no, ppu.line, ppu.dot, 'ppu', 0x2000 + r, v))
        orig_wr(r, v)

    def mw(a, v):
        if a < 0xA000:
            ppu.sync()
            writes.append((ppu.frame_no, ppu.line, ppu.dot, 'mmc3', a, v))
        orig_mw(a, v)

    ppu.write_reg, m.write = wr, mw
    nes.run_frames(frames)
    out = {
        'state': os.path.relpath(state_path, ROOT),
        'frame': ppu.frame_no,
        'ciram': bytes(ppu.ciram).hex(),
        'pal': bytes(ppu.pal).hex(),
        'oam': bytes(ppu.oam).hex(),
        'mirroring': m.mirroring,
        'lines': [],
    }
    lines = out['lines']
    start_frame = ppu.frame_no
    orig = ppu._next_line

    def rec():
        orig()
        if ppu.frame_no == start_frame and ppu.line < 240:
            lines.append([ppu.v, ppu.x, ppu.ctrl, ppu.mask, list(m.chr_banks)])

    # line 0 has already started: record it from the current registers
    lines.append([ppu.v, ppu.x, ppu.ctrl, ppu.mask, list(m.chr_banks)])
    ppu._next_line = rec
    nes.run_frame()
    ppu._next_line = orig
    ppu.write_reg, m.write = orig_wr, orig_mw
    midline = set()
    for fr, line, dot, kind, a, v in writes:
        if fr == start_frame - 1 and line == 261 and dot >= 257:
            midline.add(0)                         # after the pre-render copies / line 0 prefetch
        elif fr != start_frame:
            continue
        elif line < 240 and 1 <= dot <= 256:
            midline.add(line)                      # during the visible dots of this line
        elif line < 239 and dot >= 320:
            midline.add(line + 1)                  # around the next line's 2-tile prefetch
    out['writes'] = [list(w[1:]) for w in writes
                     if (w[0] == start_frame and w[1] < 240) or (w[0] == start_frame - 1 and w[1] == 261)]
    out['midline'] = sorted(midline)
    out['fb'] = bytes(ppu.fb).hex()
    assert len(lines) == 240, len(lines)
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('state', nargs='?')
    ap.add_argument('--frames', type=int, default=1)
    ap.add_argument('-o', '--out')
    ap.add_argument('--all', action='store_true')
    a = ap.parse_args()
    if a.all:
        paths = sorted(p for p in glob.glob(os.path.join(ROOT, 'tools/emu/states/*.state'))
                       if not os.path.basename(p).startswith('tmp_'))
        odir = os.path.join(ROOT, 'web/test/ppu_frames')
        os.makedirs(odir, exist_ok=True)
        for p in paths:
            o = os.path.join(odir, os.path.basename(p).replace('.state', '.json'))
            with open(o, 'w') as f:
                json.dump(snapshot(p, a.frames), f)
            print('wrote', os.path.relpath(o, ROOT))
        return
    if not a.state:
        ap.error('STATE or --all')
    d = snapshot(a.state, a.frames)
    if a.out:
        with open(a.out, 'w') as f:
            json.dump(d, f)
    else:
        json.dump(d, sys.stdout)


if __name__ == '__main__':
    main()
