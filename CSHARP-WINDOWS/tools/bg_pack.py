"""Starter graphics pack for the backgrounds (T15): 16-bit-style 32x32 tiles for ReC.exe's Enhanced graphics, painted
from the background layer as it appears on screen (tools/sprite_capture.py dumps of every test state).

usage: python3 tools/bg_pack.py [--in DIR] [--out DIR] [--preview PNG] [--skip-banks B,..] [--votes N] [--stats]
  --in          capture folder (default out/bg: `python3 tools/sprite_capture.py --out out/bg --every 16 <all states>`)
  --out         pack folder (default packs/backgrounds)
  --preview     default <out>/../backgrounds_preview.png: screen crops, NES (left) and with the pack (right)
  --skip-banks  1 KB CHR banks (hex) left alone (default: the font / window banks, see FONT_BANKS)
  --votes N     occurrences painted per tile + colours (default 16)
  --stats       print the 1 KB banks the background uses (keys and pixels) and exit

Per dump, background pixels (Ppu.Tags group 0-3) get the key the exe looks up -- TTTT_AABBCC, the slot 1-3 colours of
the BG palette as seen on that line (same rule as Enhance.cs Render) -- and keys seen in < 3 dumps (fade steps) or with
one colour are dropped. Identical screens are painted once. The background of a screen is repainted at the NES pixel size,
each tile placement from its own pixels only (so every copy of a tile + colours gets the same painting and floors /
walls repeat without seams), then each whole, sprite-free 8x8 occurrence of a key is rounded to 4x (Scale2x
twice with its neighbours, on the NES colours so the shapes round like the built-in look; each 4x pixel takes the
painted colour of the NES pixel it came from) and votes for its painting; the most frequent painting of each key wins (copies differ only in
how their edges round into the neighbours).

Painting (the 16-bit look of the sprite packs, made for scenery):
  - colours: the 5-tone ramp per NES colour of tools/sprite_pack.py (shadows toward blue-violet, highlights toward
    warm yellow); where a shape meets a clearly darker colour (its ground or outline), the
    pixel gets a lighter tone at the top / left and a darker one at the bottom / right (the extremes on shapes of
    >= 12 / 24 pixels, the deepest only against near-black); near-white is the art's own highlight and is never
    shaded darker; boundaries against a lighter colour are left alone (two-tone shapes are shaded in the NES art
    already); shapes of < 4 pixels (dither dots, cracks) keep their colour;
  - dark colours keep their darkness (a slight cool lift at lit edges only);
  - 4x pixels that come from slot-0 pixels (the backdrop colour) are left transparent: the built-in look shows there.
"""
import json
import os
import sys
import zlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import core_vs_web as cv  # noqa: E402
from pngio import encode  # noqa: E402
from sprite_pack import DATA, S, VALID, MIN_DUMPS, luma, mix, ramp, load_dump, slot_of  # noqa: E402

# 1 KB banks of the HUD (38: LIFE / FORCE, bars; 39: window frames, LV / MP / $), the digits / HUD words (3C) and the
# font (3D): the status bar, menus and messages stay crisp
FONT_BANKS = {0x38, 0x39, 0x3C, 0x3D}


def bg_keys(chr_, tags, fb, pal):
    """per pixel: (key, slot) of background pixels (key None for slot 0 / one-colour), else None (sprite / no tag)"""
    lp = [pal[g * 4 + s] & 0x3F if s else 0 for g in range(4) for s in range(4)]
    out = [None] * (256 * 240)
    for y in range(240):
        base = y * 256
        for x in range(256):
            t = tags[base + x]
            if not t & VALID or (t >> 19 & 7) >= 4:
                continue
            s = slot_of(chr_, t)
            if s:
                lp[(t >> 19 & 7) * 4 + s] = fb[base + x] & 0x3F
        for x in range(256):
            t = tags[base + x]
            if not t & VALID or (t >> 19 & 7) >= 4:
                continue
            g = t >> 19 & 7
            c1, c2, c3 = lp[g * 4 + 1], lp[g * 4 + 2], lp[g * 4 + 3]
            s = slot_of(chr_, t)
            key = (t & 0x1FFF, c1, c2, c3) if s and not c1 == c2 == c3 else None
            out[base + x] = (key, s)
    return out


