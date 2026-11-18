#!/usr/bin/env python3
"""Render the title/menu/cut-scene nametable screens of banks 18/19 (NtRectBlit 18:81F0).

usage: render_rects.py [--list]

Static replay of the bank 18/19 screen-setup code (window $8000-$BFFF = banks 18/19,
SwitchBank16(9)). Events are found by byte pattern, in address order:
  A9 v 8D F0 07 / A9 v 8D F1 07   BG CHR R0 / R1 (2KB, 1KB units)  -> starts a new screen
  A9 v 8D FE 06                   tile bias $06FE (added to every rect tile)
  A9 v 20 BE 82                   MenuPalLoad 18:82BE: v b7=0 -> BG palette set 18:838B + (v&15)*16
  A9 hi A2 t A0 at 20 A1 81       NtFill 18:81A1: nametable hi, tile t x $3C0, attr at x $40
  AD lo hi 85 8E AD lo+1 hi 85 8F A9 l 85 8C A9 h 85 8D 20 F0 81
                                  NtRectBlit: stream ptr word at hi:lo, PPU dest h:l
The rect stream = W H then H rows of W tiles; each byte + $06FE goes to PPU dest, row stride $20.
Writes into $23C0-$23FF land in the attribute table (modelled). Only nametable $2000 is drawn
(and $2400 to the right when any write targets it). Output gfx/rects/screen_BB_AAAA.png
(BB:AAAA = address of the first event of that screen) + gfx/rects/index.txt.
Static model: branches inside the setup code are ignored, so a screen may mix alternatives.
"""
import os, re, sys
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom, ROOT  # noqa
from meta_dump import write_png  # noqa
import palette_dump  # noqa

_, PRG, CHR = load_rom()
WIN = PRG[18 * 0x2000:20 * 0x2000]  # $8000-$BFFF


def rd(a):
    return WIN[a - 0x8000]


def cpu(off):
    return 0x8000 + off


def bankaddr(a):
    return f'{18 + ((a - 0x8000) >> 13)}:{a:04X}'


PATS = [
    ('r0', rb'\xA9(.)\x8D\xF0\x07'), ('r1', rb'\xA9(.)\x8D\xF1\x07'),
    ('bias', rb'\xA9(.)\x8D\xFE\x06'), ('pal', rb'\xA9(.)\x20\xBE\x82'),
    ('fill', rb'\xA9(.)\xA2(.)\xA0(.)\x20\xA1\x81'),
    # s68: up to 4 simple LDA #/STA zp/STA abs fillers may sit between the PPU-dest setup and the JSR
    # (19:A80F, the opening date card: LDA #0 / STA $06FE / STA $04 / STA $05 before JSR at 19:A82A)
    ('blit', rb'\xAD(..)\x85\x8E\xAD(..)\x85\x8F\xA9(.)\x85\x8C\xA9(.)\x85\x8D'
             rb'(?:\xA9.|\x85.|\x8D..){0,4}\x20\xF0\x81'),
]


def events():
    """(sort addr, name, groups, shown addr). A blit sorts at its JSR, so fillers before it
    (e.g. a $06FE bias store) apply first; it is still shown at its first LDA."""
    ev = []
    for name, p in PATS:
        for m in re.finditer(p, WIN, re.S):
            g = [x[0] if len(x) == 1 else x[0] | x[1] << 8 for x in m.groups()]
            at = cpu(m.end() - 3) if name == 'blit' else cpu(m.start())
            ev.append((at, name, g, cpu(m.start())))
    ev.sort()
    return ev


def screens(ev):
    """Split at every R0 store or NtFill (a new page); register state carries over."""
    out, cur = [], None
    for e in ev:
        if e[1] in ('r0', 'fill') or cur is None:
            if not (cur and e[1] == 'fill' and all(x[1] != 'blit' for x in cur)):
                cur = []; out.append(cur)
        cur.append(e)
    return out


