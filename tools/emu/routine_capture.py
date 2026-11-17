"""Fuzz-capture RAM + WRAM around one ROM routine (session 55, Ph7 web 6/8). Generalises sprite_capture.py fuzz.

usage (repo root):
  python3 -m tools.emu.routine_capture --load-state S [--frames N] --routine NAME[,NAME...] --cases N
         [--seed S] --out F.json
  python3 -m tools.emu.routine_capture --list

The run goes to the next ObjUpdateAll 30:CCCC entry (a gameplay frame) and takes RAM + WRAM there as the base.
Per case: random object slots / temps / sound queue (ROUTINES[name].poke), then nes.call(entry, bank) with
the routine's registers. Cases where an NMI/IRQ fired during the call are redrawn with the next seed.

The routine is entered with two return addresses on the stack (call2: like ObjUpdateAll's JSR $0010 -> JMP), so a
handler that pops its caller (AiShootCheck PLA PLA after a tail JMP) is recorded as "ret": "popped".

JSON: {"source", "wram_base": hex(8KB), "cases": [{"name", "routine", "regs": {"a","x","y"}, "sp", "ret",
"ram": hex(2KB), "wram": [[addr, val], ...] (vs wram_base), "after": [[addr, val], ...]}]} -- after: addr < $800 =
RAM, $6000+ = WRAM; only changed bytes; the stack bytes $0100+sp-15 .. $0100+sp+4 are left out.
web/test/obj_check.mjs replays them through the port.
"""
import argparse
import json
import os
import random
import sys

from .nes import NES, Stop
from . import state
from .sprite_capture import Interrupts
from ..nesrom import load_rom

_PRG = load_rom()[1]
RAND64_ZERO = [i for i in range(64) if _PRG[26 * 0x2000 + 0x17E4 + i] == 0]   # Rand64Table 26:97E4 zero entries

OBJ_UPDATE_ALL = 0xCCCC
STACK_SCRATCH = 15
OBJ_COLS = range(0x0300, 0x0700, 0x20)        # the 32 slot columns $0300-$06E0
POS_COLS = (0x70, 0x90, 0xB0, 0xD0)


def _poke_common(ram, wram, r):
    w = lambda ad, v: ram.__setitem__(ad, v & 0xFF)
    for x in range(32):
        if r.random() < 0.6:
            for c in OBJ_COLS:
                w(c + x, r.randrange(256))
        w(0x04A0 + x, r.choice([0, 0, r.randrange(1, 0x80), r.randrange(256)]))
    w(0x08, r.randrange(256)); w(0x0E, r.randrange(256))
    for a in range(0x10, 0x24):
        w(a, r.randrange(256))
    for a in range(0x30, 0x38):
        w(a, r.randrange(256))
    w(0x0100, r.choice([0, 1, 4, 5, 5, r.randrange(8)]))     # sound queue count / lock / music
    w(0x0102, r.choice([0, r.randrange(0x20), r.randrange(256)]))
    for a in range(0x0103, 0x0109):
        w(a, r.randrange(256))


def _positions(ram, r, near=True):
    """object positions clustered around slot 1 (the player) so distances < 5 cells happen"""
    w = lambda ad, v: ram.__setitem__(ad, v & 0xFF)
    px, py = r.randrange(0x40, 0x7C0), r.randrange(0x10, 0xE0)
    pyh = r.randrange(0, 4)
    for x in range(32):
        if near and r.random() < 0.8:
            ox, oy = px + r.randrange(-120, 120), py + r.randrange(-100, 100)
            oyh = pyh
            if oy < 0:
                oy += 240; oyh -= 1
            elif oy >= 240:
                oy -= 240; oyh += 1
            w(0x70 + x, ox); w(0x90 + x, ox >> 8); w(0xB0 + x, oy); w(0xD0 + x, oyh)
        else:
            for c in POS_COLS:
                w(c + x, r.randrange(256))


def _poke_obj(ram, wram, r):
    _poke_common(ram, wram, r)
    _positions(ram, r)


def _poke_bg(ram, wram, r):
    _poke_obj(ram, wram, r)
    for i in range(0x40):                          # screen grid $6300 (bank<<5 | page) + visited bits $62F0
        wram[0x300 + i] = r.randrange(256)
    for i in range(0x10):
        wram[0x2F0 + i] = r.randrange(256)
    ram[0x34] = r.randrange(256); ram[0x35] = r.randrange(8); ram[0x36] = r.randrange(0xF0); ram[0x37] = r.randrange(8)


