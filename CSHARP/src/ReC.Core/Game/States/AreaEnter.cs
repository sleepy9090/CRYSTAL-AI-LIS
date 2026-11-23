// re-c C# port — the way into gameplay (= web/src/game/states/areaenter.js, s108).
// REVERSE.md "Main states (session 63)", "New-game vs continue path", "Substates" rows 0/1, "Area entry".
//
//   MainStateNewGame(m)   MainStateNewGame 30:C957 (main_state 0 -> 1, sub_state 0)
//   MainStateContinue(m)  MainStateContinue 30:C9DA (main_state 8 -> 1, sub_state 1)
//   SubstNewGameInit(m)   SubstNewGameInit 30:CA5F (sub_state 0 -> $13, or 8 when a button is held)
//   SubstAreaEnter(m)     SubstAreaEnter 30:CA2E (sub_state 1 -> 8)
//   AreaEnterRun(m)       (JS areaEnter) AreaEnter 31:E3E9 (AreaEntryTable 31:E3D9 by area_entry b7-b5)
//   PlayerPlaceBody(m, x) PlayerPlaceBody 26:9CEF
using ReC.Game.Obj;
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class AreaEnter
{
    private const int NoFade = 0x07E8, Flash = 0x0710, Active = 0x04A0, Flags = 0x0380;

    // js: game/states/areaenter.js gameStartObjs
    /// <summary>GameStartObjs 30:C9FF: all slots off, rendering + irq_mode 0, WaitFrame, ObjInit 3:$0A 0:$06 2:$05 1:$04</summary>
    private static IEnumerable<Y> GameStartObjs(Machine m)
    {
        for (int x = 0x1F; x >= 0; x--) m.Wr(Active + x, 0);
        m.Wr(R.PpumaskShadow, 0);
        m.Wr(R.IrqMode, 0);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        for (int y = 6; y >= 0; y -= 2)
        {                                                     // GameStartObjTable 30:CA26, read backwards
            m.Wr(0x10, Prg.CpuRd(m, 0xCA26 + y));
            m.Wr(0x11, Prg.CpuRd(m, 0xCA27 + y));
            ObjInit.Init(m);
        }
    }

    // js: game/states/areaenter.js mainStateNewGame
    /// <summary>MainStateNewGame 30:C957</summary>
    public static IEnumerable<Y> MainStateNewGame(Machine m)
    {
        for (int x = 0x4F; x >= 0; x--) m.Wr(0x6428 + x, 0xFF);   // inventory + selections
        for (int x = 0; x < 0x100; x++) { m.Wr(0x6480 + x, 0); m.Wr(0x6580 + x, 0); }
        m.Wr(R.AreaId, 0);
        m.Wr(0x6D, 1);
        m.Wr(R.MainState, 1);
        m.Wr(R.SubState, 0);
        foreach (var s in GameStartObjs(m)) yield return s;
        m.Wr(0x0706, 0x1E);                                   // xp_next = 30
        foreach (int a in new[] { 0x0707, 0x0704, 0x0705, 0x0702, 0x0703, 0x0710, 0x0715, 0x0716, 0x0717, 0x0711, 0x0713,
            0x0714, 0x0712, 0x0718, 0x0719, NoFade, 0x071A, 0x0743, 0x0744, 0x02, 0x03, 0x04, 0x05, R.IrqMode }) m.Wr(a, 0);
        m.Wr(0x0708, 0x22);                                   // MP cur / max = 34
        m.Wr(0x0709, 0x22);
        foreach (var s in Death.StatRecalc(m, StatX(m))) yield return s;
    }

    // js: game/states/areaenter.js statX
    /// <summary>X at the JSR StatRecalc after GameStartObjs: the last ObjInit leaves X = its slot $10 mod $20 (s97)</summary>
    private static int StatX(Machine m) => m.Rd(0x10) & 0x1F;

    // js: game/states/areaenter.js loadGameAndInitLag
    /// <summary>LoadGameAndInit 23:BCDA as called from state 8: the copies run through one NMI (tools.emu s108); $6D /
    /// sub_state are written after it</summary>
    private static IEnumerable<Y> LoadGameAndInitLag(Machine m)
    {
        int e = m.Rd(0x6D), s = m.Rd(R.SubState);
        SaveLoad.LoadGameAndInit(m);
        m.Wr(0x6D, e); m.Wr(R.SubState, s);
        yield return Y.Frame;
        m.Wr(0x6D, 0xFF);
        m.Wr(R.SubState, 1);
    }

    // js: game/states/areaenter.js mainStateContinue
    /// <summary>MainStateContinue 30:C9DA</summary>
    public static IEnumerable<Y> MainStateContinue(Machine m)
    {
        foreach (var s in GameStartObjs(m)) yield return s;
        Prg.SetR7(m, 0x17);
        foreach (var s in LoadGameAndInitLag(m)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        foreach (var s in Death.StatRecalc(m, StatX(m))) yield return s;
        Prg.SwitchBank16(m, 9);
        foreach (var s in SaveLoad.HudWindowSetup(m)) yield return s;
        m.Wr(R.MainState, 1);
        m.Wr(R.SubState, 1);
        m.Wr(NoFade, 0);
    }

    // js: game/states/areaenter.js substNewGameInit
    /// <summary>SubstNewGameInit 30:CA5F</summary>
    public static IEnumerable<Y> SubstNewGameInit(Machine m)
    {
        Prg.SwitchBank16(m, 9);
        foreach (var s in SaveLoad.HudWindowSetup(m)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        Prg.SetR6(m, 0x1A);
        foreach (var s in Death.HudHpBar(m)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        m.Wr(R.PalBuf, 0x0F);
        for (int i = 0; i < 4; i++) { m.Wr(0x07E0 + i, 0xAF); m.Wr(0x07E4 + i, 0x4F); }
        m.Wr(NoFade, 0xFF);
        foreach (var s in SubstAreaEnter(m)) yield return s;
        m.Wr(R.SubState, 0x13);
        foreach (var s in NewGameSkipCheck(m)) yield return s;
    }

    // js: game/states/areaenter.js newGameSkipCheck
    /// <summary>30:CAA2: pal_nofade_mask 0; ReadPads; a held button skips the wake-up cut-scene (sub_state 8 + StatRecalc)</summary>
    private static IEnumerable<Y> NewGameSkipCheck(Machine m)
    {
        m.Wr(NoFade, 0);
        Pads.ReadPads(m);
        if (m.Rd(R.PadPrev) == 0) yield break;
        m.Wr(R.SubState, 8);
        foreach (var s in Death.StatRecalc(m, 0xFF)) yield return s;   // ReadPads ends its pad loop with X = $FF
    }

    // js: game/states/areaenter.js substAreaEnter
    /// <summary>SubstAreaEnter 30:CA2E</summary>
    public static IEnumerable<Y> SubstAreaEnter(Machine m)
    {
        foreach (var s in AreaEnterRun(m)) yield return s;
        Prg.SetR6(m, 0x1A);
        ObjUtil.ObjPosToTemp(m, 1);
        ObjUtil.ObjBgProbe(m, 1, 0x40);
        m.Wr(R.IrqMode, 0);
        m.Wr(NoFade, 0);
        m.Wr(Flags + 1, m.Rd(Flags + 1) & 0xBF);
        m.Wr(R.SubState, 8);
        if ((m.Rd(0x6D) & 0xE0) == 0x40)
        {                                                     // CA5B: JMP MainStateGameplay (same frame)
            var g = Play.MainState1(m);
            if (g != null) foreach (var s in g) yield return s;
        }
    }

    // js: game/states/areaenter.js farSpriteDraw
    /// <summary>FarCall_B14_800E 31:FF8C</summary>
    public static void FarSpriteDraw(Machine m)
    {
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);
        Prg.SwitchBank16(m, 0x0E);
        Sprites.SpriteDraw(m);
        Prg.FarRestore(m, r6, r7);
    }

    // js: game/states/areaenter.js clearSlots
    /// <summary>31:E61B: object slots 4-$1E off</summary>
    private static void ClearSlots(Machine m) { for (int x = 4; x < 0x1F; x++) m.Wr(Active + x, 0); }

    // js: game/states/areaenter.js areaEnter
    /// <summary>AreaEnter 31:E3E9</summary>
    public static IEnumerable<Y> AreaEnterRun(Machine m)
    {
        FarSpriteDraw(m);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        m.Wr(R.IrqMode, 0);
        m.Wr(0x07D7, 0);
        m.Wr(0x07D6, 0);
        Prg.SetR6(m, 0x1A);
        if ((m.Rd(NoFade) & 0x80) == 0) Death.PaletteBuildRom(m, 0);
        int k = (m.Rd(0x6D) & 0xE0) >> 5;                     // AreaEntryDispatch 31:E40A (LSR x4 of b7-b5 = k*2)
        m.Wr(0x10, Prg.CpuRd(m, 0xE3D9 + k * 2));
        m.Wr(0x11, Prg.CpuRd(m, 0xE3DA + k * 2));
        int to = m.Rd(0x10) | m.Rd(0x11) << 8;
        switch (to)
        {
            case 0xE422:                                      // normal entry
                Prg.SetR6(m, 0x1A);                           // 31:E611
                foreach (var s in Death.FadeOut(m, 2)) yield return s;
                ClearSlots(m);
                if ((m.Rd(Flash) & 0x0F) == 4)
                {
                    m.Wr(Flash, m.Rd(Flash) & 0xF0);
                    foreach (var s in Death.StatRecalc(m, 0x1F)) yield return s;   // X = $1F after the E61B loop
                }
                break;
            case 0xE41C:                                      // $6D = $E0-$FF (continue / menu load)
                ClearSlots(m);
                break;
            case 0xE51C:                                      // $2x: keep the position, map only
                for (int x = 0x0D; x < 0x20; x++) m.Wr(Active + x, 0);
                Area.LoadAreaMap(m);
                Prg.SetR6(m, 0x1A);
                Death.PaletteBuildRom(m, 0);
                yield break;
            case 0xE533:                                      // $4x: save + attribute wipe
                Prg.SetR7(m, 0x17);
                // s143: the full save runs through 2 NMIs here too, as at the area-tail save below
                if (SaveLoad.SaveGame(m)) { yield return Y.Frame; yield return Y.Spin; }
                foreach (var s in AttrWipe(m)) yield return s;
                ClearSlots(m);
                break;
            case 0xE544:
            {                                                 // $6x: pit fall -> AreaEdgeLink (k5)
                var ok = new Ret<bool>();
                foreach (var s in AreaPitFall(m, ok)) yield return s;
                if (!ok.V) yield break;                       // no k5 match: E603 tail-jumps out of AreaEnter
                foreach (var s in AreaEnterCommon(m, true)) yield return s;
                yield break;
            }
            default:                                          // the null entries ($8x-$Dx)
                throw new InvalidOperationException($"AreaEnter: area_entry ${m.Rd(0x6D):x} -> ${to:x} not ported");
        }
        foreach (var s in AreaEnterCommon(m)) yield return s;
    }

    // js: game/states/areaenter.js areaPitFall
    /// <summary>AreaPitFall 31:E544 (s116): one frame per step, slots 1 and 0 drop 8 px while the cell 8 px below slot 1
    /// has attr b0, with a sprite pass each step; then AreaEdgeLink. res.V = true on a k5 match</summary>
    private static IEnumerable<Y> AreaPitFall(Machine m, Ret<bool> res)
    {
        for (; ; )
        {
            foreach (var s in Death.WaitFrame(m)) yield return s;
            Prg.SwitchBank16(m, 0x0D);
            ObjUtil.ObjPosToTemp(m, 1);                       // 26:98A8 X=1
            m.Wr(0x31, 8);
            m.Wr(0x30, 0);
            ObjUtil.TempAddStep(m);                           // 26:9861
            ObjUtil.ObjBgProbe(m, 1, 0x40);
            if ((m.Rd(0x20) & 1) == 0) break;
            ObjUtil.TempToObjPos(m, 1);
            ObjUtil.TempToObjPos(m, 0);
            FarSpriteDraw(m);
        }
        foreach (var s in AreaEdgeLink(m, res)) yield return s;
    }

    // js: game/states/areaenter.js areaEdgeLink
    /// <summary>AreaEdgeLink 31:E573 (s116): AreaFadeOutClear, then 4-byte k5 records `area a21 a22 a23` (only Y wrapping
    /// to 0 ends the scan) matched on x hi / y hi against slot 0. Match -> slots 0/1 moved, area_id = a20, AreaLoadMap,
    /// $0640 = $FF, jump timer $0620 = $30 -> true. No match (E603): sub_state 8 + the unmodelled JMP $8C49 -> false</summary>
    private static IEnumerable<Y> AreaEdgeLink(Machine m, Ret<bool> res)
    {
        Prg.SetR6(m, 0x1A);                                   // AreaFadeOutClear 31:E611
        foreach (var s in Death.FadeOut(m, 2)) yield return s;
        ClearSlots(m);
        Camera.AreaSubTable(m, 5);
        int y = 0, x;
        for (; ; )
        {
            m.Wr(0x12, 4);
            for (x = 0; ;)
            {
                m.Wr(0x20 + x, Prg.IndY(m, 0x10, y));
                x++;
                y = (y + 1) & 0xFF;
                if (y == 0)
                {                                             // 31:E603
                    m.Wr(R.SubState, 8);
                    Prg.SetR6(m, 0x1A);
                    Unported(m, "AreaEdgeLink no-match JMP $8C49 with bank 10 mapped (31:E60E)");
                    res.V = false;
                    yield break;
                }
                m.Wr(0x12, (m.Rd(0x12) - 1) & 0xFF);
                if (m.Rd(0x12) == 0) break;
            }
            if (m.Rd(0x21) >> 4 != m.Rd(0x90)) continue;
            m.Wr(0x13, m.Rd(0x22) & 0xF0);
            if ((m.Rd(0x13) | (m.Rd(0x23) >> 4)) == m.Rd(0xD0)) break;
        }
        int yl = (m.Rd(0xB0) + 0x10) & 0xFF;
        m.Wr(0xB0, yl); m.Wr(0xB1, yl);
        m.Wr(0x10, (m.Rd(0x22) << 1) & 0xF0);
        int yh = (m.Rd(0x23) & 0x0F) | m.Rd(0x10);
        m.Wr(0xD0, yh); m.Wr(0xD1, yh);
        int xh = m.Rd(0x21) & 0x0F;
        m.Wr(0x90, xh); m.Wr(0x91, xh);
        int area = m.Rd(0x20);
        if ((m.Rd(Flash) & 0x0F) == 4)
        {
            m.Wr(Flash, m.Rd(Flash) & 0xF0);
            foreach (var s in Death.StatRecalc(m, x)) yield return s;   // X = 4 after the record read
        }
        m.Wr(R.AreaId, area);
        Area.LoadAreaMap(m);
        Prg.SwitchBank16(m, 0x0D);
        ObjUtil.ObjPosToTemp(m, 0);
        ObjUtil.TempToObjPos(m, 1);
        m.Wr(0x0640, 0xFF);
        m.Wr(0x0620, 0x30);
        res.V = true;
    }

    // js: game/states/areaenter.js unported
    private static void Unported(Machine m, string what)
    {
        if (m.Unported.Add(what)) m.Log($"{what} not ported yet");
    }

    // js: game/states/areaenter.js areaEnterCommon
    /// <summary>31:E439 (the common tail of AreaEnter); fromE44A = AreaEdgeLink's JMP $E44A (map + slots already set)</summary>
    private static IEnumerable<Y> AreaEnterCommon(Machine m, bool fromE44A = false)
    {
        if (!fromE44A)
        {
            Area.LoadAreaMap(m);
            Prg.SwitchBank16(m, 0x0D);
            ObjUtil.TempToObjPos(m, 0);
            ObjUtil.TempToObjPos(m, 1);
        }
        FlashAreaCheck(m);
        AreaFollowerHook(m);
        AreaChrOverride(m);
        m.Wr(R.ScrollShadow0, 0);
        m.Wr(R.CamYLo, 0);
        m.Wr(0x30, 0x80);
        m.Wr(0x31, 0x80);
        Prg.SwitchBank16(m, 0x0D);
        ObjUtil.TempAddOffset(m);
        Camera.CameraClampX(m);
        for (int x = 3; x >= 0; x--) m.Wr(0x3C + x, m.Rd(0x34 + x));
        int sub = m.Rd(R.SubState);
        if (sub != 0x14 && sub != 0x06)
        {
            Prg.SwitchBank16(m, 0x0D);
            PlayerPlaceBody(m, 1);
        }
        for (int x = 0x1F; x > 0; x--) m.Wr(0x6200 + x, 0xFF);   // PPU queue ring ($6200 itself is kept)
        foreach (var s in Camera.CameraTick(m)) yield return s;
        m.Wr(0x3A, 0);
        m.Wr(0x3B, 0);
        for (int x = 3; x >= 0; x--) m.Wr(0x34 + x, m.Rd(0x3C + x));
        m.Wr(R.FrameReady, 0);
        m.Wr(R.IrqMode, 0x80);
        m.Wr(R.PpumaskShadow, m.Rd(R.PpumaskShadow) & 0x06);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        foreach (var s in BgStream.FullScreenDraw(m, 1, BgStream.FsdLag)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int x = 8; x >= 0; x--) foreach (var s in Camera.CameraTick(m)) yield return s;
        Prg.SwitchBank16(m, 0x0E);
        Sprites.SpriteDraw(m);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        m.Wr(R.IrqMode, 0);
        m.Wr(R.PpumaskShadow, m.Rd(R.PpumaskShadow) | 0x18);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        if ((m.Rd(NoFade) & 0x80) == 0) PlaySound.Play(m, 0x2F, Sprites.SpriteDrawY(m));   // Y = ObjSpriteDraw's
        Prg.SetR6(m, 0x1A);
        if ((m.Rd(NoFade) & 0x80) == 0) foreach (var s in Death.FadeIn(m, 2)) yield return s;
        Prg.SetR7(m, 0x17);
        if (SaveLoad.SaveGame(m)) { yield return Y.Frame; yield return Y.Spin; }   // JSR $BC00: 2 NMIs (s108)
        m.Wr(0x05A0, 4);                                      // obj_freeze
        m.Wr(Flags + 1, m.Rd(Flags + 1) | 4);
        m.Wr(0x071A, m.Rd(0x071A) | 4);
    }

    // js: game/states/areaenter.js flashAreaCheck
    /// <summary>31:E799: stone-flag b6 of $0710 only survives in areas $60/$64/$68 (then slot $1E = $0C at the player)</summary>
    private static void FlashAreaCheck(Machine m)
    {
        if ((m.Rd(Flash) & 0x40) == 0) return;
        int a = m.Rd(R.AreaId);
        if (a == 0x60 || a == 0x64 || a == 0x68)
        {
            m.Wr(Active + 0x1E, 0x0C);
            ObjUtil.TempToObjPos(m, 0x1E);
        }
        else m.Wr(Flash, m.Rd(Flash) & 0xBF);
    }

    // js: game/states/areaenter.js areaFollowerHook
    /// <summary>AreaFollowerHook 31:E7C4</summary>
    private static void AreaFollowerHook(Machine m)
    {
        int a = m.Rd(R.AreaId);
        if (a == 0xCE) return;
        if (a == 0x1C && ((m.Rd(0x6488) << 2) & 0x80) == 0)
        {
            if ((m.Rd(0x648A) & 0x08) == 0) return;
            m.Wr(0x6488, m.Rd(0x6488) | 0x20);
            const int x = 0x0E;
            m.Wr(Active + x, 0x32);
            m.Wr(0x03A0 + x, 0); m.Wr(0x0620 + x, 0); m.Wr(0x0640 + x, 0);
            m.Wr(Flags + x, m.Rd(Flags + x) & 0xEF);
            m.Wr(0x0680 + x, 0x0C);
            m.Wr(0x04E0 + x, 1);
            m.Wr(0x0600 + x, 0xFF);
            m.Wr(0x0340 + x, 3);
            m.Wr(0x0600 + x, 0xFF);
            ObjUtil.TempToObjPos(m, x);
            return;
        }
        m.Wr(0x648A, m.Rd(0x648A) & 0xF7);
    }

    // js: game/states/areaenter.js areaChrOverride
    /// <summary>31:E823: area $8C with $6484 b7 -> CHR R4/R5 $FF/$51, sprite palettes 2/3 = $06/$0A</summary>
    private static void AreaChrOverride(Machine m)
    {
        if (m.Rd(R.AreaId) != 0x8C || (m.Rd(0x6484) & 0x80) == 0) return;
        m.Wr(0x07F4, 0xFF); m.Wr(0x07F5, 0x51);
        m.Wr(0x07E6, 0x06); m.Wr(0x07E7, 0x0A);
    }

    // js: game/states/areaenter.js attrWipe
    /// <summary>31:E845 (area_entry $4x): bg palette 3 = $AD, then 16 steps of OR-ing 31:E8B6 masks into the attribute
    /// shadow, each queued + 2 frames; fade out (2); bg palette 3 restored</summary>
    private static IEnumerable<Y> AttrWipe(Machine m)
    {
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        Prg.SetR6(m, 0x1A);
        int save = m.Rd(0x07E3);
        m.Wr(0x07E3, 0xAD);
        Death.PaletteBuildRom(m, 0);
        int[,] ot = { { 0, 0 }, { 1, 1 }, { 8, 2 }, { 9, 3 } };
        for (int x = 0; x != 0x40; x += 4)
        {
            m.Wr(0x11, 4);
            int y = 0;
            do
            {
                m.Wr(0x10, 4);
                do
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int o = ot[i, 0], t = ot[i, 1];
                        m.Wr(0x6100 + y + o, m.Rd(0x6100 + y + o) | Prg.CpuRd(m, 0xE8B6 + x + t));
                    }
                    y += 2;
                    m.Wr(0x10, m.Rd(0x10) - 1);
                } while (m.Rd(0x10) != 0);
                y += 8;
                m.Wr(0x11, m.Rd(0x11) - 1);
            } while (m.Rd(0x11) != 0);
            m.Wr(0x10, x);
            PpuQueue.QueueAttrAll(m);
            for (int i = 0; i < 2; i++) foreach (var s in Death.WaitFrame(m)) yield return s;   // 30:C176 (Y = 2)
        }
        foreach (var s in Death.FadeOut(m, 2)) yield return s;
        m.Wr(0x07E3, save);
    }

    // js: game/states/areaenter.js playerPlaceBody
    /// <summary>PlayerPlaceBody 26:9CEF (bank 26 mapped): slot x = slot 0 + JumpHeightTable 26:9D4D[$0620]</summary>
    public static void PlayerPlaceBody(Machine m, int x)
    {
        m.Wr(0x31, Prg.CpuRd(m, 0x9D4D + m.Rd(0x0620)));
        if (m.Rd(0x31) != 0)
        {
            m.Wr(0x03A0 + x, 0x60);
            ObjUtil.ObjPosToTemp(m, x);
            ObjUtil.ObjBgProbe(m, x, 0x40);
            m.Wr(Flags + x, m.Rd(Flags + x) & 0xBF);
        }
        else
        {
            m.Wr(0x03A0 + x, 0x50);
            m.Wr(0x10, m.Rd(Flags + x) & 7);
            m.Wr(Flags + x, (m.Rd(Flags) & 0xF8) | m.Rd(0x10));
        }
        if (m.Rd(0x31) == 0xF9)
        {                                                     // landing from the highest jump
            PlaySound.Play(m, 0x56);
            m.Wr(0x07D7, 0x10);
            m.Wr(0x07D6, 0x10);
        }
        m.Wr(0x10, x);
        ObjUtil.ObjPosToTemp(m, 0);
        m.Wr(0x30, 0);
        ObjUtil.TempAddOffset(m);
        ObjUtil.TempToObjPos(m, x);
    }
}
