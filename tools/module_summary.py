#!/usr/bin/env python3
"""Summarise web/src modules: header comment, imports, exports, ROM addresses mentioned.

usage: python3 tools/module_summary.py [ROOT=web/src] [--head N] [--width W] [--no-addr]
Used for docs/architecture_web.md (s221).
"""
import os
import re
import sys

ADDR = re.compile(r'\b([0-3]?\d):([89A-F][0-9A-F]{3})\b')
IMP = re.compile(r"^import\s+(?:.+?\s+from\s+)?['\"]([^'\"]+)['\"]")
EXP = re.compile(r'^export\s+(?:default\s+)?(?:async\s+)?(function\*?|const|let|class)\s*\*?\s*([A-Za-z_$][\w$]*)')


def main():
    args = sys.argv[1:]
    root = 'web/src'
    head, width, addrs = 4, 220, True
    i = 0
    while i < len(args):
        a = args[i]
        if a == '--head':
            head = int(args[i + 1]); i += 2; continue
        if a == '--width':
            width = int(args[i + 1]); i += 2; continue
        if a == '--no-addr':
            addrs = False; i += 1; continue
        root = a; i += 1
    files = []
    for d, _, fs in os.walk(root):
        for f in fs:
            if f.endswith('.js') or f.endswith('.mjs'):
                files.append(os.path.join(d, f))
    for path in sorted(files):
        with open(path, encoding='utf-8') as fh:
            lines = fh.read().split('\n')
        hdr = []
        for ln in lines:
            s = ln.strip()
            if s.startswith('//') or s.startswith('/*') or s.startswith('*'):
                t = s.lstrip('/*').strip()
                if t:
                    hdr.append(t)
                if len(hdr) >= head:
                    break
            elif s == '':
                if hdr:
                    break
            else:
                break
        imps = [m.group(1) for ln in lines for m in [IMP.match(ln)] if m]
        exps = [m.group(2) for ln in lines for m in [EXP.match(ln)] if m]
        print(f'== {os.path.relpath(path, root)} ({len(lines)} lines)')
        for h in hdr:
            print('  # ' + h[:width])
        if imps:
            print('  imports: ' + ', '.join(imps))
        if exps:
            print('  exports: ' + ', '.join(exps)[:width * 2])
        if addrs:
            seen = []
            for ln in lines:
                for m in ADDR.finditer(ln):
                    k = f'{int(m.group(1)):02d}:{m.group(2)}'
                    if k not in seen:
                        seen.append(k)
            if seen:
                print(f'  rom addrs ({len(seen)}): ' + ' '.join(seen[:40]) + (' ...' if len(seen) > 40 else ''))


if __name__ == '__main__':
    main()
