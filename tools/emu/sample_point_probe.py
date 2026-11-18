#!/usr/bin/env python3
"""Ph6 triage (d): do RAM diffs vs FCEUX come from *when* in the frame each side samples RAM?

usage (repo root):
  python3 -m tools.emu.sample_point_probe [--state FILE] [--frames N] [--ref FILE.ram] [--addr HEX ...]

tools.emu frames end at the 261->0 wrap; FCEUX's Lua frame callback runs near the start of vblank.
Runs N frames from a save state and prints the watched bytes both at line 241 dot 0 (vblank start)
and at the frame end, next to the reference .ram values.
"""
import argparse
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if ROOT not in sys.path:
    sys.path.append(ROOT)

from tools.emu.nes import NES      # noqa: E402
from tools.emu import state        # noqa: E402

DEFAULT = ['6E', '6F', '09', '46', '47', '40', '41']


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--state', default=os.path.join(ROOT, 'tools', 'emu', 'states', 'area1_enter.state'))
    ap.add_argument('--frames', type=int, default=14)
    ap.add_argument('--ref', default=os.path.join(ROOT, 'ref', 'frame_2460.ram'))
    ap.add_argument('--addr', nargs='+', default=DEFAULT)
    ap.add_argument('--oam', type=int, default=0, metavar='N',
                    help='also diff the $0200 OAM page vs the ref at both sample points (print N sprites)')
    a = ap.parse_args(argv)
    addrs = [int(x, 16) for x in a.addr]
    nes = NES()
    state.load(nes, a.state)
    ref = open(a.ref, 'rb').read() if a.ref and os.path.exists(a.ref) else None
    hdr = ' '.join(f'${x:04X}' for x in addrs)
    print(f'{os.path.basename(a.state)} -> {a.frames} frames; columns {hdr}')
    if ref:
        print(f'  ref {os.path.basename(a.ref):18s}        ' + '  '.join(f'{ref[x]:02X}  ' for x in addrs))
    ram = nes.bus.ram
    for _ in range(a.frames):
        f = nes.ppu.frame_no
        while not (nes.ppu.line == 241 and nes.ppu.frame_no == f):
            nes.step()
        vb = [ram[x] for x in addrs]
        oam_vb = bytes(ram[0x200:0x300])
        nes.run_frame()
        end = [ram[x] for x in addrs]
        print(f'  f{f}  vblank(241) ' + '  '.join(f'{v:02X}  ' for v in vb)
              + '|  end ' + ' '.join(f'{v:02X}' for v in end))
        if ref and a.oam:
            for tag, page in (('vblank', oam_vb), ('end', bytes(ram[0x200:0x300]))):
                d = [k for k in range(64) if page[4 * k:4 * k + 4] != ref[0x200 + 4 * k:0x204 + 4 * k]]
                print(f'      OAM vs ref at {tag:6s}: {len(d)} sprites differ {d[:20]}')
                for k in d[:a.oam]:
                    print(f'        #{k:2d} ref {ref[0x200 + 4 * k:0x204 + 4 * k].hex(" ")}  emu {page[4 * k:4 * k + 4].hex(" ")}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
