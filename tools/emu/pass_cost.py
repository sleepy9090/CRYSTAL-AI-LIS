#!/usr/bin/env python3
"""s210: per-pass CPU cost table from a save state with keys (gap (c), lag-frame model).

One line per main-loop pass (main-loop top 30:C904 -> next top): the start frame / line, the CPU cycles spent in
each stage, named by where it starts (top -> ObjUpdateAll 30:CCCC (upd) -> CameraTick 31:E8F6 (cam) ->
ObjSpriteDraw 28:800E (spr) -> ObjCollideAll 26:8F6E (col) -> 30:C919, MainLoop's frame_ready clear after the
main_state handler (end)), the frame / line of that end, the active-slot count,
and LAG when the NMI inside the pass found frame_ready != 0. Cycles include the NMI / IRQ time that fell into
a stage. --slots adds the cycles of each slot's AI dispatch (30:CCFA X = slot, up to the next dispatch or
CameraTick) as `SS:AI=cyc`.

usage: python3 -m tools.emu.pass_cost STATE FRAMES [--from F] [--to F] [--slots] [--out FILE] --keys SPEC ...
  (--keys must be last)"""
import sys

from tools.emu.nes import NES
from tools.emu import state, debug

STAGES = ['top', 'upd', 'cam', 'spr', 'col', 'end']


def main(argv):
    if len(argv) < 2:
        raise SystemExit(__doc__)
    nes = NES()
    state.load(nes, argv[0])
    frames = int(argv[1])
    opt = lambda k, d: argv[argv.index(k) + 1] if k in argv else d
    lo, hi = int(opt('--from', 0)), int(opt('--to', 1 << 30))
    out = opt('--out', None)
    slots = '--slots' in argv
    if '--keys' in argv:
        nes.keys = debug.parse_keys(argv[argv.index('--keys') + 1:])
    rows = []
    cur = {}

    def mark(tag):
        def f(n):
            c = n.cpu.cycles
            if tag == 'top':
                if cur:
                    rows.append(dict(cur))
                cur.clear()
                cur.update(f=n.ppu.frame_no, l=n.ppu.line, t={'top': c}, lag=False, sl=[], last=None,
                           act=sum(1 for s in range(0x1F) if n.bus.ram[0x04A0 + s]))
                return
            if not cur:
                return
            if tag == 'cam' and cur['last']:
                s, ai, c0 = cur['last']
                cur['sl'].append(f'{s:02X}:{ai:02X}={c - c0}')
                cur['last'] = None
            cur['t'].setdefault(tag, c)
            if tag == 'end' and 'wfl' not in cur:
                cur['wfl'] = n.ppu.line
                cur['wff'] = n.ppu.frame_no
        return f

    def nmi(n):
        if cur and n.bus.ram[0x09]:
            cur['lag'] = True

    def slot(n):
        if not cur:
            return
        c, x = n.cpu.cycles, n.cpu.x
        if cur['last']:
            s, ai, c0 = cur['last']
            cur['sl'].append(f'{s:02X}:{ai:02X}={c - c0}')
        cur['last'] = (x, n.bus.ram[0x04A0 + x] & 0x7F, c) if n.bus.ram[0x04A0 + x] else None

    nes.on_exec(30, 0xC904, mark('top'))
    nes.on_exec(30, 0xCCCC, mark('upd'))
    nes.on_exec(31, 0xE8F6, mark('cam'))
    nes.on_exec(28, 0x800E, mark('spr'))
    nes.on_exec(26, 0x8F6E, mark('col'))
    nes.on_exec(30, 0xC919, mark('end'))   # MainLoop after the main_state handler: LDA #0 / STA frame_ready
    nes.on_exec(31, 0xF3B6, nmi)
    if slots:
        nes.on_exec(30, 0xCCFA, slot)
    nes.run_frames(frames)
    lines = []
    for r in rows:
        if not lo <= r['f'] <= hi:
            continue
        t = r['t']
        seg = []
        for a, b in zip(STAGES, STAGES[1:]):
            seg.append(f'{a}{t[b] - t[a]:6}' if a in t and b in t else f'{a}     -')
        tot = t['end'] - t['top'] if 'end' in t else -1
        wf = f"f{r['wff']}L{r['wfl']:3}" if 'wfl' in r else '-'
        s = (f"f{r['f']} L{r['l']:3} act {r['act']:2} " + ' '.join(seg) + f' tot {tot:6} end {wf}'
             + (' LAG' if r['lag'] else ''))
        if slots:
            s += '  ' + ' '.join(r['sl'])
        lines.append(s)
    text = '\n'.join(lines) + '\n'
    if out:
        open(out, 'w').write(text)
        print(f'{len(lines)} passes -> {out}, {sum(r["lag"] for r in rows)} LAG')
    else:
        sys.stdout.write(text)


if __name__ == '__main__':
    main(sys.argv[1:])
