#!/usr/bin/env python3
"""Render every palette record of re-c (PaletteLoadOne 26:8C6E) to gfx/palettes.png + gfx/palettes.json.

usage: palette_dump.py [--out gfx/palettes] [--scale N]

Record slot s (0..255): bank 2 + (s>>7), CPU $A000 + ((s>>2)&$1F)*$100 + $F0 + (s&3)*4, 4 bytes.
  BG palette id p    -> slot p
  sprite palette id p -> slot (p + $B0) & $FF   (CLC/ADC #$B0 at 26:8C72, 8-bit wrap)
Colour byte $FF = "use backdrop" (copy of $6140 = colour 0 of BG palette 0, 26:8CA5).
Colour bytes are NES PPU colour indices $00-$3F.

Master palette: the widely used 2C02 RGB approximation (the 7C7C7C/0000FC/... table shipped
by many emulators and shown on the NESdev wiki "PPU palettes" page). It is an approximation;
real 2C02 output depends on the decoder.

PNG: 16 rows x 16 slots; each slot = 4 swatches (s=row*16+col). $FF swatches are drawn as a
grey/white checker. Left gutter colour bar: green = slot referenced as an area BG id (k1),
magenta = referenced as an area sprite id ($07E6/$07E7), both = yellow.
"""
import json, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom, ROOT  # noqa
from meta_dump import write_png  # noqa
import area_dump  # noqa

NES_RGB = [int(h, 16) for h in (
    '7C7C7C 0000FC 0000BC 4428BC 940084 A80020 A81000 881400 503000 007800 006800 005800 004058 000000 000000 000000 '
    'BCBCBC 0078F8 0058F8 6844FC D800CC E40058 F83800 E45C10 AC7C00 00B800 00A800 00A844 008888 000000 000000 000000 '
    'F8F8F8 3CBCFC 6888FC 9878F8 F878F8 F85898 F87858 FCA044 F8B800 B8F818 58D854 58F898 00E8D8 787878 000000 000000 '
    'FCFCFC A4E4FC B8B8F8 D8B8F8 F8B8F8 F8A4C0 F0D0B0 FCE0A8 F8D878 D8F878 B8F8B8 B8F8D8 00FCFC F8D8F8 000000 000000'
).split()]


def rgb(c):
    v = NES_RGB[c & 0x3F]
    return (v >> 16, (v >> 8) & 255, v & 255)


def slot_addr(s):
    return 2 + (s >> 7), 0xA000 + ((s >> 2) & 0x1F) * 0x100 + 0xF0 + (s & 3) * 4


def bg_slot(p):
    return p & 0xFF


def spr_slot(p):
    return (p + 0xB0) & 0xFF


def read_slots(prg):
    out = []
    for s in range(256):
        b, a = slot_addr(s)
        o = b * 0x2000 + (a & 0x1FFF)
        out.append(list(prg[o:o + 4]))
    return out


def area_refs():
    """slot -> {'bg': [areas], 'spr': [areas]} from area records + sub-table k1."""
    refs = {}
    for area in range(area_dump.table_len()):
        try:
            d = area_dump.decode(area)
            _, ps = area_dump.sub_header(area)
            k1 = [area_dump.rd10(ps[1] + i) for i in range(3)]
        except Exception:
            continue
        if not d:
            continue
        for p in k1 + [0x7F]:
            refs.setdefault(bg_slot(p), {'bg': [], 'spr': []})['bg'].append(area)
        for p in (d['e6'], d['e7']):
            refs.setdefault(spr_slot(p), {'bg': [], 'spr': []})['spr'].append(area)
    for r in refs.values():
        r['bg'] = sorted(set(r['bg'])); r['spr'] = sorted(set(r['spr']))
    return refs


def main(argv):
    out = os.path.join(ROOT, 'gfx', 'palettes'); scale = 12
    if '--out' in argv:
        i = argv.index('--out'); out = argv[i + 1]; del argv[i:i + 2]
    if '--scale' in argv:
        i = argv.index('--scale'); scale = int(argv[i + 1]); del argv[i:i + 2]
    prg = load_rom()[1]
    slots = read_slots(prg)
    refs = area_refs()
    bad = [(s, c) for s, rec in enumerate(slots) for c in rec if c != 0xFF and c > 0x3F]

    sw = scale; gut = 4; gap = 3
    cw = gut + 4 * sw + gap
    W, H = 16 * cw + gap, 16 * (sw + gap) + gap
    px = [(32, 32, 32)] * (W * H)
    for s, rec in enumerate(slots):
        ox = gap + (s & 15) * cw; oy = gap + (s >> 4) * (sw + gap)
        r = refs.get(s, {'bg': [], 'spr': []})
        gc = {(1, 0): (0, 200, 0), (0, 1): (220, 0, 220), (1, 1): (240, 220, 0)}.get(
            (int(bool(r['bg'])), int(bool(r['spr']))), (32, 32, 32))
        for y in range(sw):
            for x in range(gut - 1):
                px[(oy + y) * W + ox + x] = gc
        for i, c in enumerate(rec):
            x0 = ox + gut + i * sw
            for y in range(sw):
                for x in range(sw):
                    if c == 0xFF:
                        col = (200, 200, 200) if ((x // 3) + (y // 3)) & 1 else (255, 255, 255)
                    else:
                        col = rgb(c)
                    px[(oy + y) * W + x0 + x] = col
    write_png(out + '.png', W, H, px)

    js = {
        'format': 'slot s: bank 2+(s>>7), $A000+((s>>2)&$1F)*$100+$F0+(s&3)*4; BG id p -> slot p; '
                  'sprite id p -> slot (p+$B0)&$FF; colour $FF = backdrop ($6140)',
        'master_palette': ['%06X' % v for v in NES_RGB],
        'slots': [{'slot': s, 'bank': slot_addr(s)[0], 'addr': '%04X' % slot_addr(s)[1],
                   'colors': rec,
                   'bg_areas': refs.get(s, {}).get('bg', []),
                   'spr_areas': refs.get(s, {}).get('spr', [])} for s, rec in enumerate(slots)],
    }
    with open(out + '.json', 'w') as f:
        json.dump(js, f, indent=1)
    nff = sum(rec.count(0xFF) for rec in slots)
    used = sorted(refs)
    print(f'wrote {out}.png ({W}x{H}) + {out}.json')
    print(f'slots referenced by areas: {len(used)} '
          f'(bg {sum(1 for s in used if refs[s]["bg"])}, spr {sum(1 for s in used if refs[s]["spr"])}); '
          f'$FF colour bytes: {nff}; out-of-range bytes: {len(bad)} {bad[:8]}')
    allff = [s for s, rec in enumerate(slots) if rec == [0xFF] * 4]
    print(f'all-$FF slots: {len(allff)}  first: {" ".join("%02X" % s for s in allff[:24])}')
    first = [rec[0] for rec in slots if rec[0] != 0xFF]
    print('colour-0 byte values (non-FF):', ' '.join('%02X' % v for v in sorted(set(first))))


if __name__ == '__main__':
    main(sys.argv[1:])