class BgPainter:
    def __init__(self, nes_rgb):
        self.rgb = [((c >> 16) & 255, (c >> 8) & 255, c & 255) for c in nes_rgb]
        self.ramps = {}

    def ramp_of(self, c):
        if c not in self.ramps:
            rgb = self.rgb[c]
            if luma(rgb) < 0.14:     # dark: stay dark, a cool lift toward the light only
                lift = (0x2A, 0x2A, 0x3C)
                self.ramps[c] = [mix(rgb, (0, 0, 0), 0.3), mix(rgb, (0, 0, 0), 0.15), rgb, mix(rgb, lift, 0.35),
                                 mix(rgb, lift, 0.6)]
            else:
                self.ramps[c] = ramp(rgb)
        return self.ramps[c]

    def repaint(self, grid, origin, size):
        """grid of NES colours (None = sprite / off screen) -> RGB grid. origin[y][x] = the pixel's tile placement:
        neighbours in another placement count as unknown, so a tile is painted from its own pixels only (every copy
        of a tile + colours gets the same painting; repeats join without seams)"""
        h, w = len(grid), len(grid[0])

        def g(x, y):
            return grid[y][x] if 0 <= x < w and 0 <= y < h and origin[y][x] == o else None

        out = [[None] * w for _ in range(h)]
        for y in range(h):
            row = grid[y]
            for x in range(w):
                v = row[x]
                if v is None:
                    continue
                o = origin[y][x]
                tones = self.ramp_of(v)
                up, lf, dn, rt = g(x, y - 1), g(x - 1, y), g(x, y + 1), g(x + 1, y)
                # an edge is shaded only against a clearly darker neighbour (the shape stands out from its ground /
                # outline); against a lighter one it is the inner half of a two-tone shape the NES art shaded already.
                # Neighbours of unknown colour (sprites, screen edge) count as the same colour.
                lv = luma(self.rgb[v])
                lit_nb = next((n for n in (up, lf) if n is not None and n != v and luma(self.rgb[n]) < lv - 0.25), None)
                dk_nb = next((n for n in (dn, rt) if n is not None and n != v and luma(self.rgb[n]) < lv - 0.25), None) \
                    if lv < 0.85 else None        # near-white is the art's own highlight: never shaded darker
                tone = 2
                if size[y][x] >= 4:             # texture specks (dither, cracks) keep their colour
                    if lit_nb is not None and dk_nb is None:
                        tone = 4 if size[y][x] >= 12 and (up == lit_nb) else 3
                    elif dk_nb is not None and lit_nb is None:
                        tone = 0 if size[y][x] >= 24 and dn == dk_nb and luma(self.rgb[dk_nb]) < 0.14 else 1
                out[y][x] = tones[tone]
        return out


def region_sizes(grid, origin):
    """4-connected same-colour region size per pixel within its tile placement (None pixels: 0)"""
    h, w = len(grid), len(grid[0])
    size = [[0] * w for _ in range(h)]
    seen = [bytearray(w) for _ in range(h)]
    for y in range(h):
        for x in range(w):
            if seen[y][x] or grid[y][x] is None:
                continue
            v, o, stack, comp = grid[y][x], origin[y][x], [(x, y)], []
            seen[y][x] = 1
            while stack:
                cx, cy = stack.pop()
                comp.append((cx, cy))
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[ny][nx] and grid[ny][nx] == v and origin[ny][nx] == o:
                        seen[ny][nx] = 1
                        stack.append((nx, ny))
            n = len(comp)
            for cx, cy in comp:
                size[cy][cx] = n
    return size


def occurrences(tags, info):
    """whole 8x8 sprite-free occurrences: origin (x0, y0) -> key; all 64 pixels from one tile + one key"""
    origins = {}
    for p in range(256 * 240):
        t = tags[p]
        if not t & VALID or (t >> 19 & 7) >= 4:
            continue
        x, y = p % 256, p // 256
        o = (x - (t >> 13 & 7), y - (t >> 16 & 7), t & 0x1FFF)
        origins.setdefault(o, []).append(p)
    res = {}
    for (x0, y0, tile), ps in origins.items():
        if len(ps) != 64 or x0 < 0 or y0 < 0 or x0 > 248 or y0 > 232:
            continue
        keys = {info[p][0] for p in ps} - {None}
        if len(keys) == 1:
            res[(x0, y0)] = keys.pop()
    return res


