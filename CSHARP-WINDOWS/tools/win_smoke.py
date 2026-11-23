"""Window-mode checks of ReC.exe under Wine + Xvfb (TASKS.md T4 / T5, architecture_win.md §3.3).

usage: python3 tools/win_smoke.py [--exe PATH] [--timeout S] [--frames N] [--canon] [--no-smoke] [-- EXTRA_ARGS..]
  smoke    `xvfb-run -a wine ReC.exe --frames N --save out/smoke/smoke.sav --dump-screen out/smoke/win.png` (real
           window, real-time pacing) must exit 0, write the save, and its PNG must equal the headless run's frame N
           (`ReC.exe --headless --frames N --dump-screen out/smoke/headless.png`, same exe).
  --canon  also a windowed `--window --turbo --keys <canon keys> --frames 2247 --compare` run (the --keys script is
           posted as WM_KEYDOWN / WM_KEYUP, the keyboard's path) vs the headless CSV of the same keys (to f2460).
  --exe    default dist/ReC.exe; --timeout per run (default 600); EXTRA_ARGS go to the windowed smoke run.
  --no-xinput  WINEDLLOVERRIDES disables xinput1_4 + xinput9_1_0 (the exe must run keyboard-only, T5)
  --verbose    print the windowed run's [re-c] lines (audio device, pad DLL, underruns)
Prints one line per check and the exe's stderr tail on failures; exit 1 on any failure.
"""
import filecmp
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import core_vs_web as cv  # noqa: E402
from win_vs_web import wine_env, winpath  # noqa: E402

OUT = os.path.join(cv.ROOT, 'out', 'smoke')


EXTRA_ENV = {}


def wine(exe, args, timeout, xvfb):
    cmd = (['xvfb-run', '-a'] if xvfb else []) + ['wine', exe] + args
    try:
        p = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                           env=dict(wine_env(), **EXTRA_ENV), timeout=timeout)
    except subprocess.TimeoutExpired:
        return 124, f'timeout after {timeout} s'
    return p.returncode, p.stdout


def report(name, ok, msg, out=''):
    print(f'{"ok  " if ok else "FAIL"} {name:10s} {msg}', flush=True)
    if not ok and out:
        print('     ' + '\n     '.join(out.strip().splitlines()[-12:]))
    return ok


def smoke(exe, frames, timeout, extra):
    sav, png, hpng = (os.path.join(OUT, n) for n in ('smoke.sav', 'win.png', 'headless.png'))
    for p in (sav, png, hpng):
        if os.path.exists(p):
            os.remove(p)
    rc, out = wine(exe, ['--frames', str(frames), '--save', winpath(sav), '--dump-screen', winpath(png)] + extra,
                   timeout, True)
    if rc or not os.path.exists(png):
        return report('smoke', False, f'windowed run rc={rc}, png {"written" if os.path.exists(png) else "missing"}', out)
    if not os.path.exists(sav) or os.path.getsize(sav) != 0x2000:
        return report('smoke', False, 'save file missing or not 8 KB', out)
    rc, hout = wine(exe, ['--headless', '--frames', str(frames), '--quiet', '--dump-screen', winpath(hpng)], timeout, False)
    if rc:
        return report('smoke', False, f'headless run rc={rc}', hout)
    same = filecmp.cmp(png, hpng, shallow=False)
    if '_REC_VERBOSE' in EXTRA_ENV:
        print('     ' + '\n     '.join(ln for ln in out.splitlines() if ln.startswith('[re-c]')))
    return report('smoke', same, f'{frames} frames windowed: exit 0, save 8 KB, PNG '
                  + ('= headless' if same else '!= headless'), '' if same else out)


def canon(exe, timeout):
    c = cv.CASES['canon']
    keys = c['keys'].split()
    wcsv, hcsv = os.path.join(OUT, 'canon_win.csv'), os.path.join(OUT, 'canon_headless.csv')
    common = ['--frames', str(c['frames']), '--quiet', '--keys'] + keys
    rc, out = wine(exe, ['--window', '--turbo', '--compare', winpath(wcsv)] + common, timeout, True)
    if rc:
        return report('canon', False, f'windowed run rc={rc}', out)
    rc, hout = wine(exe, ['--headless', '--compare', winpath(hcsv)] + common, timeout, False)
    if rc:
        return report('canon', False, f'headless run rc={rc}', hout)
    rc, cmp = cv.run([sys.executable, cv.COMPARE, hcsv, wcsv])
    return report('canon', rc == 0, f'{c["frames"]} frames, windowed --keys CSV '
                  + ('= headless' if rc == 0 else '!= headless: ' + ' | '.join(cmp.strip().splitlines()[:4])))


def main(argv):
    extra = argv[argv.index('--') + 1:] if '--' in argv else []
    argv = argv[:argv.index('--')] if '--' in argv else argv
    exe, timeout, frames, do_canon, do_smoke = os.path.join(cv.ROOT, 'dist', 'ReC.exe'), 600, 600, False, True
    it = iter(argv)
    for a in it:
        if a == '--exe':
            exe = os.path.abspath(next(it))
        elif a == '--timeout':
            timeout = int(next(it))
        elif a == '--frames':
            frames = int(next(it))
        elif a == '--canon':
            do_canon = True
        elif a == '--no-smoke':
            do_smoke = False
        elif a == '--no-xinput':
            EXTRA_ENV['WINEDLLOVERRIDES'] = 'xinput1_4,xinput9_1_0=d'    # both DLLs absent: keyboard only
        elif a == '--verbose':
            EXTRA_ENV['_REC_VERBOSE'] = '1'
        elif a in ('-h', '--help'):
            print(__doc__)
            return 0
        else:
            sys.exit(f'unknown option {a}')
    if not os.path.exists(exe):
        sys.exit(f'{exe} missing')
    os.makedirs(OUT, exist_ok=True)
    ok = True
    if do_smoke:
        ok &= smoke(exe, frames, timeout, extra)
    if do_canon:
        ok &= canon(exe, timeout)
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
