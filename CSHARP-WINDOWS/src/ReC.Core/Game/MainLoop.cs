// re-c C# port — MainLoop 30:C900, MainStateTable 30:C91F, NMI 31:F3B6, NmiModeDispatch 31:F6AD
// (= web/src/game/mainloop.js).
//
// Multi-frame routines (docs/architecture_cs.md §3): a state handler is `Func<Machine, IEnumerable<Y>?>`.
// Convention: an iterator method (has `yield return`) = JS generator; MainStep starts it with
// GetEnumerator() + MoveNext() and keeps it in m.Pending. A plain method does its one frame of work and
// returns null (= a JS plain function), or returns another routine's iterator (= JS "returns g").
using ReC.Game.States;
using ReC.Gen;

namespace ReC.Game;

public sealed record MainStateEntry(string Addr, string Name, Func<Machine, IEnumerable<Y>?> Fn);

public static class MainLoop
{
    // ---------------------------------------------------------------- main states
    /// <summary>(mainloop.js MAIN_STATES)MainStateTable 30:C91F, 9 entries (6 / 7 = $0000)</summary>
    public static readonly MainStateEntry?[] MainStates =
    {
        // state 0: MainStateNewGame -> main_state 1 / sub_state 0 (states/areaenter.js)
        new("30:C957", "state0_newgame", AreaEnter.MainStateNewGame),
        // state 1: MainStateGameplay -> SubstateHandlerTable[$41] (states/play.js)
        new("30:CAB6", "state1_gameplay", Play.MainState1),
        // state 2: MainState2Init (18:9FCC -> 19:A000) ends with INC main_state (states/title.js)
        new("30:C931", "state2_init", Title.MainState2),
        // state 3: ReadPadsMenu, then JMP MainState3 19:A015 (title / menus / name entry; states/title.js)
        new("30:C939", "state3_title", Title.MainState3),
        // states 4 / 5: EndingInit 17:A000 / EndingTick 17:A100 (states/ending.js)
        new("30:C944", "state4_ending_init", Ending.MainState4),
        new("30:C94C", "state5_ending", Ending.MainState5),
        null, null,                                       // 6 / 7: $0000 in MainStateTable (30:C92B / C92D)
        // state 8: MainStateContinue (title CONTINUE) -> main_state 1 / sub_state 1 (states/areaenter.js)
        new("30:C9DA", "state8_continue", AreaEnter.MainStateContinue),
    };

    // js: game/mainloop.js mainStep
    /// <summary>MainLoop body for one frame: runs only when the NMI has set frame_ready ($09 != 0).</summary>
    public static bool MainStep(Machine m)
    {
        if (m.NoNmiResume) m.NoNmiResume = false;         // s107: the last frame's NMI was lost (NO_NMI)
        else if (m.Rd(R.FrameReady) == 0) return false;
        bool ran = false, more = false;
        if (m.Pending != null)                            // resume a multi-frame routine
        {
            more = m.Pending.MoveNext(); ran = true;
        }
        else
        {
            int ms = m.Rd(R.MainState);                   // one counted read, as the JS (lag model `top` bucket)
            var st = ms < MainStates.Length ? MainStates[ms] : null;
            if (st == null) throw new InvalidOperationException($"main_state {m.Rd(R.MainState)} out of range");
            var g = st.Fn(m);
            if (g != null) { m.Pending = g.GetEnumerator(); more = m.Pending.MoveNext(); ran = true; }
        }
        if (ran)
        {
            if (!more) m.Pending = null;
            else if (m.Pending!.Current == Y.Spin) return true;   // ROM spin loop (queue wait): frame_ready stays set
            else if (m.Pending.Current == Y.NoNmi) { m.SkipNmi = true; m.NoNmiResume = true; return true; }
        }
        if (m.Overrun)                                    // s114: CPU overrun (lag frame), see Machine.RunFrame
        {
            m.Overrun = false;
            m.OverrunNmi = true;                          // the NMI comes while frame_ready is still set
            return true;
        }
        m.Wr(R.FrameReady, 0);                            // 30:C919 (wait loops inside routines clear it too)
        return true;
    }