def _slot(r):
    return r.randrange(0x0D, 0x1F)


ROUTINES = {
    # name: (bank or None, addr, poke, regs(r) -> dict)
    'ObjInit':        (None, 0xC25D, _poke_obj, None),
    'PlaySound':      (None, 0xC125, _poke_obj, lambda r: {'a': r.randrange(256), 'y': r.randrange(256)}),
    'DirToTarget':    (26, 0x8409, _poke_obj, lambda r: {'x': r.randrange(32), 'y': r.randrange(32)}),
    'SpawnShot':      (26, 0x972D, _poke_obj, lambda r: {'a': r.randrange(0x60), 'x': _slot(r), 'y': r.randrange(16)}),
    'ObjBgProbe':     (26, 0x9A30, _poke_bg, lambda r: {'a': r.choice([0, 0x40, 0x80, 0xC0, r.randrange(256)]),
                                                         'x': r.randrange(32)}),
    'AiStraightMover': (26, 0xB0B5, _poke_bg, lambda r: {'x': _slot(r)}),
    'AiDirShot':      (26, 0xB0A8, _poke_bg, lambda r: {'x': _slot(r)}),
    'AiBurst':        (26, 0xACC0, _poke_obj, lambda r: {'x': _slot(r)}),
    'AiFanTurret':    (26, 0xB595, _poke_obj, lambda r: {'x': _slot(r)}),
    'AiTurret':       (26, 0xB5D9, _poke_obj, lambda r: {'x': _slot(r)}),
}


def _fix_case(name, ram, r, regs):
    """routine-specific pokes that keep a case on a meaningful path"""
    x = regs.get('x')
    if name == 'ObjInit':
        ram[0x10] = _slot(r) if r.random() < 0.9 else r.randrange(32)
        ram[0x11] = r.choice([1, 0xFF, r.randrange(256), r.randrange(256), r.randrange(256)])
    if name in ('AiFanTurret',) and r.random() < 0.6:
        ram[0x0640 + x] = r.choice([0, 1, 0x08, 0x09, 0x10, 0x11, 0x18, 0x19, 0x1F])
    if name in ('AiFanTurret', 'AiTurret') and r.random() < 0.4:
        # steer AiShootCheck into its attack: onscreen, the 1-in-32 frame slot, Rand64 = 0 next, player close
        if name == 'AiFanTurret':
            ram[0x0640 + x] = 0
        ram[0x0380 + x] &= 0x7F
        ram[0x08] = (-(x << 4)) & 0x1F | (r.randrange(8) << 5)
        ram[0x0E] = (r.choice(RAND64_ZERO) - 1) & 0x3F
        for c in POS_COLS:
            ram[c + x] = ram[c + 1]
        ram[0x70 + x] = (ram[0x71] + r.randrange(-40, 40)) & 0xFF
    if name in ('AiStraightMover', 'AiDirShot'):
        if r.random() < 0.7:
            ram[0x0380 + x] &= 0x7F                # onscreen most of the time
        if r.random() < 0.5:
            ram[0x0460 + x] = 0
        ram[0x0480 + x] = r.choice([1, 2, r.randrange(256)])
        ram[0x0340 + x] = r.choice([r.randrange(16), r.randrange(256)])
    if name == 'AiTurret' and r.random() < 0.5:
        ram[0x04E0 + x] = r.choice([0x01, 0x11, 0x21])
    if name in ('AiFanTurret', 'AiTurret', 'AiBurst', 'SpawnShot'):
        ram[0x0440 + x] = r.choice([0x40 + r.randrange(0x20), r.randrange(0x60)])
        if 'a' in regs:
            regs['a'] = ram[0x0440 + x]


OUTER, INNER = 0xFFFE, 0xFFFF


def call2(nes, addr, bank, regs, max_instr=200_000):
    """Call like ObjUpdateAll's JSR $0010 -> JMP handler, with a caller frame under the return address:
    push OUTER-1 (the caller's caller) then INNER-1. Returns 'rts' when the routine returns normally,
    'popped' when it pulled its return address (AiShootCheck's PLA PLA) and returned to the caller's caller."""
    cpu, m = nes.cpu, nes.mapper
    if bank is not None and 0x8000 <= addr < 0xC000:
        m.regs[6], m.regs[7] = bank & ~1, bank | 1
        m._update_prg()
    for r, v in regs.items():
        setattr(cpu, r, v & 0xFF)
    s0 = cpu.s
    for v in (OUTER - 1, INNER - 1):
        cpu._push(v >> 8)
        cpu._push(v & 0xFF)
    cpu.pc = addr
    for _ in range(max_instr):
        if cpu.pc == INNER and cpu.s == (s0 - 2) & 0xFF:
            cpu.s = s0
            return 'rts'
        if cpu.pc == OUTER and cpu.s == s0:
            return 'popped'
        nes.step()
    raise Stop(f'call ${addr:04X}: no return after {max_instr} instructions')


