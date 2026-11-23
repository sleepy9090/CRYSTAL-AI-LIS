// re-c C# port — PaletteBuild 26:8C0E / PaletteLoadOne 26:8C6E and PaletteUpload 31:F8CB (= web/src/game/palette.js).
using ReC.Gen;

namespace ReC.Game;

public static class Palette
{
    // js: game/palette.js paletteBuild
    /// <summary>PaletteBuild 26:8C0E (fade = 0) / entry 26:8C10 (A = fade): pal_buf $6140-$615F from the 8 ids
    /// $07E0-$07E7. Groups 4-7 (X >= $10) use slot (id + $B0) &amp; $FF. Per colour: $FF -> the *current* pal_buf[0];
    /// then - fade, negative -> $0F. pal_nofade_mask $07E8 bit g (LSB first) = group g ignores the fade.</summary>
    public static void PaletteBuild(Machine m, int fade = 0)
    {
        var slots = m.Data.PalSlots;
        int mask = m.Rd(0x07E8);
        for (int x = 0; x < 0x20;)
        {
            int s = m.Rd(0x07E0 + (x >> 2));
            if (x >= 0x10) s = (s + 0xB0) & 0xFF;
            int f = (mask & 1) != 0 ? 0 : fade;
            mask >>= 1;
            for (int y = 0; y < 4; y++, x++)
            {
                int c = slots[s][y];
                if (c == 0xFF) c = m.Rd(R.PalBuf);
                c = (c - f) & 0xFF;
                m.Wr(R.PalBuf + x, (c & 0x80) != 0 ? 0x0F : c);
            }
        }
    }

    // js: game/palette.js paletteUpload
    /// <summary>PaletteUpload 31:F8CB (NMI calls it with X=0): skipped when irq_mode b7 is set; otherwise PPUMASK=0,
    /// PPUCTRL = shadow &amp; ~4, $3F00-$3F1F &lt;- pal_buf+x with the backdrop forced into entry 0 of every group,
    /// then PPUADDR $3F00, $0000 (v = 0).</summary>
    public static void PaletteUpload(Machine m, int x = 0)
    {
        if ((m.Rd(R.IrqMode) & 0x80) != 0) return;
        var ppu = m.Ppu;
        ppu.WriteReg(1, 0);
        ppu.WriteReg(0, m.Rd(R.PpuctrlShadow) & 0xFB);
        ppu.ReadStatus();
        ppu.WriteReg(6, 0x3F); ppu.WriteReg(6, 0x00);
        int @base = R.PalBuf + x;
        for (int i = 0; i < 32; i++) ppu.WriteReg(7, m.Rd(@base + ((i & 3) != 0 ? i : 0)));
        ppu.WriteReg(6, 0x3F); ppu.WriteReg(6, 0x00);   // 31:F9A9-F9B6: $3F, $00, $00, $00
        ppu.WriteReg(6, 0x00); ppu.WriteReg(6, 0x00);
    }
}
