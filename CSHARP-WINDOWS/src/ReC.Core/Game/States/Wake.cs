// re-c C# port — SubstWakeCutscene 30:D9B6 (sub_state $13) -> WakeCutscene 20:9400 (= web/src/game/states/wake.js,
// s138). The new-game wake-up cut-scene: a self-timed loop in bank 20 with its own `LSR frame_ready / BCC` waits
// (Talk.LsrWait). Tail 30:D91F: StatRecalc, FarCall_B14_800E (sprite pass), PaletteBuild(0), 30:D8BE (pad_new 0, sub 8).
// REVERSE.md "New game (session 38)" + "bank 20 tables" (s72). 20:9685/9693 has no caller and is not ported.
using ReC.Gen;

namespace ReC.Game.States;

public static class Wake
{
    private const int T20 = 0x20, T21 = 0x21, T22 = 0x22, T26 = 0x26, T2A = 0x2A, T2C = 0x2C;
    private const int Cnt = 0x60F8, BaseY = 0x60FC, BaseX = 0x60FF;

    // js: game/states/wake.js wakeSound
    /// <summary>20:9756: snd_queue = id, lock, count 1, unlock (a raw queue write, not PlaySound)</summary>
    private static void WakeSound(Machine m, int id)
    {
        m.Wr(0x0103, id);
        m.Wr(0x0101, 1);
        m.Wr(0x0100, 1);
        m.Wr(0x0101, 0);
    }

    // js: game/states/wake.js waitT22
    /// <summary>20:974C: tmp_22 x LSR-wait (X = 0 after it)</summary>
    private static IEnumerable<Y> WaitT22(Machine m)
    {
        int x = m.Rd(T22);
        do
        {
            foreach (var s in Talk.LsrWait(m)) yield return s;
            x = (x - 1) & 0xFF;
        } while (x != 0);
    }

    // js: game/states/wake.js oamClear
    /// <summary>20:94F5: the whole OAM page &lt;- $F0</summary>
    private static void OamClear(Machine m)
    {
        for (int i = 0; i < 0x100; i++) m.Wr(0x0200 + i, 0xF0);
    }

    // js: game/states/wake.js palCycle
    /// <summary>20:948F: pal_buf &lt;- 16 bytes at $9B0E + (wram_60F8 &amp; $0C) * 4</summary>
    private static void PalCycle(Machine m)
    {
        int o = (m.Rd(Cnt) & 0x0C) << 2;
        for (int y = 0; y < 0x10; y++) m.Wr(R.PalBuf + y, Prg.CpuRd(m, 0x9B0E + o + y));
    }

    // js: game/states/wake.js palFade
    /// <summary>20:94A6 (A = set group): 4 x (tmp_21 LSR-waits, then WakePalPtrs[tmp_20] -> pal_buf), tmp_20 = A*8, +2</summary>
    private static IEnumerable<Y> PalFade(Machine m, int a)
    {
        m.Wr(T20, (a << 3) & 0xFF);
        for (int k = 0; k < 4; k++)
        {
            if (k != 0) m.Wr(T20, (m.Rd(T20) + 2) & 0xFF);
            int x = m.Rd(T21);                                // 94C0
            do
            {
                foreach (var s in Talk.LsrWait(m)) yield return s;
                x = (x - 1) & 0xFF;
            } while (x != 0);
            int i = m.Rd(T20);                                // 94C9
            m.Wr(T26, Prg.CpuRd(m, 0x9A6A + i));
            m.Wr(T26 + 1, Prg.CpuRd(m, 0x9A6B + i));
            int p = m.Rd(T26) | m.Rd(T26 + 1) << 8;
            for (int y = 0; y < 0x10; y++) m.Wr(R.PalBuf + y, Prg.CpuRd(m, p + y));
            m.Wr(0x23, 0);
            int c = m.Rd(R.PalBuf);
            foreach (int a2 in new[] { 0x6150, 0x6154, 0x6158, 0x615C }) m.Wr(a2, c);
        }
    }

