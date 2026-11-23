"""Oracle diff of the shipped binary: dist/ReC.exe (headless mode, under Wine) vs the web port (node ../web/test/run.mjs).

usage: python3 tools/win_vs_web.py [--canon] [--states] [--checks] [--frames N] [--only KINDS] [--keep]
                                   [--exe PATH] [--timeout S] [NAME[:FRAMES] ..]
  Cases, options and the comparison are tools/core_vs_web.py's (same CASES, same out files); the C# side is
  `wine dist/ReC.exe --headless <run.mjs options>` with paths as Z:\\... Wine paths.
  --exe PATH     exe to run (default dist/ReC.exe; build it with
                 `dotnet publish src/ReC.Win -c Release -r win-x64 -o dist`)
  --timeout S    per exe run (default 900)
Wine: WINEPREFIX=<this folder>/.wine, WINEDEBUG=-all (architecture_win.md §3). Output in out/win/<case>/.
One line per case; exit 1 on any difference.
"""
import glob
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import core_vs_web as cv  # noqa: E402

EXE = os.path.join(cv.ROOT, 'dist', 'ReC.exe')
OUT = os.path.join(cv.OUT, 'win')


def wine_env():
    env = dict(os.environ)
    env['WINEPREFIX'] = os.path.join(cv.ROOT, '.wine')
    env['WINEDEBUG'] = '-all'
    return env


def winpath(p):
    """absolute Unix path -> Wine's Z: drive path"""
    return 'Z:' + os.path.abspath(p).replace('/', '\\')


def exe_cmd(exe):
    def cmd(c, d):
        a = cv.port_args(c, d, 'cs')
        a = [winpath(x) if x.startswith('/') else x for x in a]
        return ['wine', exe, '--headless'] + a
    return cmd


def make_runner(timeout):
    def run(cmd):
        try:
            p = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                               env=wine_env() if cmd[0] == 'wine' else None, timeout=timeout)
        except subprocess.TimeoutExpired:
            return 124, f'timeout after {timeout} s: {" ".join(cmd[:3])}'
        return p.returncode, p.stdout
    return run


def main(argv):
    keep = '--keep' in argv
    names, frames, only, exe, timeout = [], None, cv.KINDS, EXE, 900
    it = iter(argv)
    for a in it:
        if a == '--only':
            only = tuple(next(it).split(','))
            if any(k not in cv.KINDS for k in only):
                sys.exit(f'--only takes {",".join(cv.KINDS)}')
        elif a == '--canon':
            names.append('canon')
        elif a == '--states':
            names += sorted(os.path.basename(p)[:-5] for p in glob.glob(os.path.join(cv.STATES, '*.json')))
        elif a == '--checks':
            names += [n for n, c in cv.CASES.items() if c.get('state') and 'last' in c]
        elif a == '--frames':
            frames = int(next(it))
        elif a == '--exe':
            exe = os.path.abspath(next(it))
        elif a == '--timeout':
            timeout = int(next(it))
        elif a == '--keep':
            pass
        elif a in ('-h', '--help'):
            print(__doc__)
            return 0
        elif a.startswith('--'):
            sys.exit(f'unknown option {a}')
        else:
            names.append(a)
    if not names:
        print(__doc__)
        return 2
    if not os.path.exists(exe):
        sys.exit(f'{exe} missing: dotnet publish src/ReC.Win -c Release -r win-x64 -o dist')
    cv.run = make_runner(timeout)      # compare_case's runs (web + exe) get the timeout and the Wine env
    fails = 0
    for n in names:
        if frames is not None and ':' not in n:
            n = f'{n}:{frames}'
        name, c = cv.case_for(n)
        ok, msg = cv.compare_case(name, c, keep, only, cs_cmd=exe_cmd(exe), out_dir=OUT)
        fails += not ok
        print(f'{"ok  " if ok else "FAIL"} {name:24s} {msg}', flush=True)
    print(f'{len(names) - fails}/{len(names)} cases identical (dist exe under Wine)')
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
