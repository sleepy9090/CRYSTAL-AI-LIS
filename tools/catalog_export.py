#!/usr/bin/env python3
"""catalog_export.py [--out-dir web/data]

Writes the data that web/catalog.html loads besides the existing gfx/*.json:
  web/data/chr.bin               raw 128KB CHR ROM (128 x 1KB banks, 2bpp planar NES tiles)
  web/data/catalog_manifest.json lists of rendered PNGs (areas, screens, rects, sprite
                                 sheets) with their index.txt lines, plus the paths of the
                                 JSON sources, all relative to web/.
Serve the project root (`python3 -m http.server` in the repo root) and open
http://localhost:8000/web/catalog.html — the page reads ../gfx/ and data/.
"""
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nesrom import load_rom  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def index_lines(path, rx):
    """map key -> index line for lines matching rx (group 1 = key)."""
    out = {}
    if not os.path.exists(path):
        return out
    with open(path) as f:
        for line in f:
            m = re.match(rx, line)
            if m:
                out[m.group(1).upper()] = line.rstrip('\n')
    return out


def listdir(d, rx):
    p = os.path.join(ROOT, d)
    if not os.path.isdir(p):
        return []
    return sorted(n for n in os.listdir(p) if re.fullmatch(rx, n))


def main():
    out_dir = os.path.join(ROOT, 'web', 'data')
    if '--out-dir' in sys.argv:
        out_dir = sys.argv[sys.argv.index('--out-dir') + 1]
    os.makedirs(out_dir, exist_ok=True)

    _, _, chr_data = load_rom()
    assert len(chr_data) == 128 * 1024, len(chr_data)
    with open(os.path.join(out_dir, 'chr.bin'), 'wb') as f:
        f.write(chr_data)

    # gfx/areas/index.txt: "  3 $03  5x7 $67=80 ..."
    a_idx = index_lines(os.path.join(ROOT, 'gfx/areas/index.txt'), r'\s*\d+ \$([0-9A-F]{2})\s')
    areas = []
    for n in listdir('gfx/areas', r'area_\d{3}\.png'):
        aid = '%02X' % int(n[5:8])
        areas.append({'id': aid, 'png': '../gfx/areas/' + n, 'info': a_idx.get(aid, '')})

    s_idx = index_lines(os.path.join(ROOT, 'gfx/sprites/index.txt'), r'area \$([0-9A-F]{2})\s')
    sheets = []
    for n in listdir('gfx/sprites', r'area_\d{3}\.png'):
        aid = '%02X' % int(n[5:8])
        sheets.append({'id': aid, 'png': '../gfx/sprites/' + n, 'info': s_idx.get(aid, '')})

    screens = ['../gfx/screens/' + n for n in listdir('gfx/screens', r'screen_\d{3}(_b10|_a[0-9A-F]{2}(_v)?)?\.png')]

    r_idx = index_lines(os.path.join(ROOT, 'gfx/rects/index.txt'), r'(screen_[0-9A-F]{2}_[0-9A-F]{4}\.png):')
    rects = [{'png': '../gfx/rects/' + n, 'info': r_idx.get(n.upper(), '')}
             for n in listdir('gfx/rects', r'screen_[0-9A-F]{2}_[0-9A-F]{4}\.png')]

    manifest = {
        'source': 'tools/catalog_export.py (session 27)',
        'chr': 'data/chr.bin',
        'json': {'sprites': '../gfx/sprites.json', 'palettes': '../gfx/palettes.json',
                 'sound': 'data/sound.json', 'text': 'data/text.json'},
        'images': {'chr_all': '../gfx/chr_all.png', 'font': '../gfx/font.png',
                   'palettes': '../gfx/palettes.png'},
        'areas': areas, 'sprite_sheets': sheets, 'screens': screens, 'rects': rects,
    }
    with open(os.path.join(out_dir, 'catalog_manifest.json'), 'w') as f:
        json.dump(manifest, f, indent=1)
    print(f'chr.bin {len(chr_data)} bytes; areas {len(areas)}, sprite sheets {len(sheets)}, '
          f'screens {len(screens)}, rects {len(rects)} -> {out_dir}')


if __name__ == '__main__':
    main()
