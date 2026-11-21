#!/usr/bin/env python3
"""Per-frame main-loop pass log from a tools.emu .state (session 134).

For each frame in the range, prints the (line) of each `SubstPlay` 30:CB62 entry and of each
frame_ready clear in `MainLoop` (any PC given by --pc), so a frame with no SubstPlay entry = a lag
frame (the previous pass overran the NMI). Optional --peek addresses are printed at each SubstPlay entry.

usage (repo root):
  python3 -m tools.emu.pass_probe --state tools/emu/states/talk_act1.state --frames 80 \
      [--keys 2600:A] [--pc 30:CB62] [--peek 00BD 048D]
"""
import argparse

from tools.emu.nes import NES
from tools.emu import state
from tools.emu.pads import parse_buttons


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--state', required=True)
    ap.add_argument('--frames', type=int, default=60)
    ap.add_argument('--keys', nargs='*', default=[], metavar='F:BTN[:N]', help='absolute frame numbers')
    ap.add_argument('--pc', nargs='*', default=['30:CB62'], metavar='BB:AAAA')
    ap.add_argument('--peek', nargs='*', default=[], metavar='HHHH')
    ap.add_argument('--write', nargs='*', default=[], metavar='HHHH', help='also log writes to these addresses')
    a = ap.parse_args(argv)
    nes = NES()
    state.load(nes, a.state)
    keys = {}
    for k in a.keys:
        f, b, *n = k.split(':')
        for i in range(int(n[0]) if n else 2):
            keys[int(f) + i] = keys.get(int(f) + i, 0) | parse_buttons(b)
    nes.keys = keys
    hits = {}
    peeks = [int(p, 16) for p in a.peek]
    for spec in a.pc:
        bank, pc = (int(s, 16) for s in spec.split(':'))
        bank = int(spec.split(':')[0])

        def f(n, spec=spec):
            r = n.bus.ram
            pk = ''.join(f' {p:04X}={r[p]:02X}' for p in peeks)
            hits.setdefault(n.ppu.frame_no, []).append(f'{spec}@L{n.ppu.line}{pk}')
        nes.on_exec(bank, pc, f)
    for spec in a.write:
        ad = int(spec, 16)

        def w(addr, val, n=nes, ad=ad):
            r = n.bus.ram
            pk = ''.join(f' {p:04X}={r[p]:02X}' for p in peeks)
            hits.setdefault(n.ppu.frame_no, []).append(f'${ad:04X}<-{val:02X}@L{n.ppu.line}{pk}')
        nes.on_write(ad, w)
    f0 = nes.ppu.frame_no
    for _ in range(a.frames):
        nes.run_frame()
    for fr in range(f0, nes.ppu.frame_no + 1):
        h = hits.get(fr, [])
        print(f'f{fr}: {"LAG (no pass)" if not h else "  ".join(h)}')


if __name__ == '__main__':
    main()
