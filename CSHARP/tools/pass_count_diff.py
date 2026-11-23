"""Lag-model input diff: per frame the stage buckets (rd/wr/rom per m.PassStage, colc, irq_mode, per-slot dispatches)
of the C# port (`ReC.Run --pass-count`) vs the web port (`node ../web/test/pass_count.mjs --stages`), same state + keys.

usage: python3 tools/pass_count_diff.py [--no-build] [--checks] [NAME[:FRAMES] ..]
  NAME     a tools/cs_vs_web.py case with a state (CASES entry or ../web/test/states stem), :FRAMES = own length
  --checks every cs_vs_web.py CASES entry taken from a *_check.mjs
Both ports must count the same Machine rd / wr / cpuRd calls per stage, or the lag model (lagmodel.js) judges a
pass differently long before the CSV shows it. Prints the first differing frame (both lines, the differing tokens)
per case; exit 1 on any difference. Files: out/<case>/pc_web.txt, pc_cs.txt.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cs_vs_web as cv  # noqa: E402

PASS_COUNT = os.path.join(cv.REPO, 'web', 'test', 'pass_count.mjs')


def tokens(line):
    return line.split()


def diff_case(name, c):
    if not c.get('state'):
        return False, 'needs a state (pass_count.mjs loads one)'
    d = os.path.join(cv.OUT, name)
    os.makedirs(d, exist_ok=True)
    f0 = c.get('frame0', cv.state_frame0(c['state']))
    last = f0 + c['frames'] - 1
    st = os.path.join(cv.STATES, c['state'] + '.json')
    keys = c['keys'].split() if c.get('keys') else []
    web, cs = os.path.join(d, 'pc_web.txt'), os.path.join(d, 'pc_cs.txt')
    rc, out = cv.run(['node', PASS_COUNT, st, str(last), '--stages', '--out', web] + keys)
    if rc:
        return False, f'web pass_count failed: {out.strip()[-300:]}'
    a = ['--load-state', st, '--frames', str(c['frames']), '--quiet', '--pass-count', cs]
    if 'frame0' in c:
        a += ['--frame0', str(c['frame0'])]
    if keys:
        a += ['--keys'] + keys
    rc, out = cv.run([cv.CS_RUN] + a)
    if rc:
        return False, f'C# run failed: {out.strip()[-300:]}'
    wl = open(web).read().splitlines()
    cl = open(cs).read().splitlines()
    for i, (x, y) in enumerate(zip(wl, cl)):
        if x != y:
            tx, ty = tokens(x), tokens(y)
            dif = [f'{p}->{q}' for p, q in zip(tx, ty) if p != q]
            if len(tx) != len(ty):
                dif.append(f'{len(tx)}->{len(ty)} tokens')
            return False, f'line {i + 1} ({tx[0] if tx else "?"}): ' + ' '.join(dif[:8])
    if len(wl) != len(cl):
        return False, f'{len(wl)} web lines vs {len(cl)} C# lines'
    lags = sum(1 for ln in wl if ln.startswith('# LAG'))
    return True, f'{len(wl) - lags} frames identical ({lags} lag-model frames)'


def main(argv):
    names = []
    for a in argv:
        if a == '--checks':
            names += [n for n, c in cv.CASES.items() if c.get('state') and 'last' in c]
        elif a == '--no-build':
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
    if '--no-build' not in argv:
        rc, out = cv.run(['dotnet', 'build', os.path.join(cv.ROOT, 'ReC.slnx'), '-c', 'Release', '-v', 'q', '-nologo'])
        if rc:
            print(out)
            return 1
    fails = 0
    for n in names:
        name, c = cv.case_for(n)
        ok, msg = diff_case(name, c)
        fails += not ok
        print(f'{"ok  " if ok else "FAIL"} {name:24s} {msg}', flush=True)
    print(f'{len(names) - fails}/{len(names)} cases identical')
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