    // ---------------------------------------------------------------- NMI
    private static readonly int[] Slot13Src = { 0x83, 0xA3, 0xC3, 0xE3 };
    private static readonly int[] ScrollSrc = { R.ScrollShadow0, R.CamXHi, R.CamYLo, R.CamYHi };

    // js: game/mainloop.js nmiWork
    /// <summary>NMI 31:F3B6</summary>
    public static void NmiWork(Machine m)
    {
        m.Ppu.ReadStatus();                               // F3B7 LDA PPUSTATUS: resets w (IrqHandler1Hud leaves w = 1)
        if (m.Rd(R.NmiDisable) != 0) return;              // F3BA
        if (m.Rd(R.NmiBusyFlag) != 0) { m.Wr(R.NmiMissedFlag, 1); return; }   // F3BE
        if (m.Rd(R.FrameReady) == 0)                      // F3CC: main loop finished -> DMA + scroll copy
        {
            m.Ppu.OamAddr = 0;                            // F3D0 STA OAMADDR (A = 0)
            m.Ppu.OamDma(m.Mem.Page(2));                  // F3D5 $4014 <- 2
            var src = (m.Rd(R.IrqMode) == 9 || m.Rd(R.IrqMode) == 7)
                ? Slot13Src                               // F3F6: object slot $13 x lo/hi, y lo/hi
                : ScrollSrc;
            for (int i = 0; i < 4; i++) m.Wr(R.ScrollXIrq + i, m.Rd(src[i]));
        }
        m.PpuQueueFlush(m);                               // F40A PpuQueueFlush 30:C67D (world/ppuqueue.js)
        Palette.PaletteUpload(m, 0);                      // F40F
        m.Ppu.WriteReg(1, m.Rd(R.PpumaskShadow));         // F412
        NmiModeDispatch(m);                               // F417
        m.Wr(R.FrameReady, m.Rd(R.FrameReady) + 1);       // F41A
    }

    // ---------------------------------------------------------------- NMI-mode routines + IRQ split schedule
    // js: game/mainloop.js setIrqHandler
    /// <summary>SetIrqHandler 31:F424: $56 = index (IrqHandlerTable 31:F433). The port keeps a split list instead of $54/$55.</summary>
    public static void SetIrqHandler(Machine m, int i, int latch)
    {
        IrqIndex(m, i);
        m.IrqSplits.Add(new IrqSplit(i, latch));
    }

    // js: game/mainloop.js irqIndex
    /// <summary>SetIrqHandler 31:F424 RAM side: $56 = index, irq_vector $54/$55 = IrqHandlerTable 31:F433[index]</summary>
    public static void IrqIndex(Machine m, int i)
    {
        m.Wr(0x56, i);
        m.Wr(0x54, Prg.CpuRd(m, 0xF433 + i * 2));
        m.Wr(0x55, Prg.CpuRd(m, 0xF434 + i * 2));
    }

    // js: game/mainloop.js ppuScrollWrite
    /// <summary>31:EB6D: PPUSCROLL &lt;- $07D8, $07DA; ppuctrl_shadow nametable bits = ($07DB b0) &lt;&lt; 1 | ($07D9 b0)</summary>
    public static void PpuScrollWrite(Machine m)
    {
        var ppu = m.Ppu;
        ppu.WriteReg(5, m.Rd(R.ScrollXIrq));
        ppu.WriteReg(5, m.Rd(R.ScrollXIrq + 2));
        int nt = ((m.Rd(R.ScrollXIrq + 3) << 1) | (m.Rd(R.ScrollXIrq + 1) & 1)) & 3;
        m.Wr(0x52, nt);                                   // EB83 STA $52 (temp)
        int ctrl = (m.Rd(R.PpuctrlShadow) & 0xFC) | nt;
        m.Wr(R.PpuctrlShadow, ctrl);
        ppu.WriteReg(0, ctrl);
    }

    // js: game/mainloop.js nmiScrollSet
    /// <summary>31:F71B: PpuScrollWrite with $07DB temporarily = ($07DA >= $E8)</summary>
    public static void NmiScrollSet(Machine m)
    {
        int yHi = m.Rd(R.ScrollXIrq + 3);
        m.Wr(R.ScrollXIrq + 3, m.Rd(R.ScrollXIrq + 2) >= 0xE8 ? 1 : 0);
        PpuScrollWrite(m);
        m.Wr(R.ScrollXIrq + 3, yHi);
    }