def _diff(before, ram, wram, sp):
    lo, hi = 0x100 + max(0, sp - STACK_SCRATCH), 0x100 + min(0xFF, sp + 4)
    out = [[i, ram[i]] for i in range(0x800) if ram[i] != before[i] and not lo <= i <= hi]
    out += [[0x6000 + i, wram[i]] for i in range(0x2000) if wram[i] != before[0x800 + i]]
    return out


def run(nes, a):
    nes.run_frames(a.frames)
    base = {}

    def grab(n):
        base['ram'], base['wram'] = bytes(n.bus.ram), bytes(n.bus.wram)
        raise Stop('base')
    nes.on_exec(None, OBJ_UPDATE_ALL, grab)
    try:
        nes.run_frames(600)
    except Stop:
        pass
    if 'ram' not in base:
        sys.exit('ObjUpdateAll 30:CCCC not reached within 600 frames of the start state')
    nes.clear_hooks()
    ints = Interrupts(nes)
    cases = []
    names = a.routine.split(',')
    for name in names:
        bank, addr, poke, regf = ROUTINES[name]
        seed, got = a.seed, 0
        while got < a.cases:
            r = random.Random(f'{name}:{seed}')
            seed += 1
            ram, wram = bytearray(base['ram']), bytearray(base['wram'])
            poke(ram, wram, r)
            regs = regf(r) if regf else {}
            _fix_case(name, ram, r, regs)
            if bank is not None:                  # shadows as ObjUpdateAll's SwitchBank16($0D) leaves them
                ram[0x6E], ram[0x6F], ram[0x50] = bank & ~1, bank | 1, 6
            nes.bus.ram[:] = ram
            nes.bus.wram[:] = wram
            i0 = ints.n
            sp = (nes.cpu.s - 4) & 0xFF          # S at the first instruction (2 return addresses pushed)
            try:
                how = call2(nes, addr, bank, regs)
            except Stop:
                continue
            if ints.n != i0:
                continue
            cases.append({'name': f'{name}#{seed - 1}', 'routine': name, 'regs': regs, 'sp': sp, 'ret': how,
                          'ram': bytes(ram).hex(),
                          'wram': [[0x6000 + i, v] for i, v in enumerate(wram) if v != base['wram'][i]],
                          'after': _diff(bytes(ram) + bytes(wram), nes.bus.ram, nes.bus.wram, sp)})
            got += 1
    return cases, base['wram']


def main(argv=None):
    ap = argparse.ArgumentParser(prog='python3 -m tools.emu.routine_capture')
    ap.add_argument('--load-state')
    ap.add_argument('--frames', type=int, default=0)
    ap.add_argument('--routine', default=','.join(ROUTINES))
    ap.add_argument('--cases', type=int, default=200)
    ap.add_argument('--seed', type=int, default=1)
    ap.add_argument('--out')
    ap.add_argument('--list', action='store_true')
    a = ap.parse_args(argv)
    if a.list:
        for n, (b, ad, _, _) in ROUTINES.items():
            print(f'{n:16} {"fixed" if b is None else b:>5}:{ad:04X}')
        return
    if not a.load_state or not a.out:
        ap.error('--load-state and --out are required')
    nes = NES()
    state.load(nes, a.load_state)
    cases, wram = run(nes, a)
    d = os.path.dirname(a.out)
    if d:
        os.makedirs(d, exist_ok=True)
    src = f'fuzz {os.path.basename(a.load_state)} frames={a.frames} seed={a.seed}'
    with open(a.out, 'w') as f:
        json.dump({'source': src, 'wram_base': bytes(wram).hex(), 'cases': cases}, f, separators=(',', ':'))
    print(f'{len(cases)} case(s), {sum(len(c["after"]) for c in cases)} changed bytes -> {a.out}')


if __name__ == '__main__':
    main()
