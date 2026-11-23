"""Oracle diff: the C# port (src/ReC.Run) vs the web port (node ../web/test/run.mjs), same start + same keys.

usage: python3 tools/cs_vs_web.py [--canon] [--states] [--no-build] [--frames N] [--keep] [NAME[:FRAMES] ..]
  NAME            a case from CASES below, or the stem of any ../web/test/states/*.json (default frames / no keys)
  NAME:FRAMES     same with its own frame count (e.g. town02_enter:1)
  --canon         from Reset over the canonical keys (tools/ref/compare_ref.py CANON_KEYS) to f2460
  --states        every ../web/test/states/*.json (CASES entry if there is one, else DEFAULT_FRAMES, no keys)
  --frames N      override the frame count of every selected case
  --only KINDS    compare only these of csv,png,ram,wram,ciram (default: all)
  --no-build      skip `dotnet build ReC.slnx -c Release`
  --keep          keep the out/ files of passing cases
Per case both ports write --compare CSV, --dump-screen PNG, --dump-ram / --dump-wram / --dump-ciram into out/<case>/;
CSV and PNG go through ../tools/compare_frames.py, the raw dumps must be byte-identical. One line per case;
exit 1 on any difference. (docs/architecture_cs.md §5)
"""
import glob
import os
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = os.path.dirname(ROOT)
STATES = os.path.join(REPO, 'web', 'test', 'states')
RUN_MJS = os.path.join(REPO, 'web', 'test', 'run.mjs')
COMPARE = os.path.join(REPO, 'tools', 'compare_frames.py')
CS_RUN = os.path.join(ROOT, 'src', 'ReC.Run', 'bin', 'Release', 'net10.0', 'ReC.Run')
OUT = os.path.join(ROOT, 'out')

CANON_KEYS = '1250:START 1400:START 1600:START 1700:START 2300:START 2340:DOWN:80'
DEFAULT_FRAMES = 120

# name -> dict(state=<json stem or None for Reset>, frames=N, keys='..', frame0=N (optional))
# Frames / keys per state come from the matching ../web/test/*_check.mjs (filled in as the modules get ported).
CASES = {
    'canon': dict(state=None, frames=2460 - 214 + 1, keys=CANON_KEYS),
    'town02_enter': dict(state='town02_enter', frames=1, keys=''),
}


def case_for(name):
    frames = None
    if ':' in name:
        name, frames = name.split(':', 1)
        frames = int(frames)
    if name in CASES:
        c = dict(CASES[name])
    elif os.path.exists(os.path.join(STATES, name + '.json')):
        c = dict(state=name, frames=DEFAULT_FRAMES, keys='')
    else:
        sys.exit(f'unknown case {name} (not in CASES, no {STATES}/{name}.json)')
    if frames is not None:
        c['frames'] = frames
        name = f'{name}_f{frames}'
    return name, c


def port_args(c, d, tag):
    a = ['--frames', str(c['frames']), '--quiet',
         '--compare', os.path.join(d, f'{tag}.csv'), '--dump-screen', os.path.join(d, f'{tag}.png'),
         '--dump-ram', os.path.join(d, f'{tag}.ram'), '--dump-wram', os.path.join(d, f'{tag}.wram'),
         '--dump-ciram', os.path.join(d, f'{tag}.ciram')]
    if c.get('state'):
        a += ['--load-state', os.path.join(STATES, c['state'] + '.json')]
    if 'frame0' in c:
        a += ['--frame0', str(c['frame0'])]
    if c.get('keys'):
        a += ['--keys'] + c['keys'].split()
    return a


def run(cmd):
    p = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    return p.returncode, p.stdout


KINDS = ('csv', 'png', 'ram', 'wram', 'ciram')


def compare_case(name, c, keep, only=KINDS):
    d = os.path.join(OUT, name)
    os.makedirs(d, exist_ok=True)
    rc, out = run(['node', RUN_MJS] + port_args(c, d, 'web'))
    if rc:
        return False, f'web run failed: {out.strip()[-300:]}'
    rc, out = run([CS_RUN] + port_args(c, d, 'cs'))
    if rc:
        return False, f'C# run failed: {out.strip()[-300:]}'
    bad = []
    if 'csv' in only:
        rc, out = run([sys.executable, COMPARE, os.path.join(d, 'web.csv'), os.path.join(d, 'cs.csv')])
        if rc:
            lines = [ln for ln in out.splitlines() if ln.strip()]
            bad.append('csv: ' + ' | '.join(lines[:4]))
    if 'png' in only:
        rc, out = run([sys.executable, COMPARE, os.path.join(d, 'web.png'), os.path.join(d, 'cs.png')])
        if rc:
            bad.append('png: ' + out.strip().splitlines()[-1])
    for ext in ('ram', 'wram', 'ciram'):
        if ext not in only:
            continue
        a = open(os.path.join(d, f'web.{ext}'), 'rb').read()
        b = open(os.path.join(d, f'cs.{ext}'), 'rb').read()
        if a != b:
            diff = [i for i in range(min(len(a), len(b))) if a[i] != b[i]]
            base = {'ram': 0, 'wram': 0x6000, 'ciram': 0}[ext]
            first = ' '.join(f'${base + i:04X}' for i in diff[:6])
            bad.append(f'{ext}: {len(diff)} bytes differ ({first}{" .." if len(diff) > 6 else ""})')
    if not bad and not keep:
        shutil.rmtree(d, ignore_errors=True)
    return not bad, '; '.join(bad) if bad else f'{c["frames"]} frames identical ({", ".join(only)})'


def main(argv):
    keep = '--keep' in argv
    names = []
    frames = None
    only = KINDS
    it = iter(argv)
    for a in it:
        if a == '--only':
            only = tuple(next(it).split(','))
            if any(k not in KINDS for k in only):
                sys.exit(f'--only takes {",".join(KINDS)}')
        elif a == '--canon':
            names.append('canon')
        elif a == '--states':
            names += sorted(os.path.basename(p)[:-5] for p in glob.glob(os.path.join(STATES, '*.json')))
        elif a == '--frames':
            frames = int(next(it))
        elif a in ('--no-build', '--keep'):
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
        rc, out = run(['dotnet', 'build', os.path.join(ROOT, 'ReC.slnx'), '-c', 'Release', '-v', 'q', '-nologo'])
        if rc:
            print(out)
            return 1
    fails = 0
    for n in names:
        if frames is not None and ':' not in n:
            n = f'{n}:{frames}'
        name, c = case_for(n)
        ok, msg = compare_case(name, c, keep, only)
        fails += not ok
        print(f'{"ok  " if ok else "FAIL"} {name:24s} {msg}', flush=True)
    print(f'{len(names) - fails}/{len(names)} cases identical')
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
