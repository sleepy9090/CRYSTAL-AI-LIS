"""Convert FCEUX gui.gdscreenshot dumps (ref/*.gd, libgd truecolor) to PNG next to them.

FCEUX 2.6.5's gui.savescreenshotas PNGs went stale under xvfb after frame 2450 (every later
screenshot repeated one picture), while gui.gdscreenshot reads the frame buffer directly.
fceux_capture.lua writes both; this rewrites each NAME.png from NAME.gd so compare_ref.py
(which reads .png first) sees the fresh picture. 256x224 or 256x240, whatever FCEUX returns.

usage: python3 tools/ref/gd2png.py [refdir]     (default ref/)
"""
import glob
import os
import struct
import sys
import zlib


def read_gd(path):
    d = open(path, 'rb').read()
    sig, w, h = struct.unpack('>HHH', d[:6])
    assert sig == 0xFFFE and d[6] == 1, f'{path}: not a truecolor gd image'
    off = 11
    rows = []
    for y in range(h):
        row = bytearray([0])
        for x in range(w):
            i = off + 4 * (y * w + x)
            row += d[i + 1:i + 4]
        rows.append(bytes(row))
    return w, h, b''.join(rows)


def write_png(path, w, h, raw):
    def chunk(tag, data):
        c = tag + data
        return struct.pack('>I', len(data)) + c + struct.pack('>I', zlib.crc32(c) & 0xFFFFFFFF)
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 2, 0, 0, 0))
    png += chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    open(path, 'wb').write(png)


def main():
    refdir = sys.argv[1] if len(sys.argv) > 1 else 'ref'
    paths = sorted(glob.glob(os.path.join(refdir, '*.gd')))
    for p in paths:
        w, h, raw = read_gd(p)
        write_png(p[:-3] + '.png', w, h, raw)
    print(f'{len(paths)} .gd -> .png in {refdir}')


if __name__ == '__main__':
    main()
