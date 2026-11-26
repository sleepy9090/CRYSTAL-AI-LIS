"""Run dist/ReC.exe under Wine + Xvfb with any arguments and a timeout; print the exit code and the last output lines.

usage: python3 tools/win_run.py [--timeout S] [--lines N] -- EXE_ARGS..
  e.g. python3 tools/win_run.py --timeout 1500 -- --frames 2300 --turbo --no-audio --menu graphics:hd,widescreen:16:9
       --keys 1250:START 1400:START 1600:START 1700:START 2300:START
Exit code: the exe's (124 on timeout).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import core_vs_web as cv  # noqa: E402
from win_video_check import run  # noqa: E402


def main(argv):
    timeout, lines = 900, 15
    if '--' not in argv:
        print(__doc__); return 2
    i = argv.index('--')
    opts, args = argv[:i], argv[i + 1:]
    if '--timeout' in opts:
        timeout = int(opts[opts.index('--timeout') + 1])
    if '--lines' in opts:
        lines = int(opts[opts.index('--lines') + 1])
    exe = os.path.join(cv.ROOT, 'dist', 'ReC.exe')
    rc, out = run(exe, args, timeout)
    print('\n'.join(out.strip().splitlines()[-lines:]))
    print(f'exit code {rc}')
    return rc


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