    // js: game/mainloop.js applyChrPlay
    /// <summary>ApplyChrPlay 31:F883: MMC3 R0-R5 &lt;- $07F0-$07F5</summary>
    public static void ApplyChrPlay(Machine m)
    {
        for (int r = 0; r < 6; r++) m.Ppu.SetChrReg(r, m.Rd(R.ChrR0Play + r));
    }

    // js: game/mainloop.js nmiMode0
    /// <summary>NmiMode0 31:F6D6 (irq_mode 0 and 7)</summary>
    private static void NmiMode0(Machine m)
    {
        if ((m.Rd(R.PpumaskShadow) & 0x18) == 0) { m.SoundUpdate(m); return; }   // F6DC JMP SoundUpdate
        NmiScrollSet(m);                                  // F6DF
        ApplyChrPlay(m);                                  // F6E2
        if ((m.Rd(R.PpuctrlShadow) & 2) == 0)             // F6E5 LSR LSR -> C = b1 (lower nametable)
        {
            int a = (0xEF - m.Rd(R.ScrollXIrq + 2)) & 0xFF;   // lines until the 240-row wrap
            if (a < 0xBC)                                 // F6F1: wrap above the HUD -> handler 0 first
            {
                m.Wr(R.Zp57, a);
                SetIrqHandler(m, 0, a);                   // F6F5-F702: latch a, IRQ on, SetIrqHandler(0)
                return;                                   // handler 0 (31:F455) re-arms handler 1 with $BE - a
            }
        }
        SetIrqHandler(m, 1, 0xBE);                        // F705: HUD split, latch $BE - $57 with $57 = 0
        m.Wr(R.Zp57, 0xBE);
    }

    // js: game/mainloop.js applyChrAlt
    /// <summary>ApplyChrAlt 31:F64F: MMC3 R0-R5 &lt;- $58-$5D</summary>
    public static void ApplyChrAlt(Machine m)
    {
        for (int r = 0; r < 6; r++) m.Ppu.SetChrReg(r, m.Rd(R.ChrR0Alt + r));
    }

    // js: game/mainloop.js nmiMode2
    /// <summary>NmiMode2 31:F751 (irq_mode 2, pause / status screen): PPUCTRL nametable 2 (shadow unchanged),
    /// scroll 0/0, ApplyChrAlt; rendering on -> handler 1 at $BE, else SoundUpdate.</summary>
    private static void NmiMode2(Machine m)
    {
        m.Ppu.WriteReg(0, (m.Rd(R.PpuctrlShadow) & 0xFC) | 2);
        m.Ppu.WriteReg(5, 0);
        m.Ppu.WriteReg(5, 0);
        ApplyChrAlt(m);
        if ((m.Rd(R.PpumaskShadow) & 0x18) == 0) { m.SoundUpdate(m); return; }
        SetIrqHandler(m, 1, 0xBE);                        // F705: latch $BE - $57 ($57 = 0 here)
        m.Wr(R.Zp57, 0xBE);
    }

    // js: game/mainloop.js nmiMode3
    /// <summary>NmiMode3 31:F771 (title / menus, no splits): PPUMASK &lt;- $01, scroll, CHR R0-R5, MMC3 IRQ off, SoundUpdate</summary>
    private static void NmiMode3(Machine m)
    {
        m.Ppu.WriteReg(1, m.Rd(R.PpumaskShadow));         // F771
        PpuScrollWrite(m);                                // F776
        ApplyChrPlay(m);                                  // F779
        m.IrqSplits = new();                              // F77C STA MMC3_IRQOFF
        m.SoundUpdate(m);                                 // F77F JMP SoundUpdate
    }

    // js: game/mainloop.js nmiMode6
    /// <summary>NmiMode6 31:F7DB (irq_mode 6, the bank 16 equipment menu): PPUCTRL &lt;- shadow, scroll 0/0,
    /// ApplyChrAlt, IRQ handler 7 (IrqMenuChrSplit) at latch $77, $57 = $77</summary>
    private static void NmiMode6(Machine m)
    {
        m.Ppu.WriteReg(0, m.Rd(R.PpuctrlShadow));
        m.Ppu.WriteReg(5, 0);
        m.Ppu.WriteReg(5, 0);
        ApplyChrAlt(m);
        SetIrqHandler(m, 7, 0x77);
        m.Wr(R.Zp57, 0x77);
    }

