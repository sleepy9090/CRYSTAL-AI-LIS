"""Stdlib PNG helpers for the tools: decode (make_icon.png_decode), encode RGB / RGBA rows of tuples."""
import struct
import zlib

from make_icon import png_decode  # noqa: F401  (re-exported)


def _chunk(t, body):
    return struct.pack('>I', len(body)) + t + body + struct.pack('>I', zlib.crc32(t + body) & 0xFFFFFFFF)


def encode(w, h, rows, alpha=False):
    """rows = h lists of w (r, g, b) or (r, g, b, a) tuples -> PNG bytes (colour type 2 or 6)"""
    n = 4 if alpha else 3
    raw = b''.join(b'\x00' + bytes(v for p in row for v in p[:n]) for row in rows)
    return (b'\x89PNG\r\n\x1a\n' + _chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6 if alpha else 2, 0, 0, 0))
            + _chunk(b'IDAT', zlib.compress(raw, 9)) + _chunk(b'IEND', b''))
