#!/usr/bin/env python3
"""Run tools.emu and the web port from the same exported state and diff them (session 88).

The state pair comes from `python3 -m tools.emu.web_state ... --out J.json --save-state S.state`
(tools.emu .state + the port's JSON, taken at the first instruction of a ported routine).
Both run N frames; per-frame CSV rows (tools/emu/compare.py columns) are diffed with
compare_frames.py, and the listed frames are pixel-diffed (emu shot f = the frame whose visible lines
follow frame f's main-loop work, as in tools/web_compare.py), printing per-tile-row mismatch counts.

usage (repo root):
  python3 tools/web_state_compare.py --state /tmp/death.state --web web/test/states/death.json \
      --frames 560 --shots 2470,2600,2880 [--cols core,obj] [--out-dir /tmp/wsc] [--diff]
"""
import argparse
import json
import os
import subprocess
import sys

import web_compare as wc   # noqa: E402  (sets up sys.path for tools.emu + render_compare)
from tools.emu.nes import NES  # noqa: E402
from tools.emu import state, compare  # noqa: E402
from tools.emu.png import write_rgb  # noqa: E402
from render_compare import read_png  # noqa: E402


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--state', required=True)
    ap.add_argument('--web', required=True)
    ap.add_argument('--frames', type=int, required=True)
    ap.add_argument('--shots', default='')
    ap.add_argument('--cols', default='core,pal,obj,oam_crc')
    ap.add_argument('--out-dir', default='/tmp/web_state_compare')
    ap.add_argument('--diff', action='store_true', help='write emu|web|mask triples for differing shots')
    a = ap.parse_args(argv)
    os.makedirs(a.out_dir, exist_ok=True)
    f0 = json.load(open(a.web))['frame']
    shots = {int(s) for s in a.shots.split(',') if s}

    nes = NES()
    state.load(nes, a.state)
    rows, eshots = [','.join(compare.columns())], {}

    def hook(n):
        f = n.ppu.frame_no - 1
        if f >= f0:
            rows.append(','.join(str(v) for v in compare.row(n)))
        if f in shots:
            eshots[f] = b''.join(wc.LUT[i & 0x3F] for i in n.ppu.fb)
    nes.on_frame(hook)
    nes.run_frames(a.frames)
    # rows written at line 240 of frame f+1 hold frame f's work; also add the final frame from RAM now
    ecsv = os.path.join(a.out_dir, 'emu.csv')
    open(ecsv, 'w').write('\n'.join(rows) + '\n')

    wcsv = os.path.join(a.out_dir, 'web.csv')
    cmd = ['node', os.path.join(wc.ROOT, 'web', 'test', 'run.mjs'), '--load-state', a.web,
           '--frames', str(a.frames), '--quiet', '--compare', wcsv]
    if shots:
        cmd += ['--shots', ','.join(map(str, sorted(shots))), a.out_dir]
    p = subprocess.run(cmd, capture_output=True, text=True)
    if p.returncode:
        sys.exit(f'run.mjs failed:\n{p.stderr}')
    print(p.stderr.strip().splitlines()[-1])

    bad_any = subprocess.run([sys.executable, os.path.join(wc.TOOLS, 'compare_frames.py'), ecsv, wcsv,
                              '--cols', a.cols]).returncode != 0
    for f in sorted(shots):
        if f not in eshots:
            print(f'shot {f}: not reached in tools.emu')
            bad_any = True
            continue
        img = read_png(os.path.join(a.out_dir, f'web_{f:04d}.png'))
        web = b''.join(bytes(px) for px in img[2])
        bad, trows = wc.pixel_diff(eshots[f], web)
        print(f'shot {f}: {len(bad)} px differ' + (f', tile rows {dict(sorted(trows.items()))}' if bad else ''))
        if bad:
            bad_any = True
            if a.diff:
                wc.triple(os.path.join(a.out_dir, f'pair_{f:04d}.png'), [eshots[f], web], bad)
        write_rgb(os.path.join(a.out_dir, f'emu_{f:04d}.png'), 256, 240, eshots[f])
    return 1 if bad_any else 0


if __name__ == '__main__':
    sys.exit(main())
