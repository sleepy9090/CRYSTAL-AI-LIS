#!/usr/bin/env python3
"""World-interaction probe (session 77, Ph3 world-interaction leftovers).

usage (repo root):
  python3 -m tools.emu.world_probe [--state F] [--area AA[.EE]] [--poke ADDR=VAL ...]
        [--seg BUTTONS:FRAMES ...] [--every N] [--watch HHHH ...] [--grid]
        [--hook BB:AAAA ...] [--tick-poke ADDR=VAL ...]

Loads a save state (default gameplay_first), optionally pokes an area (like poke_probe: $6C/$6D,
sub_state=1, waits for sub_state 8), applies --poke RAM writes, then runs the --seg button segments
(BUTTONS = tools.emu names joined by +, or NONE) and prints one line per frame (or every N):
  slot0 x,y | slot1 x,y | cam $3C-$3F | drawn $02-$05 | $3A $3B | $0620 jump | $0641 attr |
  hp $03C1 | inv $071A | $0710 status | $41 | area
--watch logs every write to the given addresses with PC; --hook logs each execution of BB:AAAA.
--tick-poke re-applies the writes before every frame (e.g. keep an enemy pinned next to the player);
--poke-at N:ADDR=VAL writes once before run frame N (0-based over all segments). --objs lists the
active object slots before and after the run.
--grid prints the collision attr of the 16x16 cells around slot 0 (computed like ObjBgProbe
26:9A30 without the b3 substitution) before the run.
"""
import argparse
import os
import sys

from tools.emu.nes import NES
from tools.emu import state
from tools.emu.pads import parse_buttons

STATES = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'states')
BTN_A = 0x01


def prg(nes, bank, addr):
    return nes.cart.prg[bank * 0x2000 + (addr & 0x1FFF)]