    // js: game/mainloop.js irqMenuChrSplit
    /// <summary>IrqMenuChrSplit 31:F61A (IRQ handler 7, end of line $77): CHR R0/R1 &lt;- $5E/$5F, then handler 1
    /// at $BC - $57 lines on</summary>
    public static void IrqMenuChrSplit(Machine m, int line0)
    {
        m.Ppu.SetChrReg(0, m.Rd(0x5E));
        m.Ppu.SetChrReg(1, m.Rd(0x5F));
        IrqIndex(m, 1);                                   // SetIrqHandler(1)
        int line = 0x78 + ((0xBC - m.Rd(R.Zp57)) & 0xFF) + 1;
        if (line < 240 && !m.Ppu.Splits.ContainsKey(line)) m.Ppu.Splits[line] = _ => m.IrqHandlers[1](m, line);
    }

    // js: game/mainloop.js nmiMode1
    /// <summary>NmiMode1 31:F731 (irq_mode 1 and 9, message window / shop text screens): NmiScrollSet,
    /// ApplyChrPlay, IRQ handler 2 at latch $57 = ((07DA &amp; 7) ^ 7) + 7</summary>
    private static void NmiMode1(Machine m)
    {
        NmiScrollSet(m);
        ApplyChrPlay(m);
        int latch = (((m.Rd(R.ScrollXIrq + 2) & 7) ^ 7) + 7) & 0xFF;
        m.Wr(R.Zp57, latch);
        SetIrqHandler(m, 2, latch);
    }

    /// <summary>s115: handlers 2 / 3 write their MMC3 reload 2 lines later than handler 0 does (mainloop.js MSG_LATE)</summary>
    private const int MsgLate = 2;

    // js: game/mainloop.js chrTimed
    /// <summary>s209: an IRQ handler's ApplyChrAlt / ApplyChrPlay (R0-R5 from RAM at `base`) on the line its hook
    /// precedes: register r switches at dot0 + r * step.</summary>
    private static void ChrTimed(Machine m, int @base, int dot0, int step)
    {
        var p = m.Ppu;
        for (int r = 0; r < 6; r++)
        {
            int rr = r, v = m.Rd(@base + r);
            p.Timed(dot0 + r * step, _ => p.SetChrReg(rr, v));
        }
    }

    // js: game/mainloop.js chainSplit
    /// <summary>A chained MMC3 IRQ: reload N written inside the handler of line `line` fires N + 1 lines later</summary>
    private static void ChainSplit(Machine m, int line, int n, int handler)
    {
        int at = line + n + 1;
        if (at < 240 && !m.Ppu.Splits.ContainsKey(at)) m.Ppu.Splits[at] = _ => m.IrqHandlers[handler](m, at);
    }

    // js: game/mainloop.js irqMsgWindowTop
    /// <summary>IrqMsgWindowTop 31:F4E5 (IRQ handler 2): PPUADDR $2800 + X 0 (the window), ApplyChrAlt; H3 after
    /// $48 lines, $57 += $46; resume row -> $5E/$5F = NtRowAddrTable 31:F5A1[row] | ($07D8 >> 3); $57 += 8</summary>
    public static void IrqMsgWindowTop(Machine m, int line)
    {
        var p = m.Ppu;
        p.ReadStatus();
        p.WriteReg(6, 0x28); p.WriteReg(6, 0x00); p.WriteReg(5, 0x00);   // previous line's hblank (dots 272-315)
        ChrTimed(m, R.ChrR0Alt, 13, 39);
        ChainSplit(m, line, 0x48 + MsgLate, 3);
        IrqIndex(m, 3);                                   // F515 SetIrqHandler(3)
        int z = (m.Rd(R.Zp57) + 0x46) & 0xFF;
        m.Wr(R.Zp57, z);
        int a = m.Rd(R.ScrollXIrq + 2) + 8;
        a = (a & 0xFF) + z;                               // CLC ADC #8 / CLC ADC $57 (carry of the 2nd add)
        if (a > 0xFF || (a & 0xFF) >= 0xF0) a = (a & 0xFF) + 0x11;   // BCS / CMP #$F0: ADC #$10 with C = 1
        int y = ((a & 0xF8) >> 2) & 0xFF;
        m.Wr(R.Zp57, (z + 8) & 0xFF);
        m.Wr(0x5E, Prg.CpuRd(m, 0xF5A2 + y));
        m.Wr(0x5F, (m.Rd(R.ScrollXIrq) >> 3) | Prg.CpuRd(m, 0xF5A1 + y));
    }

