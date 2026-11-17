#!/usr/bin/env python3
"""Rewrite labels.csv so every line has exactly 4 fields: extra commas inside the
comment (field 4+) are replaced by ';'. Quoted fields are not used in this file.

usage: labels_fix_commas.py [--dry]
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import ROOT  # noqa

path = os.path.join(ROOT, 'labels.csv')
with open(path) as f:
    lines = f.read().split('\n')
fixed = 0
out = []
for ln in lines:
    parts = ln.split(',')
    if len(parts) > 4 and not ln.startswith('#'):
        ln = ','.join(parts[:3]) + ',' + ';'.join(parts[3:])
        fixed += 1
        print('fixed:', ln[:110])
    out.append(ln)
if '--dry' not in sys.argv:
    with open(path, 'w') as f:
        f.write('\n'.join(out))
print('%d line(s) %s' % (fixed, 'would be fixed' if '--dry' in sys.argv else 'fixed'))
