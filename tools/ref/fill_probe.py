#!/usr/bin/env python3
"""Ph6 triage (b): which RAM/WRAM diffs vs the FCEUX capture are only power-on fill?

usage (repo root):
  python3 tools/ref/fill_probe.py [--ref DIR] [--out FILE] [--fill fceux|zero|ff|0xNN] [--search N]

tools.emu runs the canonical keys twice from power-on, with RAM + WRAM filled $00 and $A5.
A byte that differs between the two runs at frame N still holds (or was derived from) its
power-on value = "unwritten".  For every ref/frame_NNNN.ram:
  * the unwritten bytes' FCEUX values are tested against the candidate fills (zero, ff, fceux =
    4x$00 4x$FF) -> which pattern FCEUX used;
  * the remaining "real" diffs = written bytes where FCEUX != tools.emu (OAM page excluded),
    at the best frame offset (fewest real diffs, |d| <= --search).
Then (with --fill) a third run uses that fill, so the report also gives the full diff count
with FCEUX's pattern, i.e. what compare_ref would see with `NES(ram_fill=...)`.
"""
import argparse
import glob
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if ROOT not in sys.path:
    sys.path.append(ROOT)

from tools.emu.nes import NES, RAM_FILLS, make_fill  # noqa: E402
from tools.emu import debug                          # noqa: E402
from tools.ref.compare_ref import CANON_KEYS, load_labels, ranges  # noqa: E402


def run(frames, fill, wfill=None):
    nes = NES(ram_fill=fill, wram_fill=fill if wfill is None else wfill)
    nes.keys = debug.parse_keys([CANON_KEYS])
    snaps = {}

    def hook(n):
        f = n.ppu.frame_no - 1
        if f in frames:
            snaps[f] = (bytes(n.bus.ram), bytes(n.bus.wram))
    nes.on_frame(hook)
    nes.run_frames(max(frames) + 1)
    return snaps


def parse_fill(s):
    return int(s, 0) if s[0].isdigit() else s


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--ref', default=os.path.join(ROOT, 'ref'))
    ap.add_argument('--out', default=os.path.join(ROOT, 'docs', 'ref_fill_probe.txt'))
    ap.add_argument('--fill', default='fceux')
    ap.add_argument('--wram-fill', default='zero',
                    help='WRAM fill of the 3rd run (s73: FCEUX without a .sav leaves WRAM $00)')
    ap.add_argument('--search', type=int, default=3)
    a = ap.parse_args(argv)
    refs = {int(re.search(r'frame_(\d+)\.ram$', p).group(1)): p[:-4]
            for p in sorted(glob.glob(os.path.join(a.ref, 'frame_*.ram')))}
    if not refs:
        print('no reference capture'); return 2
    want = {n + d for n in refs for d in range(-a.search, a.search + 1)}
    s0, s1 = run(want, 0x00), run(want, 0xA5)
    fill = parse_fill(a.fill)
    sf = run(want, fill, parse_fill(a.wram_fill))
    lab = load_labels()
    cand = {k: (make_fill(k, 0x800), make_fill(k, 0x2000)) for k in RAM_FILLS}
    out = [f'fill_probe.py: unwritten = differs between fill $00 and $A5 runs; third run RAM fill={a.fill} WRAM fill={a.wram_fill}', '']
    tot_real = 0
    for n, base in sorted(refs.items()):
        rram = open(base + '.ram', 'rb').read()
        rw = open(base + '.wram', 'rb').read() if os.path.exists(base + '.wram') else None
        best = None
        for d in range(-a.search, a.search + 1):
            f = n + d
            if f not in s0:
                continue
            ram0, ram1 = s0[f][0], s1[f][0]
            real = [i for i in range(0x800) if ram0[i] == ram1[i] and rram[i] != ram0[i]
                    and not 0x200 <= i < 0x300]
            if best is None or (len(real), abs(d)) < (len(best[1]), abs(best[0])):
                best = (d, real)
        d, real = best
        f = n + d
        ram0, ram1 = s0[f][0], s1[f][0]
        unw = [i for i in range(0x800) if ram0[i] != ram1[i] and not 0x200 <= i < 0x300]
        out.append(f'== ref {n} <-> emu {f} ({d:+d}): RAM unwritten {len(unw)}, real diffs {len(real)}')
        for k, (cr, _) in cand.items():
            hit = sum(rram[i] == cr[i] for i in unw)
            out.append(f'   unwritten RAM matching fill {k:6s}: {hit}/{len(unw)}')
        miss = [i for i in unw if rram[i] != cand['fceux'][0][i]]
        if miss:
            out.append('   unwritten but != fceux fill: ' + ' '.join(
                f'${i:04X}={rram[i]:02X}' for i in miss[:24]) + (' ...' if len(miss) > 24 else ''))
        for i in real[:40]:
            out.append(f'   real ${i:04X} ref={rram[i]:02X} emu={ram0[i]:02X} {lab.get(i, "")}')
        if len(real) > 40:
            out.append(f'   ... {len(real) - 40} more')
        fbest = min(((sum(rram[i] != sf[n + e][0][i] for i in range(0x800) if not 0x200 <= i < 0x300),
                      abs(e), e) for e in range(-a.search, a.search + 1) if n + e in sf))
        fe = fbest[2]
        fr = [i for i in range(0x800) if rram[i] != sf[n + fe][0][i] and not 0x200 <= i < 0x300]
        oam = sum(rram[0x200 + 4 * k:0x204 + 4 * k] != sf[n + fe][0][0x200 + 4 * k:0x204 + 4 * k]
                  for k in range(64))
        out.append(f'   with fill={a.fill}/{a.wram_fill} (offset {fe:+d}): {len(fr)} RAM diffs (OAM excluded), '
                   f'{oam}/64 OAM sprites: ' + ' '.join(
                       f'${i:04X}={rram[i]:02X}/{sf[n + fe][0][i]:02X}' for i in fr[:30])
                   + (' ...' if len(fr) > 30 else ''))
        tot_real += len(real)
        if rw is not None:
            w0, w1 = s0[f][1], s1[f][1]
            wun = [i for i in range(0x2000) if w0[i] != w1[i]]
            wreal = [i for i in range(0x2000) if w0[i] == w1[i] and rw[i] != w0[i]]
            whit = {k: sum(rw[i] == cw[i] for i in wun) for k, (_, cw) in cand.items()}
            out.append(f'   WRAM unwritten {len(wun)} (fill hits ' + ' '.join(
                f'{k}={v}' for k, v in whit.items()) + f'), real diffs {len(wreal)}' + (
                ': ' + ' '.join(f'${0x6000 + x:04X}-${0x6000 + y:04X}' for x, y in ranges(wreal)[:16])
                if wreal else ''))
            for i in wreal[:12]:
                out.append(f'   wreal ${0x6000 + i:04X} ref={rw[i]:02X} emu={w0[i]:02X} {lab.get(0x6000 + i, "")}')
            wf = [i for i in range(0x2000) if rw[i] != sf[n + fe][1][i]]
            out.append(f'   with fill={a.fill}/{a.wram_fill}: {len(wf)} WRAM diffs' + (
                ' in ' + ' '.join(f'${0x6000 + x:04X}-${0x6000 + y:04X}' for x, y in ranges(wf)[:16])
                if wf else ''))
    out.append('')
    out.append(f'total real RAM diffs over {len(refs)} frames: {tot_real}')
    os.makedirs(os.path.dirname(a.out), exist_ok=True)
    open(a.out, 'w').write('\n'.join(out) + '\n')
    print('\n'.join(out))
    return 0


if __name__ == '__main__':
    sys.exit(main())
