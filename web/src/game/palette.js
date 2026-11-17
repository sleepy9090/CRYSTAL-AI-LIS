// re-c web port — palette upload (PaletteUpload 31:F8CB). PaletteBuild / fades (26:8C0E, 26:8C3D/8C49)
// arrive with Ph7 web 4/8.
import { R } from './ram.js';

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