def cell_attr(nes, x, y):
    """ObjBgProbe lookup (no b3 substitution): x, y = 16-bit world position (y in 240-row pages)."""
    r = nes.bus.ram
    xh, xl, yh, yl = x >> 8 & 0xFF, x & 0xFF, y >> 8 & 0xFF, y & 0xFF
    g = nes.bus.read(0x6300 + ((yh * 8 + xh) & 0xFF))
    bank = g >> 5
    page = 0xA0 | (g & 0x1F)
    m = prg(nes, bank, page * 256 + ((yl & 0xF0) | (xl >> 4)))
    return m, prg(nes, 9, r[0x69] * 256 + m)


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--state', default=os.path.join(STATES, 'gameplay_first.state'))
    ap.add_argument('--area')
    ap.add_argument('--poke', nargs='*', default=[])
    ap.add_argument('--tick-poke', nargs='*', default=[])
    ap.add_argument('--poke-at', nargs='*', default=[], help='N:ADDR=VAL = write before run frame N (0-based)')
    ap.add_argument('--seg', nargs='*', default=['NONE:60'])
    ap.add_argument('--every', type=int, default=1)
    ap.add_argument('--watch', nargs='*', default=[])
    ap.add_argument('--hook', nargs='*', default=[])
    ap.add_argument('--grid', action='store_true')
    ap.add_argument('--map', action='store_true')
    ap.add_argument('--objs', action='store_true', help='print the active object slots before the run')
    a = ap.parse_args(argv)
    nes = NES()
    state.load(nes, a.state)
    r = nes.bus.ram

    def wr(ad, v):
        if ad < 0x800:
            r[ad] = v
        else:
            nes.bus.write(ad, v)

    if a.area:
        ar, _, en = a.area.partition('.')
        r[0x6C], r[0x6D], r[0x41] = int(ar, 16), int(en or '0', 16), 1
        t0, left = nes.ppu.frame_no, False
        while nes.ppu.frame_no - t0 < 900:
            f = nes.ppu.frame_no - t0
            nes.keys = {nes.ppu.frame_no: BTN_A} if f > 150 and f % 8 == 4 else {}
            nes.run_frame()
            left |= r[0x41] != 8
            if left and r[0x41] == 8:
                break
        nes.keys = {}
        print(f'area {r[0x6C]:02X}.{r[0x6D]:02X} loaded at f{nes.ppu.frame_no} (sub_state {r[0x41]:02X})')
    for p in a.poke:
        ad, v = p.split('=')
        wr(int(ad, 16), int(v, 16))
    ticks = [(int(ad, 16), int(v, 16)) for ad, v in (p.split('=') for p in a.tick_poke)]
    at = {}
    for p in a.poke_at:
        n, _, rest = p.partition(':')
        ad, v = rest.split('=')
        at.setdefault(int(n), []).append((int(ad, 16), int(v, 16)))
    log = []
    for w in a.watch:
        ad = int(w, 16)

        def cb(addr, val, ad=ad):
            pc = nes.cpu.pc
            bank = nes.mapper.prg_bank_at(pc) if pc >= 0x8000 else -1
            log.append(f'    ${ad:04X} <- {val:02X}  pc {bank}:{pc:04X}')
        nes.on_write(ad, cb)
    for h in a.hook:
        b, ad = h.split(':')

        def hk(*_, h=h):
            c = nes.cpu
            log.append(f'    exec {h} A={c.a:02X} X={c.x:02X} Y={c.y:02X}')
        nes.on_exec(int(b), int(ad, 16), hk)
    def objs():
        print('active slots: slot AI x,y attack($03E0) defence($0400) hitflags($0420) b2($0540) param($0560)')
        for s in range(32):
            if r[0x4A0 + s]:
                print(f'  {s:02X} ai {r[0x4A0 + s] & 0x7F:02X} x,y {r[0x90 + s]:02X}{r[0x70 + s]:02X},'
                      f'{r[0xD0 + s]:02X}{r[0xB0 + s]:02X} atk {r[0x3E0 + s]:02X} def {r[0x400 + s]:02X} '
                      f'hit {r[0x420 + s]:02X} b2 {r[0x540 + s]:02X} prm {r[0x560 + s]:02X}')
    if a.objs:
        objs()
    if a.map:
        w, h = nes.bus.read(0x62FC) + 1, nes.bus.read(0x62FD) + 1
        print(f'map area {r[0x6C]:02X} {w}x{h} screens ($69={r[0x69]:02X}); one char per 16x16 cell: '
              '. 00  # b2 wall  o b1-only obstacle  p b0 pit  c b5 conveyor  s b6 sink  D b7 floor  '
              'f b4-only  v b3-only')
        for yh in range(h):
            for row in range(15):
                line = []
                for xh in range(w):
                    for col in range(16):
                        _, at = cell_attr(nes, xh << 8 | col << 4, yh << 8 | row << 4)
                        ch = ('D' if at & 0x80 else 'p' if at & 1 else 'c' if at & 0x20 else '#' if at & 4
                              else 'o' if at & 2 else 's' if at & 0x40 else 'f' if at & 0x10
                              else 'v' if at & 8 else '.')
                        line.append(ch)
                    line.append('|')
                print(f'  {yh:X}.{row * 16:02X} ' + ''.join(line))
    if a.grid:
        x0 = r[0x70] | r[0x90] << 8
        y0 = r[0xB0] | r[0xD0] << 8
        print(f'grid around slot0 {x0:04X},{y0:04X} ($69={r[0x69]:02X}): cells x-48..+48, y-48..+48 (meta:attr)')
        for dy in range(-48, 49, 16):
            row = []
            for dx in range(-48, 49, 16):
                x = x0 + dx
                my = (y0 >> 8) * 240 + (y0 & 0xFF) + dy  # 240-row pages
                yh, yl = divmod(my, 240)
                m, at = cell_attr(nes, x & 0xFFFF, (yh << 8 | yl) & 0xFFFF)
                row.append(f'{m:02X}:{at:02X}')
            print(f'  dy{dy:+4d} ' + ' '.join(row))
    segs = []
    for s in a.seg:
        b, n = s.rsplit(':', 1)
        segs.append((0 if b == 'NONE' else parse_buttons(b), int(n), s))
    i = 0
    for mask, n, name in segs:
        print(f'-- seg {name}')
        for _ in range(n):
            for ad, v in ticks:
                wr(ad, v)
            for ad, v in at.get(i, []):
                wr(ad, v)
            nes.keys = (lambda m: (lambda _f: m))(mask)
            nes.run_frame()
            i += 1
            if i % a.every == 0 or log:
                x0, y0 = r[0x70] | r[0x90] << 8, r[0xB0] | r[0xD0] << 8
                x1, y1 = r[0x71] | r[0x91] << 8, r[0xB1] | r[0xD1] << 8
                print(f'  f{nes.ppu.frame_no} p0 {x0:04X},{y0:04X} p1 {x1:04X},{y1:04X} '
                      f'cam {r[0x3D]:02X}{r[0x3C]:02X},{r[0x3F]:02X}{r[0x3E]:02X} '
                      f'drawn {r[0x03]:02X}{r[0x02]:02X},{r[0x05]:02X}{r[0x04]:02X} '
                      f'ofs {r[0x3A]:02X} {r[0x3B]:02X} jmp {r[0x620]:02X} attr {r[0x641]:02X} '
                      f'hp {r[0x3C1]:3d} inv {r[0x71A]:02X} st {r[0x710]:02X} '
                      f'ss {r[0x41]:02X} area {r[0x6C]:02X}')
            for line in log:
                print(line)
            log.clear()
    if a.objs:
        objs()
    return 0


if __name__ == '__main__':
    sys.exit(main())
