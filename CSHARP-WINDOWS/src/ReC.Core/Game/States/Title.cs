// re-c C# port — main states 2 / 3 (title side, banks 18/19) (= web/src/game/states/title.js, Ph7 web 7/8 (a), s82).
//
//   MainState2(m)   30:C931 SwitchBank16(9) -> MainState2Init 18:9FCC -> 19:A000 (INC main_state)
//   MainState3(m)   30:C939 SwitchBank16(9), ReadPadsMenu, MainState3 19:A015
//
// Ported line by line from REVERSE.md "Main states 2/3" + "State-3 modes named". MainState3 dispatches on
// state3_mode $06F3 (0 = State3SubDispatch on title_mode $06F0, 1 = TitleFadeIn, 2 = TitleFadeOut).
// Title modes: 0 TitleOpening (25 steps), 5 TitleMenuInit, 6 TitleMenu; 1 TitleStoryInit, 2 TitleIntroStory
// (28 steps), 3 TitleStatusInit, 4 TitleStatusScreen (7 steps incl. name entry) -> main_state 0.
// PpuQueueDrainWait 19:A092 is a `Y.Spin` loop: the status text and name tiles go through the $6200 queue.
//
// Routines that spin on frame_ready (WaitFrameReady 18:8198) are iterators (docs/architecture_cs.md §3); dispatch
// entries are `Func<Machine, IEnumerable<Y>?>`: plain steps return null, iterator steps the iterator.
// Direct PPU writes (NtFill 18:81A1, NtRectBlit 18:81F0) go through m.Ppu.WriteReg like the ROM's $2000-$2007.
using ReC.Gen;

namespace ReC.Game.States;

public static class Title
{
    // title RAM (labels.csv) — title.js T
    private const int title_mode = 0x06F0, title_step = 0x06F1, ram_06F2 = 0x06F2, state3_mode = 0x06F3,
        jmp_lo = 0x06FC, jmp_hi = 0x06FD, wait = 0x06FE, ctr = 0x06FF, snd = 0x06B2,
        pal_target = 0x06D0,          // $06D0-$06EF MenuPalLoad result / fade target
        match_cnt = 0x06CF;
    private const int OAM = 0x0200;

    // js: game/states/title.js inc
    private static void Inc(Machine m, int a) => m.Wr(a, m.Rd(a) + 1);
    // js: game/states/title.js dec
    private static int Dec(Machine m, int a) { int v = (m.Rd(a) - 1) & 0xFF; m.Wr(a, v); return v; }
    // js: game/states/title.js ptr16
    private static int Ptr16(Machine m, int a) => Prg.CpuRd(m, a) | Prg.CpuRd(m, a + 1) << 8;

    // ---------------------------------------------------------------- bank 18 screen-setup library
    // js: game/states/title.js ppuNmiOff
    /// <summary>PpuNmiOff 18:8174: PPUCTRL &lt;- ppuctrl_shadow with b7 clear</summary>
    private static void PpuNmiOff(Machine m) => m.Ppu.WriteReg(0, m.Rd(R.PpuctrlShadow) & 0x7F);
    // js: game/states/title.js ppuNmiOn
    /// <summary>PpuNmiOn 18:817C: PPUCTRL &lt;- ppuctrl_shadow with b7 set</summary>
    private static void PpuNmiOn(Machine m) => m.Ppu.WriteReg(0, m.Rd(R.PpuctrlShadow) | 0x80);
    // js: game/states/title.js maskOff
    /// <summary>18:8184: ppumask_shadow &amp;= $E1 (BG + sprites off), also written to PPUMASK</summary>
    private static void MaskOff(Machine m) { int v = m.Rd(R.PpumaskShadow) & 0xE1; m.Ppu.WriteReg(1, v); m.Wr(R.PpumaskShadow, v); }
    // js: game/states/title.js maskOn
    /// <summary>18:818E: ppumask_shadow |= $1E, also written to PPUMASK</summary>
    private static void MaskOn(Machine m) { int v = m.Rd(R.PpumaskShadow) | 0x1E; m.Ppu.WriteReg(1, v); m.Wr(R.PpumaskShadow, v); }

    // js: game/states/title.js waitFrameReady
    /// <summary>WaitFrameReady 18:8198: spin until frame_ready != 0, then clear it</summary>
    public static IEnumerable<Y> WaitFrameReady(Machine m)
    {
        while (m.Rd(R.FrameReady) == 0) yield return Y.Frame;
        m.Wr(R.FrameReady, 0);
    }

    // js: game/states/title.js ntFill
    /// <summary>NtFill 18:81A1: A = PPU address hi; $3C0 x tile X, then $40 x attribute Y (rendering off meanwhile)</summary>
    public static void NtFill(Machine m, int hi, int tile, int attr)
    {
        m.Wr(0x72, tile); m.Wr(0x73, attr);
        PpuNmiOff(m); MaskOff(m);
        m.Ppu.WriteReg(6, hi); m.Ppu.WriteReg(6, 0);
        for (int i = 0; i < 0x3C0; i++) m.Ppu.WriteReg(7, tile);
        m.Wr(0x88, 0);                                   // 81C3 DEC zp_88 loop ends at 0
        for (int i = 0; i < 0x40; i++) m.Ppu.WriteReg(7, attr);
        MaskOn(m); PpuNmiOn(m);
    }

    // js: game/states/title.js oamHideAll
    /// <summary>OamHideAll18 18:81D8: oam_y = $F0 for every sprite (from sprite 1 unless irq_mode is 0 or 3)</summary>
    public static void OamHideAll(Machine m)
    {
        int im = m.Rd(R.IrqMode);
        for (int x = (im == 0 || im == 3) ? 0 : 4; x < 256; x += 4) m.Wr(OAM + x, 0xF0);
    }

    // js: game/states/title.js ntRectBlit
    /// <summary>NtRectBlit 18:81F0: stream ($8E): W H, then H rows of W tiles (+ $06FE) to PPU ($8C/$8D) stride $20</summary>
    public static void NtRectBlit(Machine m)
    {
        PpuNmiOff(m); MaskOff(m);
        int src = m.Rd(0x8E) | m.Rd(0x8F) << 8;
        int w = Prg.CpuRd(m, src), h = Prg.CpuRd(m, src + 1);
        m.Wr(0x72, w); m.Wr(0x73, h);
        src += 2;
        int dst = m.Rd(0x8C) | m.Rd(0x8D) << 8;
        int bias = m.Rd(wait);
        for (int r = 0; r < h; r++)
        {
            m.Ppu.WriteReg(6, dst >> 8); m.Ppu.WriteReg(6, dst & 0xFF);
            dst = (dst + 0x20) & 0xFFFF;
            for (int x = 0; x < w; x++) m.Ppu.WriteReg(7, Prg.CpuRd(m, src + x) + bias);
            src = (src + w) & 0xFFFF;
        }
        m.Wr(0x8E, src & 0xFF); m.Wr(0x8F, src >> 8);
        m.Wr(0x8C, dst & 0xFF); m.Wr(0x8D, dst >> 8);
        m.Wr(0x73, 0);
        MaskOn(m); PpuNmiOn(m);
    }

    // js: game/states/title.js blitFrom
    /// <summary>blit helper: $8E/$8F &lt;- word at `tbl`, $8C/$8D &lt;- ppu, JSR NtRectBlit</summary>
    private static void BlitFrom(Machine m, int tbl, int ppu)
    {
        m.Wr(0x8E, Prg.CpuRd(m, tbl)); m.Wr(0x8F, Prg.CpuRd(m, tbl + 1));
        m.Wr(0x8C, ppu & 0xFF); m.Wr(0x8D, ppu >> 8);
        NtRectBlit(m);
    }

    // js: game/states/title.js palBlack
    /// <summary>MenuPalBlack 18:82B3</summary>
    public static void PalBlack(Machine m) { for (int x = 0; x < 32; x++) m.Wr(R.PalBuf + x, 0x0F); }

    // js: game/states/title.js palLoad
    /// <summary>MenuPalLoad 18:82BE: A b7 = sprite half; MenuPalSets 18:838B[A &amp; 15] -> $06D0/$06E0; then the BG
    /// backdrop ($06D0) into the four sprite-palette backdrop entries $06E0/E4/E8/EC</summary>
    public static void PalLoad(Machine m, int a)
    {
        int x = (a & 0x80) != 0 ? 0x10 : 0;
        int y = (a & 0x0F) << 4;
        for (int i = 0; i < 16; i++) m.Wr(pal_target + x++, Prg.CpuRd(m, 0x838B + y++));
        m.Wr(0x72, 0);
        int bd = m.Rd(pal_target);
        for (int i = 0; i < 4; i++) m.Wr(pal_target + 0x10 + 4 * i, bd);
    }

