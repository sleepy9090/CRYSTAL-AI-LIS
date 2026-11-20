#!/usr/bin/env python3
"""Run tools.emu and the web port from the same exported state and diff them (session 88).

The state pair comes from `python3 -m tools.emu.web_state ... --out J.json --save-state S.state`
(tools.emu .state + the port's JSON, taken at the first instruction of a ported routine).
Both run N frames; per-frame CSV rows (tools/emu/compare.py columns) are diffed with
compare_frames.py, and the listed frames are pixel-diffed (emu shot f = the frame whose visible lines
follow frame f's main-loop work, as in tools/web_compare.py), printing per-tile-row mismatch counts.

usage (repo root):
  python3 tools/web_state_compare.py --state /tmp/death.state --web web/test/states/death.json \
      --frames 560 --shots 2470,2600-2610 [--frame0 N] [--cols core,obj] [--out-dir /tmp/wsc] [--diff]

--frame0: defaults to the JSON's `frame0` (web_state.py, s89): a state taken in vblank after the NMI
(line > 240) belongs to the NEXT frame's main-loop pass (death.json: frame 2460 line 254 -> 2461).
Pipe the shot lines through tools/shot_filter.py --ignore 23-29 to skip the HUD (unported IRQ split).
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


def shift_key(spec, n):
    """'F:BTN[:N]' -> frame F + n (an @FILE spec is passed as is)"""
    if not n or spec.startswith('@'):
        return spec
    f, _, rest = spec.partition(':')
    return f'{int(f) + n}:{rest}'


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--state', required=True)
    ap.add_argument('--web', required=True)
    ap.add_argument('--frames', type=int, required=True)
    ap.add_argument('--shots', default='')
    ap.add_argument('--cols', default='core,pal,obj,oam_crc')
    ap.add_argument('--out-dir', default='/tmp/web_state_compare')
    ap.add_argument('--diff', action='store_true', help='write emu|web|mask triples for differing shots')
    ap.add_argument('--frame0', type=int, help="port's first frame number (default: the JSON's `frame`)")
    ap.add_argument('--keys', nargs='+', default=[], metavar='F:BTN[:N]',
                    help='s94: pad 1 keys (absolute frame numbers) given to both runs (run.mjs --keys)')
    ap.add_argument('--web-key-shift', type=int, default=0, metavar='N',
                    help='s98: add N to the frame of every key given to the port. tools.emu sets frame F\'s '
                         'pad state at line 0 of F, so a routine that reads the pad in vblank (line > 240, '
                         'e.g. the bank 16 menu loop after WaitFrame) sees it in the port\'s frame F+1: use 1')
    ap.add_argument('--ram-diff', action='store_true',
                    help='s98: also diff the whole RAM + WRAM after the last frame (run.mjs --dump-ram/--dump-wram)')
    ap.add_argument('--ram-ignore', default='', metavar='LO-HI,..',
                    help='hex CPU address ranges left out of --ram-diff (e.g. 0100-01FF stack, 4000 APU mirrors)')
    a = ap.parse_args(argv)
    os.makedirs(a.out_dir, exist_ok=True)
    js = json.load(open(a.web))
    f0 = js['frame']
    if a.frame0 is None:
        a.frame0 = js.get('frame0', f0)
    shots = set()                                    # F,F,.. and/or LO-HI ranges
    for s in filter(None, a.shots.split(',')):
        lo, _, hi = s.partition('-')
        shots.update(range(int(lo), int(hi or lo) + 1))

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
    if a.keys:
        from tools.emu import debug
        nes.keys = debug.parse_keys(a.keys)
    nes.run_frames(a.frames)
    # rows written at line 240 of frame f+1 hold frame f's work; also add the final frame from RAM now
    ecsv = os.path.join(a.out_dir, 'emu.csv')
    open(ecsv, 'w').write('\n'.join(rows) + '\n')

    wcsv = os.path.join(a.out_dir, 'web.csv')
    cmd = ['node', os.path.join(wc.ROOT, 'web', 'test', 'run.mjs'), '--load-state', a.web,
           '--frames', str(a.frames - (a.frame0 - f0)), '--frame0', str(a.frame0), '--quiet', '--compare', wcsv]
    if a.keys:
        cmd += ['--keys'] + [shift_key(k, a.web_key_shift) for k in a.keys]
    if shots:
        cmd += ['--shots', ','.join(map(str, sorted(shots))), a.out_dir]
    if a.ram_diff:
        cmd += ['--dump-ram', os.path.join(a.out_dir, 'web_ram.bin'),
                '--dump-wram', os.path.join(a.out_dir, 'web_wram.bin')]
    p = subprocess.run(cmd, capture_output=True, text=True)
    if p.returncode:
        sys.exit(f'run.mjs failed:\n{p.stderr}')
    print(p.stderr.strip().splitlines()[-1])
    if a.ram_diff:
        ign = set()
        for s in filter(None, a.ram_ignore.split(',')):
            lo, _, hi = s.partition('-')
            ign.update(range(int(lo, 16), int(hi or lo, 16) + 1))
        n_bad = 0
        for name, base, emu_mem in (('ram', 0, nes.bus.ram), ('wram', 0x6000, nes.bus.wram)):
            web_mem = open(os.path.join(a.out_dir, f'web_{name}.bin'), 'rb').read()
            diffs = [base + i for i in range(len(web_mem))
                     if emu_mem[i] != web_mem[i] and base + i not in ign]
            n_bad += len(diffs)
            for ad in diffs[:40]:
                print(f'{name} ${ad:04X}: emu {emu_mem[ad - base]:02X} web {web_mem[ad - base]:02X}')
        print(f'RAM+WRAM at the end: {n_bad} bytes differ (ignored: {a.ram_ignore or "none"})')

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
