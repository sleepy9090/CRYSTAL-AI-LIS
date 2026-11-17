// re-c web port — PaletteBuild 26:8C0E / PaletteLoadOne 26:8C6E (s53) and PaletteUpload 31:F8CB.
// The multi-frame fades (26:8C3D/8C49, 5 steps with WaitFrame) arrive with the substates that use them.
import { R } from './ram.js';

/**
 * PaletteBuild 26:8C0E (fade = 0) / entry 26:8C10 (A = fade): pal_buf $6140-$615F from the 8 ids
 * $07E0-$07E7. Group g = X>>2; groups 4-7 (X >= $10) use slot (id + $B0) & $FF. Per colour: $FF -> the
 * *current* pal_buf[0] (already faded, so the backdrop is faded twice); then - fade, negative -> $0F.
 * pal_nofade_mask $07E8 bit g (LSB first) = group g ignores the fade.
 */
export function paletteBuild(m, fade = 0) {
  const slots = m.data.palettes.slots;
  let mask = m.rd(0x07E8);
  for (let x = 0; x < 0x20;) {
    let s = m.rd(0x07E0 + (x >> 2));
    if (x >= 0x10) s = (s + 0xB0) & 0xFF;
    const f = (mask & 1) ? 0 : fade;
    mask >>= 1;
    for (let y = 0; y < 4; y++, x++) {
      let c = slots[s][y];
      if (c === 0xFF) c = m.rd(R.pal_buf);
      c = (c - f) & 0xFF;
      m.wr(R.pal_buf + x, (c & 0x80) ? 0x0F : c);
    }
  }
}

/**
 * PaletteUpload 31:F8CB (NMI calls it with X=0): skipped when irq_mode b7 is set; otherwise
 * PPUMASK=0, PPUCTRL = shadow & ~4 (increment 1), $3F00-$3F1F <- pal_buf+x with the backdrop
 * pal_buf[x] forced into entry 0 of every group, then PPUADDR $3F00, $0000 (v = 0).
 */
export function paletteUpload(m, x = 0) {
  if (m.rd(R.irq_mode) & 0x80) return;
  const ppu = m.ppu;
  ppu.writeReg(1, 0);
  ppu.writeReg(0, m.rd(R.ppuctrl_shadow) & 0xFB);
  ppu.readStatus();
  ppu.writeReg(6, 0x3F); ppu.writeReg(6, 0x00);
  const base = R.pal_buf + x;
  for (let i = 0; i < 32; i++) ppu.writeReg(7, m.rd(base + ((i & 3) ? i : 0)));
  ppu.writeReg(6, 0x3F); ppu.writeReg(6, 0x00);   // 31:F9A9-F9B6: $3F, $00, $00, $00
  ppu.writeReg(6, 0x00); ppu.writeReg(6, 0x00);
}