    // js: game/states/title.js palApply
    /// <summary>MenuPalApply 18:82F1</summary>
    public static void PalApply(Machine m) { for (int x = 0; x < 32; x++) m.Wr(R.PalBuf + x, m.Rd(pal_target + x)); }

    // js: game/states/title.js palFadeIn
    /// <summary>MenuPalFadeIn 18:82FD: pal_buf = target &amp; $0F, state3_mode = 1</summary>
    public static void PalFadeIn(Machine m)
    {
        for (int x = 0; x < 32; x++) m.Wr(R.PalBuf + x, m.Rd(pal_target + x) & 0x0F);
        m.Wr(state3_mode, 1);
    }

    // js: game/states/title.js titleFadeIn
    /// <summary>TitleFadeIn 18:8310 (state3_mode 1)</summary>
    private static IEnumerable<Y>? TitleFadeIn(Machine m)
    {
        if ((m.Rd(ctr) & 7) != 0) return null;
        int n = 0;
        for (int x = 31; x >= 0; x--)
        {
            int a = m.Rd(R.PalBuf + x);
            if (a == m.Rd(pal_target + x)) n++;
            else m.Wr(R.PalBuf + x, a + 0x10);
        }
        m.Wr(match_cnt, n);
        if (n == 0x20) m.Wr(state3_mode, 0);
        return null;
    }

    // js: game/states/title.js fadeOutStart
    /// <summary>TitleFadeOutStart 18:8344</summary>
    private static void FadeOutStart(Machine m) => m.Wr(state3_mode, 2);

    // js: game/states/title.js titleFadeOut
    /// <summary>TitleFadeOut 18:834A (state3_mode 2)</summary>
    private static IEnumerable<Y>? TitleFadeOut(Machine m)
    {
        if ((m.Rd(ctr) & 3) != 0) return null;
        int n = 0;
        for (int x = 31; x >= 0; x--)
        {
            int a = m.Rd(R.PalBuf + x);
            if (a == 0x0F) n++;
            else if ((a & 0xF0) == 0) m.Wr(R.PalBuf + x, 0x0F);
            else m.Wr(R.PalBuf + x, a - 0x10);
        }
        m.Wr(match_cnt, n);
        if (n == 0x20) m.Wr(state3_mode, 0);
        return null;
    }

    // js: game/states/title.js sceneMetaDraw
    /// <summary>SceneMetaDraw 18:80E7: metasprite $72 (SceneMetaPtrs 18:8F3B), frame $73, at x $88 / y $89 into OAM from 4</summary>
    public static void SceneMetaDraw(Machine m)
    {
        m.Wr(0x06C7, 0); m.Wr(0x06C5, 4);
        SceneMetaBody(m);
    }

    // js: game/states/title.js sceneMetaDrawClip
    /// <summary>18:80DF: the clipped variant: $06C7 = $FF (hide records with x outside $06C6..$06C4-1), OAM from $06C5 on</summary>
    public static void SceneMetaDrawClip(Machine m)
    {
        m.Wr(0x06C7, 0xFF);
        SceneMetaBody(m);
    }

    // js: game/states/title.js sceneMetaBody
    /// <summary>18:80F1: the shared body (Y keeps counting across records: 8-bit, as the ROM's INY)</summary>
    private static void SceneMetaBody(Machine m)
    {
        int y0 = (m.Rd(0x72) << 1) & 0xFF;
        m.Wr(0x86, Prg.CpuRd(m, 0x8F3B + y0)); m.Wr(0x87, Prg.CpuRd(m, 0x8F3C + y0));
        m.Wr(0x06C0, Prg.IndY(m, 0x86, 0));
        m.Wr(0x06C1, Prg.IndY(m, 0x86, 1));
        int y = (m.Rd(0x73) * 3 + 2) & 0xFF;
        int lo = Prg.IndY(m, 0x86, y), hi = Prg.IndY(m, 0x86, y + 1);
        m.Wr(0x06C2, Prg.IndY(m, 0x86, y + 2));
        m.Wr(0x87, hi); m.Wr(0x86, lo);
        int x = m.Rd(0x06C5);
        for (int i = 0; ; i = (i + 4) & 0xFF)
        {
            int attr = Prg.IndY(m, 0x86, i);
            if (attr == 0xFF) return;
            m.Wr(OAM + 2 + x, attr);
            m.Wr(OAM + 1 + x, Prg.IndY(m, 0x86, i + 1));
            m.Wr(OAM + 3 + x, Prg.IndY(m, 0x86, i + 2) + m.Rd(0x88));
            m.Wr(OAM + x, Prg.IndY(m, 0x86, i + 3) + m.Rd(0x89));
            if ((m.Rd(0x06C7) & 0x80) != 0)
            {                                            // 8155: clip window $06C6..$06C4
                int ox = m.Rd(OAM + 3 + x);
                if (ox < m.Rd(0x06C6) || ox >= m.Rd(0x06C4)) { m.Wr(OAM + x, 0xF0); m.Wr(0x06C5, x); continue; }
            }
            x = (x + 4) & 0xFF;
            m.Wr(0x06C5, x);
        }
    }

    // js: game/states/title.js titleSound
    /// <summary>19:A07C: queue sound A (snd_queue $0103 = A, count 1) and clear the pending title sound $06B2</summary>
    public static void TitleSound(Machine m, int a)
    {
        m.Wr(0x0103, a);
        m.Wr(0x0101, 1); m.Wr(0x0100, 1);
        m.Wr(0x0101, 0);
        m.Wr(snd, 0xFF);
    }

    // js: game/states/title.js stepWait
    /// <summary>TitleStepWait 18:9FC2: DEC $06FE, on 0 INC title_step</summary>
    private static IEnumerable<Y>? StepWait(Machine m) { if (Dec(m, wait) == 0) Inc(m, title_step); return null; }

    // ---------------------------------------------------------------- main state 2
    // js: game/states/title.js mainState2
    /// <summary>30:C931 + MainState2Init 18:9FCC -> 19:A000</summary>
    public static IEnumerable<Y>? MainState2(Machine m) => MainState2Body(m);

    private static IEnumerable<Y> MainState2Body(Machine m)
    {
        Prg.SwitchBank16(m, 9);
        PalBlack(m);
        OamHideAll(m);
        foreach (var y in WaitFrameReady(m)) yield return y;
        m.Wr(R.IrqMode, 3);
        for (int x = 0x28; x >= 0; x--) { m.Wr(0x70 + x, 0); m.Wr(0x0600 + x, 0); }
        for (int x = 0x80; x < 0x100; x++) m.Wr(0x0600 + x, 0);
        foreach (int a in new[] { title_mode, title_step, ram_06F2, state3_mode }) m.Wr(a, 0);
        m.Wr(R.FrameReady, 0);
        m.Wr(R.ScrollShadow0, 0); m.Wr(R.CamYLo, 0); m.Wr(R.CamXHi, 0); m.Wr(R.CamYHi, 0);  // 9FFD-A003
        TitleSound(m, 0);                                  // A005 (A = 0)
        m.Wr(snd, 0xFF);
        Inc(m, R.MainState);                               // A00D -> 3
        TitleSound(m, 0);
    }

    // ---------------------------------------------------------------- main state 3
    // js: game/states/title.js mainState3
    /// <summary>30:C939 + MainState3 19:A015</summary>
    public static IEnumerable<Y>? MainState3(Machine m)
    {
        Prg.SwitchBank16(m, 9);
        Pads.ReadPadsMenu(m);
        int s = m.Rd(snd);
        if ((s & 0x80) == 0) TitleSound(m, s);
        m.Wr(R.FrameReady, 0);
        Dec(m, ctr);
        int mode = m.Rd(state3_mode);
        SetJmp(m, 0xA038 + mode * 2);
        if (mode == 0) return SubDispatch(m);
        if (mode == 1) return TitleFadeIn(m);
        if (mode == 2) return TitleFadeOut(m);
        throw new InvalidOperationException($"state3_mode {mode} out of range");
    }

    // js: game/states/title.js setJmp
    /// <summary>the ROM's JMP ($06FC) dispatch: store the table word in $06FC/$06FD (kept for RAM fidelity), return it</summary>
    private static int SetJmp(Machine m, int tblAddr)
    {
        m.Wr(jmp_lo, Prg.CpuRd(m, tblAddr)); m.Wr(jmp_hi, Prg.CpuRd(m, tblAddr + 1));
        return Ptr16(m, tblAddr);
    }

    // js: game/states/title.js unported
    private static IEnumerable<Y>? Unported(Machine m, int addr)
    {
        string k = $"19:{addr:X}";
        if (m.Unported.Add(k)) m.Log($"title routine {k} not ported yet");
        return null;
    }

