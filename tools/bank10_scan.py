#!/usr/bin/env python3
"""$62FF b7 areas: could the bank-10 row/column quirk (31:EBE5 / EC04 / EC87) ever show?

usage: bank10_scan.py     -> stdout

ScreenRowRender 31:EBE5 does SetR6($0A) only for the first screen of a row; the second screen
of a row (EC04) and BgColRender 31:EC87 read the normal bank 2*(S>>6)+(S>>5&1).
For each $62FF area: W, H, grid, and per screen id with S&$3F < $20 how many of its 240
page bytes differ between bank 10 and the normal bank (0 = the quirk is invisible).
Also the camera range (CameraClampX: x -8 .. (W-1)*256, so a W=1 area still spans 2 screens
at x=-8 = left neighbour column, which is grid x -1 = outside -> $80 page). Static only (s68).
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import render_screen as rs  # noqa


def main():
    for area in range(256):
        a = rs.area_bg(area)
        if a is None or not a['bank10']:
            continue
        ids = sorted({s for row in a['grid'] for s in row})
        print(f'area ${area:02X}  W={a["W"]} H={a["H"]}  62FF={a["f62FF"]:02X}  grid '
              + ' / '.join(' '.join(f'{s:02X}' for s in row) for row in a['grid']))
        for s in ids:
            if (s & 0x3F) < 0x20:
                b10, nb = rs.page_bytes(s, True), rs.page_bytes(s, False)
                diff = sum(1 for i in range(240) if b10[i] != nb[i])
                print(f'    screen ${s:02X}: bank-10 page vs normal bank {rs.page_loc(s)[0]}: {diff}/240 bytes differ')
            else:
                print(f'    screen ${s:02X}: S&$3F >= $20, normal bank either way')


if __name__ == '__main__':
    main()
