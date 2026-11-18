#!/usr/bin/env python3
"""Ph7 web 8/8: web port vs tools.emu over the canonical --keys run (session 58).

usage: python3 tools/web_compare.py [--frames F,F,...] [--last N]

Runs tools.emu from power-on with compare_ref.CANON_KEYS up to the last frame (default: the 12
FCEUX reference frames in ref/), and `node web/test/run.mjs` with the same keys (--frame0 214,
so the frame numbers line up, CLAUDE.md "Web port tests"). For each frame:
  - full 256x240 pixel compare (both sides use palette_dump.NES_RGB, so RGB equality is exact),
    gfx/compare/web_NNNN.png = tools.emu | web port | mismatch mask (red) (the port's own shots are in
    gfx/compare/web/); frames before f214 have no port frame (the port skips the cold-boot intro);
  - the tools/emu/compare.py CSV columns at that frame (state/sub/area/pos/cam/music, snd, pal_crc,
    object slots).
Over the whole run (f214..last): the first frame each column group differs and how many frames differ.
Writes gfx/compare/web.json (read by `tools/comparison.py report` for the "Web port" column) and
gfx/compare/web_emu.csv / web_web.csv. Exit 1 if any listed frame differs in pixels.
"""
import os
import re
import subprocess
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
sys.path[:] = [p for p in sys.path if os.path.abspath(p or '.') != _HERE]   # tools/dis.py shadows stdlib dis
import json  # noqa: E402
import dis  # noqa: F401,E402
import inspect  # noqa: F401,E402

TOOLS = _HERE
ROOT = os.path.dirname(TOOLS)
sys.path.insert(0, TOOLS)
sys.path.insert(0, os.path.join(TOOLS, 'ref'))
import compare_ref  # noqa: E402
import compare_frames  # noqa: E402
from tools.emu.nes import NES  # noqa: E402
from tools.emu import debug, compare  # noqa: E402
from tools.emu.png import write_rgb  # noqa: E402
from render_compare import read_png  # noqa: E402
from palette_dump import NES_RGB  # noqa: E402

OUT = os.path.join(ROOT, 'gfx', 'compare')
LUT = [bytes(((c >> 16) & 255, (c >> 8) & 255, c & 255)) for c in NES_RGB]
FRAME0 = 214
W, H = 256, 240


def ref_frames():
    return sorted(int(m.group(1)) for p in os.listdir(os.path.join(ROOT, 'ref'))
                  for m in [re.match(r'frame_(\d+)\.png$', p)] if m)


def run_emu(frames, last, csv_path):
    nes = NES()
    nes.keys = debug.parse_keys([compare_ref.CANON_KEYS])
    shots, rows = {}, [','.join(compare.columns())]

    def hook(n):
        f = n.ppu.frame_no - 1
        if f >= FRAME0:
            rows.append(','.join(str(v) for v in compare.row(n)))
        if f in frames:
            shots[f] = b''.join(LUT[i & 0x3F] for i in n.ppu.fb)
    nes.on_frame(hook)
    nes.run_frames(last + 1)
    open(csv_path, 'w').write('\n'.join(rows) + '\n')
    return shots


def run_web(frames, last, csv_path, shot_dir):
    cmd = ['node', os.path.join(ROOT, 'web', 'test', 'run.mjs'), '--frames', str(last - FRAME0 + 1),
           '--frame0', str(FRAME0), '--quiet', '--compare', csv_path,
           '--shots', ','.join(map(str, sorted(frames))), shot_dir, '--keys'] + compare_ref.CANON_KEYS.split()
    p = subprocess.run(cmd, capture_output=True, text=True)
    if p.returncode:
        raise SystemExit(f'run.mjs failed:\n{p.stderr}')
    print(p.stderr.strip().splitlines()[-1], file=sys.stderr)
    shots = {}
    for f in frames:
        img = read_png(os.path.join(shot_dir, f'web_{f:04d}.png'))
        shots[f] = b''.join(bytes(px) for px in img[2])
    return shots


