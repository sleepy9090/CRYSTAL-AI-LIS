#!/usr/bin/env python3
"""Diff a reference-emulator capture (tools/ref/fceux_capture.lua) against tools.emu.

usage (repo root):
  python3 tools/ref/compare_ref.py [--ref DIR] [--out FILE] [--search N] [--keys SPEC ...]
  python3 tools/ref/compare_ref.py --make-fake DIR [--shift D] [--tamper]   (self-test input)

For every DIR/frame_NNNN.ram (default DIR = ref) tools.emu is run from power-on with the same
canonical --keys script and snapshotted after each frame.  The reference frame N is matched with
the tools.emu frame N+d, d in [-N..N] (--search, default 3) chosen for the fewest RAM byte
differences (FCEUX frames start at vblank, tools.emu frames end at the 261->0 wrap).  Reported:
  RAM     byte diffs in $0000-$07FF except the OAM page (address, ref, emu, label)
  OAM     $0200-$02FF shadow page = 64 sprites (y tile attr x) that differ
  WRAM    differing byte count + ranges in $6000-$7FFF
  NT      differing nametable bytes per 1KB table ($2000/$2400/$2800/$2C00)        (if .nt)
  PAL     differing palette entries ($3F10/14/18/1C folded onto $3F00/04/08/0C)    (if .pal)
  PIXELS  palette-independent screen diff: each tools.emu colour is mapped to the reference colour
          it most often sits on; pixels that disagree with that map are mismatches   (if .png/.gd)
Exit 0 = every compared field identical, 1 = differences, 2 = no reference capture found
(REVERSE.md: then everything stays "self-emu only").

--make-fake DIR writes tools.emu's own captures in the reference file format, with RGB colours
remapped and the frame labels shifted by --shift (default 1), to test this comparator end to end.
--tamper also corrupts the first fake frame ($0040, OAM sprite 5, $3F01, an 8x8 pixel block),
which the comparison must then report.
"""
import argparse   # noqa: F401  (stdlib first: tools/ on sys.path shadows dis)
import dis        # noqa: F401
import glob
import inspect    # noqa: F401
import os
import re
import struct
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if ROOT not in sys.path:
    sys.path.append(ROOT)

from tools.emu.nes import NES          # noqa: E402
from tools.emu import debug            # noqa: E402
from tools.emu.png import write_rgb    # noqa: E402
from tools.render_compare import read_png  # noqa: E402

CANON_KEYS = '1250:START 1400:START 1600:START 1700:START 2300:START 2340:DOWN:80'
FAKE_FRAMES = [120, 600, 1450, 2352, 2450]


def load_labels():
    lab = {}
    path = os.path.join(ROOT, 'labels.csv')
    if os.path.exists(path):
        for ln in open(path):
            p = ln.rstrip('\n').split(',', 3)
            if len(p) >= 3 and p[0] == '*':
                try:
                    lab[int(p[1], 16)] = p[2]
                except ValueError:
                    pass
    return lab


def read_gd(path):
    """libgd 'gd' truecolor image (gui.gdscreenshot): FFFE, w, h, 1, transparent, w*h ARGB."""
    d = open(path, 'rb').read()
    sig, w, h = struct.unpack('>HHH', d[:6])
    assert sig == 0xFFFE and d[6] == 1, f'{path}: not a truecolor gd image'
    off = 11
    px = [tuple(d[off + 4 * i + 1:off + 4 * i + 4]) for i in range(w * h)]
    return w, h, px


def pal_fold(i):
    return i & 0x0F if (i & 0x13) == 0x10 else i


# ------------------------------------------------------------------ tools.emu side
def snapshot(nes):
    ppu = nes.ppu
    nt = bytes(ppu.vram_read(0x2000 + i) for i in range(0x1000))
    rgb = ppu.to_rgb()
    return {'ram': bytes(nes.bus.ram), 'wram': bytes(nes.bus.wram), 'nt': nt,
            'pal': bytes(ppu.vram_read(0x3F00 + i) for i in range(32)),
            'px': [tuple(rgb[i:i + 3]) for i in range(0, len(rgb), 3)], 'rgb': rgb}


