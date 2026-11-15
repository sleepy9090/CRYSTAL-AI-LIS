#!/usr/bin/env python3
"""Dump / render re-c metasprites (bank 28/29 ObjDrawMeta format, see REVERSE.md "Object sprite pass").

Usage:
  meta_dump.py text [id ...]                 print records (all 256 if no ids)
  meta_dump.py png [out.png] [--chr R2,R3,R4,R5] [--cell N] [--frames N]
                                             render ids 0-255 (16x16 grid, frames side by side)

Record format (28:845C ids $00-$7F, 28:855C ids $80-$FF, ptr16 into $8000-$BFFF of banks 28/29):
  FF lo hi   -> draw record at hi:lo horizontally mirrored (dx' = -dx-8, attr ^= $40)
  00         -> nothing
  N M frames -> N sprites/frame, frame mask M; frame f data at +2+f*N*4: dx dy attr tile (dx=$80 ends frame)
Sprites are 8x8 from pattern table $1000 = MMC3 R2..R5 (1KB each); tile t -> R[2 + t>>6].
Default --chr is the in-game player set: R2=$40 (`$0713`=0), R3=$42 ($41+clamp($0711,1..5)),
R4/R5 come from per-area records (31:E16D); $50,$51 is just a guess for enemies.
"""
import os, struct, sys, zlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nesrom import load_rom, ROOT

PAIR_BASE = 28 * 0x2000          # banks 28/29 at $8000-$BFFF
TABLE_A, TABLE_B = 0x845C, 0x855C


def rd(prg, addr):
    return prg[PAIR_BASE + addr - 0x8000]


def s8(v):
    return v - 256 if v >= 128 else v


def meta_ptr(prg, mid):
    t = TABLE_A + mid * 2 if mid < 0x80 else TABLE_B + (mid - 0x80) * 2
    return rd(prg, t) | rd(prg, t + 1) << 8


def resolve(prg, mid):
    """-> (record_addr, mirrored, alias_addr_or_None)"""
    p = meta_ptr(prg, mid)
    if not 0x8000 <= p < 0xC000:
        return p, False, None
    if rd(prg, p) == 0xFF:
        q = rd(prg, p + 1) | rd(prg, p + 2) << 8
        return q, True, p
    return p, False, None


def frames(prg, mid):
    """-> list of frames, each a list of (dx, dy, attr, tile) after mirroring; [] if empty/invalid."""
    p, mir, _ = resolve(prg, mid)
    if not 0x8000 <= p < 0xC000:
        return []
    n = rd(prg, p)
    if n == 0 or n == 0xFF:
        return []
    m = rd(prg, p + 1)
    out = []
    for f in range(m + 1):
        base = p + 2 + f * n * 4
        spr = []
        for i in range(n):
            a = base + i * 4
            if a + 3 >= 0xC000:
                break
            dx, dy, at, tl = rd(prg, a), rd(prg, a + 1), rd(prg, a + 2), rd(prg, a + 3)
            if dx == 0x80:
                break
            dx = s8(dx)
            if mir:
                dx, at = -dx - 8, at ^ 0x40
            spr.append((dx, s8(dy), at, tl))
        out.append(spr)
    return out


def cmd_text(prg, ids):
    for mid in ids:
        p, mir, alias = resolve(prg, mid)
        hdr = f"id {mid:02X}: ${meta_ptr(prg, mid):04X}"
        if alias is not None:
            hdr += f" -> FF mirror of ${p:04X}"
        fr = frames(prg, mid)
        if not fr:
            print(hdr + "  (empty)")
            continue
        print(hdr + f"  N={rd(prg, p)} M={rd(prg, p + 1)}")
        for f, spr in enumerate(fr):
            print(f"  f{f}: " + " ".join(f"({dx:+d},{dy:+d} a{at:02X} t{tl:02X})" for dx, dy, at, tl in spr))


