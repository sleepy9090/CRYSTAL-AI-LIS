// re-c C# port — the save / load page of the SELECT menu and the save module (= web/src/game/states/saveload.js, s102).
// REVERSE.md "Save / load module, bank 23 `$BC00`" + "Save / load page (web port, session 102)".
//
//   MenuExitFade(m)      30:C80C (the $7000 = $FB / $AB exit: fade out, 30 frames, fade in)
//   LoadTail(m)          30:D930 + EquipMenuLoadTail 30:D942 (after a menu LOAD)
//   HudWindowSetup(m)    19:BA65 (HUD window rebuild; also used by states 2/3/8 + SubstRestoreScreen)
//   SubstRestoreScreen(m, res)  SubstRestoreScreen 30:CC4D (sub_state $1F; area $5F redraw, s120)
//   SaveGame / LoadGame / LoadGameAndInit   23:BC09 / 23:BC8E / 23:BCDA (bank 23 at $A000)
// C# s3 (T4): the menu page itself (saveLoadPage, menuSave, slotPicture, pageSetup, modeCursor, slotCursor) needs
// states/equip.js and comes with T8.
// Copy-length quirks kept: BlockCopy copies lo + 1 + hi bytes, SaveSlotCopy $2FF bytes, SaveGame's not-allowed path
// mirrors $7D00-$7FFF -> $6D00-$6FFF only from offset X = area_id.
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class SaveLoad
{
    private const int SaveState = 0x7000;    // menu_save_state: 0 / $AB (closed) / $FB (save tried) / $FA (loaded)

    /// <summary>Lag frames of the copy loops (measured in tools.emu s102): SAVE = 4, LOAD = 1 NMIs inside the copies.</summary>
    public const int SaveLag = 4, LoadLag = 1;

    // js: game/states/saveload.js busy
    public static IEnumerable<Y> Busy(Machine m, int k)
    {
        for (int i = 0; i < k; i++) yield return Y.Spin;
    }

    // js: game/states/saveload.js lsrWait
    /// <summary>`LSR frame_ready / BCC` spin (16:9BFF, 30:C816): returns once bit 0 was set, shifting it out</summary>
    public static IEnumerable<Y> LsrWait(Machine m)
    {
        for (; ; )
        {
            int v = m.Rd(R.FrameReady);
            m.Wr(R.FrameReady, v >> 1);
            if ((v & 1) != 0) yield break;
            yield return Y.Frame;
        }
    }

    // js: game/states/saveload.js wait48
    /// <summary>16:9BFD: 48 x LsrWait</summary>
    public static IEnumerable<Y> Wait48(Machine m)
    {
        for (int x = 0x30; x > 0; x--) foreach (var s in LsrWait(m)) yield return s;
    }

    // js: game/states/saveload.js saveSlotCopy
    /// <summary>SaveSlotCopy 16:9CE0: descriptor A of SaveSlotDescs 16:9D32 (src, dst); $10-$15 kept in $60C0-$60C5.
    /// The DEC/BNE count copies 255 + 256 + 256 = $2FF bytes.</summary>
    public static void SaveSlotCopy(Machine m, int a)
    {
        for (int x = 0; x < 6; x++) m.Wr(0x60C0 + x, m.Rd(0x10 + x));
        int p = 0x9D32 + ((a << 2) & 0xFF);
        int src = Prg.CpuRd(m, p) | Prg.CpuRd(m, p + 1) << 8, dst = Prg.CpuRd(m, p + 2) | Prg.CpuRd(m, p + 3) << 8;
        for (int i = 0; i < 0x2FF; i++) m.Wr((dst + i) & 0xFFFF, m.Rd((src + i) & 0xFFFF));
        for (int x = 0; x < 6; x++) m.Wr(0x10 + x, m.Rd(0x60C0 + x));
    }

    // js: game/states/saveload.js blockCopy
    /// <summary>BlockCopy 23:BCF7: ($10) -> ($12), lo + 1 + hi bytes ($14 / $15)</summary>
    private static void BlockCopy(Machine m)
    {
        int s = m.Rd(0x10) | m.Rd(0x11) << 8, d = m.Rd(0x12) | m.Rd(0x13) << 8;
        int lo = m.Rd(0x14), hi = m.Rd(0x15);
        for (; ; )
        {
            m.Wr(d, m.Rd(s));
            s = (s + 1) & 0xFFFF; d = (d + 1) & 0xFFFF;
            lo = (lo - 1) & 0xFF;
            if (lo != 0xFF) continue;
            lo = 0;
            hi = (hi - 1) & 0xFF;
            if (hi == 0xFF) break;
        }
        m.Wr(0x10, s & 0xFF); m.Wr(0x11, s >> 8); m.Wr(0x12, d & 0xFF); m.Wr(0x13, d >> 8);
        m.Wr(0x14, lo); m.Wr(0x15, hi);
    }

    // js: game/states/saveload.js copyDescs
    /// <summary>the descriptor loops of SaveGame (5 x 23:BDC0) / LoadGame (4 x 23:BDDE)</summary>
    private static void CopyDescs(Machine m, int table, int n)
    {
        for (int t = 0; t < n; t++)
        {
            m.Wr(0x17, t);
            int y = t * 6;
            m.Wr(0x10, (t << 1) & 0xFF);                         // ASL A / STA $10 (then overwritten)
            for (int x = 0; x < 6; x++) m.Wr(0x10 + x, Prg.CpuRd(m, table + y + x));
            BlockCopy(m);
        }
        m.Wr(0x17, n);
    }

    private static readonly (int A, int W)[] PState =
    {
        (0x6C, 0x7DF0), (0x70, 0x7DF1), (0x90, 0x7DF2), (0xB0, 0x7DF3), (0xD0, 0x7DF4),
        (0x03C0, 0x7DF5), (0x03C1, 0x7DF6), (0x0421, 0x7DF7), (0x0620, 0x7DF8),
    };

    // js: game/states/saveload.js saveChecksum
    /// <summary>SaveChecksum 23:BD92: 8-bit sum of $7D00-$7FFF -> $70F5 and $70F4</summary>
    private static void SaveChecksum(Machine m)
    {
        int s = 0;
        for (int x = 0; x < 0x100; x++) s = (s + m.Rd(0x7D00 + x) + m.Rd(0x7E00 + x) + m.Rd(0x7F00 + x)) & 0xFF;
        m.Wr(0x70F5, s); m.Wr(0x70F4, s);
    }

    // js: game/states/saveload.js saveGame
    /// <summary>SaveGame 23:BC09 -> true when the area allows saving (the full image rebuild, ~2 frames of CPU, s108)</summary>
    public static bool SaveGame(Machine m)
    {
        int flash = m.Rd(0x0710);
        m.Wr(0x0710, flash & 0xF0);
        for (int i = 0; i < 0x20; i++) m.Wr(0x6000 + i, m.Rd(0x10 + i));
        int x = m.Rd(R.AreaId);                                  // SaveAllowed: LDX $6C
        bool full = (Prg.CpuRd(m, 0xBF00 + x) & 0x80) != 0;
        if (full)
        {
            for (int i = 0; i < 0x100; i++) { m.Wr(0x7D00 + i, 0); m.Wr(0x7E00 + i, 0); m.Wr(0x7F00 + i, 0); }
            foreach (var (src, dst) in PState) m.Wr(dst, m.Rd(src));   // SavePlayerState 23:BD1F
            m.Wr(0x7DFD, 0x41); m.Wr(0x7DFE, 0x4D); m.Wr(0x7DFF, 0x49);
            CopyDescs(m, 0xBDC0, 5);
            SaveChecksum(m);
            x = 0;
        }
        for (; x < 0x100; x++)
        {                                                        // BC65: mirror (from X = area_id when not allowed)
            m.Wr(0x6D00 + x, m.Rd(0x7D00 + x)); m.Wr(0x6E00 + x, m.Rd(0x7E00 + x)); m.Wr(0x6F00 + x, m.Rd(0x7F00 + x));
        }
        for (int i = 0; i < 0x20; i++) m.Wr(0x10 + i, m.Rd(0x6000 + i));
        m.Wr(0x0710, flash);
        return full;
    }

    // js: game/states/saveload.js loadGame
    /// <summary>LoadGame 23:BC8E</summary>
    public static void LoadGame(Machine m)
    {
        for (int x = 0; x < 0x20; x++) m.Wr(0x6000 + x, m.Rd(0x10 + x));
        CopyDescs(m, 0xBDDE, 4);
        foreach (var (dst, src) in PState) m.Wr(dst, m.Rd(src));   // LoadPlayerState 23:BD60
        for (int x = 0; x < 0x20; x++) m.Wr(0x10 + x, m.Rd(0x6000 + x));
    }

    // js: game/states/saveload.js loadGameAndInit
    /// <summary>LoadGameAndInit 23:BCDA</summary>
    public static void LoadGameAndInit(Machine m)
    {
        LoadGame(m);
        for (int x = 0; x < 0x100; x++) { m.Wr(0x6480 + x, m.Rd(0x7E00 + x)); m.Wr(0x6580 + x, m.Rd(0x7F00 + x)); }
        m.Wr(0x6D, 0xFF);
        m.Wr(R.SubState, 1);
    }

    // js: game/states/saveload.js menuExitFade
    /// <summary>30:C80C: SwitchBank16(13), fade out (3), 1 frame, irq_mode 0, 30 frames, fade in (3), SwitchBank16(8)</summary>
    public static IEnumerable<Y> MenuExitFade(Machine m)
    {
        Prg.SwitchBank16(m, 0x0D);
        foreach (var s in Death.FadeOut(m, 3)) yield return s;
        foreach (var s in LsrWait(m)) yield return s;
        m.Wr(R.IrqMode, 0);
        for (int x = 0x1E; x > 0; x--) foreach (var s in LsrWait(m)) yield return s;
        foreach (var s in Death.FadeIn(m, 3)) yield return s;
        Prg.SwitchBank16(m, 8);
    }

    // js: game/states/saveload.js hudChargeMeter
    /// <summary>26:8D59: the charge meter ($6060-$6070: 7 x $8F, orbit level x $8E, 9 tiles 26:8DF5[26:8DDD[..]]) -> $2B46</summary>
    public static IEnumerable<Y> HudChargeMeter(Machine m)
    {
        foreach (var s in WaitSrc60(m)) yield return s;
        for (int i = 6; i >= 0; i--) m.Wr(0x6060 + i, 0x8F);
        int orb = m.Rd(0x06C0);
        for (int i = orb & 7; i > 0; i--) m.Wr(0x605F + i, 0x8E);
        int v = (m.Rd(0x0719) << 3) & 0xFF;
        m.Wr(0x10, v);
        if (orb != 0) { v |= (m.Rd(0x08) & 4) >> 2; m.Wr(0x10, v); }
        int x = Prg.CpuRd(m, 0x8DDD + (((orb & 0x18) >> 2) | v));
        for (int y = 0; y < 9; y++) m.Wr(0x6067 + y, Prg.CpuRd(m, 0x8DF5 + x + y));
        Death.HudQueue(m, 0x2B, 0x46, 0x10, 0x60);
    }

    // js: game/states/saveload.js waitSrc60
    /// <summary>PpuQueueWaitSrc 30:C72B ($60)</summary>
    private static IEnumerable<Y> WaitSrc60(Machine m)
    {
        while (PpuQueue.QueueHasSrc(m, 0x60)) yield return Y.Spin;
    }

    // js: game/states/saveload.js hudWindowSetup
    /// <summary>19:BA65: HUD window rebuild (needs banks 18/19 in: SwitchBank16(9))</summary>
    public static IEnumerable<Y> HudWindowSetup(Machine m)
    {
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int x = 0x1F; x >= 0; x--)
        {
            m.Wr(0x6000 + x, 0x19); m.Wr(0x6020 + x, 0x1C); m.Wr(0x6040 + x, 0xFF);
            m.Wr(0x6060 + x, 0xFF); m.Wr(0x6080 + x, 0xFF);
        }
        m.Wr(0x6001, 0x18); m.Wr(0x601F, 0x1A);
        foreach (var s in Pause.PpuQueueBlock(m, 0)) yield return s;
        m.Wr(0x6021, 0x1B); m.Wr(0x603F, 0x1D);
        foreach (int b in new[] { 1, 2, 3 }) foreach (var s in Pause.PpuQueueBlock(m, b)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int x = 0x5F; x >= 0; x--) m.Wr(0x6000 + x, Prg.CpuRd(m, 0xBAD9 + x));
        foreach (var s in Pause.PpuQueueBlock(m, 4)) yield return s;
        Prg.SetR6(m, 0x1A);
        foreach (var s in Death.HudHpBar(m)) yield return s;
        foreach (var s in HudChargeMeter(m)) yield return s;
        for (int x = 5; x >= 0; x--) foreach (var s in Death.HudRedraw(m, x, x)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
    }

    // js: game/states/saveload.js RESTORE_LAG
    /// <summary>s120: SubstRestoreScreen's FullScreenDraw: NMIs after rows 6/12/18/24/30 (5 NMIs)</summary>
    private static readonly Func<int, bool> RestoreLag = n => n % 6 == 0;

    // js: game/states/saveload.js substRestoreScreen
    /// <summary>SubstRestoreScreen 30:CC4D (s120; sub_state $1F after AI $70 phase 2, JMP from 30:C8AF / 30:CBB1, JSR from
    /// 30:D912): redraw the area-$5F screen after a window covered it. res.V = Y (SubstPlay goes on with it after 30:CBB1).</summary>
    public static IEnumerable<Y> SubstRestoreScreen(Machine m, Ret<int>? res = null)
    {
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        m.Wr(R.PpumaskShadow, m.Rd(R.PpumaskShadow) & 0xE7);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        for (int i = 0; i < 4; i++) m.Wr(0x34 + i, m.Rd(0x3C + i));   // pos $34-$37 <- camera $3C-$3F
        m.Wr(0x0744, 8);                                         // nametable b3 for BgRowRender's queue hi
        foreach (var s in BgStream.FullScreenDraw(m, 1, RestoreLag)) yield return s;   // CC70
        m.Wr(0x0744, 0);
        Prg.SwitchBank16(m, 9);
        foreach (var s in HudWindowSetup(m)) yield return s;     // CC7D JSR 19:BA65
        foreach (var s in Death.WaitFrame(m)) yield return s;
        for (int y = 0x3F; y >= 0; y--) m.Wr(0x6060 + y, 0xFF);
        for (int y = 0x2F; y >= 0; y--) m.Wr(0x6060 + y, m.Rd(0x6100 + y));   // attr_shadow
        foreach (var s in Pause.PpuQueueBlock(m, 3)) yield return s;
        Prg.SetR6(m, 0x0B);
        for (int y = 0; y < 0x100; y++) m.Wr(0x6000 + y, Prg.CpuRd(m, 0x9E00 + y));   // Wram6000InitTable 11:BE00
        m.Wr(0x2F, 0x3E);
        var yr = new Ret<int>();
        do
        {                                                        // blocks $3E-$43 (window pieces)
            foreach (var s in Pause.PpuQueueBlock(m, m.Rd(0x2F), yr)) yield return s;
            m.Wr(0x2F, (m.Rd(0x2F) + 1) & 0xFF);
        } while (m.Rd(0x2F) != 0x44);
        m.Wr(R.PpumaskShadow, m.Rd(R.PpumaskShadow) | 0x18);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        m.Wr(R.SubState, 8);
        if (res != null) res.V = yr.V;
    }

    // js: game/states/saveload.js restoreIfArea5F
    /// <summary>30:C8A9: JMP SubstRestoreScreen in area $5F, else RTS</summary>
    public static IEnumerable<Y> RestoreIfArea5F(Machine m)
    {
        if (m.Rd(R.AreaId) == 0x5F) foreach (var s in SubstRestoreScreen(m)) yield return s;
    }

    // js: game/states/saveload.js loadTail
    /// <summary>30:D930 + EquipMenuLoadTail 30:D942: after a menu LOAD (keep level / HP / max HP over ObjInit)</summary>
    public static IEnumerable<Y> LoadTail(Machine m)
    {
        m.Wr(SaveState, 0);
        m.Wr(R.PpumaskShadow, m.Rd(R.PpumaskShadow) & 0xE7);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        int lvl = m.Rd(0x0421), hp = m.Rd(0x03C1), hpMax = m.Rd(0x03C0);
        m.Wr(0x10, 1); m.Wr(0x11, 4); Obj.ObjInit.Init(m);
        m.Wr(0x10, 0); m.Wr(0x11, 6); Obj.ObjInit.Init(m);
        m.Wr(0x03C0, hpMax); m.Wr(0x03C1, hp); m.Wr(0x0421, lvl);
        m.Wr(0x07E8, 0);
        foreach (var s in Death.StatRecalc(m, 0)) yield return s;
        Prg.SwitchBank16(m, 9);
        foreach (var s in HudWindowSetup(m)) yield return s;
        m.Wr(0x6D, 0xFF);
        m.Wr(R.SubState, 1);
    }
}
