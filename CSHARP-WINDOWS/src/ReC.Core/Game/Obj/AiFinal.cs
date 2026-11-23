// re-c C# port — AI $70/$F0 Ai70PhaseDispatch 27:BA2E, the final-scene object (= web/src/game/obj/ai_final.js, s97).
// REVERSE.md "AI $70 final-scene phases (session 97)" + jump table row `27:BA40`.
//
//   Ai70PhaseDispatch(m, x)  iterator: JMP (JmpTable27_BA40[obj_phase]) with $10/$11 = the handler address
//
//   phase 0  BA5A  once slot $0D is free: sound $6E, the 8-pass window wipe (BA71), visited_map $62F0 = $FF, slot off
//   phase 1  BAF5  boss_cam_lock = $7F; one type-$31 object per 16 frames from table BBCE; at the table end messages
//                  $1B.05-07 (ShowMessage leaves X = 0, Y = $0A, s173), full HP / MP, player AI = 2, slot off
//   phase 2  BBE7  $6060-$609F = $FF, attr_shadow -> $6060-$608F, block $04, sub_state $1F, phase+1
//   phase 3  BC0C  pal ids $07E0-2 = $A2-$A4 each with FadeIn(9); sound $14
//   phase 4-6      hit flash (FlashIrq), pose / shot cycle
//   phase 8  BD29  shots; phase 9 BCF6 bank 11 $9E00 -> $6000, block $44; phase 10 BD19 -> sub_state $1E
//   phase 12 BDA9  follower of the leader slot obj_param2
//   phases 7, 11 (null) and >= 13 jump through garbage in the ROM: logged as unported.
// Phase handlers: an iterator = JS generator, null = plain function (AI handler convention, Objects.cs).
using ReC.Game.States;

namespace ReC.Game.Obj;

public static class AiFinal
{
    private const int Meta = 0x0300, TileOfs = 0x0320, MetaDir = 0x0360, Flags = 0x0380, Coll = 0x03A0, Active = 0x04A0;
    private const int Anim = 0x04E0, Overlay = 0x0580, Freeze = 0x05A0, Phase = 0x0600, Timer2 = 0x0620, ActTimer = 0x0640;
    private const int Param2 = 0x06A0, Orbit = 0x06C0, Behav = 0x06E0, Flags2 = 0x0340;
    private const int Buf = 0x6000;

    // js: game/obj/ai_final.js unported
    private static void Unported(Machine m, string what)
    {
        if (m.Unported.Add(what)) m.Log($"{what} not ported yet");
    }

    // js: game/obj/ai_final.js inc
    private static int Inc(Machine m, int a) { int v = (m.Rd(a) + 1) & 0xFF; m.Wr(a, v); return v; }

    // js: game/obj/ai_final.js dec
    private static int Dec(Machine m, int a) { int v = (m.Rd(a) - 1) & 0xFF; m.Wr(a, v); return v; }

    // js: game/obj/ai_final.js hitFlashPal
    /// <summary>26:9B07 (A = palette mask): obj_flags2 b7 clear -> PaletteBuild; else on odd frames every pal_buf entry
    /// whose group bit is set in A (entry 0 of each group always) turns $30, except $0F in the BG half.</summary>
    public static void HitFlashPal(Machine m, int x, int a)
    {
        m.Wr(0x10, a);
        if ((m.Rd(Flags2 + x) & 0x80) == 0) { Death.PaletteBuildRom(m, 0); return; }
        if ((m.Rd(0x08) & 1) == 0) return;
        int y = 0;
        while (y != 0x20)
        {
            if ((y & 3) == 0)
            {                                                     // 9B34: next group's bit
                int b = m.Rd(0x10);
                m.Wr(0x10, b >> 1);
                if ((b & 1) == 0) { y += 4; continue; }
            }
            if (y >= 0x10 || m.Rd(0x6140 + y) != 0x0F) m.Wr(0x6140 + y, 0x30);
            y++;
        }
    }

    // js: game/obj/ai_final.js countActive
    private static int CountActive(Machine m)
    {
        int n = 0;
        for (int s = 0; s < 0x1F; s++) if (m.Rd(Active + s) != 0) n++;
        return n;
    }

