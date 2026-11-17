"""2C02 PPU for the re-c emulator (Ph5.5 emu 3/6).

Timing model
  NTSC: 262 lines x 341 dots, 3 PPU dots per CPU cycle. Lines 0-239 visible, 240 post-render,
  241-260 vblank (flag + NMI at line 241 dot 1), 261 pre-render (flags cleared at dot 1,
  vertical scroll copy t->v at dots 280-304, odd frames skip dot 340 when rendering is on).
  The PPU keeps an absolute dot counter `abs`; `run_to(abs_dot)` advances it (the frame loop
  calls it with cpu.cycles*3 after every instruction). Every register access first calls
  `sync()`, which runs the PPU up to the bus cycle of that access (cpu.cycles + cpu.op_cycles - 1),
  so mid-line writes ($2006 splits, $2001, CHR bank switches seen through the mapper) land on
  the right pixel and $2002 polls see sprite-0 hit on the right dot ("catch-up" rendering).

Rendering (per dot range, not per dot)
  BG: the tile fetch + coarse-X increment of each 8-dot group is done as one event at dots
  8,16,..,256 (plus the next line's two prefetch tiles at dots 328/336), Y increment at 256,
  horizontal copy t->v at 257. Each fetched tile is stored as (8 pixel values, palette<<2), and
  pixel x (output at dot x+1) uses tile (x + fine_x) >> 3 of the line, so scroll changes behave
  like hardware's 2-tile fetch pipeline. Pattern bytes go through mapper.chr_read with the CHR
  banks at fetch time.
  Sprites: evaluation + pattern fetch for line N+1 happens at dot 257 of line N (before the MMC3
  clock at dot 260, so an IRQ handler's CHR switch affects the next sprite fetch, not this one).
  First 8 in-range OAM entries; a 9th sets the overflow flag (the hardware's diagonal-scan
  overflow bug is not modelled). 8x8 / 8x16, H/V flip, behind-BG priority, first opaque sprite
  wins. Sprite-0 hit: opaque sprite-0 pixel over opaque BG pixel, x != 255, both layers enabled
  and not inside a masked left column.
  Output: `fb` = 256x240 bytearray of NES colour indices ($00-$3F; greyscale applied, colour
  emphasis bits ignored). `to_rgb()` converts with the 2C02 table from palette_dump.NES_RGB.

MMC3 clock (approximate A12 model): at dot 260 of lines 0-239 and 261 while rendering, if sprites
use $1000 and BG $0000 (re-c: PPUCTRL $88) or 8x16 sprites; at dot 324 if BG $1000 / sprites $0000.

Not modelled: PPU warm-up (writes ignored for ~29658 cycles after power), the $2002-read/vblank
race that suppresses NMI, the 9th-sprite overflow scan bug, OAM corruption/$2004 writes during
rendering, open-bus decay, colour emphasis, the 2-3 dot delay of the second $2006 write.
"""

_DEC = {}


def decode(lo, hi):
    """Pattern bytes -> bytes of 8 two-bit pixel values, leftmost first."""
    k = lo << 8 | hi
    r = _DEC.get(k)
    if r is None:
        r = bytes(((lo >> (7 - i)) & 1) | (((hi >> (7 - i)) & 1) << 1) for i in range(8))
        _DEC[k] = r
    return r


def _pal_index(a):
    a &= 0x1F
    if (a & 0x13) == 0x10:          # $3F10/14/18/1C mirror $3F00/04/08/0C
        a &= 0x0F
    return a