def build(scr, st):
    vram = bytearray(0x1000)  # $2000-$2FFF, 4 nametables (mirroring not modelled)
    log, used, snap = [], set(), dict(st)
    for _, name, g, a in scr:
        if name in ('r0', 'r1', 'bias'):
            st[name] = g[0]
        elif name == 'pal':
            if not g[0] & 0x80:
                st['pal'] = g[0] & 15
            log.append(f'  {bankaddr(a)} MenuPalLoad {g[0]:02X} ({"sprite" if g[0] & 0x80 else "BG"} set {g[0] & 15})')
        elif name == 'fill':
            hi, t, at = g
            if hi in (0x20, 0x24, 0x28, 0x2C):
                base = (hi - 0x20) << 8
                used.add(base >> 10)
                vram[base:base + 0x3C0] = bytes([t]) * 0x3C0
                vram[base + 0x3C0:base + 0x400] = bytes([at]) * 0x40
                log.append(f'  {bankaddr(a)} NtFill ${hi:02X}00 tile {t:02X} attr {at:02X}')
        elif name == 'blit':
            pa, _, l, h = g
            sp = rd(pa) | rd(pa + 1) << 8
            if not 0x8000 <= sp < 0xBFFE:
                log.append(f'  {bankaddr(a)} blit ptr@{pa:04X} -> {sp:04X} (outside 18/19, skipped)')
                continue
            w, hh = rd(sp), rd(sp + 1)
            dest = h << 8 | l
            snap.update(st)
            log.append(f'  {bankaddr(a)} blit ptr@{pa:04X} data {bankaddr(sp)} {w}x{hh} -> ${dest:04X} '
                       f'bias {st["bias"]:02X} R0={st["r0"]:02X} R1={st["r1"]:02X} pal {st["pal"]}')
            p = sp + 2
            for r in range(hh):
                for c in range(w):
                    v = (dest + r * 0x20 + c) - 0x2000
                    if 0 <= v < 0x1000 and p < 0xC000:
                        vram[v] = (rd(p) + st['bias']) & 0xFF
                        used.add(v >> 10)
                    p += 1
    return vram, log, sorted(used), snap


def render(vram, st, used, out):
    regs = [st['r0'] & 0xFE, (st['r0'] & 0xFE) + 1, st['r1'] & 0xFE, (st['r1'] & 0xFE) + 1]
    pb = 0x838B + st['pal'] * 16 - 0x8000
    cols = list(WIN[pb:pb + 16])
    pals = [[palette_dump.rgb(cols[0] if i == 0 else cols[g * 4 + i]) for i in range(4)] for g in range(4)]
    nts = len(used) or 1
    W, H = 256 * nts, 240
    px = [(0, 0, 0)] * (W * H)
    for nt, ntid in enumerate(used or [0]):
        base = ntid * 0x400
        for ty in range(30):
            for tx in range(32):
                t = vram[base + ty * 32 + tx]
                ab = vram[base + 0x3C0 + (ty >> 2) * 8 + (tx >> 2)]
                g = (ab >> (((ty >> 1) & 1) * 4 + ((tx >> 1) & 1) * 2)) & 3
                o = regs[t >> 6] * 0x400 + (t & 0x3F) * 16
                d = CHR[o:o + 16]
                for y in range(8):
                    row = (ty * 8 + y) * W + nt * 256 + tx * 8
                    for x in range(8):
                        ci = ((d[y] >> (7 - x)) & 1) | (((d[y + 8] >> (7 - x)) & 1) << 1)
                        px[row + x] = pals[g][ci]
    write_png(out, W, H, px)


def main(argv):
    ev = events()
    scrs = screens(ev)
    d = os.path.join(ROOT, 'gfx', 'rects'); os.makedirs(d, exist_ok=True)
    idx = []
    st = {'r0': 0, 'r1': 2, 'bias': 0, 'pal': 0}
    n = 0
    # s68: a page whose group has no BG MenuPalLoad is drawn by the same step as the next page and
    # shown with that page's set (19:A95E: step A953 draws $2000 + $2800, then BG set 2 + fade-in)
    bgpal = [next((e[2][0] & 15 for e in s if e[1] == 'pal' and not e[2][0] & 0x80), None) for s in scrs]
    for i, s in enumerate(scrs):
        vram, log, used, snap = build(s, st)
        if not any(e[1] == 'blit' for e in s):
            continue
        n += 1
        # palette: the BG set loaded anywhere in the group (often after the blits); CHR/bias: at the last blit
        snap['pal'] = st['pal']
        if bgpal[i] is None:
            nxt = next((p for p in bgpal[i + 1:] if p is not None), None)
            if nxt is not None:
                snap['pal'] = nxt
        name = f'screen_{bankaddr(s[0][3]).replace(":", "_")}.png'
        idx.append(f'{name}: {len(s)} events, R0={snap["r0"]:02X} R1={snap["r1"]:02X} pal set {snap["pal"]} '
                   f'nametables {" ".join("$%X" % (0x2000 + u * 0x400) for u in used)}')
        idx += log
        if '--list' not in argv:
            render(vram, snap, used, os.path.join(d, name))
    scrs = [None] * n
    nblit = sum(1 for e in ev if e[1] == 'blit')
    idx.insert(0, f'{len(scrs)} screens, {nblit} blit sites matched (pattern), events {len(ev)}')
    with open(os.path.join(d, 'index.txt'), 'w') as f:
        f.write('\n'.join(idx) + '\n')
    print('\n'.join(idx))


if __name__ == '__main__':
    main(sys.argv[1:])
