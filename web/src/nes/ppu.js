// re-c web port — NES-style PPU (Ph7 web 2/8). DOM-free; runs under Node.
//
// Model: the game writes the same state the ROM writes ($2000-$2007 via writeReg, OAM via oamDma,
// MMC3 CHR registers R0-R5 via setChrReg, nametable mirroring), and renderFrame() turns it into a
// 256x240 framebuffer of NES colour indices ($00-$3F), one scanline at a time:
//   pre-render: if rendering, v <- t (vertical + horizontal copy)
//   line L    : render BG from v/fine x (33 tiles) + the sprite line evaluated at the end of L-1
//               then (if rendering) Y increment, horizontal copy t -> v, sprite evaluation for L+1,
//               then the split hook for L+1 (IRQ handlers: scroll/CHR/mask changes take effect
//               on the next line, as an MMC3 IRQ at dot 260 does).
// Rendering rules follow tools/emu/ppu.py (the self-emulator that tools/emu/ppu_lines.py snapshots):
// 8 sprites per line, first opaque sprite wins, behind-BG priority, left-8 masking, greyscale,
// sprite rows from OAM y+1, no sprites on line 0, $3F10/14/18/1C mirroring, rendering-off colour.
// Not modelled (no game code depends on it): sprite-0 hit / overflow flags, $2007 reads,
// mid-scanline register changes, colour emphasis.

const PAL_MIRROR = (a) => {
  a &= 0x1F;
  return (a & 0x13) === 0x10 ? a & 0x0F : a;
};

// 2bpp row decode cache: key lo<<8|hi -> 8 pixel values (leftmost first)
const DEC = new Uint8Array(65536 * 8);
for (let lo = 0; lo < 256; lo++) {
  for (let hi = 0; hi < 256; hi++) {
    const o = ((lo << 8) | hi) * 8;
    for (let i = 0; i < 8; i++) DEC[o + i] = ((lo >> (7 - i)) & 1) | (((hi >> (7 - i)) & 1) << 1);
  }
}

export class Ppu {
  constructor(chr) {
    this.chr = chr;                       // Uint8Array, 1KB banks (web/data/chr.bin)
    this.nChr = chr.length >> 10;
    this.fb = new Uint8Array(256 * 240);
    this.vram = new Uint8Array(0x800);    // CIRAM (2 nametables)
    this.pal = new Uint8Array(32);
    this.oam = new Uint8Array(256);
    this.chrRegs = new Uint8Array(6);     // MMC3 R0-R5
    this.sprBuf = new Uint8Array(256);    // sprite line for the next line (tag | colour)
    this.sprNext = new Uint8Array(256);
    this.splits = new Map();              // line -> fn(ppu) run before that line renders
    this.reset();
  }

  reset() {
    this.ctrl = 0; this.mask = 0; this.status = 0;
    this.v = 0; this.t = 0; this.x = 0; this.w = 0;
    this.oamAddr = 0;
    this.mirroring = 1;                   // re-c Reset writes $A000 = 1 (horizontal)
    this.chrInv = 0;                      // MMC3 $8000 b7 (re-c: always 0, CHR mode 0)
    this.chrRegs.set([0, 2, 4, 5, 6, 7]);
    this.vram.fill(0); this.pal.fill(0); this.oam.fill(0); this.fb.fill(0);
    this.sprBuf.fill(0);
    this.frameNo = 0;
  }

  get rendering() { return (this.mask & 0x18) !== 0; }

  // ------------------------------------------------------------ MMC3 CHR
  setChrReg(r, val) { this.chrRegs[r] = val & 0xFF; }
  /** eight 1KB bank numbers mapped at PPU $0000-$1FFF */
  chrBanks() {
    const r = this.chrRegs;
    const lo = [r[0] & 0xFE, r[0] | 1, r[1] & 0xFE, r[1] | 1];
    const hi = [r[2], r[3], r[4], r[5]];
    return (this.chrInv ? hi.concat(lo) : lo.concat(hi)).map((b) => b % this.nChr);
  }
  /** override the computed mapping (used by tests that replay emulator snapshots) */
  setChrBanks(banks) { this._banks = banks; }

  _bankOffsets() {
    const b = this._banks || this.chrBanks();
    return b.map((n) => n << 10);
  }