    // js: game/states/wake.js wakeSprDraw
    /// <summary>WakeSprDraw 20:9500: ($26) = count, then per sprite dy tile attr dx; OAM from tmp_2A</summary>
    private static void WakeSprDraw(Machine m)
    {
        int p = m.Rd(T26) | m.Rd(T26 + 1) << 8;
        // js: game/states/wake.js rd
        int Rd(int yy) => Prg.CpuRd(m, (p + (yy & 0xFF)) & 0xFFFF);
        int y = 0;
        m.Wr(T2C, Rd(y));
        y++;
        int x = m.Rd(T2A);
        do
        {
            int xx = x;
            // js: game/states/wake.js oam
            int Oam(int o) => 0x0200 + ((xx + o) & 0xFF);
            int dy = Rd(y);
            if (dy == 0x80)
            {                                                 // 950F hidden: all 4 bytes $F8, Y += 4 (+1 at 9557)
                for (int o = 0; o < 4; o++) m.Wr(Oam(o), 0xF8);
                y += 4;
            }
            else
            {
                m.Wr(Oam(0), (dy + m.Rd(BaseY)) & 0xFF);
                y++; m.Wr(Oam(1), Rd(y));
                y++; m.Wr(Oam(2), Rd(y));
                y++;
                int dx = Rd(y), s = dx + m.Rd(BaseX);
                bool keep = (dx & 0x80) != 0 ? s > 0xFF : s <= 0xFF;   // 953A BMI: negative dx needs the carry
                if (keep) m.Wr(Oam(3), s & 0xFF);
                else m.Wr(Oam(0), 0xF8);                      // 955D
            }
            x = (x + 4) & 0xFF;                               // 9551
            m.Wr(T2A, x);
            y++;
            m.Wr(T2C, (m.Rd(T2C) - 1) & 0xFF);
        } while (m.Rd(T2C) != 0);
    }

    // js: game/states/wake.js wakeSprFrame
    /// <summary>WakeSprFrame 20:973D: A = frame -> WakeSprFramePtrs 20:9767 -> WakeSprDraw</summary>
    private static void WakeSprFrame(Machine m, int a)
    {
        int i = (a << 1) & 0xFF;
        m.Wr(T26, Prg.CpuRd(m, 0x9767 + i));
        m.Wr(T26 + 1, Prg.CpuRd(m, 0x9768 + i));
        WakeSprDraw(m);
    }

    // js: game/states/wake.js at
    private static void At(Machine m, int x, int y) { m.Wr(BaseX, x); m.Wr(BaseY, y); }

    // js: game/states/wake.js sceneMain
    /// <summary>20:95FA: the main scene (frames $1B, $1C+n, $19, $18 when b3, $20/$21 by b2), then tmp_22 waits</summary>
    private static IEnumerable<Y> SceneMain(Machine m)
    {
        m.Wr(T2A, 4);
        int c = m.Rd(Cnt);
        int i = (c & 3) << 1;
        At(m, Prg.CpuRd(m, 0x9675 + i), Prg.CpuRd(m, 0x9676 + i));
        WakeSprFrame(m, 0x1B);
        At(m, 0x60, 0x6B);
        WakeSprFrame(m, (c & 3) + 0x1C);
        i = (c & 0x0C) >> 1;
        At(m, Prg.CpuRd(m, 0x967D + i), Prg.CpuRd(m, 0x967E + i));
        WakeSprFrame(m, 0x19);
        if ((c & 8) != 0) { At(m, 0x58, 0x53); WakeSprFrame(m, 0x18); }
        At(m, 0x80, 0x43);
        WakeSprFrame(m, 0x20 + ((c >> 2) & 1));               // 9659: b2 ends in the carry, LDA #0 / ADC #$20
        foreach (var s in WaitT22(m)) yield return s;
    }

    // js: game/states/wake.js sceneRise
    /// <summary>20:95B8 (rise) / 20:95D5 (with frame $1A): one 8-step frame at (80,73), 3 waits</summary>
    private static IEnumerable<Y> SceneRise(Machine m, bool second)
    {
        At(m, 0x80, 0x73);
        m.Wr(T2A, 4);
        if (second) WakeSprFrame(m, 0x1A);
        WakeSprFrame(m, (m.Rd(Cnt) & 7) + (second ? 8 : 0));
        m.Wr(T22, 3);
        foreach (var s in WaitT22(m)) yield return s;
    }

