#!/usr/bin/env python3
"""Line up the FCEUX per-frame trace with the tools.emu one and place every lag-frame difference (s91).

usage (repo root):
  python3 -m tools.emu.lag_probe --to 2470 --trace docs/emu_trace.txt      (once)
  python3 tools/ref/trace_diff.py [ref/trace.txt] [docs/emu_trace.txt] [--out FILE]

Both files: `frame $40 $41 $06F0 $06F1 $06F3 $06FF $0617 $0008 $6423` per frame (fceux_capture.lua
samples after frameadvance() = vblank start; lag_probe samples at the 261->0 wrap).
Prints
  1. per trace the "no-change" frames of title_ctr $06FF (= lag frames while MainState3 runs),
  2. the frame of every ($40,$41) change in both runs and the offset ref->emu,
  3. for each $06FF value the first frame it appears in each run, collapsed into runs of equal
     offset, so each offset step is pinned to the frames between which it happens.
"""
import sys

COLS = ['40', '41', '6F0', '6F1', '6F3', '6FF', '617', '08', '6423']


def load(path):
    rows = {}
    for ln in open(path):
        if ln.startswith('#') or not ln.strip():
            continue
        p = ln.split()
        rows[int(p[0])] = dict(zip(COLS, (int(x, 16) for x in p[1:])))
    return rows


def nochange(rows, col):
    fs = sorted(rows)
    return [f for f in fs[1:] if f - 1 in rows and rows[f][col] == rows[f - 1][col]]


def runs(xs):
    out, s = [], None
    for i, x in enumerate(xs):
        if s is None:
            s = x
        if i + 1 == len(xs) or xs[i + 1] != x + 1:
            out.append(f'{s}' if s == x else f'{s}-{x}')
            s = None
    return out


def events(rows, keys=('40', '41')):
    ev, prev = [], None
    for f in sorted(rows):
        k = tuple(rows[f][c] for c in keys)
        if k != prev:
            ev.append((f, k))
            prev = k
    return ev


def counter_firsts(rows, col):
    """[(frame, value)] for every change of col (each new value's first frame)."""
    out, prev = [], None
    for f in sorted(rows):
        v = rows[f][col]
        if v != prev:
            out.append((f, v))
            prev = v
    return out


def main(argv):
    out_path = None
    if '--out' in argv:
        i = argv.index('--out')
        out_path = argv[i + 1]
        argv = argv[:i] + argv[i + 2:]
    rp = argv[0] if argv else 'ref/trace.txt'
    ep = argv[1] if len(argv) > 1 else 'docs/emu_trace.txt'
    ref, emu = load(rp), load(ep)
    hi = min(max(ref), max(emu))
    o = [f'# trace_diff {rp} (FCEUX) vs {ep} (tools.emu), frames 0-{hi}']
    o.append('## 1. $06FF unchanged from the previous frame (title lag frames; after main_state 0 it stops)')
    for name, rows in (('ref', ref), ('emu', emu)):
        nc = [f for f in nochange(rows, '6FF') if f <= hi]
        o.append(f'  {name}: {len(nc)} frames: ' + ' '.join(runs(nc)))
    o.append('## 2. ($40 main_state, $41 sub_state) changes: ref frame, emu frame, offset emu-ref')
    er, ee = events(ref), events(emu)
    for f, k in er:
        if f > hi:
            break
        near = [g for g, kk in ee if kk == k and abs(g - f) <= 40]
        m = min(near, key=lambda g: abs(g - f)) if near else None
        o.append(f'  ${k[0]:02X}/${k[1]:02X}  ref f{f:5d}  emu ' + (f'f{m:5d}  d={m - f:+d}' if m is not None
                                                                     else '(not reached)'))
    o.append('## 3. offset of each $06FF value (emu first frame - ref first frame), runs of equal offset')
    rf, ef = counter_firsts(ref, '6FF'), counter_firsts(emu, '6FF')
    ie, cur, start, last = 0, None, None, None
    seg = []
    for f, v in rf:
        m = None
        for j in range(ie, min(ie + 40, len(ef))):
            if ef[j][1] == v:
                m, ie = ef[j][0], j + 1
                break
        if m is None:
            continue
        d = m - f
        if d != cur:
            if cur is not None:
                seg.append((start, last, cur))
            cur, start = d, f
        last = f
    if cur is not None:
        seg.append((start, last, cur))
    for s, e, d in seg:
        o.append(f'  ref f{s:5d}-f{e:5d}: d={d:+d}')
    txt = '\n'.join(o)
    print(txt)
    if out_path:
        open(out_path, 'w').write(txt + '\n')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