  // ------------------------------------------------------------ VRAM
  ntIndex(a) {
    a &= 0x0FFF;
    if (this.mirroring === 0) return a & 0x7FF;                 // vertical
    return ((a >> 1) & 0x400) | (a & 0x3FF);                    // horizontal
  }
  vramWrite(a, val) {
    a &= 0x3FFF;
    if (a < 0x2000) return;                                     // CHR ROM: ignored
    if (a < 0x3F00) this.vram[this.ntIndex(a)] = val;
    else this.pal[PAL_MIRROR(a)] = val & 0x3F;
  }
  vramRead(a) {
    a &= 0x3FFF;
    if (a < 0x2000) { const o = this._bankOffsets(); return this.chr[o[a >> 10] + (a & 0x3FF)]; }
    if (a < 0x3F00) return this.vram[this.ntIndex(a)];
    return this.pal[PAL_MIRROR(a)];
  }

  // ------------------------------------------------------------ CPU registers ($2000-$2007, vblank semantics)
  readStatus() {                                                // $2002: clears w (vblank flag not modelled)
    this.w = 0;
    return this.status;
  }
  writeReg(r, val) {
    val &= 0xFF;
    switch (r & 7) {
      case 0: this.ctrl = val; this.t = (this.t & 0xF3FF) | ((val & 3) << 10); break;
      case 1: this.mask = val; break;
      case 3: this.oamAddr = val; break;
      case 4: this.oam[this.oamAddr] = val; this.oamAddr = (this.oamAddr + 1) & 0xFF; break;
      case 5:
        if (!this.w) { this.t = (this.t & 0x7FE0) | (val >> 3); this.x = val & 7; this.w = 1; }
        else { this.t = (this.t & 0x0C1F) | ((val & 7) << 12) | ((val & 0xF8) << 2); this.w = 0; }
        break;
      case 6:
        if (!this.w) { this.t = (this.t & 0x00FF) | ((val & 0x3F) << 8); this.w = 1; }
        else { this.t = (this.t & 0x7F00) | val; this.v = this.t; this.w = 0; }
        break;
      case 7:
        this.vramWrite(this.v, val);
        this.v = (this.v + ((this.ctrl & 4) ? 32 : 1)) & 0x7FFF;
        break;
      default: break;
    }
  }
  oamDma(src, off = 0) {                                        // $4014: 256 bytes from RAM page
    for (let i = 0; i < 256; i++) this.oam[(this.oamAddr + i) & 0xFF] = src[off + i];
  }

  // ------------------------------------------------------------ scroll helpers (loopy v)
  _incX() {
    const v = this.v;
    this.v = (v & 0x1F) === 31 ? (v & ~0x1F) ^ 0x400 : v + 1;
  }
  _incY() {
    let v = this.v;
    if ((v & 0x7000) !== 0x7000) { this.v = v + 0x1000; return; }
    v &= ~0x7000;
    let y = (v & 0x3E0) >> 5;
    if (y === 29) { y = 0; v ^= 0x800; } else if (y === 31) y = 0; else y++;
    this.v = (v & ~0x3E0) | (y << 5);
  }

