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
from tools.emu import state, debug


class _Hit(Exception):
    pass


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--load-state', required=True)
    ap.add_argument('--to-play', action='store_true', help='first tap A (pad 1) until sub_state 8')
    ap.add_argument('--frames', type=int, default=0, help='frames to run before the pokes')
    ap.add_argument('--poke', action='append', default=[], metavar='HHHH=VV')
    ap.add_argument('--until', default='19:B900', metavar='BB:AAAA')
    ap.add_argument('--keys', nargs='+', default=[], metavar='F:BTN[:N]',
                    help='s94: pad 1 keys (absolute frame_no, tools.emu syntax) during the --until search, '
                         'e.g. 2470:START to stop at PauseScreen 15:BC40')
    ap.add_argument('--force', action='append', default=[], metavar='BB:AAAA:HHHH=VV',
                    help='s203: when bank BB (decimal) PC AAAA first runs during the --until search, write VV to '
                         'HHHH (RAM only), e.g. 30:D552:0022=01 forces the EventActionTable act')
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
    for fs in a.force:                              # s203: one-shot RAM write at an exec hook
        fb, fpc, fw = fs.split(':')
        fad, fv = (int(s, 16) for s in fw.split('='))
        done = []

        def fhook(_n, fad=fad, fv=fv, done=done):
            if not done:
                done.append(1)
                r[fad] = fv
        nes.on_exec(int(fb, 10), int(fpc, 16), fhook)
    nes.on_exec(bank, addr, hit)
    if a.keys:
        nes.keys = debug.parse_keys(a.keys)
    try:
        for _ in range(a.max_frames):
            nes.run_frame()
        sys.exit(f'{a.until} not reached in {a.max_frames} frames (frame {nes.ppu.frame_no}, '
                 f'state {r[0x40]} sub {r[0x41]} hp {r[0x03C1]})')
    except _Hit:
        pass
    nes.clear_hooks()
    export(nes, a.until, os.path.basename(a.load_state), a.out, a.save_state)


def export(nes, until, src, out_path, save_state=None):
    """s160: write the port JSON (+ optional tools.emu .state) for the instant `nes` is stopped at"""
    p, m = nes.ppu, nes.mapper
    # frame0 (s89): taken after the NMI (vblank, line > 240), the work runs mostly in frame_no + 1, and
    # tools.emu's CSV row frame_no (sampled at line 0 of the next frame) only sees its vblank start
    frame0 = p.frame_no + 1 if p.line > 240 else p.frame_no
    out = dict(frame=p.frame_no, frame0=frame0, line=p.line, until=until, src=src,
               ram=bytes(nes.bus.ram).hex(), wram=bytes(nes.bus.wram).hex(),
               ciram=bytes(p.ciram).hex(), pal=bytes(p.pal).hex(), oam=bytes(p.oam).hex(),
               ppu=dict(ctrl=p.ctrl, mask=p.mask, v=p.v, t=p.t, x=p.x, w=p.w),
               mapper=dict(regs=list(m.regs), mirroring=m.mirroring, chr_inv=1 if m.chr_inv else 0))
    os.makedirs(os.path.dirname(out_path) or '.', exist_ok=True)
    with open(out_path, 'w') as f:
        json.dump(out, f)
    if save_state:
        state.save(nes, save_state)
    print(f'{until} at frame {p.frame_no} line {p.line} -> {out_path}')


if __name__ == '__main__':
    main()
