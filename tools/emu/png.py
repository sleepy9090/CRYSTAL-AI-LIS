"""Tiny RGB PNG writer (zlib only) for emulator screenshots."""
import struct
import zlib


def _chunk(tag, data):
    c = tag + data
    return struct.pack('>I', len(data)) + c + struct.pack('>I', zlib.crc32(c) & 0xFFFFFFFF)


def write_rgb(path, w, h, rgb, scale=1):
    """rgb: bytes of w*h*3. scale: integer nearest-neighbour upscale."""
    rows = []
    stride = w * 3
    for y in range(h):
        row = rgb[y * stride:(y + 1) * stride]
        if scale > 1:
            row = b''.join(row[i:i + 3] * scale for i in range(0, stride, 3))
        line = b'\x00' + row
        rows.extend([line] * scale)
    raw = b''.join(rows)
    png = (b'\x89PNG\r\n\x1a\n'
           + _chunk(b'IHDR', struct.pack('>IIBBBBB', w * scale, h * scale, 8, 2, 0, 0, 0))
           + _chunk(b'IDAT', zlib.compress(raw, 9))
           + _chunk(b'IEND', b''))
    with open(path, 'wb') as f:
        f.write(png)