    // js: game/obj/ai_final.js flashIrq
    /// <summary>27:BCD7: hit flash (mask 7), irq_mode 0, or 2 with chr_r0_alt $58-$5D = chr_r0_play $07F0-$07F5 when $08 b2 clear</summary>
    private static void FlashIrq(Machine m, int x)
    {
        // s174 lag-frame heuristic for phases 4-6 (see the JS); s220: with LagMode2 the cycle model judges these passes
        if (!(m.LagModelOn && m.LagMode2))
        {
            int n = CountActive(m);
            if (n >= 17 || (n >= 14 && (m.Rd(0x08) & 1) == 0)) m.Overrun = true;
        }
        // s220: exact ROM cycles BCD7..BCF5 (+ RTS) booked as stage `fl70` when obj_flags2 b7 is clear (PaletteBuild path)
        string stage = m.PassStage;
        bool exact = (m.Rd(Flags2 + x) & 0x80) == 0;
        if (exact) { m.PassStage = "fl70"; m.Cyc += (m.Rd(0x08) & 4) != 0 ? 2175 : 2264; }
        HitFlashPal(m, x, 7);
        m.Wr(0x51, 0);
        if ((m.Rd(0x08) & 4) == 0)
        {
            m.Wr(0x51, 2);
            for (int y = 5; y >= 0; y--) m.Wr(0x58 + y, m.Rd(0x07F0 + y));
        }
        if (exact) m.PassStage = stage;
    }

    // js: game/obj/ai_final.js windowImage
    /// <summary>27:BAB8: the window image in $6000-$60DB (10 rows of $16: $1A ... $6F/$72/$75 ... $1B, corners $7F)</summary>
    private static void WindowImage(Machine m)
    {
        for (int x = 0xDC; x > 0; x--) m.Wr(0x5FFF + x, 0x7C);
        for (int x = 0x16; x >= 0; x--)
        {
            m.Wr(0x6001 + x, 0x6F); m.Wr(0x6017 + x, 0x72); m.Wr(0x60C7 + x, 0x75);
        }
        for (int x = 0, y = 10; y != 0; y--, x += 0x16) { m.Wr(Buf + x, 0x1A); m.Wr(0x6015 + x, 0x1B); }
        m.Wr(0x60C6, 0x7F); m.Wr(0x60DB, 0x7F);
    }

    // js: game/obj/ai_final.js phase0
    private static IEnumerable<Y> Phase0(Machine m, int x)
    {
        if (m.Rd(0x04AD) != 0) yield break;
        PlaySound.Play(m, 0x6E, 0);                               // Y = obj_phase*2 = 0
        m.Wr(0x2F, x);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;  // BA71
        WindowImage(m);
        m.Wr(0x1F, 7);
        do
        {
            foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
            m.Wr(0x09, 0);
            for (int i = Prg.CpuRd(m, 0xBA9A + m.Rd(0x1F)); i < 0xDC; i += 8)
            {                                                     // BAA2
                if (m.Rd(Buf + i) == 0x7C) m.Wr(Buf + i, 0x7E);
            }
            foreach (var s in Pause.PpuQueueBlock(m, 0x2E)) yield return s;
        } while ((Dec(m, 0x1F) & 0x80) == 0);
        m.Wr(0x62F0, 0xFF);
        m.Wr(Active + x, 0);
    }