    // js: game/mainloop.js irqMsgWindowEnd
    /// <summary>IrqMsgWindowEnd 31:F54B (IRQ handler 3): PPUADDR $5E/$5F + X = $07D8, ApplyChrPlay; then H0 (wrap)
    /// or H1 (HUD) at $BE - $57.</summary>
    public static void IrqMsgWindowEnd(Machine m, int line)
    {
        var p = m.Ppu;
        p.ReadStatus();
        p.WriteReg(6, m.Rd(0x5E)); p.WriteReg(6, m.Rd(0x5F)); p.WriteReg(5, m.Rd(R.ScrollXIrq));   // hblank
        ChrTimed(m, R.ChrR0Play, 4, 42);
        int a = (0xEF - m.Rd(R.ScrollXIrq + 2)) & 0xFF;
        int z = m.Rd(R.Zp57);
        if (a < 0xBC && a >= z)
        {
            ChainSplit(m, line, ((a - z) & 0xFF) + MsgLate, 0);
            m.Wr(R.Zp57, a);
            IrqIndex(m, 0);
        }
        else
        {
            ChainSplit(m, line, ((0xBE - z) & 0xFF) + MsgLate, 1);
            IrqIndex(m, 1);
        }
    }

    // js: game/mainloop.js irqWrapSplit
    /// <summary>IrqHandler0 31:F455 (the playfield's 240-line wrap): PPUADDR $00:(07D8 >> 3), fine X = $07D8;
    /// then handler 1 (HUD) after $BC - $57 lines</summary>
    public static void IrqWrapSplit(Machine m, int line)
    {
        var p = m.Ppu;
        p.ReadStatus();
        p.WriteReg(6, 0x00);
        p.WriteReg(6, m.Rd(R.ScrollXIrq) >> 3);
        p.WriteReg(5, m.Rd(R.ScrollXIrq));
        ChainSplit(m, line, (0xBC - m.Rd(R.Zp57)) & 0xFF, 1);
        IrqIndex(m, 1);                                   // SetIrqHandler(1)
    }

    // js: game/mainloop.js irqHud
    /// <summary>IrqHandler1Hud 31:F488: IRQ off, PPUMASK sprites off (BG kept), PPUADDR $2B00 + X 0 (the HUD rows of
    /// nametable 2), CHR R0/R1 = $3C/$38, then the gameplay SoundUpdate under nmi_busy_flag</summary>
    public static void IrqHud(Machine m, int line)
    {
        var p = m.Ppu;
        p.ReadStatus();
        p.WriteReg(1, (m.Rd(R.PpumaskShadow) & 0x0E) | 0x08);   // line 190 dot 331-338 (hblank): whole next line
        // s209: the rest lands inside the next line (2nd PPUADDR at dot ~41; see mainloop.js)
        p.Timed(HudDotAddr, _ => { p.ReadStatus(); p.WriteReg(6, 0x2B); p.WriteReg(6, 0x00); });
        p.Timed(HudDotAddr + 18, _ => p.WriteReg(5, 0x00));       // STA PPUSCROLL 31:F4AF (+6 cycles)
        // tools.emu's mapper writes do not sync the PPU, so a CHR switch already applies from the dot its STA starts
        p.Timed(HudDotAddr + 45, _ => p.SetChrReg(0, 0x3C));
        p.Timed(HudDotAddr + 81, _ => p.SetChrReg(1, 0x38));
        int start = MusicStartId(m);
        m.Wr(R.NmiBusyFlag, 1);
        m.SoundUpdate(m);
        // s174: a SoundUpdate that starts a track longer than the time to line 241 loses the NMI (hudNmiLost)
        if (start >= 0 && m.Rd(0x0111) == start && MusicStartCycles[start] > HudSoundBudget)
        {
            m.HudNmiLost = true;
            return;
        }
        m.Wr(R.NmiBusyFlag, 0);
        if (m.Rd(R.NmiMissedFlag) != 0) NmiModeDispatch(m);
        m.Wr(R.NmiMissedFlag, 0);
    }

