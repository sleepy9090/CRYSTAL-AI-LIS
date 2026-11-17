// re-c web port — background pipeline: screen pages -> metatiles -> tiles/attributes (Ph7 web 4/8, s53).
// REVERSE.md "Screen / metatile pipeline" + "Background details"; data = web/data/screens*.bin, metatiles.bin.
//
//   screenPage(m, S, rowPath)    ScreenPagePtr 31:EF36 (+ the $62FF b7 bank-10 branch of ScreenRowRender 31:EBE5)
//   tileSubst(m, M, visited)     ScreenTileSubst 31:ED99 (visited screens: M < $20 -> $BE00 table, ptr $6A/$6B)
//   metatileTiles(m, M)          MetatileExpand 31:EDBA: TL TR BL BR from pages ($67&$FC)|q
//   metatileAttr(m, M)           MetatileAttrBits 31:EECC via MetatileAttrPtrTable 31:EEEF[($67&$3C)>>1]
//   screenAt(m, xhi, yhi)        ScreenAtCamera 31:EF1F: $6300[(yhi<<3 & $78) | (xhi & 7)]
//   screenVisited(m, xhi, yhi)   ScreenVisitedBit 31:EC6C: $62F0[yhi] & BitMask[xhi]
//   renderArea(m, {visited})     whole k0 grid as NES colour indices (W*256 x H*240) from pal_buf; the static
//                                reference view that web/test/world_check.mjs diffs against render_level.py.
// The row/column streaming into the PPU queue (BgRowRender 31:EB91, BgColRender 31:EC87, MetatileAttr
// 31:EDE5/EE31 -> $6100 shadow, queue ring $6200) is built on these helpers by the camera/queue task.

const BIT = [0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80];     // BitMask 30:C000

/** {buf, off}: the 256-byte page of screen id S (240 metatile ids + 16-byte tail) */
export function screenPage(m, S, rowPath = false) {
  const lo = S & 0x3F;
  if (rowPath && (m.rd(0x62FF) & 0x80) && lo < 0x20) return { buf: m.data.screensB10, off: lo << 8 };
  return { buf: m.data.screens, off: S << 8 };                     // bank 2*(S>>6) + (lo>>5), page lo&$1F
}

export const mtByte = (m, addr) => m.data.metatiles[addr - 0x8000];  // banks 8/9 as $8000-$BFFF

export function tileSubst(m, M, visited) {
  if (!visited || M >= 0x20) return M;
  return mtByte(m, ((m.rd(0x6B) << 8) | m.rd(0x6A)) + M);
}

export function metatileTiles(m, M) {
  const p = m.rd(0x67) & 0xFC;
  return [0, 1, 2, 3].map((q) => mtByte(m, ((p | q) << 8) | M));
}

export function metatileAttr(m, M) {
  const base = 0xB000 + ((m.rd(0x67) & 0x3C) >> 2) * 0x40;
  return (mtByte(m, base + (M >> 2)) >> (2 * (M & 3))) & 3;
}

export const screenAt = (m, xhi, yhi) => m.rd(0x6300 + (((yhi << 3) & 0x78) | (xhi & 7)));
export const screenVisited = (m, xhi, yhi) => (m.rd(0x62F0 + yhi) & BIT[xhi & 7]) !== 0;

/** BG tile pixel rows: 2bpp from CHR with R0/R1 (2KB each) at PPU $0000/$0800, as ppu.js chrBanks() maps them */
function bgTilePixel(chr, r0, r1, t, x, y) {
  const bank = t < 0x80 ? (r0 & 0xFE) + (t >> 6) : (r1 & 0xFE) + ((t >> 6) & 1);
  const o = (bank % (chr.length >> 10)) * 0x400 + (t & 0x3F) * 16 + y;
  return ((chr[o] >> (7 - x)) & 1) | (((chr[o + 8] >> (7 - x)) & 1) << 1);
}

/**
 * Static render of the loaded area (k0 grid in $6300, k1 in $67/$07F0/$07F1, pal_buf built):
 * every grid cell drawn as a full screen through the same page/subst/expand/attr helpers.
 * visited: false = no substitution, true = every screen visited, 'ram' = the $62F0 bitmap.
 * Pages are read like render_level.py does (bank 10 for the whole screen when $62FF b7, i.e. the
 * row path's first screen) so the output is comparable cell by cell.
 */
export function renderArea(m, { visited = false } = {}) {
  const W = m.rd(0x62FC) + 1, H = m.rd(0x62FD) + 1;
  const w = W * 256, h = H * 240, fb = new Uint8Array(w * h);
  const chr = m.data.chr, r0 = m.rd(0x07F0), r1 = m.rd(0x07F1);
  const pal = (g, c) => m.rd(0x6140 + (c ? g * 4 + c : 0));        // PaletteUpload: entry 0 = backdrop
  for (let gy = 0; gy < H; gy++) {
    for (let gx = 0; gx < W; gx++) {
      const S = screenAt(m, gx, gy);
      const vis = visited === 'ram' ? screenVisited(m, gx, gy) : !!visited;
      const { buf, off } = screenPage(m, S, true);
      for (let r = 0; r < 15; r++) {
        for (let c = 0; c < 16; c++) {
          const M = tileSubst(m, buf[off + r * 16 + c], vis);
          const g = metatileAttr(m, M), tiles = metatileTiles(m, M);
          for (let q = 0; q < 4; q++) {
            const x0 = gx * 256 + c * 16 + (q & 1) * 8, y0 = gy * 240 + r * 16 + (q >> 1) * 8;
            for (let y = 0; y < 8; y++) {
              for (let x = 0; x < 8; x++) fb[(y0 + y) * w + x0 + x] = pal(g, bgTilePixel(chr, r0, r1, tiles[q], x, y)) & 0x3F;
            }
          }
        }
      }
    }
  }
  return { w, h, fb };
}