    // js: game/obj/ai_final.js phase1
    private static IEnumerable<Y> Phase1(Machine m, int x)
    {
        m.Wr(0x07D7, 0x7F);
        if ((m.Rd(Behav + x) & 0x80) == 0) { Inc(m, Behav + x); yield break; }
        int anim = Inc(m, Anim + x);
        if ((anim & 0x0F) != 0) yield break;
        int y = anim >> 2;
        if (Prg.CpuRd(m, 0xBBCE + y) != 0xFF)
        {                                                         // BB79: one type-$31 object from table BBCE
            int n = (x + (anim >> 4)) & 0xFF;
            m.Wr(0x10, n); m.Wr(0x11, 0x31);
            ObjInit.FarObjInit(m);
            m.Wr(0x70 + n, Prg.CpuRd(m, 0xBBCE + y)); m.Wr(0x90 + n, 0);
            m.Wr(0xB0 + n, Prg.CpuRd(m, 0xBBCF + y)); m.Wr(0xD0 + n, 0);
            m.Wr(TileOfs + n, Prg.CpuRd(m, 0xBBD0 + y));
            m.Wr(Meta + n, Prg.CpuRd(m, 0xBBD1 + y));
            m.Wr(Phase, 0); m.Wr(Timer2, 0); m.Wr(Orbit, 0); m.Wr(MetaDir, 0);
            m.Wr(Freeze, 0xFF);
            m.Wr(0x70, 0x80); m.Wr(0xB0, 0xA0);
            yield break;
        }
        Dec(m, Anim + x);                                         // BB1A: table end
        if ((m.Rd(Orbit + x) & 0x80) == 0) { Inc(m, Orbit + x); yield break; }
        m.Wr(0x07DF, 0x1B); m.Wr(0x07DE, 5);
        int sy = y;                                               // BB30 TXA PHA (the slot comes back at BB6C)
        do
        {
            PlaySound.Play(m, 0x29, sy);
            int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);                 // BB37 JSR FarCall_D354
            foreach (var s in Talk.ShowMessage(m)) yield return s;
            Prg.FarRestore(m, r6, r7);
            sy = 0x0A;                                            // s173 probe: ShowMessage leaves X = 0, Y = $0A
        } while (Inc(m, 0x07DE) != 8);
        m.Wr(0x0676, 1); m.Wr(0x04B6, 0x53); m.Wr(0x05B6, 0);
        PlaySound.Play(m, 0x25, sy);
        m.Wr(0x03C1, m.Rd(0x03C0));
        foreach (var s in Death.HudHpBar(m)) yield return s;
        m.Wr(0x0708, m.Rd(0x0709));
        foreach (var s in Death.HudRedraw(m, 4, 0)) yield return s;   // X = 0 from ShowMessage ($14 = 0)
        m.Wr(Active, 2);
        m.Wr(Active + x, 0);
    }

    // js: game/obj/ai_final.js phase2
    private static IEnumerable<Y> Phase2(Machine m, int x)
    {
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int y = 0x3F; y >= 0; y--) m.Wr(0x6060 + y, 0xFF);
        for (int y = 0x2F; y >= 0; y--) m.Wr(0x6060 + y, m.Rd(0x6100 + y));
        foreach (var s in Pause.PpuQueueBlock(m, 4)) yield return s;
        m.Wr(0x41, 0x1F);
        Inc(m, Phase + x);
    }

    // js: game/obj/ai_final.js phase3
    private static IEnumerable<Y> Phase3(Machine m, int x)
    {
        Inc(m, Phase + x);
        m.Wr(0x2F, x);
        for (int i = 2; i >= 0; i--)
        {
            m.Wr(0x07E8, Prg.CpuRd(m, 0xBC34 + i));
            m.Wr(0x07E0 + i, i + 0xA2);
            foreach (var s in Death.FadeIn(m, 9)) yield return s;
        }
        m.Wr(0x07E8, 0);
        PlaySound.Play(m, 0x14, 4);                               // Y = 4 from PaletteLoadOne's last group
    }

    // js: game/obj/ai_final.js phase4
    private static IEnumerable<Y>? Phase4(Machine m, int x)
    {
        m.Wr(Coll + x, 0);
        FlashIrq(m, x);
        m.Wr(Overlay + x, 0xFC);
        m.Wr(Meta + x, 0xFB);
        m.Wr(Anim + x, 0);
        int t = Inc(m, Timer2 + x);
        m.Wr(Behav + x, Prg.CpuRd(m, 0xBC61 + (t & 0x0F)));
        Inc(m, Phase + x);
        return null;
    }

    // js: game/obj/ai_final.js phase5
    private static IEnumerable<Y>? Phase5(Machine m, int x)
    {
        FlashIrq(m, x);
        if (Dec(m, Behav + x) != 0) return null;
        Inc(m, Behav + x);
        m.Wr(Coll + x, 0x34);
        if (Inc(m, Anim + x) != 0x1F) return null;
        Inc(m, Phase + x);
        m.Wr(Behav + x, 0x20);
        return null;
    }

    // js: game/obj/ai_final.js phase6
    private static IEnumerable<Y>? Phase6(Machine m, int x)
    {
        FlashIrq(m, x);
        if ((m.Rd(0x064E) & 3) != 0 && (m.Rd(0x08) & 7) == 0 && ObjUtil.SpawnShot(m, x, 0x29, 4))
        {
            int n = m.Rd(0x10);
            m.Wr(0x70 + n, (m.Rd(0x70 + n) + 4) & 0xFF);
            m.Wr(0xB0 + n, (m.Rd(0xB0 + n) + 0x12) & 0xFF);
        }
        if (Dec(m, Behav + x) != 0) return null;
        Inc(m, Behav + x);
        if (Dec(m, Anim + x) != 0) return null;
        m.Wr(Phase + x, 4);
        return null;
    }

    // js: game/obj/ai_final.js phase8
    private static IEnumerable<Y>? Phase8(Machine m, int x)
    {
        int f = m.Rd(0x08);
        if ((m.Rd(0x034D) & 0x80) != 0)
        {                                                         // BD68
            if ((f & 7) != 0) return null;
            m.Wr(0x10, m.Rd(0x70) >> 4);
            ObjUtil.SpawnShot(m, x, 0x28, Prg.CpuRd(m, 0xBD89 + (((x & 1) << 4) | m.Rd(0x10))));
            return null;
        }
        if (f == 0) Inc(m, ActTimer + x);
        if ((((x << 2) + m.Rd(ActTimer + x)) & 7) != 0 || (f & 1) != 0) return null;
        int d = Prg.CpuRd(m, 0xBD58 + ((f & 0x1E) >> 1));
        if ((d & 0x80) == 0) ObjUtil.SpawnShot(m, x, 0x2A, d);
        return null;
    }

    // js: game/obj/ai_final.js phase9
    private static IEnumerable<Y> Phase9(Machine m, int x)
    {
        Inc(m, Phase + x);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        Prg.SetR6(m, 0x0B);
        for (int y = 0; y < 0x100; y++) m.Wr(Buf + y, Prg.CpuRd(m, 0x9E00 + y));
        foreach (var s in Pause.PpuQueueBlock(m, 0x44)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        Prg.SetR6(m, 0x1A);
    }

    // js: game/obj/ai_final.js phase10
    private static IEnumerable<Y>? Phase10(Machine m, int x)
    {
        if (Inc(m, Timer2 + x) != 0) return null;
        m.Wr(0x41, 0x1E);
        m.Wr(Active + x, 0);
        return null;
    }

    // js: game/obj/ai_final.js phase12
    private static IEnumerable<Y>? Phase12(Machine m, int x)
    {
        int l = m.Rd(Param2 + x);                                 // BDA9 LDY obj_param2,X (the leader slot)
        if (m.Rd(Active + l) != 0x2E) { m.Wr(Active + x, 0); return null; }
        if (m.Rd(Freeze + l) == 0)
        {
            m.Wr(Coll + x, 0);
            m.Wr(Meta + x, 0xF4);
            int a = (m.Rd(Anim + x) - 4) & 0xFF;
            m.Wr(Anim + x, a);
            if ((a & 0x7F) == 0 && (m.Rd(Flags + x) & 0x80) == 0) PlaySound.Play(m, 0x6F, l);
        }
        m.Wr(0x1F, x);                                            // BDDF
        ObjUtil.ObjPosToTemp(m, l);
        m.Wr(0x10, m.Rd(ActTimer + l));
        int k = m.Rd(Meta + l) & 3;
        m.Wr(0x11, k);
        int y = k << 1;
        if (m.Rd(0x10) != 0)
        {
            y |= 8;
            m.Wr(Meta + x, k + 0xF5);
            m.Wr(Anim + x, k << 4);
            m.Wr(Coll + x, (k & 1) | 0x46);
            m.Wr(MetaDir + x, k << 1);
        }
        m.Wr(0x30, Prg.CpuRd(m, 0xBE30 + y)); m.Wr(0x31, Prg.CpuRd(m, 0xBE31 + y));
        ObjUtil.TempAddOffset(m);
        ObjUtil.TempToObjPos(m, x);
        return null;
    }

    private static readonly Func<Machine, int, IEnumerable<Y>?>?[] Phases =
        [Phase0, Phase1, Phase2, Phase3, Phase4, Phase5, Phase6, null, Phase8, Phase9, Phase10, null, Phase12];

    // js: game/obj/ai_final.js ai70PhaseDispatch
    /// <summary>AI $70/$F0 27:BA2E</summary>
    public static IEnumerable<Y> Ai70PhaseDispatch(Machine m, int x)
    {
        int p = m.Rd(Phase + x), y = (p << 1) & 0xFF;
        m.Wr(0x10, Prg.CpuRd(m, 0xBA40 + y));
        m.Wr(0x11, Prg.CpuRd(m, 0xBA41 + y));
        var h = p < Phases.Length ? Phases[p] : null;
        if (h == null) { Unported(m, $"AI $70 phase {p} (JMP through ${(m.Rd(0x11) << 8 | m.Rd(0x10)):x})"); yield break; }
        var g = h(m, x);
        if (g != null) foreach (var s in g) yield return s;
    }

    /// <summary>(ai_final.js AI_FINAL)</summary>
    public static readonly Dictionary<int, AiFn> Ai = new()
    {
        [0x70] = (m, x, r) => Ai70PhaseDispatch(m, x),
    };
}
