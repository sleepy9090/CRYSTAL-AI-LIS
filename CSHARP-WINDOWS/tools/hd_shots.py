"""Screenshots of one game state in several graphics modes from dist/ReC.exe under Wine (for HD look reviews).

usage: python3 tools/hd_shots.py STATE FRAMES OUTDIR [MODE ..] [--keys K ..]
  MODE = a --menu list, e.g. graphics:32bit,size:4x  graphics:hd,size:4x  graphics:hd+fullscreen (default: original,
  32-bit and HD at 4x); writes OUTDIR/<n>_<mode>.png (window picture) and prints the exe's HD line.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import core_vs_web as cv  # noqa: E402
from win_vs_web import winpath  # noqa: E402
from win_video_check import run  # noqa: E402


def main(argv):
    if len(argv) < 3:
        print(__doc__); return 1
    keys = []
    if '--keys' in argv:
        i = argv.index('--keys'); keys = argv[i + 1:]; argv = argv[:i]
    state, frames, out = argv[0], argv[1], argv[2]
    modes = argv[3:] or ['graphics:original,size:4x', 'graphics:32bit,size:4x', 'graphics:hd,size:4x']
    os.makedirs(out, exist_ok=True)
    exe = os.path.join(cv.ROOT, 'dist', 'ReC.exe')
    sp = os.path.join(cv.ROOT, '..', 'web', 'test', 'states', state + '.json')
    for n, mode in enumerate(modes):
        ini = os.path.join(out, 'shots.ini')
        if os.path.exists(ini):
            os.remove(ini)
        menu, fs = mode.replace('+fullscreen', ''), mode.endswith('+fullscreen')
        png = os.path.join(out, f'{n}_{menu.split(",")[0].split(":")[-1]}{"_fs" if fs else ""}.png')
        rc, text = run(exe, ['--load-state', winpath(sp), '--frames', frames, '--turbo', '--no-audio',
                             '--video-config', winpath(ini), '--save', winpath(os.path.join(out, 'shots.sav')),
                             '--menu', menu, '--dump-window', winpath(png)] + (['--fullscreen'] if fs else []) +
                       (['--keys'] + keys if keys else []), 600)
        hd = next((l for l in text.splitlines() if 'HD graphics' in l), '')
        print(f'{"ok  " if rc == 0 and os.path.exists(png) else "FAIL"} {png} {hd}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