    /// <summary>s209: PPU dot of IrqHandler1Hud's 2nd PPUADDR write (31:F4AA) on the line after the IRQ line</summary>
    private const int HudDotAddr = 41;

    /// <summary>s174: CPU cycles of the SoundUpdate call that starts music id $00-$1F</summary>
    private static readonly int[] MusicStartCycles =
    {
        2677, 6010, 414, 4512, 5297, 414, 5614, 5764, 5822, 5016, 4305, 5683, 4959, 4171, 4555, 4715,
        414, 5097, 6228, 4443, 5668, 6197, 3941, 5012, 4981, 5487, 5910, 4579, 6091, 5033, 5341, 4869,
    };

    /// <summary>IrqHandler1Hud sets nmi_busy_flag at about L191 dot 172; the NMI is at L241 dot 1: (50 * 341 - 171) / 3</summary>
    private const int HudSoundBudget = 5626;

    // js: game/mainloop.js musicStartId
    /// <summary>The music id the next SoundUpdate starts (its F832 resume or F843 queue path, id &lt; $20), else -1</summary>
    private static int MusicStartId(Machine m)
    {
        int pend = m.Rd(0x0102);
        if ((pend & 0x80) != 0)
        {
            bool resume = (m.Rd(0xFE) & 0x80) != 0
                ? ((m.Rd(0x0168) | m.Rd(0x0169) | m.Rd(0x016A) | m.Rd(0x016B)) & 0x0F) == 0
                : m.Rd(0x0111) == 0xFF;
            if (resume) return (pend & 0x7F) < 0x20 ? pend & 0x7F : -1;
        }
        if (m.Rd(0x0101) == 0 && m.Rd(0x0100) != 0 && m.Rd(0x0103) < 0x20) return m.Rd(0x0103);
        return -1;
    }

    // js: game/mainloop.js nmiLostToHud
    /// <summary>s174: the NMI that found nmi_busy_flag set (31:F3BE): PPUSTATUS read, nmi_missed_flag; then the end
    /// of IrqHandler1Hud: busy off, NmiModeDispatch, missed flag off</summary>
    public static void NmiLostToHud(Machine m)
    {
        m.Ppu.ReadStatus();
        if (m.Rd(R.NmiDisable) != 0) { m.Wr(R.NmiBusyFlag, 0); return; }
        m.Wr(R.NmiBusyFlag, 0);
        NmiModeDispatch(m);
        m.Wr(R.NmiMissedFlag, 0);
    }

    // js: game/mainloop.js nmiMode5
    /// <summary>NmiMode5 31:F7C5 (irq_mode 5, the cold-boot intro): PpuScrollWrite, ApplyChrPlay, IRQ handler 6 at latch $58</summary>
    private static void NmiMode5(Machine m)
    {
        PpuScrollWrite(m);
        ApplyChrPlay(m);
        SetIrqHandler(m, 6, m.Rd(R.ChrR0Alt));
    }

    // js: game/mainloop.js irqIntroSplit
    /// <summary>IrqHandler6 31:F691: (DelayX $07FE) IRQ off, PPUADDR &lt;- $59:$5A (the intro: $2800 at line 137)</summary>
    public static void IrqIntroSplit(Machine m, int line)
    {
        var p = m.Ppu;
        p.ReadStatus();
        p.WriteReg(6, m.Rd(R.ChrR0Alt + 1));
        p.WriteReg(6, m.Rd(R.ChrR0Alt + 2));
    }

