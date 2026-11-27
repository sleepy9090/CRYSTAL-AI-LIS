"""Painted HD art for the spike field (playtest 11: "make the spikes more realistic / modernize"): tiles $0842 / $0843
(area $AD and the other spike floors; BG palette slots 1-3 = $1B $0C $0F) as 32x32 pack tiles.

usage: python3 tools/spike_art.py [OUT_DIR=packs/backgrounds] [--preview PNG]

The NES field: one spike every 8 pixels, each a 1-pixel stick (green tip, orange shaft, blue base) on black; $0842 holds
a row of spikes with the shaft in column 6, $0843 the same row shifted by 4 (column 2), and the two rows alternate down
the floor. Each tile is painted from that field in world terms (its own spike plus the base of the one above, and the
neighbours left / right), so the tiles join without seams: tapered steel cones lit from the top left (cylinder shading,
a specular streak, darker toward the base), a dark socket ring where each spike leaves the floor, a soft cast shadow
down-right, and a dark stone floor with fine grain. 4 x 4 supersampling per output pixel.
"""
import math
import os
import struct
import sys
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
KEY_COLOURS = '1B0C0F'
TILES = {0x0842: 6, 0x0843: 2}          # tile -> shaft column of its own row of spikes
SIZE, SUB = 32, 4                         # output pixels per tile, samples per pixel per axis
LIGHT = (-0.55, 0.83)                     # from the top left (x, "toward the viewer")


def h2(x, y):
    """a hash noise value in 0..1"""
    n = (int(x) * 374761393 + int(y) * 668265263) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177 & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFF) / 65535.0


def floor_colour(wx, wy):
    """dark stone: blue-grey with grain and faint large blotches (world position in NES pixels)"""
    g = h2(wx * 4, wy * 4) * 0.5 + h2(wx * 4 // 3, wy * 4 // 3) * 0.5
    b = 0.85 + 0.3 * h2(wx // 6, wy // 6)
    v = (0.75 + 0.5 * g) * b
    return (18 * v, 21 * v, 28 * v)


def spikes(shaft):
    """the spikes reaching into a tile whose own row has its shaft at column `shaft`: (centre x, tip y)"""
    other = (shaft + 4) % 8
    out = []
    for k in (-1, 0, 1):
        out.append((shaft + 0.5 + 8 * k, 0.0))          # this tile's row
        out.append((other + 0.5 + 8 * k, -8.0))         # the row above (its base comes into this tile)
        out.append((other + 0.5 + 8 * k, 8.0))          # the row below (only its shadow could)
    return out


HEIGHT = 7.6


def sample(px, py, shaft):
    """colour + coverage at a point in tile NES coordinates (0..8)"""
    col = floor_colour(px, py)
    sp = spikes(shaft)
    # cast shadows (down-right) and socket rings, under every cone
    shade = 1.0
    for cx, ty in sp:
        by = ty + HEIGHT
        # socket: a dark ring around the base, with a thin lit rim on its lower edge
        dx, dy = (px - cx) / 2.0, (py - by) / 0.9
        r = math.hypot(dx, dy)
        if r < 1.0:
            shade *= 0.35 + 0.4 * r
        elif r < 1.25 and dy > 0:
            shade *= 1.35
        # the cone's shadow: the cone shape sheared down-right
        sx, syy = px - 1.3, py - 0.7
        t = (syy - ty) / HEIGHT
        if 0 <= t <= 1 and abs(sx - cx) < 0.25 + 1.45 * t:
            shade *= 0.55
    col = tuple(c * shade for c in col)
    # cones, back (higher) first so the lower ones overlap them
    for cx, ty in sorted(sp, key=lambda s: s[1]):
        t = (py - ty) / HEIGHT
        if not 0 <= t <= 1:
            continue
        w = 0.22 + 1.5 * t
        s = (px - cx) / w
        if abs(s) >= 1:
            continue
        nz = math.sqrt(1 - s * s)
        lam = max(0.0, s * LIGHT[0] + nz * LIGHT[1])
        spec = math.exp(-((s + 0.42) / 0.14) ** 2) * (1.2 - 0.5 * t)
        ao = 0.68 + 0.32 * (1 - t)                # darker toward the floor
        base = (128, 142, 150)                    # steel, a touch of the NES teal
        v = (0.22 + 0.78 * lam) * ao
        c = [b * v + 255 * spec * 0.75 for b in base]
        if t < 0.18:                              # a bright point at the tip
            c = [x + 60 * (1 - t / 0.18) for x in c]
        c[1] += 10 * (1 - t)                      # cool teal sheen near the tip, as the NES green
        col = tuple(min(255.0, x) for x in c)
    return col


def tile(shaft):
    px = []
    for oy in range(SIZE):
        row = []
        for ox in range(SIZE):
            acc = [0.0, 0.0, 0.0]
            for sy in range(SUB):
                for sx in range(SUB):
                    x = (ox + (sx + 0.5) / SUB) * 8 / SIZE
                    y = (oy + (sy + 0.5) / SUB) * 8 / SIZE
                    c = sample(x, y, shaft)
                    for k in range(3):
                        acc[k] += c[k]
            row.append(tuple(int(round(min(255, a / (SUB * SUB)))) for a in acc))
        px.append(row)
    return px


def png(rows, path):
    h, w = len(rows), len(rows[0])
    raw = b''.join(b'\x00' + b''.join(bytes((r, g, b, 255)) for r, g, b in row) for row in rows)

    def chunk(t, d):
        return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xFFFFFFFF)

    data = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
    data += chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    with open(path, 'wb') as f:
        f.write(data)


def main(argv):
    preview = None
    if '--preview' in argv:
        i = argv.index('--preview'); preview = argv[i + 1]; argv = argv[:i] + argv[i + 2:]
    out = argv[0] if argv else os.path.join(ROOT, 'packs', 'backgrounds')
    os.makedirs(out, exist_ok=True)
    tiles = {}
    for t, shaft in TILES.items():
        tiles[t] = tile(shaft)
        png(tiles[t], os.path.join(out, f'{t:04X}_{KEY_COLOURS}.png'))
    print(f'{len(tiles)} spike tiles -> {out}')
    if preview:                                   # a field: rows of $0842 / $0843, 8 x 6 tiles
        rows = []
        for ty in range(6):
            t = tiles[0x0842 if ty % 2 == 0 else 0x0843]
            for y in range(SIZE):
                rows.append([p for _ in range(8) for p in t[y]])
        png(rows, preview)
        print('preview ->', preview)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