def run_emu(keys, frames):
    """frames: set of frame labels (= ppu.frame_no - 1 after run_frame, as compare.py)."""
    nes = NES()
    nes.keys = debug.parse_keys(keys)
    snaps = {}

    def hook(n):
        f = n.ppu.frame_no - 1
        if f in frames:
            snaps[f] = snapshot(n)
    nes.on_frame(hook)
    nes.run_frames(max(frames) + 1)
    return snaps


# ------------------------------------------------------------------ reference side
def load_ref(base):
    r = {}
    for ext in ('ram', 'wram', 'nt', 'pal'):
        p = f'{base}.{ext}'
        if os.path.exists(p):
            r[ext] = open(p, 'rb').read()
    if os.path.exists(base + '.png'):
        r['img'] = read_png(base + '.png')
    elif os.path.exists(base + '.gd'):
        r['img'] = read_gd(base + '.gd')
    return r


def ram_diffs(a, b):
    return [i for i in range(0x800) if a[i] != b[i] and not 0x200 <= i < 0x300]


def ranges(idx):
    out, s = [], None
    for i in idx:
        if s is None or i != e + 1:
            if s is not None:
                out.append((s, e))
            s = i
        e = i
    if s is not None:
        out.append((s, e))
    return out


def pixel_diff(ref_img, emu_px):
    w, h, rp = ref_img
    if (w, h) == (256, 224):      # FCEUX crops overscan: compare tools.emu rows 8-231 (session 43)
        emu_px = emu_px[8 * 256:232 * 256]
    elif (w, h) != (256, 240):
        return None, f'reference image is {w}x{h}, expected 256x240 or 256x224'
    votes = {}
    for e, r in zip(emu_px, rp):
        d = votes.setdefault(e, {})
        d[r] = d.get(r, 0) + 1
    cmap = {e: max(d, key=d.get) for e, d in votes.items()}
    bad = [i for i, (e, r) in enumerate(zip(emu_px, rp)) if cmap[e] != r]
    rows = {}
    top = 8 if h == 224 else 0    # per tile row in screen coordinates
    for i in bad:
        rows[(i // 256 + top) // 8] = rows.get((i // 256 + top) // 8, 0) + 1
    many = {}
    for e, r in cmap.items():
        many.setdefault(r, []).append(e)
    merged = sum(1 for v in many.values() if len(v) > 1)
    return bad, (f'{len(bad)} px ({100.0 * len(bad) / (w * h):.2f}%), colour map {len(cmap)} emu -> '
                 f'{len(many)} ref colours ({merged} ref colours hit by >1 emu colour)'
                 + (', per tile row ' + ' '.join(f'{k}:{v}' for k, v in sorted(rows.items()))
                    if bad else ''))


def compare_frame(n, ref, snaps, search, lab, out):
    cands = [(len(ram_diffs(ref['ram'], snaps[n + d]['ram'])), abs(d), d)
             for d in range(-search, search + 1) if n + d in snaps]
    nd, _, d = min(cands)
    s = snaps[n + d]
    out.append(f'== ref frame {n}  <->  tools.emu frame {n + d} (offset {d:+d})')
    clean = True
    rd = ram_diffs(ref['ram'], s['ram'])
    out.append(f'  RAM   {len(rd)} byte diffs (OAM page excluded)')
    for i in rd[:48]:
        out.append(f'        ${i:04X} ref={ref["ram"][i]:02X} emu={s["ram"][i]:02X} {lab.get(i, "")}')
    if len(rd) > 48:
        out.append(f'        ... {len(rd) - 48} more')
    oam = [k for k in range(64) if ref['ram'][0x200 + 4 * k:0x204 + 4 * k] != s['ram'][0x200 + 4 * k:0x204 + 4 * k]]
    out.append(f'  OAM   {len(oam)}/64 sprites differ' + (f' (ids {oam[:16]})' if oam else ''))
    clean &= not rd and not oam
    if 'wram' in ref:
        wd = [i for i in range(0x2000) if ref['wram'][i] != s['wram'][i]]
        rg = ranges(wd)
        out.append(f'  WRAM  {len(wd)} byte diffs' + (' in ' + ' '.join(
            f'${0x6000 + a:04X}-${0x6000 + b:04X}' for a, b in rg[:12]) + (' ...' if len(rg) > 12 else '')
            if wd else ''))
        clean &= not wd
    if 'nt' in ref:
        per = [sum(ref['nt'][t * 0x400 + i] != s['nt'][t * 0x400 + i] for i in range(0x400)) for t in range(4)]
        out.append('  NT    diffs per table $2000/$2400/$2800/$2C00: ' + ' '.join(map(str, per)))
        clean &= not any(per)
    if 'pal' in ref:
        pd = sorted({pal_fold(i) for i in range(32)
                     if (ref['pal'][pal_fold(i)] & 0x3F) != (s['pal'][pal_fold(i)] & 0x3F)})
        out.append(f'  PAL   {len(pd)} entries differ' + (': ' + ' '.join(
            f'$3F{i:02X} ref={ref["pal"][i] & 0x3F:02X} emu={s["pal"][i]:02X}' for i in pd) if pd else ''))
        clean &= not pd
    if 'img' in ref:
        bad, msg = pixel_diff(ref['img'], s['px'])
        out.append('  PIXELS ' + msg)
        clean &= bad == []
    else:
        out.append('  PIXELS (no screenshot in capture)')
    return clean


def make_fake(dirpath, shift, keys, tamper=False):
    os.makedirs(dirpath, exist_ok=True)
    snaps = run_emu(keys, set(FAKE_FRAMES))
    for k, f in enumerate(FAKE_FRAMES):
        s, n = snaps[f], f - shift
        base = os.path.join(dirpath, f'frame_{n:04d}')
        data = {ext: bytearray(s[ext]) for ext in ('ram', 'wram', 'nt', 'pal')}
        rgb = bytearray((255 - v) for v in s['rgb'])   # a different "palette": inverted colours
        if tamper and k == 0:      # negative test: $0040, sprite 5, $3F01, an 8x8 block at (64,64)
            data['ram'][0x40] ^= 0x55
            data['ram'][0x214] ^= 0x01
            data['pal'][1] ^= 0x01
            for y in range(64, 72):
                for x in range(64, 72):
                    rgb[(y * 256 + x) * 3] ^= 0x80
        for ext, v in data.items():
            open(f'{base}.{ext}', 'wb').write(v)
        write_rgb(base + '.png', 256, 240, bytes(rgb))
    print(f'wrote {len(FAKE_FRAMES)} fake reference frames to {dirpath} (labels shifted by -{shift})')


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--ref', default=os.path.join(ROOT, 'ref'))
    ap.add_argument('--out', default=os.path.join(ROOT, 'docs', 'ref_compare.txt'))
    ap.add_argument('--search', type=int, default=3)
    ap.add_argument('--keys', nargs='+', default=[CANON_KEYS])
    ap.add_argument('--make-fake', metavar='DIR')
    ap.add_argument('--shift', type=int, default=1)
    ap.add_argument('--tamper', action='store_true')
    a = ap.parse_args(argv)
    if a.make_fake:
        make_fake(a.make_fake, a.shift, a.keys, a.tamper)
        return 0
    refs = {}
    for p in sorted(glob.glob(os.path.join(a.ref, 'frame_*.ram'))):
        refs[int(re.search(r'frame_(\d+)\.ram$', p).group(1))] = p[:-4]
    if not refs:
        print(f'no reference capture in {a.ref} (run tools/ref/fceux_capture.lua in FCEUX first); '
              'all checks stay self-emu only')
        return 2
    want = {n + d for n in refs for d in range(-a.search, a.search + 1) if n + d >= 0}
    print(f'{len(refs)} reference frames; running tools.emu to frame {max(want)} ...', file=sys.stderr)
    snaps = run_emu(a.keys, want)
    lab = load_labels()
    out = [f'# compare_ref: {a.ref} vs tools.emu, keys: {" ".join(a.keys)}, offset search +-{a.search}']
    clean = [n for n in sorted(refs) if compare_frame(n, load_ref(refs[n]), snaps, a.search, lab, out)]
    out.append(f'# {len(clean)}/{len(refs)} frames identical in every compared field')
    text = '\n'.join(out) + '\n'
    if a.out != '-':
        os.makedirs(os.path.dirname(a.out) or '.', exist_ok=True)
        open(a.out, 'w').write(text)
    print(text, end='')
    return 0 if len(clean) == len(refs) else 1


if __name__ == '__main__':
    sys.exit(main())
