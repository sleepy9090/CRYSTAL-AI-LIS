"""Capture RAM before/after the object sprite pass ObjSpriteDraw 28:800E (session 54, Ph7 web 5/8).

usage (repo root):
  python3 -m tools.emu.sprite_capture real --load-state S --frames N [--keys SPEC ...] [--every K] --out F.json
  python3 -m tools.emu.sprite_capture fuzz --load-state S [--frames N] --cases N [--seed S] --out F.json

real: every call of 28:800E during the run (every K-th kept). `before` = RAM at the first instruction of
      28:800E, `after` = RAM at the RTS that ends the pass (OrbitSprites exits 28:80E2 / 8144 / 81A4 with S
      back at its entry value). Calls interrupted by an NMI or IRQ are dropped.
fuzz: run the frames, then take the RAM at the next 28:800E entry as a base and, per case, poke random
      camera / draw rotation / frame counter / hit flash / orbit + burst state, random OAM bytes and random
      object slots (active, x/y near the camera, meta id, dir add, flags, tile add, $0340, anim, overlay),
      then nes.call(28:800E). Cases where an interrupt fired during the call are redrawn with a new seed.

JSON: {"source": ..., "cases": [{"name", "sp", "before": hex(2048), "after": [[addr, val], ...]}]}
`after` lists only bytes that differ from `before`. The stack bytes $0100+sp-15 .. $0100+sp+2 (JSR/PHA
scratch inside the pass + the return address; fuzz cases get nes.call's sentinel there) are left out. web/test/sprite_check.mjs replays every case through the port.
"""
import argparse
import json
import os
import random
import sys

from .nes import NES, Stop
from . import state, debug

ENTRY = 0x800E
EXITS = (0x80E2, 0x8144, 0x81A4)
BANK = 28
NMI_ENTRY = 0xF3B6
STACK_SCRATCH = 15


def _vec(nes, a):
    return nes.bus.read(a) | nes.bus.read(a + 1) << 8


def _diff(before, after, sp):
    lo, hi = 0x100 + max(0, sp - STACK_SCRATCH), 0x100 + min(0xFF, sp + 2)   # + the return address
    return [[i, after[i]] for i in range(0x800) if after[i] != before[i] and not lo <= i <= hi]


class Interrupts:
    """Counts NMI/IRQ handler entries (first instruction at the vector targets)."""
    def __init__(self, nes):
        self.n = 0
        nmi, irq = _vec(nes, 0xFFFA), _vec(nes, 0xFFFE)

        def hit(_nes):
            self.n += 1
        nes.on_exec(None, nmi, hit)
        if irq != nmi:
            nes.on_exec(None, irq, hit)


def run_real(nes, a):
    cases, cur, seen = [], {}, {'n': 0}
    ints = Interrupts(nes)

    def enter(n):
        cur.clear()
        cur.update(before=bytes(n.bus.ram), sp=n.cpu.s, ints=ints.n, frame=n.ppu.frame_no)

    def leave(n):
        if not cur or n.cpu.s != cur['sp']:
            return
        if ints.n == cur['ints']:
            seen['n'] += 1
            if (seen['n'] - 1) % a.every == 0:
                cases.append({'name': f'f{cur["frame"]}', 'sp': cur['sp'], 'before': cur['before'].hex(),
                              'after': _diff(cur['before'], n.bus.ram, cur['sp'])})
        cur.clear()

    nes.on_exec(BANK, ENTRY, enter)
    for e in EXITS:
        nes.on_exec(BANK, e, leave)
    nes.run_frames(a.frames)
    return cases