    // js: game/states/wake.js sprPalFade
    /// <summary>20:9706: sprite palette 0 ($6150-$6153) &lt;- $972D + 0/4/8/$C, 8 waits each</summary>
    private static IEnumerable<Y> SprPalFade(Machine m)
    {
        for (int x = 0; x < 0x10; x += 4)
        {
            for (int y = 0; y < 4; y++) m.Wr(0x6150 + y, Prg.CpuRd(m, 0x972D + x + y));
            m.Wr(T22, 8);
            foreach (var s in WaitT22(m)) yield return s;
        }
    }

    // js: game/states/wake.js wakeCutscene
    /// <summary>WakeCutscene 20:9400</summary>
    private static IEnumerable<Y> WakeCutscene(Machine m)
    {
        foreach (var s in Talk.LsrWait(m)) yield return s;
        for (int x = 0; x < 0x0C; x++) m.Wr(0x6154 + x, Prg.CpuRd(m, 0x9B02 + x));
        OamClear(m);
        m.Wr(0x07F5, 0x73);                                   // chr_r5_play
        m.Wr(Cnt, 0);
        m.Wr(T21, 8);
        foreach (var s in PalFade(m, 2)) yield return s;
        m.Wr(Cnt, 0xC0);
        m.Wr(T22, 3);
        WakeSound(m, 0x60);
        for (; ; )
        {                                                     // 9435
            PalCycle(m);
            OamClear(m);
            foreach (var s in SceneMain(m)) yield return s;
            int c = (m.Rd(Cnt) - 1) & 0xFF;
            m.Wr(Cnt, c);
            if (c == 0) break;
            if (c == 0x60) m.Wr(T22, 1);
            else if (c == 0xA0) m.Wr(T22, 2);
        }
        WakeSound(m, 0x61);                                   // 945F
        OamClear(m);
        foreach (var s in PalFade(m, 1)) yield return s;
        for (int x = 0x78; x != 0; x--) foreach (var s in Talk.LsrWait(m)) yield return s;   // 9481
        m.Wr(R.SubState, 8);
        foreach (var s in PalFade(m, 0)) yield return s;
        m.Wr(T22, 0x60);
        foreach (var s in WaitT22(m)) yield return s;
        OamClear(m);
        m.Wr(Cnt, 7);                                         // 9565
        WakeSound(m, 0x5D);
        do
        {
            foreach (var s in SceneRise(m, false)) yield return s;
            m.Wr(Cnt, (m.Rd(Cnt) - 1) & 0xFF);
        } while (m.Rd(Cnt) != 0xFF);
        WakeSound(m, 0x5C);
        m.Wr(T22, 0x30);
        foreach (var s in WaitT22(m)) yield return s;
        m.Wr(T2A, 4);
        WakeSprFrame(m, 0x1A);
        WakeSprFrame(m, 0);
        foreach (var s in SprPalFade(m)) yield return s;
        m.Wr(T22, 0x60);
        foreach (var s in WaitT22(m)) yield return s;
        WakeSound(m, 0x5E);
        m.Wr(Cnt, 7);
        do
        {
            foreach (var s in SceneRise(m, true)) yield return s;
            m.Wr(Cnt, (m.Rd(Cnt) - 1) & 0xFF);
        } while (m.Rd(Cnt) != 0xFF);
    }

    // js: game/states/wake.js substWakeCutscene
    /// <summary>SubstWakeCutscene 30:D9B6 + its tail 30:D91F</summary>
    public static IEnumerable<Y> SubstWakeCutscene(Machine m)
    {
        Prg.SwitchBank16(m, 0x0A);
        foreach (var s in WakeCutscene(m)) yield return s;
        foreach (var s in Death.StatRecalc(m, 0)) yield return s;   // X = 0 after the last 974C wait
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);                 // FarCall_B14_800E
        Prg.SwitchBank16(m, 0x0E);
        Obj.Sprites.SpriteDraw(m);
        Prg.FarRestore(m, r6, r7);
        Prg.SetR6(m, 0x1A);
        Death.PaletteBuildRom(m, 0);                          // 26:8C0E
        m.Wr(R.PadNew, 0);                                    // 30:D8BE
        m.Wr(R.SubState, 8);
    }
}