    // js: game/states/title.js subDispatch
    /// <summary>State3SubDispatch 19:A03E: State3SubTable 19:A052[title_mode]</summary>
    private static IEnumerable<Y>? SubDispatch(Machine m)
    {
        int a = SetJmp(m, 0xA052 + m.Rd(title_mode) * 2);
        return Modes.TryGetValue(a, out var fn) ? fn(m) : Unported(m, a);
    }

    // js: game/states/title.js stepDispatch
    /// <summary>step-table dispatch (TitleOpening JmpTable19_A7BC, TitleMenu JmpTable19_A6F3)</summary>
    private static IEnumerable<Y>? StepDispatch(Machine m, int tbl)
    {
        int a = SetJmp(m, tbl + m.Rd(title_step) * 2);
        return Steps.TryGetValue(a, out var fn) ? fn(m) : Unported(m, a);
    }

    // ---------------------------------------------------------------- title mode 0: TitleOpening 19:A79F
    // js: game/states/title.js titleOpening
    private static IEnumerable<Y>? TitleOpening(Machine m)
    {
        if ((m.Rd(R.PadNew) & 0x30) != 0)
        {                                                  // START/SELECT -> 19:A7EE
            TitleSound(m, 0);
            return TitleOpeningEnd(m);
        }
        return StepDispatch(m, 0xA7BC);
    }

    // js: game/states/title.js titleOpeningEnd
    /// <summary>TitleOpeningEnd 19:ABAB</summary>
    private static IEnumerable<Y>? TitleOpeningEnd(Machine m) { FadeOutStart(m); m.Wr(title_mode, 5); return null; }

    // js: game/states/title.js stepFadeOut
    /// <summary>19:A064 TitleStepFadeOut</summary>
    private static IEnumerable<Y>? StepFadeOut(Machine m) { FadeOutStart(m); m.Wr(wait, 0x1E); Inc(m, title_step); return null; }
    // js: game/states/title.js stepBlack
    /// <summary>19:A070</summary>
    private static IEnumerable<Y>? StepBlack(Machine m) { PalBlack(m); m.Wr(wait, 0x3C); Inc(m, title_step); return null; }

