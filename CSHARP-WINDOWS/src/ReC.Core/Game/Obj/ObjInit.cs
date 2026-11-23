// re-c C# port — ObjInit 30:C25D + FarCall_C25D 31:FF80 (= web/src/game/obj/objinit.js, s55). REVERSE.md "Object type record".
//
//   Init(m)      (JS objInit) slot = $10, type = $11. Type 1 = no-op. SetR7(13) (left mapped), ptr $12/$13 =
//                ObjTypeTable 13:AC00[type*2] ($AD00 for types >= $80), PlaySound(rec[0]) if non-zero, clear all 32
//                columns $0300-$06E0 of the slot, then (type != $FF) 4 groups of (bitmask byte, values for the set bits,
//                MSB first) for the columns $0300+g*$100+b*$20.
//   FarObjInit(m)  FarCall_C25D: the same, then RestoreBanks (R7 then R6 back, $50 ends = 6)
namespace ReC.Game.Obj;

public static class ObjInit
{
    // js: game/obj/objinit.js objInit
    public static void Init(Machine m)
    {
        int type = m.Rd(0x11);
        if (type == 1) return;
        Prg.SetR7(m, 0x0D);                                       // C268
        int yy = (type << 1) & 0xFF, b = (type & 0x80) != 0 ? 0xAD00 : 0xAC00;
        m.Wr(0x12, Prg.CpuRd(m, b + yy));
        m.Wr(0x13, Prg.CpuRd(m, b + yy + 1));
        int snd = Prg.IndY(m, 0x12, 0);
        if (snd != 0) PlaySound.Play(m, snd, 0);                  // C28D (Y = 0 at the call)
        int x = m.Rd(0x10);
        for (int c = x; c < 0x100; c += 0x20)
        {                                                         // C295: carry ends the loop
            m.Wr(0x0300 + c, 0); m.Wr(0x0400 + c, 0); m.Wr(0x0500 + c, 0); m.Wr(0x0600 + c, 0);
        }
        if (m.Rd(0x11) == 0xFF) return;                           // C2A9 (type re-read: PlaySound keeps $11)
        int xs = x & 0x1F;                                        // s97: the clear loop's carry leaves X = $10 mod $20
        int y = 1;
        for (int g = 0; g < 4; g++)
        {
            m.Wr(0x11, (Prg.IndY(m, 0x12, y) << 1) & 0xFF);       // header ASL A -> $11, bit 7 in C
            int c = Prg.IndY(m, 0x12, y) & 0x80;
            y = (y + 1) & 0xFF;
            for (int bit = 0; bit < 8; bit++)
            {
                if (bit > 0)
                {                                                 // ASL $11
                    int v = m.Rd(0x11);
                    c = v & 0x80; m.Wr(0x11, (v << 1) & 0xFF);
                }
                if (c != 0)
                {
                    m.Wr(0x0300 + g * 0x100 + bit * 0x20 + xs, Prg.IndY(m, 0x12, y));
                    y = (y + 1) & 0xFF;
                }
            }
        }
        m.Wr(0x11, type);                                         // C406 PLA
    }

    // js: game/obj/objinit.js farObjInit
    public static void FarObjInit(Machine m)
    {
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);
        Init(m);
        Prg.FarRestore(m, r6, r7);
    }
}
