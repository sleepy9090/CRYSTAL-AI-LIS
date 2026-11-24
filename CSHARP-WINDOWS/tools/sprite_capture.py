"""Capture on-screen sprites for tools/sprite_pack.py (T12): run the Linux headless core over the combat / boss states
with --dump-tags (Ppu.Tags + Fb + palette every EVERY frames).

usage: python3 tools/sprite_capture.py [--out DIR] [--frames N] [--every K] [STATE ..]
  default DIR out/sprites, N 480, K 8; default states = the enemy scenarios of ../web/test/states (group*, boss*,
  final*, fan*, thunder, kill*, pitfall, ai70msg, restore5f, death); each state gets DIR/<state>/tFFFFF.bin.
  Keys (relative to the state's first frame): a walk around in all 4 directions with sword swings, so enemies move,
  turn and attack.
"""
import json
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import core_vs_web as cv  # noqa: E402

STATES = os.path.join(cv.ROOT, '..', 'web', 'test', 'states')
RUN = os.path.join(cv.ROOT, 'src', 'ReC.Run', 'bin', 'Release', 'net10.0', 'ReC.Run')
PREFIXES = ('group', 'boss', 'final', 'fan', 'thunder', 'kill', 'pitfall', 'ai70msg', 'restore5f', 'death')
# (offset, buttons, frames held)
WALK = [(14, 'LEFT', 24), (40, 'A', 4), (60, 'UP', 24), (86, 'A', 4), (106, 'RIGHT', 30), (138, 'A', 4),
        (160, 'DOWN', 24), (186, 'A', 4), (206, 'LEFT', 16), (230, 'B', 60), (300, 'RIGHT', 20), (322, 'A', 4),
        (340, 'UP', 16), (360, 'A', 4), (380, 'DOWN', 30), (412, 'A', 4), (430, 'LEFT', 20), (452, 'A', 4)]


def main(argv):
    out, frames, every, names = os.path.join(cv.ROOT, 'out', 'sprites'), 480, 8, []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--out':
            out = argv[i + 1]; i += 1
        elif a == '--frames':
            frames = int(argv[i + 1]); i += 1
        elif a == '--every':
            every = int(argv[i + 1]); i += 1
        elif a in ('-h', '--help'):
            print(__doc__); return 0
        else:
            names.append(a)
        i += 1
    if not names:
        names = sorted(f[:-5] for f in os.listdir(STATES) if f.endswith('.json') and f.startswith(PREFIXES))
    if not os.path.exists(RUN):
        subprocess.run(['dotnet', 'build', os.path.join(cv.ROOT, 'src', 'ReC.Run'), '-c', 'Release'], check=True)
    for n in names:
        p = os.path.join(STATES, n + '.json')
        st = json.load(open(p))
        f0 = st.get('frame0') or st['frame']
        keys = [f'{f0 + o}:{b}:{h}' for o, b, h in WALK]
        d = os.path.join(out, n)
        os.makedirs(d, exist_ok=True)
        for f in os.listdir(d):
            os.remove(os.path.join(d, f))
        r = subprocess.run([RUN, '--load-state', p, '--frames', str(frames), '--quiet', '--dump-tags', d, str(every),
                            '--keys'] + keys, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, timeout=600)
        print(f'{"ok  " if r.returncode == 0 else "FAIL"} {n:14s} {len(os.listdir(d))} dumps' +
              ('' if r.returncode == 0 else ' ' + r.stdout[-300:]), flush=True)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