    // js: game/states/title.js openingDateCard
    /// <summary>OpeningDateCard 19:A7F6 (step 2)</summary>
    private static IEnumerable<Y>? OpeningDateCard(Machine m)
    {
        PalBlack(m); OamHideAll(m);
        m.Wr(R.ChrR0Play, 0x3C); m.Wr(R.ChrR0Play + 1, 0x34);
        NtFill(m, 0x20, 0x10, 0xFF);
        m.Wr(wait, 0); m.Wr(R.CamYLo, 0); m.Wr(R.CamYHi, 0);
        BlitFrom(m, 0xB162, 0x21A0);
        PalLoad(m, 0); PalFadeIn(m);
        m.Wr(wait, 0x78); m.Wr(snd, 0x0A);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js openingPage1
    /// <summary>OpeningPage1 19:A843 (step 6)</summary>
    private static IEnumerable<Y>? OpeningPage1(Machine m)
    {
        PalBlack(m);
        for (int r = 2; r < 6; r++) m.Wr(R.ChrR0Play + r, 0x7C);
        m.Wr(R.ChrR0Play + 1, 0x16);
        NtFill(m, 0x20, 0x10, 0xFF);
        m.Wr(wait, 0);
        BlitFrom(m, 0x847B, 0x2042);
        BlitFrom(m, 0x8C37, 0x23C0);
        BlitFrom(m, 0xB1C0, 0x2240);
        PalLoad(m, 0); PalLoad(m, 0x80); PalFadeIn(m);
        foreach (int a in new[] { 0x0600, 0x0618, 0x0620, 0x0628, 0x06B3 }) m.Wr(a, 0);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js openingPage1Anim
    /// <summary>OpeningPage1Anim 19:A8D2 (step 7): frames $0620 of metasprite $0618, frame time $06C2; tables 19:A8C8/A8CD</summary>
    private static IEnumerable<Y>? OpeningPage1Anim(Machine m)
    {
        OamHideAll(m);
        m.Wr(0x72, m.Rd(0x0618)); m.Wr(0x73, m.Rd(0x0620));
        m.Wr(0x88, 0x80); m.Wr(0x89, 0x10);
        SceneMetaDraw(m);
        if (m.Rd(0x0600) == 0)
        {
            m.Wr(0x0628, m.Rd(0x06C2));
            Inc(m, 0x0600);
            return null;
        }
        if ((Dec(m, 0x0628) & 0x80) == 0) return null;
        m.Wr(0x0600, 0);
        int y = m.Rd(0x06B3);
        if (Prg.CpuRd(m, 0xA8C8 + y) == m.Rd(0x0620))
        {
            Inc(m, 0x06B3);
            m.Wr(snd, Prg.CpuRd(m, 0xA8CD + y));
        }
        Inc(m, 0x0620);
        if (m.Rd(0x0620) == m.Rd(0x06C0))
        {
            m.Wr(wait, 0x78);
            Inc(m, title_step);
        }
        return null;
    }

    // js: game/states/title.js openingPage1Flash
    /// <summary>OpeningPage1Flash 19:A92D (step 8)</summary>
    private static IEnumerable<Y>? OpeningPage1Flash(Machine m)
    {
        int a = (m.Rd(ctr) >> 1) & 1;
        PalLoad(m, a); PalLoad(m, a + 0x80); PalApply(m);
        if (Dec(m, wait) != 0) return null;
        PalLoad(m, 0); PalApply(m);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js openingPage2
    /// <summary>OpeningPage2 19:A953 (step 11): two pages, $2000 and $2800</summary>
    private static IEnumerable<Y> OpeningPage2(Machine m)
    {
        PalBlack(m); OamHideAll(m);
        m.Wr(R.ChrR0Play + 1, 0x10);
        NtFill(m, 0x20, 0x10, 0xFF);
        m.Wr(wait, 0x80);
        BlitFrom(m, 0x8607, 0x2042);
        m.Wr(wait, 0);
        BlitFrom(m, 0x8C37, 0x23C0);
        BlitFrom(m, 0xB25A, 0x2240);
        foreach (var y in WaitFrameReady(m)) yield return y;   // A9B0
        NtFill(m, 0x28, 0x10, 0xFF);
        m.Wr(wait, 0x80);
        BlitFrom(m, 0x8793, 0x2842);
        m.Wr(wait, 0);
        BlitFrom(m, 0x8C37, 0x2BC0);
        BlitFrom(m, 0xB25A, 0x2A40);
        PalLoad(m, 2); PalFadeIn(m);
        m.Wr(wait, 0xF0);
        Inc(m, title_step);
    }

    // js: game/states/title.js openingPage2Flip
    /// <summary>OpeningPage2Flip 19:AA16 (steps 12, 13)</summary>
    private static IEnumerable<Y>? OpeningPage2Flip(Machine m)
    {
        m.Wr(R.CamYHi, (m.Rd(ctr) >> 4) & 1);             // ROR x4 AND 1 (carry rotates in at b7: no effect on b0)
        if (Dec(m, wait) != 0) return null;
        m.Wr(wait, 0x3C);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js openingPage3
    /// <summary>OpeningPage3 19:AA30 (step 16). Lag frame: the NMI fires when the second blit's PpuNmiOn (18:817C)
    /// re-enables NMI inside vblank (tools.emu trace s82); the `yield` after that blit is that NMI.</summary>
    private static IEnumerable<Y> OpeningPage3(Machine m)
    {
        PalBlack(m);
        m.Wr(R.ChrR0Play + 3, 0x11);
        m.Wr(0x72, 1); m.Wr(0x73, 0);
        m.Wr(0x0610, 0x28); m.Wr(0x88, 0x28);
        m.Wr(0x0608, 0x20); m.Wr(0x89, 0x20);
        SceneMetaDraw(m);
        NtFill(m, 0x20, 0x10, 0xFF);
        m.Wr(R.CamYHi, 0);
        m.Wr(wait, 0x80);
        BlitFrom(m, 0x891F, 0x2042);
        m.Wr(wait, 0);
        BlitFrom(m, 0x8C37, 0x23C0);
        yield return Y.Frame;                              // NMI (lag frame, see above)
        BlitFrom(m, 0xB337, 0x2240);
        PalLoad(m, 3); PalLoad(m, 0x88); PalFadeIn(m);
        m.Wr(0x0628, 2);
        m.Wr(wait, 0xF0);
        Inc(m, title_step);
    }

    // js: game/states/title.js openingPage3Anim
    /// <summary>OpeningPage3Anim 19:AAC2 (step 17)</summary>
    private static IEnumerable<Y>? OpeningPage3Anim(Machine m)
    {
        if ((m.Rd(ctr) & 3) == 0) Inc(m, 0x0610);
        m.Wr(0x72, 1); m.Wr(0x73, 0);
        m.Wr(0x88, m.Rd(0x0610)); m.Wr(0x89, m.Rd(0x0608));
        SceneMetaDraw(m);
        if (Dec(m, wait) != 0) return null;
        if (Dec(m, 0x0628) != 0) return null;
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js openingPage4
    /// <summary>OpeningPage4 19:AAF1 (step 20)</summary>
    private static IEnumerable<Y>? OpeningPage4(Machine m)
    {
        PalBlack(m);
        for (int r = 2; r < 6; r++) m.Wr(R.ChrR0Play + r, 0x7D);
        OamHideAll(m);
        m.Wr(0x72, 2); m.Wr(0x73, 0);
        m.Wr(0x0610, 0x70); m.Wr(0x88, 0x70);
        m.Wr(0x0608, 0x38); m.Wr(0x89, 0x38);
        SceneMetaDraw(m);
        NtFill(m, 0x20, 0x10, 0xFF);
        m.Wr(wait, 0x80);
        BlitFrom(m, 0x8AAB, 0x2042);
        m.Wr(wait, 0);
        BlitFrom(m, 0x8C5B, 0x23C0);
        BlitFrom(m, 0xB40D, 0x2240);
        PalLoad(m, 4); PalFadeIn(m);
        m.Wr(wait, 0x28);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js openingPage4Anim
    /// <summary>OpeningPage4Anim 19:AB81 (step 21)</summary>
    private static IEnumerable<Y>? OpeningPage4Anim(Machine m)
    {
        if ((m.Rd(ctr) & 3) != 0) return null;
        Dec(m, 0x0608);
        m.Wr(0x72, 2); m.Wr(0x73, 0);
        m.Wr(0x88, m.Rd(0x0610)); m.Wr(0x89, m.Rd(0x0608));
        SceneMetaDraw(m);
        if (Dec(m, wait) != 0) return null;
        Inc(m, title_step);
        return null;
    }

    // ---------------------------------------------------------------- title modes 5 / 6: the title menu
    // js: game/states/title.js titleMenuInit
    /// <summary>TitleMenuInit 19:A653 (mode 5), falls into TitleMenu</summary>
    private static IEnumerable<Y> TitleMenuInit(Machine m)
    {
        PalBlack(m);
        foreach (var y in WaitFrameReady(m)) yield return y;
        NtFill(m, 0x28, 0xFF, 0x00);
        m.Wr(R.ChrR0Play, 0x1C); m.Wr(R.ChrR0Play + 1, 0x1E);
        m.Wr(R.ChrR0Play + 3, 0x73); m.Wr(R.ChrR0Play + 5, 0x7B);
        foreach (int a in new[] { title_step, ram_06F2, state3_mode, R.ScrollShadow0, R.CamYLo, R.CamXHi, R.CamYHi }) m.Wr(a, 0);
        m.Wr(R.CamYLo, 8);
        OamHideAll(m);
        foreach (var y in WaitFrameReady(m)) yield return y;
        m.Wr(wait, 0);
        BlitFrom(m, 0x94BE, 0x2000);
        PalLoad(m, 0x0C); PalLoad(m, 0x8E); PalFadeIn(m);
        m.Wr(0x82, Prg.CpuRd(m, 0xA6DD)); m.Wr(0x83, Prg.CpuRd(m, 0xA6DE));
        m.Wr(0x84, 0x0E);
        m.Wr(0x06B4, 0x96); m.Wr(0x06B5, 2);
        m.Wr(snd, 0x0B);
        Inc(m, title_mode);
        var r = TitleMenu(m);                              // A6DA JMP TitleMenu
        if (r != null) foreach (var y in r) yield return y;
    }

    // js: game/states/title.js titleMenu
    /// <summary>TitleMenu 19:A6DF (mode 6): JmpTable19_A6F3[title_step]</summary>
    private static IEnumerable<Y>? TitleMenu(Machine m) => StepDispatch(m, 0xA6F3);

    // js: game/states/title.js titleMenuSprites
    /// <summary>title-menu step 0 19:A6FE: $84 OAM records (index, y, tile, attr, x) from ($82); cursor sprite $02F0</summary>
    private static IEnumerable<Y>? TitleMenuSprites(Machine m)
    {
        do
        {
            int x = Prg.IndY(m, 0x82, 0);
            m.Wr(OAM + x, Prg.IndY(m, 0x82, 1));
            m.Wr(OAM + 1 + x, Prg.IndY(m, 0x82, 2));
            m.Wr(OAM + 2 + x, Prg.IndY(m, 0x82, 3));
            m.Wr(OAM + 3 + x, Prg.IndY(m, 0x82, 4));
            int p = (m.Rd(0x82) | m.Rd(0x83) << 8) + 5;
            m.Wr(0x82, p & 0xFF); m.Wr(0x83, (p >> 8) & 0xFF);
        } while (Dec(m, 0x84) != 0);
        Inc(m, title_step);
        m.Wr(0x82, 0);
        m.Wr(0x02F0, 0xAF); m.Wr(0x02F1, 0x7E); m.Wr(0x02F2, 0); m.Wr(0x02F3, Prg.CpuRd(m, 0xA77B));
        return null;
    }

    // js: game/states/title.js titleMenuInput
    /// <summary>TitleMenuInput 19:A749 (step 1): START -> 19:A77D, SELECT toggles title_menu_sel $82, idle timer $06B4/$06B5</summary>
    private static IEnumerable<Y>? TitleMenuInput(Machine m)
    {
        if ((m.Rd(R.PadNew) & 0x10) != 0)
        {                                                  // TitleMenuStart 19:A77D
            TitleSound(m, 0);
            m.Wr(snd, 9);
            if (m.Rd(0x82) != 0) { m.Wr(R.MainState, 8); return null; }
            m.Wr(title_mode, 1);
            return null;
        }
        if ((m.Rd(R.PadNew) & 0x20) != 0)
        {
            m.Wr(snd, 0x4B);
            int s = m.Rd(0x82) ^ 1;
            m.Wr(0x82, s);
            m.Wr(0x02F3, Prg.CpuRd(m, 0xA77B + s));
            return null;
        }
        if (Dec(m, 0x06B4) != 0) return null;
        if (Dec(m, 0x06B5) != 0) return null;
        FadeOutStart(m);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js titleMenuRestart
    /// <summary>TitleMenuRestart 19:A796 (step 2): DEC main_state (-> 2, MainState2Init again)</summary>
    private static IEnumerable<Y> TitleMenuRestart(Machine m)
    {
        m.Wr(R.MainState, m.Rd(R.MainState) - 1);
        PalBlack(m);
        foreach (var y in WaitFrameReady(m)) yield return y;
    }

    // ---------------------------------------------------------------- shared helpers of modes 2-4 (s107)
    // js: game/states/title.js ppuQueueDrainWait
    /// <summary>PpuQueueDrainWait 19:A092: spin until the NMI has drained the PPU queue ($0A = $0B), then reset both</summary>
    private static IEnumerable<Y> PpuQueueDrainWait(Machine m)
    {
        while (m.Rd(0x0A) != m.Rd(0x0B)) yield return Y.Spin;
        PpuNmiOff(m);
        m.Wr(0x0A, 0); m.Wr(0x0B, 0);
        PpuNmiOn(m);
    }

    // js: game/states/title.js queuePair
    /// <summary>two $6200 records at $0B (hi, lo, len, src each) under PpuNmiOff18 / PpuNmiOn18, $0B += 8</summary>
    private static void QueuePair(Machine m, int hi1, int lo1, int hi2, int lo2, int len, int src)
    {
        PpuNmiOff(m);
        int x = m.Rd(0x0B);
        m.Wr(0x6200 + x, hi1); m.Wr(0x6204 + x, hi2);
        m.Wr(0x6201 + x, lo1); m.Wr(0x6205 + x, lo2);
        m.Wr(0x6202 + x, len); m.Wr(0x6206 + x, len);
        m.Wr(0x6203 + x, src); m.Wr(0x6207 + x, src);
        m.Wr(0x0B, (x + 8) & 0x1F);
    }

    // js: game/states/title.js slotAnim
    /// <summary>the cut-scene slot animation (19:AD85 / AF3B / A1D8): phase $0600,X = 0 -> timer $0628,X = $06C2,
    /// phase 1; else DEC timer, on &lt; 0 phase 0 + next frame $0620,X (wrap to $06C1 at $06C0)</summary>
    private static void SlotAnim(Machine m, int x)
    {
        if (m.Rd(0x0600 + x) == 0)
        {
            m.Wr(0x0628 + x, m.Rd(0x06C2));
            Inc(m, 0x0600 + x);
            return;
        }
        if ((Dec(m, 0x0628 + x) & 0x80) == 0) return;
        m.Wr(0x0600 + x, 0);
        Inc(m, 0x0620 + x);
        if (m.Rd(0x0620 + x) == m.Rd(0x06C0)) m.Wr(0x0620 + x, m.Rd(0x06C1));
    }

    // js: game/states/title.js slotDraw
    /// <summary>$72/$73/$88/$89 &lt;- slot X ($0618 id, $0620 frame, $0610 x, $0608 y), then 18:80DF</summary>
    private static void SlotDraw(Machine m, int x)
    {
        m.Wr(0x72, m.Rd(0x0618 + x)); m.Wr(0x73, m.Rd(0x0620 + x));
        m.Wr(0x88, m.Rd(0x0610 + x)); m.Wr(0x89, m.Rd(0x0608 + x));
        SceneMetaDrawClip(m);
    }

    // js: game/states/title.js slotInit
    /// <summary>slot tables: $0618/$0610/$0608,X &lt;- tbls[0..2][X], $0600/$0620/$0628,X &lt;- 0 (or extra tables)</summary>
    private static void SlotInit(Machine m, int n, int ids, int xs, int ys, int frames = 0, int timers = 0)
    {
        for (int x = 0; x < n; x++)
        {
            m.Wr(0x0618 + x, Prg.CpuRd(m, ids + x)); m.Wr(0x0610 + x, Prg.CpuRd(m, xs + x)); m.Wr(0x0608 + x, Prg.CpuRd(m, ys + x));
            m.Wr(0x0620 + x, frames != 0 ? Prg.CpuRd(m, frames + x) : 0);
            m.Wr(0x0628 + x, timers != 0 ? Prg.CpuRd(m, timers + x) : 0);
            m.Wr(0x0600 + x, 0);
        }
    }

    // js: game/states/title.js camZero
    private static void CamZero(Machine m)
    {
        foreach (int a in new[] { title_step, ram_06F2, state3_mode, R.FrameReady, R.ScrollShadow0, R.CamYLo, R.CamXHi, R.CamYHi }) m.Wr(a, 0);
    }

    // ---------------------------------------------------------------- title mode 1: TitleStoryInit 19:ABB4
    // js: game/states/title.js titleStoryInit
    private static IEnumerable<Y> TitleStoryInit(Machine m)
    {
        PalBlack(m);
        foreach (var y in WaitFrameReady(m)) yield return y;
        m.Wr(R.IrqMode, 3);
        m.Wr(title_mode, 2);
        CamZero(m);
        for (int r = 2; r < 6; r++) m.Wr(R.ChrR0Play + r, 0x7E);
        OamHideAll(m);
    }

    // ---------------------------------------------------------------- title mode 2: TitleIntroStory 19:ABEA (28 steps)
    // js: game/states/title.js titleIntroStory
    private static IEnumerable<Y>? TitleIntroStory(Machine m)
    {
        if ((m.Rd(R.PadNew) & 0x30) != 0) return TitleStoryEnd(m);
        return StepDispatch(m, 0xAC07);
    }

    // js: game/states/title.js titleStoryEnd
    /// <summary>TitleStoryEnd 19:B15C</summary>
    private static IEnumerable<Y>? TitleStoryEnd(Machine m) { m.Wr(title_mode, 3); return null; }

    // js: game/states/title.js storyScrollPage
    /// <summary>19:AC3F (step 0): the scroll page, NT $2000 + $2800. Lag frame: the NMI fires at the 2nd blit's
    /// PpuNmiOn18 (tools.emu trace s107), the rest of the step runs in the next frame.</summary>
    private static IEnumerable<Y> StoryScrollPage(Machine m)
    {
        m.Wr(R.ChrR0Play, 0x3C); m.Wr(R.ChrR0Play + 1, 0x12);
        m.Wr(R.ChrR0Play + 2, 0x7C); m.Wr(R.ChrR0Play + 4, 0x7C);
        m.Wr(R.ChrR0Play + 3, 0x7D); m.Wr(R.ChrR0Play + 5, 0x7D);
        m.Wr(R.CamYHi, 1); m.Wr(R.CamYLo, 0xE8);
        NtFill(m, 0x20, 0x80, 0x00);
        NtFill(m, 0x28, 0x80, 0x00);
        m.Wr(wait, 0x80);
        BlitFrom(m, 0x8C7F, 0x2814);
        m.Wr(wait, 0);
        BlitFrom(m, 0x8EAF, 0x2BC0);
        yield return Y.Frame;                              // NMI (lag frame, see above)
        PalLoad(m, 5); PalLoad(m, 0x89); PalApply(m);
        Inc(m, title_step);
        for (int x = 5; x >= 0; x--)
        {
            m.Wr(0x0618 + x, Prg.CpuRd(m, 0xACDA + x)); m.Wr(0x0610 + x, Prg.CpuRd(m, 0xACDF + x)); m.Wr(0x0608 + x, Prg.CpuRd(m, 0xACE4 + x));
            m.Wr(0x0600 + x, 0); m.Wr(0x0620 + x, 0); m.Wr(0x0628 + x, 0);
        }
    }

    // js: game/states/title.js storyScrollUp
    /// <summary>19:ACE9 (step 1): cam_y_lo -= 8 until $68</summary>
    private static IEnumerable<Y>? StoryScrollUp(Machine m)
    {
        int v = (m.Rd(R.CamYLo) - 8) & 0xFF;
        m.Wr(R.CamYLo, v);
        if (v == 0x68) Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js storyScrollSprite
    /// <summary>19:ACF9 (step 2): slot 0 drawn, its y += 8 while cam_y_lo -= 8 down to 0</summary>
    private static IEnumerable<Y>? StoryScrollSprite(Machine m)
    {
        m.Wr(0x72, m.Rd(0x0618)); m.Wr(0x73, m.Rd(0x0620));
        m.Wr(0x88, m.Rd(0x0610)); m.Wr(0x89, m.Rd(0x0608));
        SceneMetaDraw(m);
        m.Wr(0x0608, m.Rd(0x0608) + 8);
        int v = (m.Rd(R.CamYLo) - 8) & 0xFF;
        m.Wr(R.CamYLo, v);
        if (v == 0) Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js storyBirdsInit
    /// <summary>19:AD27 (step 3): clip window $98..$EF, slot 0 y -= 8, $0609 = $68</summary>
    private static IEnumerable<Y>? StoryBirdsInit(Machine m)
    {
        m.Wr(0x06C6, 0x98); m.Wr(0x06C4, 0xF0);
        m.Wr(wait, 0);
        m.Wr(0x0608, m.Rd(0x0608) - 8);
        m.Wr(0x0609, 0x68);
        m.Wr(0x72, m.Rd(0x0618)); m.Wr(0x73, 0);
        m.Wr(0x88, m.Rd(0x0610)); m.Wr(0x89, m.Rd(0x0608));
        SceneMetaDraw(m);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js storySlots
    /// <summary>19:AD5E (step 4) / 19:AF14 (step 12): n slots drawn clipped + animated, moving every 2nd frame by
    /// dx/dy tables; a slot that leaves the box restarts from its init x/y</summary>
    private static void StorySlots(Machine m, int n, int dxT, int dyT, int x0T, int y0T, Func<int, int, bool> @out)
    {
        OamHideAll(m);
        m.Wr(0x06C5, 4);
        for (int x = 0; x < n; x++)
        {
            m.Wr(0x06C3, x);
            SlotDraw(m, x);
            SlotAnim(m, x);
            if ((m.Rd(ctr) & 1) != 0)
            {
                int nx = (m.Rd(0x0610 + x) - Prg.CpuRd(m, dxT + x)) & 0xFF;
                m.Wr(0x0610 + x, nx);
                int ny = (m.Rd(0x0608 + x) - Prg.CpuRd(m, dyT + x)) & 0xFF;
                m.Wr(0x0608 + x, ny);
                if (@out(nx, ny)) { m.Wr(0x0610 + x, Prg.CpuRd(m, x0T + x)); m.Wr(0x0608 + x, Prg.CpuRd(m, y0T + x)); }
            }
        }
        m.Wr(0x06C3, n);
    }

    // js: game/states/title.js storyBirds
    private static IEnumerable<Y>? StoryBirds(Machine m)
    {
        StorySlots(m, 5, 0xADF5, 0xADFA, 0xACDF, 0xACE4, (nx, _) => nx < 0x70);
        if (Dec(m, wait) == 0) Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js storyFadeOut
    /// <summary>19:ADFF / 19:AFCB: TitleFadeOutStart, next step</summary>
    private static IEnumerable<Y>? StoryFadeOut(Machine m) { FadeOutStart(m); Inc(m, title_step); return null; }

    // js: game/states/title.js storyTextPage
    /// <summary>19:AE06 (step 6) / 19:AFD2 (step 14): a text page on NT $2800 at $29A0, BG set 0 fade-in</summary>
    private static IEnumerable<Y>? StoryTextPage(Machine m, int ptr)
    {
        OamHideAll(m);
        NtFill(m, 0x28, 0x10, 0xFF);
        m.Wr(wait, 0);
        BlitFrom(m, ptr, 0x29A0);
        PalLoad(m, 0); PalFadeIn(m);
        m.Wr(wait, 0);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js storyPage2
    /// <summary>19:AE3D (step 10): the second scroll page (8 slots, tables 19:AEC2-AEE2)</summary>
    private static IEnumerable<Y>? StoryPage2(Machine m)
    {
        NtFill(m, 0x20, 0x80, 0x00);
        NtFill(m, 0x28, 0x80, 0x00);
        m.Wr(R.CamYHi, 0); m.Wr(R.CamYLo, 0);
        m.Wr(wait, 0x80);
        BlitFrom(m, 0x8D73, 0x28C2);
        m.Wr(wait, 0);
        BlitFrom(m, 0x8EAF, 0x2BC0);
        PalLoad(m, 7); PalLoad(m, 0x89); PalApply(m);
        Inc(m, title_step);
        for (int x = 7; x >= 0; x--)
        {
            m.Wr(0x0618 + x, Prg.CpuRd(m, 0xAEC2 + x)); m.Wr(0x0610 + x, Prg.CpuRd(m, 0xAECA + x));
            m.Wr(0x0608 + x, Prg.CpuRd(m, 0xAED2 + x)); m.Wr(0x0620 + x, Prg.CpuRd(m, 0xAEDA + x));
            m.Wr(0x0628 + x, Prg.CpuRd(m, 0xAEE2 + x)); m.Wr(0x0600 + x, 0);
        }
        return null;
    }

    // js: game/states/title.js storyScrollDown
    /// <summary>19:AEEA (step 11): cam_y_lo += 8 to $F0, then the next NT; clip window $08..$5F, timer $06CC/$06CD = $80/2</summary>
    private static IEnumerable<Y>? StoryScrollDown(Machine m)
    {
        int v = (m.Rd(R.CamYLo) + 8) & 0xFF;
        m.Wr(R.CamYLo, v);
        if (v != 0xF0) return null;
        m.Wr(R.CamYLo, 0);
        Inc(m, R.CamYHi);
        m.Wr(0x06C6, 0x08); m.Wr(0x06C4, 0x60);
        m.Wr(0x06CC, 0x80); m.Wr(0x06CD, 2);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js storyPage2Anim
    /// <summary>19:AF14 (step 12)</summary>
    private static IEnumerable<Y>? StoryPage2Anim(Machine m)
    {
        StorySlots(m, 8, 0xAFBB, 0xAFC3, 0xAECA, 0xAED2, (nx, ny) => nx < 0x08 || ny < 0x20);
        if (Dec(m, 0x06CC) != 0) return null;
        if (Dec(m, 0x06CD) != 0) return null;
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js storyPage3
    /// <summary>19:B009 (step 18): picture page on NT $2000 with one animated metasprite (id 7)</summary>
    private static IEnumerable<Y>? StoryPage3(Machine m)
    {
        OamHideAll(m);
        NtFill(m, 0x20, 0x10, 0xFF);
        m.Wr(R.ChrR0Play + 1, 0x16);
        m.Wr(wait, 0); m.Wr(R.CamYLo, 0); m.Wr(R.CamYHi, 0);
        BlitFrom(m, 0x8E67, 0x20CC);
        BlitFrom(m, 0x8EF3, 0x23C0);
        BlitFrom(m, 0xB5DB, 0x21C0);
        PalLoad(m, 6); PalLoad(m, 0x86); PalFadeIn(m);
        m.Wr(0x06C8, 2); m.Wr(0x06C9, 0xA0); m.Wr(0x06CA, 8);
        m.Wr(0x0618, 7);
        m.Wr(0x0600, 0); m.Wr(0x0620, 0); m.Wr(0x0628, 0);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js storyPage3Anim
    /// <summary>19:B092 (step 19): the metasprite at (60, 30); its frame time $06CA shrinks by 2 per loop; $06C9/$06C8 timer</summary>
    private static IEnumerable<Y>? StoryPage3Anim(Machine m)
    {
        OamHideAll(m);
        m.Wr(0x72, m.Rd(0x0618)); m.Wr(0x73, m.Rd(0x0620));
        m.Wr(0x88, 0x60); m.Wr(0x89, 0x30);
        SceneMetaDraw(m);
        if (m.Rd(0x0600) == 0)
        {
            m.Wr(0x0628, m.Rd(0x06CA));
            Inc(m, 0x0600);
        }
        else if ((Dec(m, 0x0628) & 0x80) != 0)
        {
            m.Wr(0x0600, 0);
            Inc(m, 0x0620);
            if (m.Rd(0x0620) == m.Rd(0x06C0))
            {
                m.Wr(0x0620, 0); m.Wr(0x0628, 0);
                Dec(m, 0x06CA);
                if ((Dec(m, 0x06CA) & 0x80) != 0) m.Wr(0x06CA, 0);
            }
        }
        if (Dec(m, 0x06C9) != 0) return null;
        if (Dec(m, 0x06C8) != 0) return null;
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js storyLastPage
    /// <summary>19:B0FB (step 22): the last page, NT $2000, wait $B4</summary>
    private static IEnumerable<Y>? StoryLastPage(Machine m)
    {
        OamHideAll(m);
        NtFill(m, 0x20, 0x10, 0xFF);
        m.Wr(wait, 0);
        BlitFrom(m, 0x8E8B, 0x20CC);
        BlitFrom(m, 0x8F17, 0x23C0);
        BlitFrom(m, 0xB6ED, 0x2200);
        PalLoad(m, 6); PalFadeIn(m);
        m.Wr(wait, 0xB4);
        Inc(m, title_step);
        return null;
    }

    // ---------------------------------------------------------------- title mode 3: TitleStatusInit 19:A0B5
    // js: game/states/title.js titleStatusInit
    private static IEnumerable<Y> TitleStatusInit(Machine m)
    {
        m.Wr(snd, 0);
        PalBlack(m); OamHideAll(m);
        foreach (var y in WaitFrameReady(m)) yield return y;
        m.Wr(R.CamYLo, 0); m.Wr(R.CamYHi, 0);
        m.Wr(R.ChrR0Play, 0x3C); m.Wr(R.ChrR0Play + 1, 0x36);
        m.Wr(R.ChrR0Play + 2, 0xC7); m.Wr(R.ChrR0Play + 3, 0xC7);
        PalLoad(m, 9); PalLoad(m, 0x8B); PalApply(m);
        PalLoad(m, 0x0D);
        m.Wr(0x06DD, 0x0F);
        m.Wr(wait, 0);
        BlitFrom(m, 0x99D0, 0x2000);
        BlitFrom(m, 0x9DD4, 0x2203);
        // Lost NMI (tools.emu trace s107): the blits hold NMI off through f1702's vblank; PpuNmiOn18 comes at f1703
        // line 27, so f1702 has no NMI at all; the rest runs in f1703 before its NMI.
        yield return Y.NoNmi;
        m.Wr(title_step, 0);
        Inc(m, title_mode);
        m.Wr(0x0617, 0x0D); m.Wr(0x0627, 0);
        SlotInit(m, 4, 0xA16A, 0xA16E, 0xA172);
        m.Wr(0x06B4, 0x78); m.Wr(0x06BB, 0x78);
        m.Wr(0x8B, 0x10); m.Wr(0x8A, 3);
    }

    // ---------------------------------------------------------------- title mode 4: TitleStatusScreen 19:A176
    // js: game/states/title.js titleStatusScreen
    private static IEnumerable<Y>? TitleStatusScreen(Machine m)
    {
        OamHideAll(m);
        StatusAnimTick(m);
        StatusSlotAnim(m);
        return StepDispatch(m, 0xA193);
    }

    // js: game/states/title.js statusAnimTick
    /// <summary>StatusAnimTick 19:A212: a 7-sprite strip at OAM $58 (y $58, x $18 + 8i) from 19:9908[frame * 8]; every
    /// 8th pass status_anim_ctr $0617 -= 1 and the next frame (byte 7 of the record = jump target unless $FF)</summary>
    private static void StatusAnimTick(Machine m)
    {
        if (m.Rd(0x06BB) == 0) return;
        int y = (m.Rd(0x0627) << 3) & 0xFF;
        m.Wr(0x061F, 0x18);
        for (int i = 0, x = 0x58; i < 7; i++, x += 4, y++)
        {
            m.Wr(OAM + x, 0x58);
            m.Wr(OAM + 1 + x, Prg.CpuRd(m, 0x9908 + y));
            m.Wr(OAM + 2 + x, 0);
            m.Wr(OAM + 3 + x, m.Rd(0x061F));
            m.Wr(0x061F, m.Rd(0x061F) + 8);
        }
        m.Wr(0x062F, 0);
        if ((m.Rd(ctr) & 7) != 0) return;
        Dec(m, 0x0617);
        Inc(m, 0x0627);
        int a = Prg.CpuRd(m, 0x9908 + y);
        if (a != 0xFF) m.Wr(0x0627, a);
    }

    // js: game/states/title.js statusSlotAnim
    /// <summary>StatusSlotAnim 19:A1AA: 4 slots, OAM from $80, no clipping window ($00..$FE)</summary>
    private static void StatusSlotAnim(Machine m)
    {
        m.Wr(0x06C5, 0x80); m.Wr(0x06C6, 0); m.Wr(0x06C4, 0xFF);
        for (int x = 0; x < 4; x++)
        {
            m.Wr(0x06C8, x);
            SlotDraw(m, x);
            SlotAnim(m, x);
        }
        m.Wr(0x06C8, 4);
    }

    // js: game/states/title.js statusSound
    /// <summary>status step 0 19:A1A1: sound $57</summary>
    private static IEnumerable<Y>? StatusSound(Machine m) { m.Wr(snd, 0x57); Inc(m, title_step); return null; }

    // js: game/states/title.js statusWait5
    /// <summary>status step 1 19:A26B: at status_anim_ctr 5, sound $1F</summary>
    private static IEnumerable<Y>? StatusWait5(Machine m)
    {
        if (m.Rd(0x0617) != 5) return null;
        m.Wr(snd, 0x1F);
        Inc(m, title_step);
        return null;
    }

    // js: game/states/title.js statusTextStart
    /// <summary>status step 2 19:A27C: at status_anim_ctr 0, fade in and set up the first text line</summary>
    private static IEnumerable<Y>? StatusTextStart(Machine m)
    {
        if (m.Rd(0x0617) != 0) return null;
        PalFadeIn(m);
        Inc(m, title_step);
        m.Wr(0x92, 0);
        return StatusLineSetup(m);
    }

    // js: game/states/title.js statusLineSetup
    /// <summary>19:A28C: text line $92 (count 19:B7C3, text ptr 19:B7C7), typed at $2084 with cursor sprite $0220; the
    /// two rows at $2084/$20A4 and $2884/$28A4 are blanked from $6018 ($10 x $18), one queue pair per frame</summary>
    private static IEnumerable<Y> StatusLineSetup(Machine m)
    {
        int x = m.Rd(0x92);
        m.Wr(0x92, x + 1);
        m.Wr(0x06CB, Prg.CpuRd(m, 0xB7C3 + x));
        m.Wr(0x90, Prg.CpuRd(m, 0xB7C7 + 2 * x)); m.Wr(0x91, Prg.CpuRd(m, 0xB7C8 + 2 * x));
        m.Wr(0x93, 0);
        m.Wr(0x84, 0x84); m.Wr(0x85, 0x20);
        m.Wr(0x06B4, Prg.CpuRd(m, 0xA349 + m.Rd(0x92)));
        m.Wr(0x0220, 0x27); m.Wr(0x0221, 0x22); m.Wr(0x0222, 0x03); m.Wr(0x0223, 0x20);
        for (int i = 0x17; i >= 0; i--) m.Wr(0x6018 + i, 0x10);
        m.Wr(snd, 0x5A);
        foreach (var y in PpuQueueDrainWait(m)) yield return y;
        QueuePair(m, m.Rd(0x85), m.Rd(0x84), m.Rd(0x85), m.Rd(0x84) + 0x20, 0x18, 0x18);
        PpuNmiOn(m);
        foreach (var y in WaitFrameReady(m)) yield return y;
        foreach (var y in PpuQueueDrainWait(m)) yield return y;
        int hi = m.Rd(0x85) + 8;
        QueuePair(m, hi, m.Rd(0x84), hi, m.Rd(0x84) + 0x20, 0x18, 0x18);
        PpuNmiOn(m);
    }

    // js: game/states/title.js statusTypeStep
    /// <summary>StatusTypeStep 19:A34E (step 3): every 2nd pass one character (2 tiles: $6002 upper, $6003 glyph) at $84,
    /// written vertically to NT $2000 and $2800; when the line is done, a pause $06B4, then the next line (19:A28C)
    /// or, after line 4, the name-entry palette ($06DD = $20), fade-in and step 4</summary>
    private static IEnumerable<Y>? StatusTypeStep(Machine m)
    {
        if ((m.Rd(ctr) & 1) != 0) return null;
        if ((m.Rd(0x06CB) & 0x80) != 0)
        {
            if (Dec(m, 0x06B4) != 0) return null;
            if (m.Rd(0x92) != 4) return StatusLineSetup(m);
            m.Wr(0x06DD, 0x20);
            m.Wr(0x614D, 0);
            Inc(m, state3_mode);
            Inc(m, title_step);
            return null;
        }
        return StatusTypeChar(m);
    }

    // js: game/states/title.js statusTypeChar
    private static IEnumerable<Y> StatusTypeChar(Machine m)
    {
        int y = m.Rd(0x93);
        m.Wr(0x6002, Prg.IndY(m, 0x90, y)); m.Wr(0x6003, Prg.IndY(m, 0x90, y + 1));
        m.Wr(0x93, y + 2);
        foreach (var s in PpuQueueDrainWait(m)) yield return s;
        int hi = m.Rd(0x85) | 0xC0;
        QueuePair(m, hi, m.Rd(0x84), hi + 8, m.Rd(0x84), 2, 2);
        Inc(m, 0x84);
        PpuNmiOn(m);
        m.Wr(0x0220, 0x27);
        m.Wr(0x0223, m.Rd(0x0223) + 8);
        if ((Dec(m, 0x06CB) & 0x80) != 0) m.Wr(0x0220, 0xF0);
    }

    // js: game/states/title.js nameEntryInit
    /// <summary>status step 4 19:A3DD: blank name ($6410/$6418 = $10 x 6), cursor $6420 = 0, name_entry_frames = 0</summary>
    private static IEnumerable<Y>? NameEntryInit(Machine m)
    {
        Inc(m, title_step);
        for (int x = 5; x >= 0; x--) { m.Wr(0x6410 + x, 0x10); m.Wr(0x6418 + x, 0x10); }
        m.Wr(0x6420, 0); m.Wr(0x6423, 0);
        return null;
    }

    // js: game/states/title.js nameEntryInput
    /// <summary>NameEntryInput 19:A3F6 (step 5)</summary>
    private static IEnumerable<Y>? NameEntryInput(Machine m)
    {
        Inc(m, 0x6423);
        m.Wr(0x0220, 0x49); m.Wr(0x0221, 0x22); m.Wr(0x0222, 0x03);
        m.Wr(0x0223, (m.Rd(0x6420) * 8 + 0x68) & 0xFF);
        NameEntryCursor(m);
        int p = m.Rd(R.PadNew);
        if ((p & 0x40) != 0) { NameCursorLeft(m); return null; }    // B
        if ((p & 0x10) != 0) { NameEntryFinish(m); return null; }   // START
        if ((p & 0x80) == 0) return null;                           // A
        int c = m.Rd(0x06CC);
        if (c == 0xF2) { NameCursorLeft(m); return null; }
        if (c == 0xF1) { NameCursorRight(m); return null; }
        if (c == 0xF0) { NameEntryFinish(m); return null; }
        return NameEntryType(m);
    }

    // js: game/states/title.js nameEntryType
    /// <summary>19:A448: type the cell under the cursor at $6420 (both tile bytes), advance (wrap at 6), queue the two
    /// tiles vertically at $210D + position in NT $2000 and $2800</summary>
    private static IEnumerable<Y> NameEntryType(Machine m)
    {
        m.Wr(snd, 0x58);
        int x = m.Rd(0x6420);
        m.Wr(0x6002, m.Rd(0x06CC)); m.Wr(0x6410 + x, m.Rd(0x06CC));
        m.Wr(0x6003, m.Rd(0x06CD)); m.Wr(0x6418 + x, m.Rd(0x06CD));
        int pos = x;
        x++;
        if (x == 6) x = 0;
        m.Wr(0x6420, x);
        int a = pos + Prg.CpuRd(m, 0xA4B1);
        m.Wr(0x72, a & 0xFF);
        m.Wr(0x73, (Prg.CpuRd(m, 0xA4B2) + (a >> 8)) & 0xFF);
        foreach (var y in PpuQueueDrainWait(m)) yield return y;
        int hi = m.Rd(0x73) | 0xC0;
        QueuePair(m, hi, m.Rd(0x72), hi + 8, m.Rd(0x72), 2, 2);
        PpuNmiOn(m);
    }

    // js: game/states/title.js nameCursorLeft
    /// <summary>19:A4BF: name cursor left (wrap 0..5), name_entry_frames = 0</summary>
    private static void NameCursorLeft(Machine m)
    {
        m.Wr(0x6423, 0);
        int x = m.Rd(0x6420) - 1;
        if (x < 0) x = 5;
        m.Wr(0x6420, x);
    }

    // js: game/states/title.js nameCursorRight
    /// <summary>19:A4D0: name cursor right (wrap 0..5), name_entry_frames = 0</summary>
    private static void NameCursorRight(Machine m)
    {
        m.Wr(0x6423, 0);
        int x = m.Rd(0x6420) + 1;
        if (x == 6) x = 0;
        m.Wr(0x6420, x);
    }

    // js: game/states/title.js nameEntryFinish
    /// <summary>19:A4E3: finish. Length = last non-blank of $6418; all blank -> the default name (19:A56C ptr: glyph row,
    /// 0, upper row, 0) and again. Builds player_name $6400: upper-row code $76 -> glyph + $F8, $75 -> glyph | $80,
    /// else the glyph; 0-terminated. Then fade out, step 6.</summary>
    private static void NameEntryFinish(Machine m)
    {
        for (; ; )
        {
            int xx = 5;
            while (xx >= 0 && m.Rd(0x6418 + xx) == 0x10) xx--;
            if (xx >= 0) { xx++; m.Wr(0x6420, xx); m.Wr(0x6421, xx); break; }
            int p = Prg.CpuRd(m, 0xA56C) | Prg.CpuRd(m, 0xA56D) << 8;   // 19:A53B
            m.Wr(0x92, p & 0xFF); m.Wr(0x93, p >> 8);
            int yy = 0, a;
            for (xx = 0; (a = Prg.CpuRd(m, p + yy++)) != 0; xx++) m.Wr(0x6418 + xx, a);
            for (xx = 0; (a = Prg.CpuRd(m, p + yy++)) != 0; xx++) m.Wr(0x6410 + xx, a);
        }
        int x = 0;
        for (int y = 0; ; y++)
        {
            int hi = m.Rd(0x6410 + y), g = m.Rd(0x6418 + y);
            if (hi == 0x76) { m.Wr(0x6400 + x, g); x++; m.Wr(0x6400 + x, 0xF8); }
            else if (hi == 0x75) m.Wr(0x6400 + x, g | 0x80);
            else m.Wr(0x6400 + x, g);
            x++;
            if (Dec(m, 0x6421) == 0) break;
        }
        m.Wr(0x6400 + x, 0);
        FadeOutStart(m);
        Inc(m, title_step);
    }

    // js: game/states/title.js nameEntryCursor
    /// <summary>NameEntryCursor 19:A585: pad_new b0-b3 = right / left / down / up (first set bit wins), skipping $FF cells
    /// of NameCellGrid 18:9EC2; cursor sprite $0208; $06CC/$06CD &lt;- the cell code / glyph (19:A63A alt set if $05)</summary>
    private static void NameEntryCursor(Machine m)
    {
        int p = m.Rd(R.PadNew);
        if ((p & 0x0F) != 0) m.Wr(snd, 0x4B);
        // js: game/states/title.js cell
        int Cell() { int y = CellIndex(m); return Prg.CpuRd(m, 0x9EC2 + y); }
        if ((p & 1) != 0)
        {
            do { int a = m.Rd(0x8A) + 1; if (a == 0x20) a = 0; m.Wr(0x8A, a); } while (Cell() == 0xFF);
        }
        else if ((p & 2) != 0)
        {
            do { int a = (m.Rd(0x8A) - 1) & 0xFF; if ((a & 0x80) != 0) a = 0x1F; m.Wr(0x8A, a); } while (Cell() == 0xFF);
        }
        else if ((p & 4) != 0)
        {
            do { int a = m.Rd(0x8B) + 2; if (a == 0x18) a = 0x10; m.Wr(0x8B, a); } while (Cell() == 0xFF);
        }
        else if ((p & 8) != 0)
        {
            do { int a = m.Rd(0x8B) - 2; if (a == 0x0E) a = 0x16; m.Wr(0x8B, a); } while (Cell() == 0xFF);
        }
        m.Wr(0x06B6, RorChain(p, (p & 1) != 0 ? 1 : (p & 2) != 0 ? 2 : (p & 4) != 0 ? 3 : 4));
        m.Wr(0x0208, (((m.Rd(0x8B) + 1) << 3) + 1) & 0xFF);
        m.Wr(0x0209, 0x22); m.Wr(0x020A, 0x03);
        m.Wr(0x020B, (m.Rd(0x8A) << 3) & 0xFF);
        int yi = CellIndex(m);
        m.Wr(0x06CC, Prg.CpuRd(m, 0x9EC2 + yi));
        m.Wr(0x06CD, Prg.CpuRd(m, 0x9F42 + yi));
        if (m.Rd(R.CamYHi) == 0) return;
        int g = m.Rd(0x06CD);
        if (g == 0x10 || g == 0x0C) return;
        m.Wr(0x06CD, g + 0x32);
    }

    // js: game/states/title.js rorChain
    /// <summary>$06B6 = pad_new after k ROR's (1 for RIGHT, 2 LEFT, 3 DOWN, else 4). Carry in = 0 (19:A40E CLC / ADC).</summary>
    private static int RorChain(int p, int k)
    {
        int v = p, c = 0;
        for (int i = 0; i < k; i++) { int nc = v & 1; v = (v >> 1) | (c << 7); c = nc; }
        return v;
    }

    // js: game/states/title.js cellIndex
    /// <summary>19:A0A5: cell index ($8B - $10) * 16 + $8A -> $88</summary>
    private static int CellIndex(Machine m)
    {
        int y = (((m.Rd(0x8B) - 0x10) << 4) + m.Rd(0x8A)) & 0xFF;
        m.Wr(0x88, y);
        return y;
    }

    // js: game/states/title.js nameEntryDone
    /// <summary>NameEntryDone 19:A57C (step 6) -> StartNewGame 19:A6F9</summary>
    private static IEnumerable<Y> NameEntryDone(Machine m)
    {
        PalBlack(m);
        foreach (var y in WaitFrameReady(m)) yield return y;
        m.Wr(R.MainState, 0);
    }

    // ---------------------------------------------------------------- dispatch maps (ROM address -> port), = JS MODES / STEPS
    private static readonly Dictionary<int, Func<Machine, IEnumerable<Y>?>> Modes = new()
    {
        [0xA79F] = TitleOpening, [0xA653] = TitleMenuInit, [0xA6DF] = TitleMenu,
        [0xABB4] = TitleStoryInit, [0xABEA] = TitleIntroStory, [0xA0B5] = TitleStatusInit, [0xA176] = TitleStatusScreen,
    };

    private static readonly Dictionary<int, Func<Machine, IEnumerable<Y>?>> Steps = new()
    {
        [0xA070] = StepBlack, [0x9FC2] = StepWait, [0xA064] = StepFadeOut, [0xABAB] = TitleOpeningEnd,
        [0xA7F6] = OpeningDateCard, [0xA843] = OpeningPage1, [0xA8D2] = OpeningPage1Anim, [0xA92D] = OpeningPage1Flash,
        [0xA953] = OpeningPage2, [0xAA16] = OpeningPage2Flip, [0xAA30] = OpeningPage3, [0xAAC2] = OpeningPage3Anim,
        [0xAAF1] = OpeningPage4, [0xAB81] = OpeningPage4Anim,
        [0xA6FE] = TitleMenuSprites, [0xA749] = TitleMenuInput, [0xA796] = TitleMenuRestart,
        // story steps (JmpTable19_AC07)
        [0xAC3F] = StoryScrollPage, [0xACE9] = StoryScrollUp, [0xACF9] = StoryScrollSprite, [0xAD27] = StoryBirdsInit,
        [0xAD5E] = StoryBirds, [0xADFF] = StoryFadeOut, [0xAE06] = m => StoryTextPage(m, 0xB42F), [0xAE3D] = StoryPage2,
        [0xAEEA] = StoryScrollDown, [0xAF14] = StoryPage2Anim, [0xAFCB] = StoryFadeOut,
        [0xAFD2] = m => StoryTextPage(m, 0xB505), [0xB009] = StoryPage3, [0xB092] = StoryPage3Anim,
        [0xB0FB] = StoryLastPage, [0xB15C] = TitleStoryEnd,
        // status / name-entry steps (JmpTable19_A193)
        [0xA1A1] = StatusSound, [0xA26B] = StatusWait5, [0xA27C] = StatusTextStart, [0xA34E] = StatusTypeStep,
        [0xA3DD] = NameEntryInit, [0xA3F6] = NameEntryInput, [0xA57C] = NameEntryDone,
    };
}