class PPU:
    LINES = 262
    DOTS = 341

    def __init__(self):
        self.cpu = None
        self.mapper = None
        self.power_on()

    def attach(self, cpu, mapper):
        self.cpu = cpu
        self.mapper = mapper

    # ------------------------------------------------------------ state
    def power_on(self):
        self.ctrl = self.mask = 0
        self.status = 0xA0          # typical power-up value (vblank + overflow set)
        self.oam_addr = 0
        self.v = self.t = self.x = self.w = 0
        self.buf = 0
        self.io_latch = 0
        self.oam = bytearray(256)
        self.ciram = bytearray(0x800)
        self.pal = bytearray(32)
        self.fb = bytearray(256 * 240)
        self.line = 0
        self.dot = 0
        self.abs = 0
        self.frame_no = 0
        self.odd = 0
        self.tiles = [None] * 34
        self.next_tiles = [None, None]
        self.spr_line = None
        self.spr_next = None
        self.nmi_count = 0
        self.on_frame = None        # optional callable(ppu) when line 240 starts (frame complete)
        self.split_log = None       # optional list: (frame, line, dot, reg, value) of writes on lines 0-239

    _STATE_KEYS = ('ctrl', 'mask', 'status', 'oam_addr', 'v', 't', 'x', 'w', 'buf', 'io_latch',
                   'line', 'dot', 'abs', 'frame_no', 'odd', 'nmi_count')

    def state(self):
        d = {k: getattr(self, k) for k in self._STATE_KEYS}
        for k in ('oam', 'ciram', 'pal', 'fb'):
            d[k] = bytes(getattr(self, k))
        d['tiles'] = list(self.tiles)
        d['next_tiles'] = list(self.next_tiles)
        d['spr_line'] = bytes(self.spr_line) if self.spr_line else None
        d['spr_next'] = bytes(self.spr_next) if self.spr_next else None
        return d

    def load_state(self, d):
        for k in self._STATE_KEYS:
            setattr(self, k, d[k])
        for k in ('oam', 'ciram', 'pal', 'fb'):
            setattr(self, k, bytearray(d[k]))
        self.tiles = list(d['tiles'])
        self.next_tiles = list(d['next_tiles'])
        self.spr_line = bytearray(d['spr_line']) if d['spr_line'] else None
        self.spr_next = bytearray(d['spr_next']) if d['spr_next'] else None

    @property
    def rendering(self):
        return bool(self.mask & 0x18)

    # ------------------------------------------------------------ VRAM
    def vram_read(self, a):
        a &= 0x3FFF
        if a < 0x2000:
            return self.mapper.chr_read(a)
        if a < 0x3F00:
            return self.ciram[self.mapper.nt_index(a)]
        return self.pal[_pal_index(a)]

    def vram_write(self, a, v):
        a &= 0x3FFF
        if a < 0x2000:
            self.mapper.chr_write(a, v)
        elif a < 0x3F00:
            self.ciram[self.mapper.nt_index(a)] = v
        else:
            self.pal[_pal_index(a)] = v & 0x3F

    # ------------------------------------------------------------ CPU registers
    def sync(self):
        cpu = self.cpu
        if cpu is not None:
            self.run_to((cpu.cycles + cpu.op_cycles - 1) * 3)

    def read_reg(self, r):
        self.sync()
        if r == 2:
            v = (self.status & 0xE0) | (self.io_latch & 0x1F)
            self.status &= 0x7F
            self.w = 0
        elif r == 4:
            v = self.oam[self.oam_addr]
            if (self.oam_addr & 3) == 2:
                v &= 0xE3
        elif r == 7:
            a = self.v & 0x3FFF
            if a >= 0x3F00:
                v = (self.pal[_pal_index(a)] & 0x3F) | (self.io_latch & 0xC0)
                self.buf = self.vram_read(a - 0x1000)
            else:
                v = self.buf
                self.buf = self.vram_read(a)
            self._inc_v()
        else:
            v = self.io_latch          # write-only registers read the PPU bus latch
        self.io_latch = v
        return v

    def write_reg(self, r, v):
        self.sync()
        self.io_latch = v
        if self.split_log is not None and self.line < 240 and r in (0, 1, 5, 6):
            self.split_log.append((self.frame_no, self.line, self.dot, r, v))
        if r == 0:
            old = self.ctrl
            self.ctrl = v
            self.t = (self.t & 0xF3FF) | ((v & 3) << 10)
            if (v & 0x80) and not (old & 0x80) and (self.status & 0x80) and self.cpu:
                self.cpu.nmi()
                self.nmi_count += 1
        elif r == 1:
            self.mask = v
        elif r == 3:
            self.oam_addr = v
        elif r == 4:
            self.oam[self.oam_addr] = v
            self.oam_addr = (self.oam_addr + 1) & 0xFF
        elif r == 5:
            if not self.w:
                self.t = (self.t & 0x7FE0) | (v >> 3)
                self.x = v & 7
                self.w = 1
            else:
                self.t = (self.t & 0x0C1F) | ((v & 7) << 12) | ((v & 0xF8) << 2)
                self.w = 0
        elif r == 6:
            if not self.w:
                self.t = (self.t & 0x00FF) | ((v & 0x3F) << 8)
                self.w = 1
            else:
                self.t = (self.t & 0x7F00) | v
                self.v = self.t
                self.w = 0
        elif r == 7:
            self.vram_write(self.v, v)
            self._inc_v()

    def _inc_v(self):
        if (self.mask & 0x18) and (self.line < 240 or self.line == 261):
            self._inc_x()                # $2007 access while rendering: coarse X + Y glitch
            self._inc_y()
        else:
            self.v = (self.v + (32 if self.ctrl & 4 else 1)) & 0x7FFF

    def oam_dma(self, data):
        self.sync()
        a = self.oam_addr
        oam = self.oam
        for i, b in enumerate(data):
            oam[(a + i) & 0xFF] = b

    # ------------------------------------------------------------ scroll helpers
    def _inc_x(self):
        v = self.v
        if (v & 0x1F) == 31:
            self.v = (v & ~0x1F) ^ 0x400
        else:
            self.v = v + 1

    def _inc_y(self):
        v = self.v
        if (v & 0x7000) != 0x7000:
            self.v = v + 0x1000
            return
        v &= ~0x7000
        y = (v & 0x3E0) >> 5
        if y == 29:
            y = 0
            v ^= 0x800
        elif y == 31:
            y = 0
        else:
            y += 1
        self.v = (v & ~0x3E0) | (y << 5)

    def _fetch_tile(self):
        v = self.v
        nti = self.mapper.nt_index
        ci = self.ciram
        tile = ci[nti(0x2000 | (v & 0x0FFF))]
        at = ci[nti(0x23C0 | (v & 0x0C00) | ((v >> 4) & 0x38) | ((v >> 2) & 0x07))]
        pal = (at >> (((v >> 4) & 4) | (v & 2))) & 3
        pa = (0x1000 if self.ctrl & 0x10 else 0) + tile * 16 + ((v >> 12) & 7)
        cr = self.mapper.chr_read
        return (decode(cr(pa), cr(pa + 8)), pal << 2)

    # ------------------------------------------------------------ timeline
    def run_to(self, target):
        while self.abs < target:
            line, dot = self.line, self.dot
            if line == 261 and dot < 339:
                ll = 339                                  # event boundary for the odd-frame skip
            elif line == 261:
                ll = 340 if (self.odd and (self.mask & 0x18)) else 341
            else:
                ll = 341
            end = min(dot + (target - self.abs), ll)
            if end > dot:
                self._range(line, dot, end)
                self.abs += end - dot
            if end >= ll and not (line == 261 and ll == 339):
                self._next_line()
            else:
                self.dot = end

    def _next_line(self):
        self.dot = 0
        self.line += 1
        if self.line == 262:
            self.line = 0
            self.frame_no += 1
            self.odd ^= 1
        self.tiles = self.next_tiles + [None] * 32
        self.next_tiles = [None, None]
        self.spr_line = self.spr_next
        self.spr_next = None
        if self.line == 240 and self.on_frame:
            self.on_frame(self)

    def _range(self, line, d0, d1):
        """Process dots d0 <= d < d1 of `line`."""
        if line < 240 or line == 261:
            if self.mask & 0x18:
                self._render_events(line, d0, d1)
            if line < 240:
                x0 = d0 - 1 if d0 > 0 else 0
                x1 = d1 - 1 if d1 < 257 else 256
                if x1 > x0:
                    self._pixels(line, x0, x1)
            if line == 261 and d0 <= 1 < d1:
                self.status &= 0x1F
        elif line == 241 and d0 <= 1 < d1:
            self.status |= 0x80
            if self.ctrl & 0x80 and self.cpu:
                self.cpu.nmi()
                self.nmi_count += 1

    def _render_events(self, line, d0, d1):
        # BG fetch groups, dots 8..256
        a = max(d0, 8)
        a = (a + 7) & ~7
        b = min(d1 - 1, 256)
        if a <= b:
            tiles = self.tiles
            for d in range(a, b + 1, 8):
                tiles[1 + (d >> 3)] = self._fetch_tile()
                self._inc_x()
                if d == 256:
                    self._inc_y()
        if d0 <= 257 < d1:
            self.v = (self.v & ~0x041F) | (self.t & 0x041F)
            if line < 239:
                self._eval_sprites(line)
        ctrl = self.ctrl
        if d0 <= 260 < d1 and (ctrl & 0x20 or (ctrl & 0x18) == 0x08):
            self._mmc3_clock()
        if d0 <= 324 < d1 and (ctrl & 0x38) == 0x10:
            self._mmc3_clock()
        if line == 261 and d0 <= 304 and d1 > 280:
            self.v = (self.v & ~0x7BE0) | (self.t & 0x7BE0)
        if d0 <= 328 < d1:
            self.next_tiles[0] = self._fetch_tile()
            self._inc_x()
        if d0 <= 336 < d1:
            self.next_tiles[1] = self._fetch_tile()
            self._inc_x()

    def _mmc3_clock(self):
        f = getattr(self.mapper, 'clock_scanline', None)
        if f:
            f()

    def _eval_sprites(self, line):
        """Sprites for line+1 (OAM y = line means visible on line+1)."""
        ctrl = self.ctrl
        h = 16 if ctrl & 0x20 else 8
        oam = self.oam
        cr = self.mapper.chr_read
        buf = None
        n = 0
        for i in range(0, 256, 4):
            row = line - oam[i]
            if not (0 <= row < h):
                continue
            if n == 8:
                self.status |= 0x20
                break
            n += 1
            tile, at, sx = oam[i + 1], oam[i + 2], oam[i + 3]
            if at & 0x80:
                row = h - 1 - row
            if h == 16:
                pa = ((tile & 1) << 12) + ((tile & 0xFE) + (row >> 3)) * 16 + (row & 7)
            else:
                pa = (0x1000 if ctrl & 8 else 0) + tile * 16 + row
            pix = decode(cr(pa), cr(pa + 8))
            if at & 0x40:
                pix = pix[::-1]
            if buf is None:
                buf = bytearray(256)
            tag = ((at & 3) << 2) | (0x40 if at & 0x20 else 0) | (0x80 if i == 0 else 0)
            for k in range(8):
                xx = sx + k
                if xx > 255:
                    break
                c = pix[k]
                if c and not buf[xx]:
                    buf[xx] = tag | c
        self.spr_next = buf

    def _pixels(self, line, x0, x1):
        fb = self.fb
        pal = self.pal
        base = line * 256
        mask = self.mask
        grey = 0x30 if mask & 1 else 0x3F
        if not (mask & 0x18):
            v = self.v
            c = pal[_pal_index(v)] if (v & 0x3F00) == 0x3F00 else pal[0]
            c &= grey
            fb[base + x0:base + x1] = bytes([c]) * (x1 - x0)
            return
        bg_from = 0 if mask & 2 else 8
        sp_from = 0 if mask & 4 else 8
        if not (mask & 8):
            bg_from = 256
        spr = self.spr_line if mask & 0x10 else None
        tiles = self.tiles
        fx = self.x
        for x in range(x0, x1):
            bp = 0
            if x >= bg_from:
                p = x + fx
                t = tiles[p >> 3]
                if t is not None:
                    c = t[0][p & 7]
                    if c:
                        bp = t[1] | c
            sp = spr[x] if (spr is not None and x >= sp_from) else 0
            if sp:
                if bp:
                    if sp & 0x80 and x != 255:
                        self.status |= 0x40
                    col = pal[bp] if sp & 0x40 else pal[0x10 | (sp & 0x0F)]
                else:
                    col = pal[0x10 | (sp & 0x0F)]
            else:
                col = pal[bp]
            fb[base + x] = col & grey

    # ------------------------------------------------------------ output
    def to_rgb(self):
        from palette_dump import NES_RGB        # tools/ is on sys.path via emu/__init__
        lut = [bytes(((c >> 16) & 255, (c >> 8) & 255, c & 255)) for c in NES_RGB]
        return b''.join(lut[i & 0x3F] for i in self.fb)
