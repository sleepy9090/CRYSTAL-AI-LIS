#!/usr/bin/env python3
"""Ending probe (session 83): poke main_state = 4 from a gameplay save-state and run the bank-17 ending.

usage (repo root):
  python3 -m tools.emu.ending_probe [--state F] [--frames N] [--out DIR]
Logs every change of (obj_phase $0600, mode $0602, sub-effect $060E, script index $8D) with its frame, every
$0103 sound request and every $40 write (with PC), and dumps OUT/step_NN.png on the first frame of each step
(the screen the previous step left), OUT/script_XX.png when step 19 moves its script index, plus OUT/final.png. See REVERSE.md "Ending sequence, bank 17".
"""
import argparse
import os
import sys

from tools.emu.nes import NES
from tools.emu import state

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--state', default=os.path.join(HERE, 'states', 'area1_enter.state'))
    ap.add_argument('--frames', type=int, default=6000)
    ap.add_argument('--out', default=os.path.join(ROOT, 'gfx', 'ending'))
    a = ap.parse_args(argv)
    os.makedirs(a.out, exist_ok=True)
    nes = NES()
    state.load(nes, a.state)
    r = nes.bus.ram
    log = []

    def wlog(name):
        def w(addr, val):
            pc = nes.cpu.pc
            bank = nes.mapper.prg_bank_at(pc) if pc >= 0x8000 else -1
            log.append(f'    f{nes.ppu.frame_no}: {name} <- {val:02X}  pc {bank}:{pc:04X}')
        return w

    nes.on_write(0x40, wlog('$40'))
    nes.on_write(0x0103, wlog('snd $0103'))
    for _ in range(4):
        nes.keys = {}
        nes.run_frame()
    r[0x40] = 4
    print(f'poke $40=4 at f{nes.ppu.frame_no} (sub_state {r[0x41]:02X})')
    prev = None
    for i in range(a.frames):
        nes.keys = {}
        nes.run_frame()
        cur = (r[0x0600], r[0x0602], r[0x060E], r[0x8D] if r[0x0600] >= 19 else 0)
        for line in log:
            print(line)
        log.clear()
        if prev is not None and cur[0] != prev[0]:
            nes.dump_screen(os.path.join(a.out, f'step_{cur[0]:02d}.png'))
        if cur[0] == 19 and prev is not None and cur[3] != prev[3]:
            nes.dump_screen(os.path.join(a.out, f'script_{cur[3]:02X}.png'))
        if cur != prev:
            print(f'  f{nes.ppu.frame_no}: $40 {r[0x40]} step {cur[0]:2d} mode {cur[1]} sub {cur[2]} $8D {cur[3]:02X}'
                  f' cam_y {r[0x05]:X}:{r[0x04]:02X} hero {r[0x71]:02X},{r[0x70]:02X}')
            prev = cur
        if r[0x0600] == 29 and i > 0 and prev and getattr(main, 'done', 0) == 0:
            main.done = nes.ppu.frame_no
        if getattr(main, 'done', 0) and nes.ppu.frame_no - main.done > 120:
            break
    nes.dump_screen(os.path.join(a.out, 'final.png'))
    print(f'end f{nes.ppu.frame_no}: step {r[0x0600]} $40 {r[0x40]}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