def _poke_case(ram, r):
    w = lambda ad, v: ram.__setitem__(ad, v & 0xFF)
    camx = r.randrange(0, 0x800)
    camy_hi = r.randrange(0, 4)
    w(0x02, camx); w(0x03, camx >> 8); w(0x04, r.randrange(0, 0xF0)); w(0x05, camy_hi)
    w(0x07FF, r.randrange(256)); w(0x08, r.randrange(256)); w(0x07EF, r.randrange(256))
    w(0x0710, r.randrange(256))
    w(0x06C0, r.choice([0, r.randrange(8), r.randrange(8, 0x28), r.randrange(256)]))
    w(0x06C1, r.choice([0, 0, r.randrange(1, 0x20), r.randrange(256)]))
    for i in range(0x200, 0x300):
        w(i, r.randrange(256))
    for x in range(32):
        w(0x04A0 + x, r.randrange(1, 256) if r.random() < 0.7 else 0)
        ox = camx + r.randrange(-48, 304)
        w(0x70 + x, ox); w(0x90 + x, ox >> 8)
        w(0xB0 + x, r.randrange(256)); w(0xD0 + x, camy_hi + r.choice([-1, 0, 0, 0, 1]))
        fl = r.randrange(256)
        dir_add = r.choice([0, 1, 2, 3, r.randrange(256)])
        while True:      # effective id $9A has pointer $0000 (tools/meta_ptr_check.py): garbage reads, skip it
            mid = r.randrange(256)
            if (mid + (0 if fl & 1 else dir_add)) & 0xFF != 0x9A:
                break
        ov = r.randrange(256)
        while ov == 0x9A:
            ov = r.randrange(256)
        w(0x0300 + x, mid)
        w(0x0360 + x, dir_add)
        w(0x0380 + x, fl)
        w(0x0580 + x, ov)
        w(0x0320 + x, r.choice([0, 0x40, 0xC0, r.randrange(256)]))
        w(0x0340 + x, r.randrange(256))
        w(0x04E0 + x, r.randrange(256))
        w(0x05C0 + x, r.randrange(256)); w(0x05E0 + x, r.randrange(256))


def run_fuzz(nes, a):
    nes.run_frames(a.frames)
    base = {}

    def grab(n):
        base['ram'] = bytes(n.bus.ram)
        raise Stop('base')
    nes.on_exec(BANK, ENTRY, grab)
    try:
        nes.run_frames(600)
    except Stop:
        pass
    if 'ram' not in base:
        sys.exit('fuzz: 28:800E not reached within 600 frames of the start state')
    nes.clear_hooks()
    ints = Interrupts(nes)
    cases, seed = [], a.seed
    while len(cases) < a.cases:
        r = random.Random(seed)
        ram = bytearray(base['ram'])
        _poke_case(ram, r)
        nes.bus.ram[:] = ram
        i0 = ints.n
        sp = (nes.cpu.s - 2) & 0xFF          # S at the first instruction, after call() pushed the sentinel
        nes.call(ENTRY, BANK)
        if ints.n == i0:
            cases.append({'name': f'seed{seed}', 'sp': sp, 'before': bytes(ram).hex(),
                          'after': _diff(ram, nes.bus.ram, sp)})
        seed += 1
    return cases


def main(argv=None):
    ap = argparse.ArgumentParser(prog='python3 -m tools.emu.sprite_capture')
    ap.add_argument('mode', choices=('real', 'fuzz'))
    ap.add_argument('--load-state', required=True)
    ap.add_argument('--frames', type=int, default=0)
    ap.add_argument('--keys', nargs='+', action='extend', default=[])
    ap.add_argument('--every', type=int, default=1)
    ap.add_argument('--cases', type=int, default=200)
    ap.add_argument('--seed', type=int, default=1)
    ap.add_argument('--out', required=True)
    a = ap.parse_args(argv)
    nes = NES()
    state.load(nes, a.load_state)
    if a.keys:
        nes.keys = debug.parse_keys(a.keys)
    cases = run_real(nes, a) if a.mode == 'real' else run_fuzz(nes, a)
    d = os.path.dirname(a.out)
    if d:
        os.makedirs(d, exist_ok=True)
    src = f'{a.mode} {os.path.basename(a.load_state)} frames={a.frames}'
    if a.keys:
        src += ' keys=' + ' '.join(a.keys)
    if a.mode == 'fuzz':
        src += f' seed={a.seed}'
    with open(a.out, 'w') as f:
        json.dump({'source': src, 'cases': cases}, f, separators=(',', ':'))
    nd = sum(len(c['after']) for c in cases)
    print(f'{len(cases)} case(s), {nd} changed bytes -> {a.out}')


if __name__ == '__main__':
    main()
