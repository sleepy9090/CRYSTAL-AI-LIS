#!/usr/bin/env python3
"""Export a tools.emu machine state for the web port (`web/test/run.mjs --load-state F.json`), session 88.

The port cannot resume a 6502 routine half way, so the state is taken at the first instruction of a
routine the port runs from its start (default `PlayerDeath` 19:B900): load a .state, optionally run
some frames and poke RAM, then run until PC hits --until. At that instant the main loop has left its
frame_ready wait for this frame, so the port's next `runFrame` (mainStep -> render -> NMI) lines up
with the rest of tools.emu frame `frame_no` (= the JSON's `frame`, used as run.mjs --frame0).

JSON: frame, until, ram/wram/ciram/pal/oam (hex strings), ppu {ctrl mask v t x w}, mapper
{regs[8], mirroring, chr_inv}. With --save-state the same instant is also saved as a tools.emu state,
so `python3 -m tools.emu --load-state S --frames N --compare emu.csv` gives the reference rows.

usage (repo root):
  python3 -m tools.emu.web_state --load-state tools/emu/states/area1_enter.state --frames 10 \
      --poke 03C1=00 --until 19:B900 --out web/test/states/death.json --save-state /tmp/death.state
"""
import argparse
import json
import os
import sys

from tools.emu.nes import NES
from tools.emu import state


class _Hit(Exception):
    pass


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--load-state', required=True)
    ap.add_argument('--to-play', action='store_true', help='first tap A (pad 1) until sub_state 8')
    ap.add_argument('--frames', type=int, default=0, help='frames to run before the pokes')
    ap.add_argument('--poke', action='append', default=[], metavar='HHHH=VV')
    ap.add_argument('--until', default='19:B900', metavar='BB:AAAA')
    ap.add_argument('--max-frames', type=int, default=600)
    ap.add_argument('--out', required=True)
    ap.add_argument('--save-state')
    a = ap.parse_args(argv)

    nes = NES()
    state.load(nes, a.load_state)
    r = nes.bus.ram
    while a.to_play and r[0x41] != 8:               # scenario.py: tap A until sub_state 8 (play)
        nes.keys = {nes.ppu.frame_no: 0x01} if nes.ppu.frame_no % 8 == 4 else {}
        nes.run_frame()
    nes.keys = None
    nes.run_frames(a.frames)
    for p in a.poke:
        ad, v = (int(s, 16) for s in p.split('='))
        if ad < 0x800:
            nes.bus.ram[ad] = v
        else:
            nes.bus.wram[ad - 0x6000] = v
    bank, addr = a.until.split(':')
    bank, addr = int(bank, 10), int(addr, 16)       # bank decimal (BB:AAAA), addr hex

    def hit(_n):
        raise _Hit()
    nes.on_exec(bank, addr, hit)
    try:
        for _ in range(a.max_frames):
            nes.run_frame()
        sys.exit(f'{a.until} not reached in {a.max_frames} frames (frame {nes.ppu.frame_no}, '
                 f'state {r[0x40]} sub {r[0x41]} hp {r[0x03C1]})')
    except _Hit:
        pass
    nes.clear_hooks()
    p, m = nes.ppu, nes.mapper
    out = dict(frame=p.frame_no, line=p.line, until=a.until, src=os.path.basename(a.load_state),
               ram=bytes(nes.bus.ram).hex(), wram=bytes(nes.bus.wram).hex(),
               ciram=bytes(p.ciram).hex(), pal=bytes(p.pal).hex(), oam=bytes(p.oam).hex(),
               ppu=dict(ctrl=p.ctrl, mask=p.mask, v=p.v, t=p.t, x=p.x, w=p.w),
               mapper=dict(regs=list(m.regs), mirroring=m.mirroring, chr_inv=1 if m.chr_inv else 0))
    os.makedirs(os.path.dirname(a.out) or '.', exist_ok=True)
    with open(a.out, 'w') as f:
        json.dump(out, f)
    if a.save_state:
        state.save(nes, a.save_state)
    print(f'{a.until} at frame {p.frame_no} line {p.line} -> {a.out}')


if __name__ == '__main__':
    main()
