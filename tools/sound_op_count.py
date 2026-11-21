#!/usr/bin/env python3
"""s127: count the stream ops in a sound_export.py JSON and list where a given op is used.

usage: sound_op_count.py [sound.json] [op]   (defaults web/data/sound.json, sweepreg)
Walks music[]/sfx[] -> channels[] -> ops[] (the static stream disassembly sound_export.py makes from each
header, so it covers every op reachable from a track start, incl. calls and loops).
"""
import json
import sys
from collections import Counter

path = sys.argv[1] if len(sys.argv) > 1 else 'web/data/sound.json'
want = sys.argv[2] if len(sys.argv) > 2 else 'sweepreg'
d = json.load(open(path))
cnt = Counter()
users = []
for kind in ('music', 'sfx'):
    for t in d.get(kind, []):
        for c in t.get('channels', []):
            ops = [o for o in c.get('ops', []) if isinstance(o, dict)]
            cnt.update(o.get('op') for o in ops)
            n = sum(o.get('op') == want for o in ops)
            if n:
                users.append(f"{kind} ${t['id']:02X} {c.get('chn')} x{n}")
print('ops:', ' '.join(f'{k}x{n}' for k, n in sorted(cnt.items(), key=lambda kv: str(kv[0]))))
print(f'-- {want}: {cnt[want]} op(s) in {len(users)} channel(s): {", ".join(users) or "none"}')
