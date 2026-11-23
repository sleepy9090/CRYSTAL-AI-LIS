// re-c C# port — banked PRG reads + the MMC3 PRG bank setters (= web/src/game/prg.js).
//   PrgByte(m, bank, addr)  byte of 8KB PRG bank `bank` at CPU address addr (any window)
//   CpuRd(m, a)             CPU read with the current mapping: RAM, WRAM, $8000 = m.PrgR6, $A000 = m.PrgR7,
//                           $C000 = bank 30, $E000 = bank 31 (MMC3 PRG mode 0)
//   SetR6 / SetR7           SetR6 30:C418 / SetR7 30:C427: shadow $6E / $6F, $50 = 6 / 7, then the mapping
//   FarRestore(m, r6, r7)   RestoreBanks 31:FFDB (SetR7 then SetR6, so $50 ends as 6)
namespace ReC.Game;

public static class Prg
{
    // js: game/prg.js prgByte
    public static int PrgByte(Machine m, int bank, int addr) => m.Data.Prg[(bank & 0x1F) * 0x2000 + (addr & 0x1FFF)];

    // js: game/prg.js cpuRd
    public static int CpuRd(Machine m, int a)
    {
        a &= 0xFFFF;
        m.RomRd++;                                         // s211: counted for web/test/pass_count.mjs
        if (a < 0x2000) return m.Mem.RamBytes[a & 0x7FF];
        if (a >= 0x6000 && a < 0x8000) return m.Mem.Wram[a - 0x6000];
        if (a < 0x6000) throw new InvalidOperationException($"cpuRd ${a:x}: PPU/APU/open bus not modelled");
        if (a < 0xA000) return PrgByte(m, m.PrgR6, a);
        if (a < 0xC000) return PrgByte(m, m.PrgR7, a);
        return PrgByte(m, a < 0xE000 ? 30 : 31, a);
    }

    // js: game/prg.js indY
    /// <summary>(zp),Y: 16-bit pointer at zp, zp+1 plus Y</summary>
    public static int IndY(Machine m, int zp, int y) =>
        CpuRd(m, (m.Rd(zp) | m.Rd((zp + 1) & 0xFF) << 8) + (y & 0xFF));

    // js: game/prg.js setR6
    public static void SetR6(Machine m, int v)
    {
        m.Wr(0x6E, v); m.Wr(0x50, 6); m.PrgR6 = m.Rd(0x6E);
    }

    // js: game/prg.js setR7
    public static void SetR7(Machine m, int v)
    {
        m.Wr(0x6F, v); m.Wr(0x50, 7); m.PrgR7 = m.Rd(0x6F);
    }

    // js: game/prg.js switchBank16
    /// <summary>SwitchBank16 30:C40E: $6E = 2n, SetR7(2n+1), SetR6(2n) ($50 ends as 6)</summary>
    public static void SwitchBank16(Machine m, int n)
    {
        m.Wr(0x6E, n << 1); SetR7(m, (n << 1) | 1); SetR6(m, n << 1);
    }

    // js: game/prg.js farRestore
    public static void FarRestore(Machine m, int r6, int r7)
    {
        SetR7(m, r7); SetR6(m, r6);
    }
}
