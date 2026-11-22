"""Run compare_frames.py over several frame ranges, each with its own shift (session 145).

usage: python3 tools/phase_compare.py A.csv B.csv --cols COLS LO-HI:SHIFT [LO-HI:SHIFT ...]

Useful when a port run is in step with tools.emu in some phases and one frame behind in others (ROM work
that runs in the vblank right after an NMI lands in the same tools.emu row but in the port's next row).
Prints each range's summary; exit 1 if any range differs.
"""
import subprocess
import sys
from pathlib import Path


def main(argv):
    if len(argv) < 4 or '--cols' not in argv:
        print(__doc__)
        return 2
    i = argv.index('--cols')
    cols = argv[i + 1]
    rest = argv[:i] + argv[i + 2:]
    a, b, ranges = rest[0], rest[1], rest[2:]
    tool = Path(__file__).with_name('compare_frames.py')
    bad = 0
    for r in ranges:
        span, shift = r.split(':')
        p = subprocess.run([sys.executable, str(tool), a, b, '--cols', cols, '--frames', span, '--shift', shift],
                           capture_output=True, text=True)
        lines = p.stdout.splitlines()
        start = next((k for k, l in enumerate(lines) if l.startswith('CSV')), len(lines))
        print(f'== {span} shift {shift}: ' + ('ok' if p.returncode == 0 else 'DIFF'))
        for l in lines[start:start + 8]:
            print('   ' + l)
        bad |= p.returncode != 0
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
