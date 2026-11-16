#!/usr/bin/env python3
"""Dump / render re-c metasprites (bank 28/29 ObjDrawMeta format, see REVERSE.md "Object sprite pass").

Usage:
  meta_dump.py text [id ...]                 print records (all 256 if no ids)
  meta_dump.py png [out.png] [--chr R2,R3,R4,R5] [--cell N] [--frames N]
                                             render ids 0-255 (16x16 grid, frames side by side)
  meta_dump.py sprites [area ...]            per-area sprite sheets with the area's R4/R5 CHR and
                                             sprite palettes -> gfx/sprites/area_NNN.png + index.txt,
                                             and gfx/sprites.json (all 256 records + per-area sets)

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


# --- per-area sprite sets (session 25) ----------------------------------------
PLAYER_IDS = list(range(8))          # ids 0-7 = player 4 facings + 4 attack poses (session 6)


def spawn_objects(prg, area, rec):
    """Area record spawn entries -> list of dicts with the object type and its initial sprite fields.

    Handler -> type (31:E218, see REVERSE.md "Area records"): h0 p+$50, h1 bank 4/5 record
    (type $30 with meta=$23 / tile add=$22&$70, or type=$21 if $20=$FF), h2 $0D/$0E, h3 p+$D0, h4 p.
    Initial sprite fields come from ObjTypeTable 13:AC00 (objtype_dump.decode): $0300 meta id,
    $0320 tile add, $0380 flags (b0 no-dir add, b5 tile+$40). The spawn b2 b7 also sets $0380 b5.
    """
    import area_dump, objtype_dump
    out = []
    for slot, e in rec['ents']:
        if not isinstance(e, tuple):
            continue
        y, x, h, p, b2 = e
        meta_ovr = tile_ovr = None
        if h == 0:
            t = (p + 0x50) & 0xFF
        elif h == 1:
            _, r = area_dump.indirect_rec(p)
            if r[0] == 0xFF:
                t = r[1]
            else:
                t, meta_ovr = 0x30, r[3]
                if not r[2] & 0x80:
                    tile_ovr = r[2] & 0x70
        elif h == 2:
            t = 0x0E if p & 0x80 else 0x0D
            if not p & 0x80 and p in (0x25, 0x3B, 0x3C):
                meta_ovr = 0x4C
        elif h == 3:
            t = (p + 0xD0) & 0xFF
        elif h == 4:
            t = p
        else:
            continue
        cols = {} if t == 1 else objtype_dump.decode(t)[2]
        meta = cols.get(0x300) if meta_ovr is None else meta_ovr
        tadd = cols.get(0x320, 0) if tile_ovr is None else tile_ovr
        f380 = cols.get(0x380, 0) | (0x20 if b2 & 0x80 else 0)
        if f380 & 0x20:
            tadd = (tadd + 0x40) & 0xFF
        out.append({'slot': slot, 'x': x, 'y': y, 'handler': h, 'param': p, 'type': t,
                    'ai': cols.get(0x4A0), 'meta': meta, 'tile_add': tadd, 'f380': f380})
    return out


def area_sprite_set(prg, area):
    """-> dict (chr regs, palette ids/colours, objects) or None for null areas."""
    import area_dump, palette_dump, render_screen
    try:
        rec = area_dump.decode(area)
    except Exception:
        return None
    if not rec or 'ents' not in rec:
        return None
    objs = spawn_objects(prg, area, rec)
    e6, e7 = rec['e6'], rec['e7']
    if any(o['handler'] == 4 and o['param'] & 0xF0 == 0x40 for o in objs):
        e6 = e7 = ((area & 7) + 9) & 0xFF            # flag-item handler override (31:E222)
    bg = render_screen.area_bg(area)
    slots = render_screen.SLOTS
    bd = 0x0F
    if bg:
        b = slots[bg['pal'][0] & 0xFF][0]
        bd = 0x0F if b == 0xFF else b
    pal_ids = [0x00, 0x01, e6, e7]                  # $07E4=0, $07E5=clamp(sword,1..5) -> 1 at start
    pals = []
    for p in pal_ids:
        rec4 = slots[palette_dump.spr_slot(p)]
        pals.append([bd] + [bd if c == 0xFF else c for c in rec4[1:]])
    return {'area': area, 'chr': [0x40, 0x42, rec['r4'], rec['r5']], 'pal_ids': pal_ids,
            'pal_slots': [palette_dump.spr_slot(p) for p in pal_ids], 'pals': pals,
            'objects': objs}


def meta_json(prg):
    out = {}
    for mid in range(256):
        p, mir, alias = resolve(prg, mid)
        fr = frames(prg, mid)
        d = {'ptr': f"{meta_ptr(prg, mid):04X}", 'frames': [[list(s) for s in f] for f in fr]}
        if alias is not None:
            d['mirror_of_rec'] = f"{p:04X}"
        if fr:
            d['n'], d['mask'] = rd(prg, p), rd(prg, p + 1)
        out[f"{mid:02X}"] = d
    return out


def draw_meta(px, W, ox, oy, spr, chr_rom, regs, rgbpals, tadd, cell):
    """Draw one frame with origin (ox,oy) relative to a cell at px; clipped to the cell."""
    for dx, dy, at, tl in reversed(spr):
        tp = tile_pixels(chr_rom, regs, (tl + tadd) & 0xFF)
        pal = rgbpals[at & 3]
        for y in range(8):
            for x in range(8):
                c = tp[7 - y if at & 0x80 else y][7 - x if at & 0x40 else x]
                if c:
                    X, Y = ox[1] + dx + x, oy[1] + dy + 1 + y
                    if 0 <= X < cell and 0 <= Y < cell:
                        px[(oy[0] + Y) * W + ox[0] + X] = pal[c]


def cmd_sprites(prg, chr_rom, areas, cell=40, maxfr=4):
    import json
    import palette_dump
    outdir = os.path.join(ROOT, "gfx", "sprites")
    os.makedirs(outdir, exist_ok=True)
    js = {'format': {
        'record': 'FF lo hi = mirrored alias; 00 = empty; N M {frames}: N sprites/frame, mask M',
        'frame_select': 'f = ($04E0,X >> 3) & M  (anim counter; frame changes every 8 counts)',
        'sprite': '[dx s8, dy s8 (OAM y = sy+dy, shown +1), attr, tile]; dx=$80 ends a frame',
        'tile': 'final tile = (tile + $0320,X + ($0380 b5 ? $40 : 0)) & $FF; CHR 1KB bank = chr[tile>>6]',
        'chr': '[R2,R3,R4,R5]; R2=$40 player, R3=$41+clamp(sword,1..5) (start: $42), R4/R5 per area',
        'palettes': 'sprite pal 0..3 = ids [$07E4=0, $07E5=sword(1), e6, e7] -> slot (id+$B0)&$FF; colour 0 transparent'},
          'metasprites': meta_json(prg), 'areas': {}}
    index = []
    for area in areas:
        s = area_sprite_set(prg, area)
        if not s:
            continue
        rgbpals = [[palette_dump.rgb(c) for c in p] for p in s['pals']]
        rows = [('player', i, 0) for i in PLAYER_IDS]        # first row: player ids 0-7
        seen = set()
        for o in s['objects']:
            k = (o['meta'], o['tile_add'])
            if o['meta'] is None or k in seen:
                continue
            seen.add(k)
            rows.append((f"t{o['type']:02X}", o['meta'], o['tile_add']))
        cols = 8                                          # entries per row, each maxfr frames wide
        nrow = (len(rows) + cols - 1) // cols
        W, H = cols * maxfr * cell, nrow * cell
        px = [BG] * (W * H)
        for i, (_, mid, tadd) in enumerate(rows):
            cx, cy = (i % cols) * maxfr * cell, (i // cols) * cell
            for x in range(maxfr * cell):
                px[cy * W + cx + x] = GRID
            for y in range(cell):
                px[(cy + y) * W + cx] = GRID
            for f, spr in enumerate(frames(prg, mid)[:maxfr]):
                fx = cx + f * cell
                draw_meta(px, W, (fx, cell // 2), (cy, cell * 3 // 4), spr, chr_rom, s['chr'],
                          rgbpals, tadd, cell)
        fn = os.path.join(outdir, f"area_{area:03d}.png")
        write_png(fn, W, H, px)
        js['areas'][f"{area:02X}"] = {
            'chr': [f"{r:02X}" for r in s['chr']], 'pal_ids': [f"{p:02X}" for p in s['pal_ids']],
            'pal_slots': [f"{p:02X}" for p in s['pal_slots']],
            'pals': [[f"{c:02X}" for c in p] for p in s['pals']],
            'objects': [{k: (f"{v:02X}" if isinstance(v, int) and k not in ('x', 'y', 'handler') else v)
                         for k, v in o.items()} for o in s['objects']],
            'png': os.path.relpath(fn, ROOT)}
        index.append(f"area ${area:02X}  chr {','.join(js['areas'][f'{area:02X}']['chr'])}  "
                     f"pal {','.join(js['areas'][f'{area:02X}']['pal_ids'])}  " +
                     " ".join(f"{n}:{m:02X}" + (f"+{t:02X}" if t else "") for n, m, t in rows[8:]))
    with open(os.path.join(ROOT, "gfx", "sprites.json"), "w") as f:
        json.dump(js, f, indent=1)
    with open(os.path.join(outdir, "index.txt"), "w") as f:
        f.write("; meta_dump.py sprites: per area, entries after the 8 player ids = type:meta[+tile_add]\n")
        f.write("\n".join(index) + "\n")
    print(f"wrote {len(index)} area sheets to {os.path.relpath(outdir, ROOT)}/, gfx/sprites.json, index.txt")
    # stats: object counts, invisible ($4C/empty) objects, and areas whose R4/R5=$FF would be
    # read by a spawned object's initial frames (tests the session-8 "$FF = unused" guess)
    nobj = ninv = 0
    pairs, ff_hits = set(), []
    for ak, a in js['areas'].items():
        regs = [int(r, 16) for r in a['chr']]
        for o in a['objects']:
            nobj += 1
            if o['meta'] is None:
                ninv += 1
                continue
            mid, tadd = int(o['meta'], 16), int(o['tile_add'], 16)
            fr = frames(prg, mid)
            if not fr:
                ninv += 1
                continue
            pairs.add((mid, tadd))
            used = {regs[((t + tadd) & 0xFF) >> 6] for f in fr for _, _, _, t in f}
            if 0xFF in used:
                ff_hits.append(f"{ak}:t{o['type']}:{o['meta']}")
    print(f"objects {nobj}, invisible/empty {ninv}, distinct (meta,tile_add) {len(pairs)}")
    print("objects drawing from a $FF CHR reg: " + (" ".join(sorted(set(ff_hits))) or "none"))


def main(argv):
    if not argv or argv[0] not in ("text", "png", "sprites"):
        print(__doc__)
        return 1
    _, prg, chr_rom = load_rom()
    if argv[0] == "sprites":
        import area_dump
        areas = [int(a, 16) for a in argv[1:]] or range(area_dump.table_len())
        cmd_sprites(prg, chr_rom, areas)
        return 0
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
