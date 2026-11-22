#!/usr/bin/env python3
"""CRC32 of a PNG's decoded 8-bit RGB pixels (session 208).

usage: python3 tools/png_crc.py A.png [B.png ..]

Prints `<crc32 hex> <w>x<h> <file>` per file. The value equals node's `zlib.crc32(rgb)` over
`fbToRgb(m.ppu.fb, nesRgb)` (web/test/harness.mjs) for the same screen, so a web check can pin a
tools.emu screenshot as one number (e.g. web/test/nmimode4_check.mjs) instead of storing the PNG.
Supports 8-bit RGB / RGBA non-interlaced PNGs with any row filter.
"""
import struct
import sys
import zlib


def read_png_rgb(path):
    b = open(path, 'rb').read()
    if b[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError(f'{path}: not a PNG')
    p, idat, w, h, ch = 8, [], 0, 0, 3
    while p < len(b):
        n, tag = struct.unpack('>I4s', b[p:p + 8])
        d = b[p + 8:p + 8 + n]
        if tag == b'IHDR':
            w, h, depth, ctype, _, _, inter = struct.unpack('>IIBBBBB', d)
            if depth != 8 or ctype not in (2, 6) or inter:
                raise ValueError(f'{path}: need 8-bit RGB/RGBA non-interlaced')
            ch = 3 if ctype == 2 else 4
        elif tag == b'IDAT':
            idat.append(d)
        p += 12 + n
    raw = zlib.decompress(b''.join(idat))
    stride = w * ch
    prev = bytearray(stride)
    out = bytearray()
    for y in range(h):
        o = y * (stride + 1)
        f = raw[o]
        cur = bytearray(raw[o + 1:o + 1 + stride])
        for i in range(stride):
            a = cur[i - ch] if i >= ch else 0
            up = prev[i]
            c = prev[i - ch] if i >= ch else 0
            if f == 1:
                cur[i] = (cur[i] + a) & 255
            elif f == 2:
                cur[i] = (cur[i] + up) & 255
            elif f == 3:
                cur[i] = (cur[i] + ((a + up) >> 1)) & 255
            elif f == 4:
                pa, pb, pc = abs(up - c), abs(a - c), abs(a + up - 2 * c)
                pr = a if pa <= pb and pa <= pc else (up if pb <= pc else c)
                cur[i] = (cur[i] + pr) & 255
        prev = cur
        if ch == 4:
            for x in range(w):
                out += cur[x * 4:x * 4 + 3]
        else:
            out += cur
    return w, h, bytes(out)


def main(argv):
    if not argv or argv[0] in ('-h', '--help'):
        print(__doc__.strip())
        return 0 if argv else 2
    for f in argv:
        w, h, rgb = read_png_rgb(f)
        print(f'{zlib.crc32(rgb):08x} {w}x{h} {f}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
