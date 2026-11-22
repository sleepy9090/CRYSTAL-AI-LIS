#!/usr/bin/env python3
"""s176: run every `node web/test/*_check.mjs` and print one line per check (exit code + last output line).

usage: python3 tools/run_checks.py [NAME_SUBSTRING ...]   (exit 1 if any check fails)"""
import glob
import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def main(argv):
    files = sorted(glob.glob(os.path.join(ROOT, 'web', 'test', '*_check.mjs')))
    if argv:
        files = [f for f in files if any(s in os.path.basename(f) for s in argv)]
    bad = 0
    for f in files:
        p = subprocess.run(['node', f], cwd=ROOT, capture_output=True, text=True, timeout=1200)
        lines = (p.stdout + p.stderr).strip().splitlines()
        fails = [ln for ln in lines if 'FAIL' in ln][:3]
        print(f'{p.returncode} {os.path.basename(f):24s} | {(lines[-1] if lines else "")[:140]}')
        for ln in fails:
            print(f'      {ln[:160]}')
        bad += p.returncode != 0
    print(f'{len(files) - bad}/{len(files)} checks pass')
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