    // js: game/mainloop.js nmiMode4
    /// <summary>NmiMode4 31:F782 (irq_mode 4, dead in the ROM): PpuScrollWrite, ApplyChrPlay; $58 = 0 -> ppumask_shadow
    /// BG bit 3 off + IRQ off; else $59 = NmiMode4R1Table 31:F7B5[$58], $5A = 0, handler 4 at latch 4.</summary>
    private static void NmiMode4(Machine m)
    {
        PpuScrollWrite(m);                                // F782
        ApplyChrPlay(m);                                  // F785
        int n = m.Rd(R.ChrR0Alt);                         // F788 LDA $58
        if (n == 0)
        {
            m.Wr(R.PpumaskShadow, m.Rd(R.PpumaskShadow) & 0xF7);   // F7AA-F7AE (STA $0001 absolute)
            m.IrqSplits = new();                          // F7B1 STA MMC3_IRQOFF
            return;
        }
        m.Wr(R.ChrR0Alt + 1, Prg.CpuRd(m, 0xF7B5 + n));   // F78E $59
        m.Wr(R.ChrR0Alt + 2, 0);                          // F795 $5A
        SetIrqHandler(m, 4, 4);                           // F797-F7A7: latch 4, IRQ on, SetIrqHandler(4)
    }

    // js: game/mainloop.js irqRowStretch
    /// <summary>IrqRowStretch 31:F5E1 (IRQ handler 4, dead like NmiMode4): PPUADDR &lt;- NtRowAddrTable 31:F5A1[$5A],
    /// $5A++, $59--; $59 &lt; 0 -> handler 5, else the same handler again every $58 + 2 lines.</summary>
    public static void IrqRowStretch(Machine m, int line)
    {
        var p = m.Ppu;
        p.ReadStatus();                                   // F5E7
        int row = m.Rd(R.ChrR0Alt + 2);
        int y = (row << 1) & 0xFF;                        // F5EA ASL TAY
        p.WriteReg(6, Prg.CpuRd(m, 0xF5A2 + y));
        p.WriteReg(6, Prg.CpuRd(m, 0xF5A1 + y));
        m.Wr(R.ChrR0Alt + 2, (row + 1) & 0xFF);           // F5FA INC $5A
        int left = (m.Rd(R.ChrR0Alt + 1) - 1) & 0xFF;
        m.Wr(R.ChrR0Alt + 1, left);                       // F5FC DEC $59
        if ((left & 0x80) != 0) { IrqOffSound(m, line); return; }   // F5FE BMI IrqOffSound
        ChainSplit(m, line, m.Rd(R.ChrR0Alt) + RowLate, 4);   // F600-F605 latch $58 + reload
    }

    private const int RowLate = 1;

    // js: game/mainloop.js irqOffSound
    /// <summary>IrqOffSound 31:F60E (IRQ handler 5; only reached from F5FE): MMC3 IRQ off, SoundUpdate.</summary>
    public static void IrqOffSound(Machine m, int line)
    {
        m.SoundUpdate(m);                                 // F611
    }

    /// <summary>(mainloop.js NMI_MODES)JmpTable31_F6C2: irq_mode -> NMI routine</summary>
    public static readonly Action<Machine>?[] NmiModes =
    {
        NmiMode0, NmiMode1, NmiMode2, NmiMode3,
        NmiMode4, NmiMode5, NmiMode6, NmiMode0,
        null, NmiMode1,
    };

    // js: game/mainloop.js nmiModeDispatch
    /// <summary>NmiModeDispatch 31:F6AD: $57 = 0, JMP (JmpTable31_F6C2[irq_mode]); ASL drops b7</summary>
    public static void NmiModeDispatch(Machine m)
    {
        m.Wr(R.Zp57, 0);
        m.IrqSplits = new();
        int mode = m.Rd(R.IrqMode) & 0x7F;
        m.Wr(0x52, Prg.CpuRd(m, 0xF6C2 + mode * 2));      // F6B5-F6BD: $52/$53 = the JMP ($0052) vector
        m.Wr(0x53, Prg.CpuRd(m, 0xF6C3 + mode * 2));
        var fn = mode < NmiModes.Length ? NmiModes[mode] : null;
        if (fn == null) throw new InvalidOperationException($"irq_mode {m.Rd(R.IrqMode)}: null NMI-mode entry");
        fn(m);
    }
}