def pixel_diff(a, b):
    bad = [i for i in range(W * H) if a[i * 3:i * 3 + 3] != b[i * 3:i * 3 + 3]]
    rows = {}
    for i in bad:
        rows[i // W // 8] = rows.get(i // W // 8, 0) + 1
    return bad, rows


def triple(path, imgs, bad):
    mask = bytearray(c // 3 for c in imgs[0])
    for i in bad:
        mask[i * 3:i * 3 + 3] = b'\xff\x20\x20'
    imgs = imgs + [bytes(mask)]
    gap = b'\x40\x40\x40' * 4
    out = b''.join(gap.join(im[y * W * 3:(y + 1) * W * 3] for im in imgs) for y in range(H))
    write_rgb(path, W * 3 + 8, H, out)


def csv_groups(pe, pw):
    ce, re_ = compare_frames.read_csv(pe)
    cw, rw = compare_frames.read_csv(pw)
    assert ce == cw, 'CSV columns differ'
    idx = {c: i for i, c in enumerate(ce)}
    groups = dict(compare_frames.GROUPS)
    groups['obj'] = [c for c in ce if re.match(r'o[0-9A-F]{2}_', c)]
    out = {}
    common = sorted(set(re_) & set(rw))
    for g, cols in groups.items():
        diff = [f for f in common if any(re_[f][idx[c]] != rw[f][idx[c]] for c in cols)]
        first = None
        if diff:
            f = diff[0]
            first = {'frame': f, 'cols': {c: [re_[f][idx[c]], rw[f][idx[c]]] for c in cols
                                          if re_[f][idx[c]] != rw[f][idx[c]]}}
        out[g] = {'n_diff': len(diff), 'of': len(common), 'first': first,
                  'ranges': compare_frames.ranges(diff)}
    return out, ce, re_, rw


def main(argv):
    frames = ref_frames()
    if '--frames' in argv:
        frames = sorted(int(x) for x in argv[argv.index('--frames') + 1].split(','))
    last = max(frames)
    if '--last' in argv:
        last = max(last, int(argv[argv.index('--last') + 1]))
    os.makedirs(OUT, exist_ok=True)
    pe, pw = os.path.join(OUT, 'web_emu.csv'), os.path.join(OUT, 'web_web.csv')
    emu = run_emu(set(frames), last, pe)
    raw = os.path.join(OUT, 'web')          # the port's own shots (the pairs go to OUT/web_NNNN.png)
    os.makedirs(raw, exist_ok=True)
    web = run_web([f for f in frames if f >= FRAME0], last, pw, raw)
    groups, cols, re_, rw = csv_groups(pe, pw)
    idx = {c: i for i, c in enumerate(cols)}
    res = {'frames': [], 'groups': groups, 'last': last}
    fail = 0
    for f in frames:
        if f not in web:
            res['frames'].append({'frame': f, 'bad': None, 'note': f'before f{FRAME0}: the port skips the '
                                  'cold-boot intro (bank 18), so it has no frame here'})
            print(f'f{f}: n/a (port starts at f{FRAME0})')
            continue
        bad, trows = pixel_diff(emu[f], web[f])
        path = os.path.join(OUT, f'web_{f:04d}.png')
        triple(path, [emu[f], web[f]], bad)
        e, w = re_.get(f), rw.get(f)
        st = {k: [e[idx[k]], w[idx[k]]] for k in ('state', 'sub', 'area', 'music', 'pal_crc')} if e and w else {}
        res['frames'].append({'frame': f, 'bad': len(bad), 'pct': 100.0 * len(bad) / (W * H),
                              'tile_rows': trows, 'img': os.path.relpath(path, ROOT), 'state': st})
        fail |= bool(bad)
        tr = ' '.join(f'{k}:{v}' for k, v in sorted(trows.items()))
        print(f'f{f}: {len(bad)} px ({100.0 * len(bad) / (W * H):.2f} %) emu/web state '
              + ' '.join(f'{k}={v[0]}/{v[1]}' for k, v in st.items() if k != 'pal_crc')
              + (f' pal {"=" if st and st["pal_crc"][0] == st["pal_crc"][1] else "!="}')
              + (f' rows {tr}' if tr else ''))
    for g, r in groups.items():
        print(f'{g}: {r["n_diff"]}/{r["of"]} frames differ; first '
              + (f'f{r["first"]["frame"]} {r["first"]["cols"]}' if r['first'] else '-'))
    json.dump(res, open(os.path.join(OUT, 'web.json'), 'w'), indent=0)
    return 1 if fail else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
