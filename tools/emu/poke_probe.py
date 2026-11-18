#!/usr/bin/env python3
"""Area-poke probe (session 75): load a chain of areas the way tour.py's poke legs do and log what
happens after the last poke, frame by frame.

usage (repo root):
  python3 -m tools.emu.poke_probe AREA[.ENTRY] [AREA[.ENTRY] ...] [--frames N] [--state F]
                                  [--watch HHHH ...]
Every area but the last is poked and waited for (sub_state 8 with that area_id, A tapped like
tour.py). After the last poke it prints one line per frame for --frames frames:
$40 $41 $6C $6D slot-0 x/y, plus every write to $6C/$6D/$41 (and --watch addresses) with its PC.
"""
import argparse
import os
import sys

from tools.emu.nes import NES
from tools.emu import state

STATES = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'states')
BTN_A = 0x01


def parse(s):
    a, _, e = s.partition('.')
    return int(a, 16), int(e or '0', 16)


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('areas', nargs='+')
    ap.add_argument('--frames', type=int, default=120)
    ap.add_argument('--state', default=os.path.join(STATES, 'area1_enter.state'))
    ap.add_argument('--watch', nargs='*', default=[])
    ap.add_argument('--wait', type=int, default=0,
                    help='frames to run before the last poke (A tapped while sub_state != 8, like tour.py)')
    ap.add_argument('--tap', action='store_true', help='tap A every 8 frames after the last poke')
    a = ap.parse_args(argv)
    nes = NES()
    state.load(nes, a.state)
    r = nes.bus.ram
    chain = [parse(x) for x in a.areas]

    def settle(target, limit=600):
        t0, left = nes.ppu.frame_no, False
        while nes.ppu.frame_no - t0 < limit:
            f = nes.ppu.frame_no - t0
            nes.keys = {nes.ppu.frame_no: BTN_A} if f > 150 and f % 8 == 4 else {}
            nes.run_frame()
            left |= r[0x41] != 8
            if left and r[0x41] == 8 and r[0x6C] == target:
                nes.keys = {}
                return f
        return None

    for ar, en in chain[:-1]:
        r[0x6C], r[0x6D], r[0x41] = ar, en, 1
        print(f'poke {ar:02X}.{en:02X}: loaded in {settle(ar)} frames')
    for i in range(a.wait):
        nes.keys = {nes.ppu.frame_no: BTN_A} if r[0x41] != 8 and i % 8 == 4 else {}
        nes.run_frame()
        if r[0x41] != 8 and i % 30 == 0:
            print(f'  wait f{nes.ppu.frame_no}: $40/$41 {r[0x40]}/{r[0x41]:02X} area {r[0x6C]:02X}')
    while r[0x41] != 8:
        nes.keys = {nes.ppu.frame_no: BTN_A} if nes.ppu.frame_no % 8 == 4 else {}
        nes.run_frame()
    nes.keys = {}
    log = []
    watch = [0x6C, 0x6D, 0x41] + [int(x, 16) for x in a.watch]
    for ad in watch:
        def w(addr, val, ad=ad):
            pc = nes.cpu.pc
            bank = nes.mapper.prg_bank_at(pc) if pc >= 0x8000 else -1
            log.append(f'    f{nes.ppu.frame_no} line {nes.ppu.line}: ${ad:04X} <- {val:02X}  pc {bank}:{pc:04X}')
        nes.on_write(ad, w)
    ar, en = chain[-1]
    r[0x6C], r[0x6D], r[0x41] = ar, en, 1
    print(f'poke {ar:02X}.{en:02X} at f{nes.ppu.frame_no}')
    for i in range(a.frames):
        nes.keys = {nes.ppu.frame_no: BTN_A} if a.tap and i % 8 == 4 else {}
        nes.run_frame()
        x = r[0x70] | r[0x90] << 8
        y = r[0xB0] | r[0xD0] << 8
        print(f'  f{nes.ppu.frame_no} $40/$41 {r[0x40]}/{r[0x41]:02X} area {r[0x6C]:02X}.{r[0x6D]:02X} '
              f'p0 {x:04X},{y:04X} pc {nes.mapper.prg_bank_at(nes.cpu.pc)}:{nes.cpu.pc:04X} '
              f'sp {nes.cpu.s:02X} stack {bytes(r[0x100 + nes.cpu.s + 1:0x100 + nes.cpu.s + 9]).hex()}')
        for line in log:
            print(line)
        log.clear()
    return 0


if __name__ == '__main__':
    sys.exit(main())
