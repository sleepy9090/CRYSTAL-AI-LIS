"""Generate src/ReC.Win/app.ico from an intro frame of the port (TASKS.md T6).

usage: python3 tools/make_icon.py [--frame F] [--crop X,Y,S] [--png IN.png] [--out PATH]
  Runs `src/ReC.Run/bin/Release/net10.0/ReC.Run --frames F-213 --dump-screen out/icon_src.png` (frame_no F, default
  1000 = the intro's burning-city picture) unless --png is given, crops the S x S square at X,Y (default 64,4,128),
  and writes a Windows .ico with 256 / 48 / 32 / 16 px PNG entries (box-filtered down, nearest-neighbour up).
  Stdlib only (zlib + struct). Needs a Release build of ReC.Run.
"""
import os
import struct
import subprocess
import sys
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RUN = os.path.join(ROOT, 'src', 'ReC.Run', 'bin', 'Release', 'net10.0', 'ReC.Run')
SIZES = (256, 48, 32, 16)


def png_decode(data):
    """8-bit RGB / RGBA non-interlaced PNG -> (w, h, rows of (r, g, b) tuples)"""
    assert data[:8] == b'\x89PNG\r\n\x1a\n', 'not a PNG'
    pos, idat, w = 8, b'', 0
    while pos < len(data):
        n, t = struct.unpack('>I4s', data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + n]
        if t == b'IHDR':
            w, h, depth, ctype, _, _, inter = struct.unpack('>IIBBBBB', body)
            assert depth == 8 and ctype in (2, 6) and inter == 0, 'unsupported PNG'
            bpp = 3 if ctype == 2 else 4
        elif t == b'IDAT':
            idat += body
        pos += 12 + n
    raw = zlib.decompress(idat)
    stride = w * bpp
    rows, prev = [], bytearray(stride)
    for y in range(h):
        f = raw[y * (stride + 1)]
        line = bytearray(raw[y * (stride + 1) + 1:(y + 1) * (stride + 1)])
        for i in range(stride):
            a = line[i - bpp] if i >= bpp else 0
            b = prev[i]
            c = prev[i - bpp] if i >= bpp else 0
            if f == 1:
                line[i] = (line[i] + a) & 255
            elif f == 2:
                line[i] = (line[i] + b) & 255
            elif f == 3:
                line[i] = (line[i] + (a + b) // 2) & 255
            elif f == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[i] = (line[i] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        rows.append([tuple(line[x * bpp:x * bpp + 3]) for x in range(w)])
        prev = line
    return w, h, rows


def png_encode(size, px):
    """px = size rows of (r, g, b) -> RGBA PNG bytes (opaque)"""
    raw = b''.join(b'\x00' + bytes(v for p in row for v in (*p, 255)) for row in px)

    def chunk(t, body):
        return struct.pack('>I', len(body)) + t + body + struct.pack('>I', zlib.crc32(t + body) & 0xFFFFFFFF)
    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', size, size, 8, 6, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))


def resize(px, s, size):
    if size >= s:                      # nearest-neighbour up (integer factor for 256 / 128)
        return [[px[y * s // size][x * s // size] for x in range(size)] for y in range(size)]
    out = []                            # box filter down
    for y in range(size):
        y0, y1 = y * s // size, max(y * s // size + 1, (y + 1) * s // size)
        row = []
        for x in range(size):
            x0, x1 = x * s // size, max(x * s // size + 1, (x + 1) * s // size)
            cells = [px[yy][xx] for yy in range(y0, y1) for xx in range(x0, x1)]
            row.append(tuple(round(sum(c[k] for c in cells) / len(cells)) for k in range(3)))
        out.append(row)
    return out


def ico(images):
    """images = [(size, png bytes)] -> .ico bytes (PNG-compressed entries, Vista+)"""
    head = struct.pack('<HHH', 0, 1, len(images))
    off = 6 + 16 * len(images)
    dirs, blobs = b'', b''
    for size, png in images:
        dirs += struct.pack('<BBBBHHII', size % 256, size % 256, 0, 0, 1, 32, len(png), off + len(blobs))
        blobs += png
    return head + dirs + blobs


def main(argv):
    frame, crop, src, out = 1000, (64, 4, 128), None, os.path.join(ROOT, 'src', 'ReC.Win', 'app.ico')
    it = iter(argv)
    for a in it:
        if a == '--frame':
            frame = int(next(it))
        elif a == '--crop':
            crop = tuple(int(v) for v in next(it).split(','))
        elif a == '--png':
            src = next(it)
        elif a == '--out':
            out = next(it)
        else:
            print(__doc__)
            return 2
    if src is None:
        src = os.path.join(ROOT, 'out', 'icon_src.png')
        os.makedirs(os.path.dirname(src), exist_ok=True)
        subprocess.run([RUN, '--frames', str(frame - 213), '--quiet', '--dump-screen', src], check=True)
    w, h, rows = png_decode(open(src, 'rb').read())
    x0, y0, s = crop
    assert x0 + s <= w and y0 + s <= h, 'crop outside the frame'
    px = [row[x0:x0 + s] for row in rows[y0:y0 + s]]
    images = [(n, png_encode(n, resize(px, s, n))) for n in SIZES]
    data = ico(images)
    with open(out, 'wb') as f:
        f.write(data)
    os.makedirs(os.path.join(ROOT, 'out'), exist_ok=True)
    with open(os.path.join(ROOT, 'out', 'app_icon_256.png'), 'wb') as f:    # preview
        f.write(images[0][1])
    print(f'{out}: {len(data)} bytes, sizes {", ".join(map(str, SIZES))} from frame {frame} crop {crop}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
