"""Capture SoundUpdate 31:F7FE call sequences: APU register writes + RAM changes per call (session 56, Ph7 web 7/8).

usage (repo root):
  python3 -m tools.emu.sound_capture --load-state S [--calls N] [--seed S] --out F.json [--only NAME,...]

From the state's RAM, each scenario first runs SndInit 24:8020 (clean driver), then calls SoundUpdate N times
back to back (one call = one driver frame). Before some calls the scenario pokes the sound queue
($0100 count, $0103+ ids) the way PlaySound 30:C125 leaves it, so every music id, every SFX id over music,
pause/unpause ($80), fade ($FF) and random queue traffic are covered. NMI and IRQ are blocked during the
calls (cpu.nmi patched out, I flag set) so only the driver runs. tools.emu reads $4015 as 0 (apu.py stub),
so the replay must too.

JSON: {"source", "scenarios": [{"name", "ram": hex(2KB, after SndInit), "steps": [{"p": [[addr, val], ...]
(pokes before the call, optional), "w": "AAVV..." (APU writes in order: AA = addr - $4000, VV = value),
"d": [[addr, val], ...] (RAM bytes changed by the call, stack scratch left out)}]}]}
web/test/sound_check.mjs replays them through web/src/game/sound/.
"""
import argparse
import json
import os
import random

from .nes import NES
from . import state

SOUND_UPDATE, SND_INIT = 0xF7FE, 0x8020
STACK_LO = 24                      # bytes below S at the call that the driver may use as stack


def scenarios(r, calls):
    out = []
    for m in range(0x20):
        out.append((f'music_{m:02X}', [(0, [m])], calls))
    music_cycle = [0x16, 0x03, 0x0A, 0x01, 0x12, 0x07]
    for i, s in enumerate(range(0x20, 0x78)):
        m = music_cycle[i % len(music_cycle)]
        out.append((f'sfx_{s:02X}_over_{m:02X}', [(0, [m]), (20, [s])], 170))
    out.append(('sfx_pairs', [(0, [0x16])] + [(10 + 25 * k, [r.randrange(0x20, 0x78)]) for k in range(16)], 420))
    out.append(('pause_unpause', [(0, [0x03]), (60, [0x80]), (120, [0x80]), (130, [0x25]), (200, [0x80]),
                                  (230, [0x80])], 300))
    out.append(('fade_ff', [(0, [0x0A]), (90, [0xFF])], 900))
    out.append(('jingle_93', [(0, [0x12]), (40, [0x93])], 400))     # $13 interrupting jingle (b7 path in PlaySound)
    for k in range(6):
        ev = []
        for c in range(0, 600, r.randrange(8, 40)):
            ev.append((c, [r.choice([r.randrange(0x20), r.randrange(0x20, 0x78), r.randrange(0x20, 0x78),
                                     0x80, 0xFF, r.randrange(0xA0, 0x100)]) for _ in range(r.randrange(1, 3))]))
            # (queue values PlaySound can leave: $00-$77 ids, $A0-$FF commands; $78-$7F would read SFX headers
            # past the 88-entry SfxPtrTable, pointing into PPU-register space)
        out.append((f'random_{k}', ev, 600))
    return out


def run(nes, a):
    cpu = nes.cpu
    cpu.nmi = lambda: None
    base = bytes(nes.bus.ram)
    r = random.Random(a.seed)
    res = []
    only = set(a.only.split(',')) if a.only else None
    for name, events, n in scenarios(r, a.calls):
        if only and name not in only:
            continue
        nes.bus.ram[:] = base
        cpu.p |= 0x04
        nes.call(SND_INIT, bank=24)
        nes.mapper.regs[6], nes.mapper.regs[7] = nes.bus.ram[0x6E], nes.bus.ram[0x6F]
        nes.mapper._update_prg()
        ram0 = bytes(nes.bus.ram)
        ev = dict(events)
        steps = []
        for c in range(n):
            st = {}
            ram = nes.bus.ram
            if c in ev:                    # queue ids as PlaySound leaves the FIFO (count <= 5)
                pk = []
                q = ram[0x0100]
                for i in ev[c]:
                    if q >= 5:
                        break
                    pk.append([0x0103 + q, i & 0xFF])
                    q += 1
                pk.append([0x0100, q])
                for ad, v in pk:
                    ram[ad] = v
                st['p'] = pk
            before = bytes(ram)
            sp = cpu.s
            nes.apu.log = []
            cpu.p |= 0x04
            nes.call(SOUND_UPDATE)
            st['w'] = ''.join(f'{ad - 0x4000:02X}{v:02X}' for _, _, ad, v in nes.apu.log)
            lo, hi = 0x100 + max(0, sp - STACK_LO), 0x100 + sp
            st['d'] = [[i, ram[i]] for i in range(0x800) if ram[i] != before[i] and not lo <= i <= hi]
            steps.append(st)
        nes.apu.log = None
        res.append({'name': name, 'ram': ram0.hex(), 'steps': steps})
    return res


def main(argv=None):
    ap = argparse.ArgumentParser(prog='python3 -m tools.emu.sound_capture')
    ap.add_argument('--load-state', required=True)
    ap.add_argument('--calls', type=int, default=480, help='calls per music scenario')
    ap.add_argument('--seed', type=int, default=1)
    ap.add_argument('--only')
    ap.add_argument('--out', required=True)
    a = ap.parse_args(argv)
    nes = NES()
    state.load(nes, a.load_state)
    res = run(nes, a)
    d = os.path.dirname(a.out)
    if d:
        os.makedirs(d, exist_ok=True)
    with open(a.out, 'w') as f:
        json.dump({'source': f'{os.path.basename(a.load_state)} seed={a.seed}', 'scenarios': res}, f,
                  separators=(',', ':'))
    nw = sum(len(s['w']) // 4 for sc in res for s in sc['steps'])
    print(f'{len(res)} scenario(s), {sum(len(sc["steps"]) for sc in res)} calls, {nw} APU writes -> {a.out}')


if __name__ == '__main__':
    main()