  // ------------------------------------------------------------ rendering
  /** Render one visible line from the current v (= line start, before the 2-tile prefetch). */
  renderLine(line) {
    const fb = this.fb, pal = this.pal, base = line * 256, mask = this.mask;
    const grey = (mask & 1) ? 0x30 : 0x3F;
    if (!(mask & 0x18)) {
      const v = this.v;
      const c = ((v & 0x3F00) === 0x3F00 ? pal[PAL_MIRROR(v)] : pal[0]) & grey;
      fb.fill(c, base, base + 256);
      return;
    }
    // BG: 33 tiles starting at v
    const bgLine = this._bgLine || (this._bgLine = new Uint8Array(264));
    if (mask & 0x08) {
      const offs = this._bankOffsets();
      const chr = this.chr, vram = this.vram, ptBase = (this.ctrl & 0x10) ? 0x1000 : 0;
      const v0 = this.v;
      let v = v0;
      for (let t = 0; t < 33; t++) {
        const tile = vram[this.ntIndex(0x2000 | (v & 0x0FFF))];
        const at = vram[this.ntIndex(0x23C0 | (v & 0x0C00) | ((v >> 4) & 0x38) | ((v >> 2) & 0x07))];
        const p = ((at >> (((v >> 4) & 4) | (v & 2))) & 3) << 2;
        const pa = ptBase + tile * 16 + ((v >> 12) & 7);
        const lo = chr[offs[pa >> 10] + (pa & 0x3FF)];
        const pb = pa + 8;
        const hi = chr[offs[pb >> 10] + (pb & 0x3FF)];
        const d = ((lo << 8) | hi) * 8;
        for (let i = 0; i < 8; i++) { const c = DEC[d + i]; bgLine[t * 8 + i] = c ? (p | c) : 0; }
        v = (v & 0x1F) === 31 ? (v & ~0x1F) ^ 0x400 : v + 1;
      }
      this.v = v0;
    } else bgLine.fill(0);
    const bgFrom = (mask & 0x08) ? ((mask & 2) ? 0 : 8) : 256;
    const spFrom = (mask & 4) ? 0 : 8;
    const spr = (mask & 0x10) ? this.sprBuf : null;
    const fx = this.x;
    for (let x = 0; x < 256; x++) {
      const bp = x >= bgFrom ? bgLine[x + fx] : 0;
      const sp = (spr && x >= spFrom) ? spr[x] : 0;
      let col;
      if (sp) col = (bp && (sp & 0x40)) ? pal[bp] : pal[0x10 | (sp & 0x0F)];
      else col = pal[bp];
      fb[base + x] = col & grey;
    }
  }

  /** Sprite evaluation for line `line` (OAM y = line-1); result used by the next renderLine. */
  evalSprites(line) {
    const buf = this.sprBuf;
    buf.fill(0);
    if (line <= 0 || line >= 240) return;
    const ctrl = this.ctrl, h = (ctrl & 0x20) ? 16 : 8, oam = this.oam;
    const offs = this._bankOffsets(), chr = this.chr;
    let n = 0;
    for (let i = 0; i < 256; i += 4) {
      let row = line - 1 - oam[i];
      if (row < 0 || row >= h) continue;
      if (n === 8) { this.status |= 0x20; break; }
      n++;
      const tile = oam[i + 1], at = oam[i + 2], sx = oam[i + 3];
      if (at & 0x80) row = h - 1 - row;
      const pa = h === 16
        ? ((tile & 1) << 12) + ((tile & 0xFE) + (row >> 3)) * 16 + (row & 7)
        : ((ctrl & 8) ? 0x1000 : 0) + tile * 16 + row;
      const lo = chr[offs[pa >> 10] + (pa & 0x3FF)];
      const hi = chr[offs[(pa + 8) >> 10] + ((pa + 8) & 0x3FF)];
      const d = ((lo << 8) | hi) * 8;
      const tag = ((at & 3) << 2) | ((at & 0x20) ? 0x40 : 0) | (i === 0 ? 0x80 : 0);
      for (let k = 0; k < 8; k++) {
        const xx = sx + k;
        if (xx > 255) break;
        const c = DEC[d + ((at & 0x40) ? 7 - k : k)];
        if (c && !buf[xx]) buf[xx] = tag | c;
      }
    }
  }

  /** Render a whole frame. Split hooks (this.splits: line -> fn(ppu)) run before their line. */
  renderFrame() {
    this.status &= 0x1F;
    if (this.rendering) this.v = this.t;
    this.sprBuf.fill(0);                    // no sprites on line 0
    for (let line = 0; line < 240; line++) {
      const hook = this.splits.get(line);
      if (hook) hook(this);
      this.renderLine(line);
      if (this.rendering) {
        this._incY();
        this.v = (this.v & ~0x041F) | (this.t & 0x041F);
        this.evalSprites(line + 1);
      } else this.sprBuf.fill(0);
    }
    this.status |= 0x80;                    // vblank
    this.frameNo++;
  }
}

/** helper for tests/snapshots: v at dot 0 (after the 2-tile prefetch) -> v at the line start */
export function unprefetch(v) {
  for (let i = 0; i < 2; i++) v = (v & 0x1F) === 0 ? (v | 0x1F) ^ 0x400 : v - 1;
  return v;
}
