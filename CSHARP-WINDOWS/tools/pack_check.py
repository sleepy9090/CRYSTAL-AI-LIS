"""Check a graphics-pack folder for ReC.exe's Enhanced graphics (T10/T11), the same rules the exe applies.

usage: python3 tools/pack_check.py DIR [--tiles]
  Each *.png must be named TTTT_AABBCC.png (TTTT = tile 0000-1FFF hex, AA BB CC = NES colours 00-3F hex), be 32x32,
  and decode as an 8-bit PNG. Prints one line per problem, then a summary (tiles per colour key);
  --tiles also lists every tile. Exit 1 on any problem (the exe would skip those files).
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pngio import png_decode  # noqa: E402

NAME = re.compile(r'^([0-9A-Fa-f]{4})_([0-9A-Fa-f]{2})([0-9A-Fa-f]{2})([0-9A-Fa-f]{2})\.png$')


def main(argv):
    if not argv or argv[0] in ('-h', '--help'):
        print(__doc__); return 0
    d, listing = argv[0], '--tiles' in argv
    bad, ok, keys = 0, [], {}
    for f in sorted(os.listdir(d)):
        if not f.lower().endswith('.png'):
            continue
        m = NAME.match(f)
        if not m:
            print(f'bad name   {f} (want TTTT_AABBCC.png)'); bad += 1; continue
        t = int(m.group(1), 16)
        cs = [int(m.group(k), 16) for k in (2, 3, 4)]
        if t > 0x1FFF or any(c > 0x3F for c in cs):
            print(f'bad value  {f} (tile <= 1FFF, colours <= 3F)'); bad += 1; continue
        data = open(os.path.join(d, f), 'rb').read()
        try:
            w, h, _ = png_decode(data)
        except (AssertionError, ValueError, KeyError) as e:
            print(f'bad png    {f}: {e or "unsupported (8-bit RGB / RGBA only here)"}'); bad += 1; continue
        if (w, h) != (32, 32):
            print(f'bad size   {f}: {w}x{h}, want 32x32'); bad += 1; continue
        ok.append(f)
        keys[m.group(2) + m.group(3) + m.group(4)] = keys.get(m.group(2) + m.group(3) + m.group(4), 0) + 1
        if listing:
            print(f'ok         {f}')
    print(f'{d}: {len(ok)} tiles ok, {bad} bad; colour keys: ' + ', '.join(f'{k} x{n}' for k, n in sorted(keys.items())))
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
