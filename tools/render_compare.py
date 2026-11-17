#!/usr/bin/env python3
"""Pixel-compare two PNGs (emulator screen vs static renderer, or emu vs web port).

usage: render_compare.py IMG1 IMG2 [--diff OUT.png] [--crop X,Y,W,H] [--offset DX,DY]
                         [--search R] [--tol N]
  --crop X,Y,W,H  compare only this rectangle of IMG1 (default: whole IMG1)
  --offset DX,DY  IMG1 pixel (x,y) is compared with IMG2 pixel (x+DX, y+DY) (default 0,0)
  --search R      try every offset in [-R..R]^2 around --offset, keep the best
  --tol N         per-channel tolerance (default 0 = exact RGB match)
  --diff OUT.png  write IMG1-sized image: matches dimmed grey, mismatches red
Prints size, compared pixels, mismatches (count + %), mismatch bounding box and the
mismatch count per 8x8 tile row (to spot a split or a single bad row).
Exit code 0 if identical within --tol, 1 otherwise.  Stdlib only (8-bit RGB/RGBA/grey/palette PNG).
"""
import os
import struct
import sys
import zlib

sys.path.insert(0, os.path.dirname(__file__))
from meta_dump import write_png  # noqa: E402


def read_png(path):
    data = open(path, 'rb').read()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', f'{path}: not a PNG'
    pos, idat, plte = 8, b'', None
    while pos < len(data):
        n, typ = struct.unpack('>I4s', data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + n]
        if typ == b'IHDR':
            w, h, depth, ctype, _, _, inter = struct.unpack('>IIBBBBB', body)
        elif typ == b'PLTE':
            plte = [tuple(body[i:i + 3]) for i in range(0, len(body), 3)]
        elif typ == b'IDAT':
            idat += body
        pos += 12 + n
    assert depth == 8 and inter == 0, f'{path}: only 8-bit non-interlaced PNG supported'
    bpp = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[ctype]
    raw = zlib.decompress(idat)
    stride = w * bpp
    rows, prev, i = [], bytearray(stride), 0
    for _ in range(h):
        f = raw[i]
        line = bytearray(raw[i + 1:i + 1 + stride])
        i += 1 + stride
        for x in range(stride):
            a = line[x - bpp] if x >= bpp else 0
            b = prev[x]
            c = prev[x - bpp] if x >= bpp else 0
            if f == 1:
                line[x] = (line[x] + a) & 255
            elif f == 2:
                line[x] = (line[x] + b) & 255
            elif f == 3:
                line[x] = (line[x] + ((a + b) >> 1)) & 255
            elif f == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pr = a if pa <= pb and pa <= pc else (b if pb <= pc else c)
                line[x] = (line[x] + pr) & 255
        rows.append(line)
        prev = line
    px = []
    for line in rows:
        for x in range(w):
            if ctype == 2 or ctype == 6:
                px.append(tuple(line[x * bpp:x * bpp + 3]))
            elif ctype == 3:
                px.append(plte[line[x]])
            else:
                g = line[x * bpp]
                px.append((g, g, g))
    return w, h, px


def compare(a, b, rect, dx, dy, tol):
    (w1, h1, p1), (w2, h2, p2) = a, b
    x0, y0, w, h = rect
    bad, total = [], 0
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            x2, y2 = x + dx, y + dy
            if not (0 <= x2 < w2 and 0 <= y2 < h2):
                continue
            total += 1
            c1, c2 = p1[y * w1 + x], p2[y2 * w2 + x2]
            if any(abs(u - v) > tol for u, v in zip(c1, c2)):
                bad.append((x, y))
    return total, bad


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    opts = dict(zip(argv[2::2], argv[3::2]))
    a, b = read_png(argv[0]), read_png(argv[1])
    rect = tuple(int(v) for v in opts['--crop'].split(',')) if '--crop' in opts else (0, 0, a[0], a[1])
    dx, dy = (int(v) for v in opts.get('--offset', '0,0').split(','))
    tol = int(opts.get('--tol', 0))
    best = None
    r = int(opts.get('--search', 0))
    for sy in range(-r, r + 1):
        for sx in range(-r, r + 1):
            t, bad = compare(a, b, rect, dx + sx, dy + sy, tol)
            if t and (best is None or len(bad) < len(best[2])):
                best = (dx + sx, dy + sy, bad, t)
    dx, dy, bad, total = best
    print(f'IMG1 {argv[0]} {a[0]}x{a[1]}  IMG2 {argv[1]} {b[0]}x{b[1]}')
    print(f'rect={rect} offset={dx},{dy} tol={tol} compared={total} '
          f'mismatch={len(bad)} ({100.0 * len(bad) / max(total, 1):.2f}%)')
    if bad:
        xs, ys = [p[0] for p in bad], [p[1] for p in bad]
        print(f'mismatch bbox x={min(xs)}..{max(xs)} y={min(ys)}..{max(ys)}')
        rows = {}
        for _, y in bad:
            rows[y // 8] = rows.get(y // 8, 0) + 1
        print('per tile row: ' + ' '.join(f'{k}:{v}' for k, v in sorted(rows.items())))
    if '--diff' in opts:
        w1, h1, p1 = a
        out = [tuple(v // 3 for v in c) for c in p1]
        for x, y in bad:
            out[y * w1 + x] = (255, 0, 0)
        write_png(opts['--diff'], w1, h1, out)
        print(f'wrote {opts["--diff"]}')
    return 0 if not bad else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