# --- rendering ---------------------------------------------------------------
PAL = [  # per sprite palette (attr & 3): colours 1..3 (colour 0 = transparent)
    [(0x60, 0x60, 0x60), (0xA8, 0xA8, 0xA8), (0xF8, 0xF8, 0xF8)],
    [(0x80, 0x30, 0x20), (0xD8, 0x70, 0x40), (0xF8, 0xC8, 0x90)],
    [(0x20, 0x60, 0x20), (0x50, 0xB0, 0x50), (0xB0, 0xF0, 0xB0)],
    [(0x20, 0x40, 0x90), (0x50, 0x80, 0xE0), (0xA8, 0xD0, 0xF8)],
]
BG, GRID, ORIGIN = (0x18, 0x18, 0x28), (0x30, 0x30, 0x48), (0x60, 0x20, 0x60)


def tile_pixels(chr_rom, regs, t):
    bank = regs[t >> 6]
    o = bank * 0x400 + (t & 0x3F) * 16
    d = chr_rom[o:o + 16]
    return [[((d[y] >> (7 - x)) & 1) | (((d[y + 8] >> (7 - x)) & 1) << 1) for x in range(8)] for y in range(8)]


def write_png(path, w, h, px):
    raw = b"".join(b"\x00" + bytes(c for p in px[y * w:(y + 1) * w] for c in p) for y in range(h))
    def chunk(t, d):
        return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def cmd_png(prg, chr_rom, out, regs, cell, nfr):
    cols = 16
    cw = cell * nfr
    W, H = cols * cw, 16 * cell
    px = [BG] * (W * H)
    for mid in range(256):
        cx, cy = (mid % cols) * cw, (mid // cols) * cell
        for x in range(cw):                      # cell border
            px[cy * W + cx + x] = GRID
        for y in range(cell):
            px[(cy + y) * W + cx] = GRID
        ox, oy = cell // 2, cell * 3 // 4       # object origin inside each frame cell
        for f, spr in enumerate(frames(prg, mid)[:nfr]):
            fx = cx + f * cell
            px[(cy + oy) * W + fx + ox] = ORIGIN
            for dx, dy, at, tl in reversed(spr):  # first OAM entry has priority -> draw last
                tp = tile_pixels(chr_rom, regs, tl)
                pal = PAL[at & 3]
                for y in range(8):
                    for x in range(8):
                        sx = 7 - x if at & 0x40 else x
                        sy = 7 - y if at & 0x80 else y
                        c = tp[sy][sx]
                        if not c:
                            continue
                        X, Y = ox + dx + x, oy + dy + 1 + y   # OAM Y is displayed +1
                        if 1 <= X < cell and 1 <= Y < cell:
                            px[(cy + Y) * W + fx + X] = pal[c - 1]
    write_png(out, W, H, px)
    print(f"wrote {out} ({W}x{H}), CHR R2-R5 = " + ",".join(f"${r:02X}" for r in regs))


def main(argv):
    if not argv or argv[0] not in ("text", "png"):
        print(__doc__)
        return 1
    _, prg, chr_rom = load_rom()
    if argv[0] == "text":
        ids = [int(a, 16) for a in argv[1:]] or range(256)
        cmd_text(prg, ids)
        return 0
    out = os.path.join(ROOT, "gfx", "metasprites.png")
    regs, cell, nfr = [0x40, 0x42, 0x50, 0x51], 48, 2
    i = 1
    while i < len(argv):
        a = argv[i]
        if a == "--chr":
            regs = [int(v, 16) for v in argv[i + 1].split(",")]; i += 1
        elif a == "--cell":
            cell = int(argv[i + 1]); i += 1
        elif a == "--frames":
            nfr = int(argv[i + 1]); i += 1
        else:
            out = a
        i += 1
    if len(regs) != 4:
        print("--chr needs 4 values (R2,R3,R4,R5)")
        return 1
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    cmd_png(prg, chr_rom, out, regs, cell, nfr)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
