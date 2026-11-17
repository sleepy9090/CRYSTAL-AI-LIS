#!/usr/bin/env python3
"""PPU / frame-loop tests for tools/emu (Ph5.5 emu 3/6).

Run: python3 tools/emu/test_ppu.py [--rom]   (from the repo root; --rom boots rom/re-c.nes)
Synthetic tests drive the PPU directly with a fake CPU (cycle counter + NMI/IRQ flags) and a
CHR-RAM mapper; the MMC3 split test uses the real cart's MMC3.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from emu.ppu import PPU, decode  # noqa: E402
from emu.pads import Pads, parse_buttons  # noqa: E402
from test_cpu import eq  # noqa: E402

_results = []
FRAME_DOTS = 262 * 341


def test(fn):
    try:
        fn()
        _results.append((fn.__name__, None))
    except AssertionError as e:
        _results.append((fn.__name__, str(e) or 'assert'))
    except Exception as e:  # noqa: BLE001
        import traceback
        traceback.print_exc()
        _results.append((fn.__name__, f'{type(e).__name__}: {e}'))
    return fn


class FakeCpu:
    def __init__(self):
        self.cycles = 0
        self.op_cycles = 1
        self.nmis = 0
        self.irq_flags = 0

    def nmi(self):
        self.nmis += 1


class RamMapper:
    """8KB CHR-RAM, mirroring 0=vertical 1=horizontal, counts scanline clocks."""

    def __init__(self, mirroring=1):
        self.chr = bytearray(0x2000)
        self.mirroring = mirroring
        self.clocks = []
        self.ppu = None

    def chr_read(self, a):
        return self.chr[a & 0x1FFF]

    def chr_write(self, a, v):
        self.chr[a & 0x1FFF] = v

    def nt_index(self, a):
        a &= 0x0FFF
        if self.mirroring == 0:
            return a & 0x7FF
        return ((a >> 1) & 0x400) | (a & 0x3FF)

    def clock_scanline(self):
        self.clocks.append((self.ppu.line, self.ppu.dot))


def mk(mirroring=1):
    p = PPU()
    cpu = FakeCpu()
    m = RamMapper(mirroring)
    m.ppu = p
    p.attach(cpu, m)
    p.status = 0
    return p, cpu, m


def goto(p, cpu, line, dot, frame=0):
    """Run the PPU to (frame, line, dot) and park the fake CPU there (so sync() is a no-op)."""
    while p.frame_no < frame or (p.frame_no == frame and (p.line, p.dot) < (line, dot)):
        p.run_to(p.abs + 1)
    cpu.cycles = p.abs // 3 + 1
    cpu.op_cycles = 0          # sync target = (cycles - 1) * 3 <= abs


def w(p, r, v):
    p.write_reg(r, v)


def set_addr(p, a):
    w(p, 6, a >> 8)
    w(p, 6, a & 0xFF)


def solid_tile(m, base, tile, colour):
    """Fill tile `tile` of the pattern table at `base` with 2-bit colour `colour` (1-3)."""
    for row in range(8):
        m.chr[base + tile * 16 + row] = 0xFF if colour & 1 else 0
        m.chr[base + tile * 16 + 8 + row] = 0xFF if colour & 2 else 0


# ---------------------------------------------------------------- register semantics
@test
def scroll_latch_t_v_x_w():
    # NESdev "PPU scrolling" worked example
    p, cpu, m = mk()
    w(p, 0, 0x00)
    eq(p.t & 0x0C00, 0, 't NT bits')
    p.read_reg(2)
    eq(p.w, 0, 'w cleared by $2002')
    w(p, 5, 0x7D)
    eq(p.t & 0x1F, 0x0F, 't coarse X'); eq(p.x, 5, 'fine x'); eq(p.w, 1, 'w')
    w(p, 5, 0x5E)
    eq(p.t, 0x616F, 't after 2nd $2005 (fine Y 6, coarse Y 11, coarse X 15)')
    eq(p.w, 0, 'w')
    w(p, 6, 0x3D)
    eq(p.t & 0xFF00, 0x3D00, 't hi (bit 14 cleared)'); eq(p.w, 1, 'w')
    w(p, 6, 0xF0)
    eq(p.t, 0x3DF0, 't'); eq(p.v, 0x3DF0, 'v = t'); eq(p.w, 0, 'w')
    w(p, 0, 0x03)
    eq(p.t & 0x0C00, 0x0C00, '$2000 NT bits -> t')


@test
def status_read_clears_vblank_and_w():
    p, cpu, m = mk()
    p.status = 0xE0
    p.io_latch = 0x1F
    w(p, 5, 0)      # w=1
    p.io_latch = 0x15
    v = p.read_reg(2)
    eq(v, 0xF5, 'top bits status + low bits latch')
    eq(p.status & 0x80, 0, 'vblank cleared'); eq(p.status & 0x60, 0x60, 'hit/overflow kept')
    eq(p.w, 0, 'w reset')


@test
def vram_read_buffer_and_increment():
    p, cpu, m = mk()
    m.chr[0x0123:0x0126] = bytes([0x11, 0x22, 0x33])
    set_addr(p, 0x0123)
    eq(p.read_reg(7), 0, 'first read = stale buffer')
    eq(p.read_reg(7), 0x11, 'buffered')
    eq(p.read_reg(7), 0x22, 'buffered')
    eq(p.v, 0x0126, '+1 increment')
    w(p, 0, 0x04)
    set_addr(p, 0x2000)
    w(p, 7, 0xAA)
    eq(p.v, 0x2020, '+32 increment')
    w(p, 7, 0xBB)
    eq(p.ciram[0x20], 0xBB, 'write at $2020')
    # palette reads are immediate; buffer gets the nametable byte underneath ($2F00 -> $2700)
    w(p, 0, 0x00)
    set_addr(p, 0x2700)
    w(p, 7, 0x5A)
    set_addr(p, 0x3F00)
    w(p, 7, 0x2C)
    set_addr(p, 0x3F00)
    eq(p.read_reg(7) & 0x3F, 0x2C, 'palette read immediate')
    eq(p.buf, p.ciram[m.nt_index(0x2F00)], 'buffer <- NT under palette')


@test
def palette_mirroring():
    p, cpu, m = mk()
    set_addr(p, 0x3F10)
    w(p, 7, 0x21)
    eq(p.pal[0], 0x21, '$3F10 -> $3F00')
    set_addr(p, 0x3F0C)
    w(p, 7, 0x16)
    set_addr(p, 0x3F1C)
    eq(p.read_reg(7) & 0x3F, 0x16, '$3F1C reads $3F0C')
    set_addr(p, 0x3F11)
    w(p, 7, 0x30)
    eq(p.pal[0x11], 0x30, '$3F11 own entry'); eq(p.pal[0x01], 0, '$3F01 untouched')
    set_addr(p, 0x3FE0)
    w(p, 7, 0x0F)
    eq(p.pal[0], 0x0F, '$3FE0 mirrors $3F00')


@test
def nametable_mirroring():
    p, cpu, m = mk(mirroring=1)          # horizontal: $2000=$2400
    set_addr(p, 0x2405)
    w(p, 7, 0x77)
    set_addr(p, 0x2005)
    p.read_reg(7)
    eq(p.read_reg(7), 0x77, 'horizontal: $2405 == $2005')
    p, cpu, m = mk(mirroring=0)          # vertical: $2000=$2800
    set_addr(p, 0x2805)
    w(p, 7, 0x66)
    set_addr(p, 0x2005)
    p.read_reg(7)
    eq(p.read_reg(7), 0x66, 'vertical: $2805 == $2005')


@test
def oam_dma_and_2004():
    p, cpu, m = mk()
    w(p, 3, 0x10)
    p.oam_dma(bytes(range(256)))
    eq(p.oam[0x10], 0, 'DMA starts at OAMADDR'); eq(p.oam[0x0F], 0xFF, 'wraps')
    w(p, 3, 0x02)
    eq(p.read_reg(4), 0xF2 & 0xE3, 'attribute byte bits 2-4 read 0')
    w(p, 4, 0x99)
    eq(p.oam[2], 0x99, '$2004 write'); eq(p.oam_addr, 3, 'OAMADDR++')


# ---------------------------------------------------------------- timing
@test
def vblank_nmi_and_frame_length():
    p, cpu, m = mk()
    w(p, 0, 0x80)
    goto(p, cpu, 241, 1)
    eq(p.status & 0x80, 0, 'not yet at 241:1 start')
    p.run_to(p.abs + 1)
    eq(p.status & 0x80, 0x80, 'vblank at 241:1'); eq(cpu.nmis, 1, 'NMI')
    eq(p.abs, 241 * 341 + 2, 'abs dot')
    goto(p, cpu, 261, 2)
    eq(p.status & 0xE0, 0, 'flags cleared at 261:1')
    # frame lengths: rendering off -> 89342 every frame; on -> odd frames 1 dot shorter
    goto(p, cpu, 0, 0, frame=1)
    eq(p.abs, FRAME_DOTS, 'frame 0 length (even, off)')
    w(p, 1, 0x08)
    a = p.abs
    goto(p, cpu, 0, 0, frame=2)
    eq(p.abs - a, FRAME_DOTS - 1, 'odd frame with rendering skips a dot')
    a = p.abs
    goto(p, cpu, 0, 0, frame=3)
    eq(p.abs - a, FRAME_DOTS, 'even frame full length')


@test
def nmi_on_ctrl_write_during_vblank():
    p, cpu, m = mk()
    goto(p, cpu, 250, 0)
    eq(cpu.nmis, 0, 'NMI off')
    w(p, 0, 0x80)
    eq(cpu.nmis, 1, 'enabling NMI inside vblank fires it')
    w(p, 0, 0x80)
    eq(cpu.nmis, 1, 'no second NMI without a 0->1 edge')


@test
def sync_uses_cpu_cycles():
    p, cpu, m = mk()
    cpu.cycles, cpu.op_cycles = 1000, 4
    p.read_reg(2)
    eq(p.abs, (1000 + 4 - 1) * 3, 'register access syncs PPU to the bus cycle')


# ---------------------------------------------------------------- rendering
def setup_bg(p, m, ctrl=0x08, mask=0x1E):
    solid_tile(m, 0x0000, 1, 1)
    solid_tile(m, 0x0000, 2, 2)
    solid_tile(m, 0x1000, 1, 3)      # sprite tile 1 = colour 3
    for i, c in enumerate([0x0F, 0x01, 0x02, 0x03, 0x0F, 0x05, 0x06, 0x07]):
        p.pal[i] = c
    for i, c in enumerate([0x0F, 0x11, 0x12, 0x13, 0x0F, 0x25, 0x26, 0x27]):
        p.pal[0x10 + i] = c
    w(p, 0, ctrl)
    w(p, 1, mask)


@test
def bg_render_scroll_and_left_mask():
    p, cpu, m = mk()
    # NT0: column 3 = tile 1 (colour 1 -> $01), everything else 0 (transparent -> backdrop $0F)
    for r in range(30):
        p.ciram[r * 32 + 3] = 1
    p.ciram[0x3C0] = 0x00
    setup_bg(p, m)
    goto(p, cpu, 0, 0, frame=2)
    fb = p.fb
    eq(fb[10 * 256 + 24], 0x01, 'tile column 3 at x=24'); eq(fb[10 * 256 + 31], 0x01, 'x=31')
    eq(fb[10 * 256 + 32], 0x0F, 'backdrop at x=32')
    # fine scroll x=4: column 3 now at x=20..27
    w(p, 5, 4)
    w(p, 5, 0)
    goto(p, cpu, 0, 0, frame=3)
    eq(fb[10 * 256 + 20], 0x01, 'scrolled 4px'); eq(fb[10 * 256 + 28], 0x0F, 'end of tile')
    # column 0 under the left mask
    for r in range(30):
        p.ciram[r * 32 + 0] = 2
    w(p, 5, 0)
    w(p, 5, 0)
    w(p, 1, 0x1C)               # BG left 8 hidden
    goto(p, cpu, 0, 0, frame=4)
    eq(fb[10 * 256 + 3], 0x0F, 'left 8 masked')
    w(p, 1, 0x1E)
    goto(p, cpu, 0, 0, frame=5)
    eq(fb[10 * 256 + 3], 0x02, 'left 8 shown')


@test
def attribute_palette_select():
    p, cpu, m = mk()
    for r in range(30):
        for c in range(32):
            p.ciram[r * 32 + c] = 1
    p.ciram[0x3C0] = 0b00000001          # top-left 16x16 uses palette 1
    setup_bg(p, m)
    goto(p, cpu, 0, 0, frame=2)
    eq(p.fb[5 * 256 + 5], 0x05, 'palette 1 colour 1 = $05')
    eq(p.fb[5 * 256 + 20], 0x01, 'palette 0 colour 1 = $01')


@test
def sprites_priority_flip_limit_and_8x16():
    p, cpu, m = mk()
    setup_bg(p, m)
    # half-opaque sprite tile 2: left 4 columns colour 1
    for row in range(8):
        m.chr[0x1000 + 2 * 16 + row] = 0xF0
    p.oam[0:4] = bytes([49, 1, 0x00, 100])        # sprite 0 on lines 50-57 at x=100, colour 3 -> $13
    p.oam[4:8] = bytes([49, 1, 0x01, 104])        # behind sprite 0 (overlaps x 104-107), palette 1
    p.oam[8:12] = bytes([59, 2, 0x40, 10])        # H-flipped: opaque at x=14..17
    p.oam[12:16] = bytes([69, 1, 0x20, 30])       # behind BG; BG row 70 transparent -> sprite shows
    for i in range(9):                            # 9 sprites on line 80-87: 9th dropped + overflow
        p.oam[16 + 4 * i:20 + 4 * i] = bytes([79, 1, 0x00, 120 + 8 * i])
    goto(p, cpu, 200, 0, frame=1)              # lines 0-199 drawn; flags not yet cleared
    fb = p.fb
    eq(fb[50 * 256 + 100], 0x13, 'sprite 0 colour'); eq(fb[49 * 256 + 100], 0x0F, 'not on line y')
    eq(fb[57 * 256 + 100], 0x13, 'last row'); eq(fb[58 * 256 + 100], 0x0F, '8 lines')
    eq(fb[50 * 256 + 105], 0x13, 'lower OAM index wins'); eq(fb[50 * 256 + 110], 0x27, 'sprite palette 1 ($3F17)')
    eq(fb[60 * 256 + 10], 0x0F, 'H flip: left half empty'); eq(fb[60 * 256 + 14], 0x11, 'H flip')
    eq(fb[70 * 256 + 30], 0x13, 'behind-BG sprite over transparent BG')
    eq(fb[80 * 256 + 120 + 7 * 8], 0x13, '8th sprite drawn')
    eq(fb[80 * 256 + 120 + 8 * 8], 0x0F, '9th sprite dropped')
    eq(p.status & 0x20, 0x20, 'overflow set')
    # 8x16: tile index bit 0 selects the table, top = even tile
    p, cpu, m = mk()
    setup_bg(p, m, ctrl=0x20)
    solid_tile(m, 0x1000, 4, 1)
    solid_tile(m, 0x1000, 5, 2)
    p.oam[0:4] = bytes([19, 0x05, 0x00, 50])      # tile $05: table $1000, tiles 4 (top) / 5 (bottom)
    p.oam[4:8] = bytes([39, 0x05, 0x80, 50])      # V flip: bottom tile on top
    goto(p, cpu, 0, 0, frame=2)
    eq(p.fb[22 * 256 + 52], 0x11, '8x16 top'); eq(p.fb[30 * 256 + 52], 0x12, '8x16 bottom')
    eq(p.fb[42 * 256 + 52], 0x12, 'V flip top = bottom tile'); eq(p.fb[50 * 256 + 52], 0x11, 'V flip')


@test
def sprite_behind_opaque_bg_and_sprite0_hit():
    p, cpu, m = mk()
    for r in range(30):
        p.ciram[r * 32 + 12] = 1                  # opaque BG column x=96..103
    setup_bg(p, m)
    p.oam[0:4] = bytes([99, 1, 0x20, 98])         # sprite 0 behind BG, overlaps x=98..103 on line 100
    goto(p, cpu, 0, 0, frame=1)
    goto(p, cpu, 100, 99, frame=1)
    eq(p.status & 0x40, 0, 'no hit before the overlap dot')
    goto(p, cpu, 100, 100, frame=1)
    eq(p.status & 0x40, 0x40, 'hit at line 100 x=98 (dot 99)')
    eq(p.fb[100 * 256 + 98], 0x01, 'BG wins over behind sprite')
    eq(p.fb[100 * 256 + 104], 0x13, 'sprite shows over transparent BG')
    goto(p, cpu, 261, 2, frame=1)
    eq(p.status & 0x40, 0, 'hit cleared on pre-render')
    # no hit when BG is masked in the left 8 and the overlap is at x<8
    p, cpu, m = mk()
    for r in range(30):
        p.ciram[r * 32] = 1
    setup_bg(p, m, mask=0x18)
    p.oam[0:4] = bytes([99, 1, 0x00, 0])
    goto(p, cpu, 0, 0, frame=2)
    eq(p.status & 0x40, 0, 'masked left column: no hit')


@test
def midframe_split_via_2006_and_mmc3_clock_points():
    # NT0 rows = tile 1, NT2 ($2800, horizontal mirroring -> second CIRAM page) = tile 2.
    p, cpu, m = mk(mirroring=1)
    for i in range(0x3C0):
        p.ciram[i] = 1
        p.ciram[0x400 + i] = 2
    setup_bg(p, m)                 # ctrl $08: sprites $1000, BG $0000 -> clock at dot 260
    goto(p, cpu, 0, 0, frame=1)
    goto(p, cpu, 120, 300, frame=1)
    # hblank split on line 120: point v at NT2 row 0 (what an IRQ handler would do)
    w(p, 6, 0x28)
    w(p, 6, 0x00)
    goto(p, cpu, 0, 0, frame=2)
    fb = p.fb
    eq(fb[120 * 256 + 50], 0x01, 'line 120 still NT0')
    eq(fb[121 * 256 + 50], 0x02, 'line 121 from NT2 (after hori copy + prefetch)')
    eq(fb[239 * 256 + 50], 0x02, 'rest of frame NT2')
    eq(fb[0 * 256 + 50], 0x01, 'next frame restarts at t (NT0)')
    lines = [ln for ln, d in m.clocks[-241:]]
    eq(len([c for c in m.clocks if c[1] == 260]), len(m.clocks), 'all clocks at dot 260')
    eq(lines[:3], [0, 1, 2], 'clocked on visible lines'); eq(261 in lines, True, 'and on pre-render')
    eq(240 in lines, False, 'not on post-render')
    # BG at $1000, sprites $0000 -> dot 324
    m.clocks.clear()
    w(p, 0, 0x10)
    goto(p, cpu, 5, 0, frame=3)
    eq(set(d for _, d in m.clocks), {324}, 'BG $1000 -> clock at dot 324')
    m.clocks.clear()
    w(p, 1, 0x00)
    goto(p, cpu, 0, 0, frame=4)
    eq(m.clocks, [], 'no clocks with rendering off')


@test
def mmc3_irq_split_line():
    """Real MMC3: latch N -> IRQ asserted at dot 260 of line N (counter reload on line 0)."""
    from emu.loader import load
    from emu.mapper import MMC3
    mm = MMC3(load())
    p = PPU()
    cpu = FakeCpu()

    class B:
        pass
    mm.bus = B()
    mm.bus.cpu = cpu
    p.attach(cpu, mm)
    w(p, 0, 0x08)
    w(p, 1, 0x18)
    goto(p, cpu, 0, 0, frame=1)
    mm.write(0xC000, 39)       # latch
    mm.write(0xC001, 0)        # reload
    mm.write(0xE001, 0)        # enable
    goto(p, cpu, 39, 260, frame=1)
    eq(cpu.irq_flags & 1, 0, 'not before line 39 dot 260')
    goto(p, cpu, 39, 261, frame=1)
    eq(cpu.irq_flags & 1, 1, 'IRQ at line 39 (latch+1 clocks: line 0 reloads, 39 decrements to 0)')


@test
def pads_shift_and_parse():
    pd = Pads()
    pd.set(0, parse_buttons('A+START+RIGHT'))
    pd.write_strobe(1)
    eq(pd.read(0), 1, 'strobe high -> A')
    pd.write_strobe(0)
    bits = [pd.read(0) for _ in range(10)]
    eq(bits, [1, 0, 0, 1, 0, 0, 0, 1, 1, 1], 'A B Sel St U D L R then 1s')
    eq(parse_buttons('none'), 0, 'none')


@test
def decode_pattern():
    eq(list(decode(0b10000001, 0b10000010)), [3, 0, 0, 0, 0, 0, 2, 1], 'bit planes')


# ---------------------------------------------------------------- ROM
def rom_boot():
    from emu.nes import NES
    nes = NES()
    nes.run_frames(120)
    p = nes.ppu
    eq(p.frame_no, 120, 'frames')
    eq(nes.cpu.cycles // 120 in (29780, 29781), True, f'~29780.67 CPU cycles/frame ({nes.cpu.cycles})')
    eq(p.nmi_count >= 115, True, f'NMI every frame after init ({p.nmi_count})')
    eq(p.ctrl, 0x88, 'PPUCTRL $88 (NMI on, sprites $1000) as documented')
    cols = sorted(set(p.fb))
    eq(len(cols) >= 2, True, f'logo frame has >1 colour ({cols})')
    eq(nes.mapper.irq_clocks > 0, True, 'MMC3 clocked by rendering')


if __name__ == '__main__':
    if '--rom' in sys.argv:
        test(rom_boot)
    bad = [(n, e) for n, e in _results if e]
    for n, e in _results:
        print(f'{"FAIL" if e else "ok  "} {n}' + (f': {e}' if e else ''))
    print(f'{len(_results) - len(bad)}/{len(_results)} pass')
    sys.exit(1 if bad else 0)
