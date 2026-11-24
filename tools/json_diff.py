"""Differences between two JSON files: paths whose values differ / exist in one only (top-level dict keys first).

usage: python3 tools/json_diff.py A.json B.json [--max N]
"""
import json
import sys


def walk(a, b, path, out, mx):
    if len(out) >= mx:
        return
    if isinstance(a, dict) and isinstance(b, dict):
        for k in sorted(set(a) | set(b), key=str):
            if k not in a:
                out.append(f'only in B: {path}/{k} = {json.dumps(b[k])[:160]}')
            elif k not in b:
                out.append(f'only in A: {path}/{k} = {json.dumps(a[k])[:160]}')
            else:
                walk(a[k], b[k], f'{path}/{k}', out, mx)
    elif isinstance(a, list) and isinstance(b, list) and len(a) == len(b):
        for i, (x, y) in enumerate(zip(a, b)):
            walk(x, y, f'{path}[{i}]', out, mx)
    elif a != b:
        out.append(f'differs: {path}: {json.dumps(a)[:120]} -> {json.dumps(b)[:120]}')


def main(argv):
    if len(argv) < 2:
        print(__doc__); return 1
    mx = int(argv[argv.index('--max') + 1]) if '--max' in argv else 50
    out = []
    walk(json.load(open(argv[0])), json.load(open(argv[1])), '', out, mx)
    print('\n'.join(out) if out else 'identical')
    return 1 if out else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
