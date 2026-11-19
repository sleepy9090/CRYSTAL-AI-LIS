#!/usr/bin/env python3
"""Who calls a routine? Log every entry to BANK:ADDR in a frame range with the stack above it (s91).

usage (repo root):
  python3 -m tools.emu.caller_probe BANK ADDR [--from F] [--to F] [--keys SPEC ...] [--fill fceux]
For each entry prints frame, $40/$41/$06F1/$06FF, the raw stack bytes S+1..S+12 and the first stack
word + 1 as the JSR return address (the caller's JSR is at ret-3).  Uses the canonical --keys script by default (tools/ref/compare_ref.CANON_KEYS).
"""
import argparse
import sys

from tools.emu.nes import NES
from tools.emu import debug
from tools.ref.compare_ref import CANON_KEYS


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('bank', type=int)
    ap.add_argument('addr', type=lambda s: int(s, 16))
    ap.add_argument('--from', dest='lo', type=int, default=0)
    ap.add_argument('--to', dest='hi', type=int, default=2470)
    ap.add_argument('--keys', nargs='+', default=[CANON_KEYS])
    ap.add_argument('--fill', default='fceux')
    a = ap.parse_args(argv)
    nes = NES(ram_fill=a.fill, wram_fill='zero')
    nes.keys = debug.parse_keys(a.keys)
    r = nes.bus.ram

    def hit(n):
        f = n.ppu.frame_no
        if not a.lo <= f <= a.hi:
            return
        s = n.cpu.s
        st = [r[0x100 | ((s + i) & 0xFF)] for i in range(1, 13)]
        ret = (st[0] | st[1] << 8) + 1
        print(f'f{f} main {r[0x40]:02X} sub {r[0x41]:02X} '
              f'step {r[0x6F1]:02X} ctr {r[0x6FF]:02X}  ret ${ret:04X} (JSR at ${ret - 3:04X})  stack '
              + ' '.join(f'{v:02X}' for v in st))
    nes.on_exec(a.bank, a.addr, hit)
    nes.run_frames(a.hi + 1)
    return 0


if __name__ == '__main__':
    sys.exit(main())
