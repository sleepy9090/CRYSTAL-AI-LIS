// re-c C# port — RAM / WRAM at the ROM's addresses (= web/src/game/ram.js; docs/architecture_web.md rule 1).
// ram = $0000-$07FF (CPU RAM), wram = $6000-$7FFF (battery WRAM). The address names of ram.js `R` are in
// Gen/R.cs (generated from web/data/ram_labels.json, the same source core_check.mjs checks R against).
namespace ReC.Game;

// js: game/ram.js Ram
public sealed class Ram
{
    public readonly byte[] RamBytes;
    public readonly byte[] Wram;

    // js: game/ram.js constructor
    public Ram()
    {
        RamBytes = new byte[0x800];
        Wram = new byte[0x2000];
    }

    // js: game/ram.js rd
    public int Rd(int a)
    {
        if (a < 0x2000) return RamBytes[a & 0x7FF];
        if (a >= 0x6000 && a < 0x8000) return Wram[a - 0x6000];
        throw new InvalidOperationException($"rd ${a:x}: not RAM/WRAM");
    }

    // js: game/ram.js wr
    public void Wr(int a, int v)
    {
        if (a < 0x2000) RamBytes[a & 0x7FF] = (byte)v;
        else if (a >= 0x6000 && a < 0x8000) Wram[a - 0x6000] = (byte)v;
        else throw new InvalidOperationException($"wr ${a:x}: not RAM/WRAM");
    }

    // js: game/ram.js page
    /// <summary>256-byte page view (OAM DMA source, e.g. page 2 = OAM shadow $0200)</summary>
    public ArraySegment<byte> Page(int p) => new(RamBytes, (p & 7) << 8, 256);
}
