"""How much of each captured state the graphics packs cover: per state, the share of sprite pixels (not the hero's
banks) and of background pixels (slot 1-3) whose tile + colour key has a PNG in one of the pack folders.

usage: python3 tools/pack_coverage.py DUMPS_DIR PACK_DIR[,PACK_DIR..] [STATE ..] [--bg]
  --bg: the top missing keys include background keys (default: sprite keys only)
  DUMPS_DIR = a tools/sprite_capture.py output (one sub-folder per state). Uses the renderer's per-line colour keys.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sprite_pack import load_dump, slot_of, VALID, HERO_BANKS, DATA  # noqa: E402


def keys_of(chr_, tags, fb, pal):
    """per pixel (key, is_sprite) for pixels with slot != 0, using Enhance.cs Render's per-line palette"""
    lp = [pal[(0x10 if g >= 4 else 0) + (g & 3) * 4 + s] & 0x3F for g in range(8) for s in range(4)]
    out = []
    for y in range(240):
        base = y * 256
        row = []
        for x in range(256):
            t = tags[base + x]
            if not t & VALID:
                continue
            s = slot_of(chr_, t)
            if s:
                lp[(t >> 19 & 7) * 4 + s] = fb[base + x] & 0x3F
                row.append((t, s))
        for t, s in row:
            g = t >> 19 & 7
            out.append(((t & 0x1FFF, lp[g * 4 + 1], lp[g * 4 + 2], lp[g * 4 + 3]), g >= 4))
    return out


def main(argv):
    if len(argv) < 2:
        print(__doc__); return 1
    bg_missing = '--bg' in argv
    argv = [a for a in argv if a != '--bg']
    src, packs, only = argv[0], argv[1].split(','), set(argv[2:])
    have = set()
    for d in packs:
        for f in os.listdir(d):
            if f.endswith('.png') and len(f) == 15:
                have.add((int(f[:4], 16), int(f[5:7], 16), int(f[7:9], 16), int(f[9:11], 16)))
    chr_ = open(os.path.join(DATA, 'chr.bin'), 'rb').read()
    for st in sorted(os.listdir(src)):
        if only and st not in only:
            continue
        n = [0, 0, 0, 0]         # sprite total, sprite covered, bg total, bg covered
        missing = {}
        for fn in sorted(os.listdir(os.path.join(src, st))):
            for k, spr in keys_of(chr_, *load_dump(os.path.join(src, st, fn))):
                if spr and (k[0] >> 6) in HERO_BANKS:
                    continue
                i = 0 if spr else 2
                n[i] += 1
                if k in have:
                    n[i + 1] += 1
                elif spr or bg_missing:
                    missing[k] = missing.get(k, 0) + 1
        top = sorted(missing.items(), key=lambda kv: -kv[1])[:10]
        print(f'{st:14s} sprites {n[1] * 100 // max(n[0], 1):3d} % of {n[0]:8d}   background {n[3] * 100 // max(n[2], 1):3d} % of {n[2]:8d}'
              + ('   top missing sprite keys: ' + ' '.join(f'{k[0]:04X}_{k[1]:02X}{k[2]:02X}{k[3]:02X}x{c}' for k, c in top) if top else ''))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