def scale2x_by(g):
    """Scale2x (AdvanceMAME rules) on cells (key, ...): the rules compare cell[0] only, a picked cell is copied whole"""
    h, w = len(g), len(g[0])
    out = [[None] * (2 * w) for _ in range(2 * h)]
    for y in range(h):
        up, row, dn = g[max(y - 1, 0)], g[y], g[min(y + 1, h - 1)]
        for x in range(w):
            e, b, hh = row[x], up[x], dn[x]
            d, f = row[max(x - 1, 0)], row[min(x + 1, w - 1)]
            e0 = e1 = e2 = e3 = e
            if b[0] != hh[0] and d[0] != f[0]:
                e0 = d if d[0] == b[0] else e
                e1 = f if b[0] == f[0] else e
                e2 = d if d[0] == hh[0] else e
                e3 = f if hh[0] == f[0] else e
            out[2 * y][2 * x], out[2 * y][2 * x + 1], out[2 * y + 1][2 * x], out[2 * y + 1][2 * x + 1] = e0, e1, e2, e3
    return out


def main(argv):
    src, out_dir, preview = os.path.join(cv.ROOT, 'out', 'bg'), os.path.join(cv.ROOT, 'packs', 'backgrounds'), None
    skip_banks, votes_max, stats = set(FONT_BANKS), 16, False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--in':
            src = argv[i + 1]; i += 1
        elif a == '--out':
            out_dir = argv[i + 1]; i += 1
        elif a == '--preview':
            preview = argv[i + 1]; i += 1
        elif a == '--skip-banks':
            skip_banks = {int(b, 16) for b in argv[i + 1].split(',') if b}; i += 1
        elif a == '--votes':
            votes_max = int(argv[i + 1]); i += 1
        elif a == '--stats':
            stats = True
        elif a in ('-h', '--help'):
            print(__doc__); return 0
        i += 1
    preview = preview or os.path.join(os.path.dirname(os.path.abspath(out_dir)), 'backgrounds_preview.png')
    chr_ = open(os.path.join(DATA, 'chr.bin'), 'rb').read()
    painter = BgPainter(json.load(open(os.path.join(DATA, 'palettes.json')))['nesRgb'])
    files = [os.path.join(src, st, fn) for st in sorted(os.listdir(src)) if os.path.isdir(os.path.join(src, st))
             for fn in sorted(os.listdir(os.path.join(src, st)))] if os.path.isdir(src) else []
    if not files:
        print(f'no dumps in {src} (run tools/sprite_capture.py --out {src} ...)'); return 1
    # pass 1: unique screens, how many dumps show each key
    seen_n, uniq, hashes = {}, [], set()
    bank_keys, bank_px = {}, {}
    for f in files:
        h = zlib.crc32(open(f, 'rb').read())
        if h in hashes:
            continue
        tags, fb, pal = load_dump(f)
        hashes.add(h)
        info = bg_keys(chr_, tags, fb, pal)
        ks = {e[0] for e in info if e is not None} - {None}
        for k in ks:
            seen_n[k] = seen_n.get(k, 0) + 1
            bank_keys.setdefault(k[0] >> 6, set()).add(k)
        if stats:
            for e in info:
                if e is not None and e[0] is not None:
                    bank_px[e[0][0] >> 6] = bank_px.get(e[0][0] >> 6, 0) + 1
        uniq.append(f)
    if stats:
        for b in sorted(bank_keys):
            print(f'bank {b:02X}: {len(bank_keys[b]):4d} keys, {len({k[0] for k in bank_keys[b]}):3d} tiles, '
                  f'{bank_px.get(b, 0):9d} px')
        print(f'{len(uniq)} unique screens of {len(files)} dumps')
        return 0
    stable = {k for k, n in seen_n.items() if n >= MIN_DUMPS and (k[0] >> 6) not in skip_banks}
    # pass 2: paint, vote
    votes = {}                    # key -> {painting bytes: count}
    shots = []
    painted = 0
    for f in uniq:
        tags, fb, pal = load_dump(f)
        info = bg_keys(chr_, tags, fb, pal)
        occ = {o: k for o, k in occurrences(tags, info).items() if k in stable and sum(votes.get(k, {}).values()) < votes_max}
        if not occ:
            continue
        painted += 1
        grid = [[(fb[y * 256 + x] & 0x3F) if info[y * 256 + x] is not None else None for x in range(256)] for y in range(240)]
        origin = [[(x - (tags[y * 256 + x] >> 13 & 7), y - (tags[y * 256 + x] >> 16 & 7))
                   if info[y * 256 + x] is not None else None for x in range(256)] for y in range(240)]
        size = region_sizes(grid, origin)
        col = painter.repaint(grid, origin, size)
        for (x0, y0), k in occ.items():
            # 12x12 window (2 px border, clamped at the screen edge), Scale2x twice, crop the tile
            # 12x12 window (2 px border, clamped at the screen edge) of (NES colour, painted colour, drawn), rounded
            # with Scale2x twice on the NES colours (the shapes round exactly like the built-in look), cut to the tile
            win = []
            for yy in range(-2, 10):
                py = min(max(y0 + yy, 0), 239)
                win.append([])
                for xx in range(-2, 10):
                    q = py * 256 + min(max(x0 + xx, 0), 255)
                    e = info[q]
                    win[-1].append((grid[py][q % 256], col[py][q % 256], e is not None and e[1] != 0) if e is not None
                                   else (-1, None, False))
            big = scale2x_by(scale2x_by(win))
            cell = []
            for yy in range(32):
                for xx in range(32):
                    _, c, drawn = big[8 + yy][8 + xx]
                    cell.append((*c, 255) if drawn else (0, 0, 0, 0))
            blob = tuple(cell)
            d = votes.setdefault(k, {})
            d[blob] = d.get(blob, 0) + 1
        if len(shots) < 8 and len(occ) >= 200 and f.split(os.sep)[-2] not in {s[0] for s in shots}:
            shots.append((f.split(os.sep)[-2], f))
    art = {k: max(d.items(), key=lambda kv: kv[1])[0] for k, d in votes.items()}
    os.makedirs(out_dir, exist_ok=True)
    for fn in os.listdir(out_dir):
        if fn.endswith('.png'):
            os.remove(os.path.join(out_dir, fn))
    written = 0
    for (t, c1, c2, c3), cell in sorted(art.items()):
        if not any(p[3] for p in cell):
            continue
        rows = [list(cell[y * 32:(y + 1) * 32]) for y in range(32)]
        open(os.path.join(out_dir, f'{t:04X}_{c1:02X}{c2:02X}{c3:02X}.png'), 'wb').write(encode(32, 32, rows, alpha=True))
        written += 1
    # preview: a 128x96 crop of each shot screen, NES 4x | pack over NES 4x
    cw, chh, pad = 128 * S, 96 * S, 8
    W, H = 2 * cw + 3 * pad, len(shots) * (chh + pad) + pad
    img = [[(60, 64, 72)] * W for _ in range(H)]
    for n, (_, f) in enumerate(shots):
        tags, fb, pal = load_dump(f)
        info = bg_keys(chr_, tags, fb, pal)
        cx, cy = 64, 72
        oy = pad + n * (chh + pad)
        for y in range(96 * S):
            for x in range(128 * S):
                p = (cy + y // S) * 256 + cx + x // S
                c = painter.rgb[fb[p] & 0x3F]
                img[oy + y][pad + x] = c
                t = tags[p]
                e = info[p]
                if e is not None and e[0] in art:
                    a = art[e[0]][((t >> 16 & 7) * S + y % S) * 32 + (t >> 13 & 7) * S + x % S]
                    if a[3]:
                        c = a[:3]
                img[oy + y][2 * pad + cw + x] = c
    open(preview, 'wb').write(encode(W, H, img))
    print(f'{out_dir}: {written} pack tiles ({len({k[0] for k in art})} distinct tiles) from {painted} painted screens '
          f'({len(uniq)} unique of {len(files)} dumps); preview {preview} ({len(shots)} screens)')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
