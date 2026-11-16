#!/usr/bin/env python3
"""Render re-c background screens (static model of ScreenRowRender 31:EBC1 / column 31:EC88).

usage:
  render_screen.py <screen_id_hex> --area <area_hex> [--visited] [--out FILE.png]
  render_screen.py --area <area_hex> --all [--visited]     every screen in that area's grid
  render_screen.py --stats                                   grid-id / bank / tileset statistics

Pipeline (all raw tables, see REVERSE.md "Screen / metatile pipeline"):
  1. page: ScreenPagePtr 31:EF36 -> SwitchBank16(S>>6); $10/$11 = $8000 + (S&$3F)*$100.
     (S&$3F) < $20 -> bank 2*(S>>6) ; else bank 2*(S>>6)+1.
     $62FF b7 (k0 byte 4) -> SetR6($0A) at 31:EBEA: the $8000 half comes from bank 10 instead
     (row render only, first screen of the row; the column render 31:EC88 never does this).
  2. page byte r*16+c = metatile id M (16 cols x 15 rows, bytes $F0-$FF are another table).
  3. visited subst ScreenTileSubst 31:ED99: M < $20 and the screen's $62F0 bit set ->
     M = bank 9 byte $BE00 + ($67&$3C)*8 + M.
  4. MetatileExpand 31:EDBA (banks 8/9): tile(q) = byte at (($67&$FC)|q)<<8 | M, q 0=TL 1=TR 2=BL 3=BR.
  5. MetatileAttr 31:EDE5 + $EECC: palette = (bank9[$B000 + (($67&$3C)>>2)*$40 + (M>>2)] >> 2*(M&3)) & 3
     (MetatileAttrPtrTable 31:EEEF = $B000,$B040..$B3C0; post-substitution M).
  6. BG CHR = 2KB R0 @ $0000, 2KB R1 @ $0800 (k1 R0/R1, 1KB units; low bit ignored by MMC3).
  7. BG palettes = slots k1 p0 p1 p2 and $7F (palette_dump.py); colour 0 of every group and
     every $FF byte = backdrop (colour 0 of p0, PaletteUpload 31:F8CB / PaletteBuild 26:8C0E).
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom, ROOT  # noqa
from meta_dump import write_png  # noqa
import area_dump  # noqa
import palette_dump  # noqa

_, PRG, CHR = load_rom()
SLOTS = palette_dump.read_slots(PRG)


def prg_at(bank, addr):
    return PRG[bank * 0x2000 + (addr & 0x1FFF)]


def page_loc(s, bank10=False):
    """Screen id -> (bank, cpu addr of the 256-byte page)."""
    n, lo = s >> 6, s & 0x3F
    bank = 2 * n + (lo >> 5)
    if bank10 and lo < 0x20:
        bank = 10
    return bank, 0x8000 + lo * 0x100


def page_bytes(s, bank10=False):
    b, a = page_loc(s, bank10)
    o = b * 0x2000 + (a & 0x1FFF)
    return PRG[o:o + 0x100]


def mt_byte(addr):
    """Byte in the banks 8/9 window ($8000-$BFFF, SwitchBank16(4))."""
    return prg_at(8 + ((addr - 0x8000) >> 13), addr)


def subst(v67, m):
    return prg_at(9, 0xBE00 + (v67 & 0x3C) * 8 + m)


def mt_attr(v67, m):
    a = 0xB000 + ((v67 & 0x3C) >> 2) * 0x40 + (m >> 2)
    return (prg_at(9, a) >> (2 * (m & 3))) & 3


def mt_tiles(v67, m):
    return [mt_byte((((v67 & 0xFC) | q) << 8) | m) for q in range(4)]


def area_bg(area):
    """k0 + k1 of an area -> dict, or None for null/garbage areas."""
    try:
        if not area_dump.decode(area):
            return None
        _, ps = area_dump.sub_header(area)
    except Exception:
        return None
    rd = area_dump.rd10
    p = ps[0]
    snd, w, h, fe, ff = (rd(p + i) for i in range(5))
    if w > 7 or h > 11:
        return None
    grid = [[rd(p + 5 + r * (w + 1) + c) for c in range(w + 1)] for r in range(h + 1)]
    k1 = [rd(ps[1] + i) for i in range(7)]
    return {'area': area, 'snd': snd, 'W': w + 1, 'H': h + 1, 'f62FE': fe, 'f62FF': ff,
            'grid': grid, 'pal': k1[0:3] + [0x7F], 'v67': k1[3], 'v69': k1[4],
            'R0': k1[5], 'R1': k1[6], 'bank10': bool(ff & 0x80)}


class Renderer:
    def __init__(self, a):
        self.a = a
        r0, r1 = a['R0'] & 0xFE, a['R1'] & 0xFE
        self.regs = [r0, r0 + 1, r1, r1 + 1]
        bd = SLOTS[a['pal'][0] & 0xFF][0]
        self.backdrop = 0x0F if bd == 0xFF else bd
        self.pals = []
        for p in a['pal']:
            rec = SLOTS[p & 0xFF]
            cols = [self.backdrop] + [self.backdrop if c == 0xFF else c for c in rec[1:]]
            self.pals.append([palette_dump.rgb(c) for c in cols])
        self.tcache = {}

    def tile(self, t, pal):
        k = (t, pal)
        if k not in self.tcache:
            bank = self.regs[t >> 6]
            o = bank * 0x400 + (t & 0x3F) * 16
            d = CHR[o:o + 16]
            P = self.pals[pal]
            self.tcache[k] = [[P[((d[y] >> (7 - x)) & 1) | (((d[y + 8] >> (7 - x)) & 1) << 1)]
                               for x in range(8)] for y in range(8)]
        return self.tcache[k]

    def screen(self, s, visited=False, px=None, W=256, ox=0, oy=0):
        """Draw screen s into px (row-major RGB list of width W) at (ox,oy); returns px."""
        v67 = self.a['v67']
        if px is None:
            px = [None] * (256 * 240); W = 256
        page = page_bytes(s, self.a['bank10'])
        for r in range(15):
            for c in range(16):
                m = page[r * 16 + c]
                if visited and m < 0x20:
                    m = subst(v67, m)
                pal = mt_attr(v67, m)
                for q, t in enumerate(mt_tiles(v67, m)):
                    tp = self.tile(t, pal)
                    x0 = ox + c * 16 + (q & 1) * 8
                    y0 = oy + r * 16 + (q >> 1) * 8
                    for y in range(8):
                        base = (y0 + y) * W + x0
                        px[base:base + 8] = tp[y]
        return px


def render_one(area, s, visited, out):
    a = area_bg(area)
    if a is None:
        sys.exit(f'area {area:02X}: null/garbage header')
    px = Renderer(a).screen(s, visited)
    write_png(out, 256, 240, px)
    return out


def stats():
    from collections import Counter
    ids, banks, tsets, b10, holes, holes80 = Counter(), Counter(), Counter(), [], 0, []
    n = 0
    for area in range(area_dump.table_len()):
        a = area_bg(area)
        if not a:
            continue
        n += 1
        tsets[a['v67']] += 1
        if a['bank10']:
            b10.append(area)
        for r, row in enumerate(a['grid']):
            for c, s in enumerate(row):
                ids[s] += 1
                banks[page_loc(s, a['bank10'])[0]] += 1
                if s == 0x80:
                    holes80.append((area, c, r))
    print(f'areas with a valid k0/k1: {n}')
    print('grid cells per page bank:', ' '.join(f'{b}:{k}' for b, k in sorted(banks.items())))
    print(f'distinct screen ids: {len(ids)}; id ranges used:',
          ' '.join(f'{lo:02X}-{lo + 0x3F:02X}:{sum(v for s, v in ids.items() if lo <= s < lo + 0x40)}'
                   for lo in (0, 0x40, 0x80, 0xC0)))
    print('tilesets $67:', ' '.join(f'{t:02X}x{k}' for t, k in sorted(tsets.items())))
    print('areas with $62FF b7 (bank 10 pages):', ' '.join(f'{x:02X}' for x in b10))
    print(f'$80 cells inside a W x H grid: {len(holes80)}; first:',
          ' '.join(f'{a:02X}({c},{r})' for a, c, r in holes80[:20]))


def every():
    """gfx/screens/screen_NNN.png for every placed screen id, using the first area that places it."""
    d = os.path.join(ROOT, 'gfx', 'screens'); os.makedirs(d, exist_ok=True)
    first = {}
    for area in range(area_dump.table_len()):
        a = area_bg(area)
        if a:
            for row in a['grid']:
                for s in row:
                    first.setdefault((s, a['bank10'] and (s & 0x3F) < 0x20), area)
    for (s, b10), area in sorted(first.items()):
        name = f'screen_{s:03d}{"_b10" if b10 else ""}.png'
        render_one(area, s, False, os.path.join(d, name))
    print(f'wrote {len(first)} screens to {d} (screen id x bank-10 variant)')


def main(argv):
    if '--stats' in argv:
        return stats()
    if '--every' in argv:
        return every()
    visited = '--visited' in argv
    argv = [x for x in argv if x != '--visited']
    out = None
    if '--out' in argv:
        i = argv.index('--out'); out = argv[i + 1]; del argv[i:i + 2]
    if '--area' not in argv:
        sys.exit(__doc__)
    i = argv.index('--area'); area = int(argv[i + 1], 16); del argv[i:i + 2]
    d = os.path.join(ROOT, 'gfx', 'screens'); os.makedirs(d, exist_ok=True)
    sfx = '_v' if visited else ''
    if '--all' in argv:
        a = area_bg(area)
        for s in sorted({s for row in a['grid'] for s in row}):
            print(render_one(area, s, visited, os.path.join(d, f'screen_{s:03d}_a{area:02X}{sfx}.png')))
        return
    s = int(argv[0], 16)
    print(render_one(area, s, visited, out or os.path.join(d, f'screen_{s:03d}_a{area:02X}{sfx}.png')))


if __name__ == '__main__':
    main(sys.argv[1:])
