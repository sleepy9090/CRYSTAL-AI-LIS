#!/usr/bin/env python3
"""s175: print the OAM shadow $0200-$02FF (as the compare CSV's oam_crc row sees it) at chosen frames of a
tools.emu run from a save state. Port counterpart: node web/test/oam_dump.mjs.

usage: python3 -m tools.emu.oam_dump STATE FRAME[,FRAME..] [--keys SPEC ..]
Output: one line per frame and 4-byte sprite (Y tile attr X), only sprites with Y < $F0."""
import sys
import zlib

from tools.emu.nes import NES
from tools.emu import debug, state


def main(argv):
    show_all = '--all' in argv                         # s193: also the hidden (Y >= $F0) entries
    argv = [a for a in argv if a != '--all']
    st, frames = argv[0], sorted(int(f) for f in argv[1].split(','))
    keys = argv[argv.index('--keys') + 1:] if '--keys' in argv else []
    nes = NES()
    state.load(nes, st)
    if keys:
        nes.keys = debug.parse_keys(keys)
    while nes.ppu.frame_no <= frames[-1]:
        nes.run_frame()
        f = nes.ppu.frame_no - 1                       # the row just finished (compare.py row numbering)
        if f in frames:
            o = bytes(nes.bus.ram[0x200:0x300])
            spr = [f'{i:02X}:{o[i * 4]:02X} {o[i * 4 + 1]:02X} {o[i * 4 + 2]:02X} {o[i * 4 + 3]:02X}'
                   for i in range(64) if o[i * 4] < 0xF0 or show_all]
            print(f'f{f} crc {zlib.crc32(o)}  ' + '  '.join(spr))


if __name__ == '__main__':
    main(sys.argv[1:])
