// re-c C# port — the save / load page of the SELECT menu and the save module (= web/src/game/states/saveload.js, s102).
// REVERSE.md "Save / load module, bank 23 `$BC00`" + "Save / load page (web port, session 102)".
//
//   MenuExitFade(m)      30:C80C (the $7000 = $FB / $AB exit: fade out, 30 frames, fade in)
//   LoadTail(m)          30:D930 + EquipMenuLoadTail 30:D942 (after a menu LOAD)
//   HudWindowSetup(m)    19:BA65 (HUD window rebuild; also used by states 2/3/8 + SubstRestoreScreen)
//   SubstRestoreScreen(m, res)  SubstRestoreScreen 30:CC4D (sub_state $1F; area $5F redraw, s120)
//   SaveGame / LoadGame / LoadGameAndInit   23:BC09 / 23:BC8E / 23:BCDA (bank 23 at $A000)
//   SaveLoadPage(m)      SaveLoadPage 16:9AEE + SaveLoadLoop (START in the SELECT menu; C# s5, with states/equip.js)
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

    private const int Mode = 0x646A;         // b0: 0 = SAVE, 1 = LOAD
    private const int Slot = 0x646B;         // save_slot_sel b0

    // js: game/states/saveload.js slotPicture
    /// <summary>16:88A8: 3x3 picture at column $26 / row $27 of the $2800 window, tiles $29.. ($FF = blank 16:892A)</summary>
    public static IEnumerable<Y> SlotPicture(Machine m)
    {
        int c = (m.Rd(0x26) << 3) & 0xFF, r = m.Rd(0x27);
        for (int i = 0; i < 3; i++) { int cb = r & 1; r >>= 1; c = (c >> 1) | (cb << 7); }
        m.Wr(0x26, c); m.Wr(0x27, r);
        m.Wr(0x20, c); m.Wr(0x21, (r + 0x28) & 0xFF);
        m.Wr(0x24, 3); m.Wr(0x25, 0);
        bool blank = m.Rd(0x29) == 0xFF;
        if (blank) m.Wr(0x29, 0);
        for (int row = 0; row < 3; row++)
        {                                                 // 88D9 / 8905 x 3
            foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
            for (int x = 0; x < 3; x++)
            {
                int t = m.Rd(0x29);
                m.Wr(0x6000 + x, blank ? Prg.CpuRd(m, 0x892A + t) : t);
                m.Wr(0x29, (t + 1) & 0xFF);
            }
            Equip.MenuFlush(m);
            int v = m.Rd(0x20) + 0x20;
            m.Wr(0x20, v & 0xFF);
            m.Wr(0x21, (m.Rd(0x21) + (v >> 8)) & 0xFF);
        }
    }

    // js: game/states/saveload.js pageSetup
    /// <summary>16:9D52: hide OAM, strings $0F $13 $35, CHR alt $3C/$38, 6 x frame rows (X = 9) from $28C0</summary>
    private static IEnumerable<Y> PageSetup(Machine m)
    {
        foreach (var s in Equip.HideOam(m)) yield return s;
        foreach (int str in new[] { 0x0F, 0x13, 0x35 }) foreach (var s in Equip.MenuPrint(m, str)) yield return s;
        foreach (var s in Death.WaitFrame(m)) yield return s;
        m.Wr(0x58, 0x3C); m.Wr(0x59, 0x38);
        m.Wr(0x21, 0x28); m.Wr(0x20, 0xC0); m.Wr(0x25, 0); m.Wr(0x24, 0x40);
        m.Wr(0x19, 6);
        do
        {
            foreach (var s in Equip.FrameRows(m, 9)) yield return s;
            m.Wr(0x19, (m.Rd(0x19) - 1) & 0xFF);
        } while (m.Rd(0x19) != 0);
    }

    // js: game/states/saveload.js modeCursor
    /// <summary>16:9C85 (X = 0 SAVE / 1 LOAD): the mode cursor (shape 3, tables 16:9D9C-9DA3) at OAM slot $39 + SFX $4B</summary>
    private static void ModeCursor(Machine m, int x)
    {
        m.Wr(0x60FB, Prg.CpuRd(m, 0x9D9C + x));
        m.Wr(0x60F8, Prg.CpuRd(m, 0x9D9E + x));
        m.Wr(0x60F9, Prg.CpuRd(m, 0x9DA2 + x));
        m.Wr(0x60FA, Prg.CpuRd(m, 0x9DA0 + x));
        m.Wr(0x12, 3); m.Wr(0x2E, 0x39);
        Equip.MenuSprDraw(m);
        Equip.MenuSnd(m, 0x4B);
    }

    // js: game/states/saveload.js slotCursor
    /// <summary>16:9CAE: the two slot arrows (OAM $027C / $0280, tile $FE, y 16:9CDE[$646B &amp; 1]) + SFX $4B</summary>
    private static void SlotCursor(Machine m)
    {
        int y = Prg.CpuRd(m, 0x9CDE + (m.Rd(Slot) & 1));
        m.Wr(0x0280, y); m.Wr(0x027C, y);
        m.Wr(0x0281, 0xFE); m.Wr(0x027D, 0xFE);
        m.Wr(0x0282, 0x00); m.Wr(0x027E, 0x40);
        m.Wr(0x0283, 0x30); m.Wr(0x027F, 0xD0);
        Equip.MenuSnd(m, 0x4B);
    }

    // js: game/states/saveload.js menuSave
    /// <summary>MenuSave 16:9BE1: back.V = true to go back to SaveLoadLoop (confirm = NO), false = SaveLoadExit</summary>
    private static IEnumerable<Y> MenuSave(Machine m, Ret<bool> back)
    {
        m.Wr(SaveState, 0xFB);
        int msg = 0;
        if ((Prg.CpuRd(m, 0xBF00 + m.Rd(R.AreaId)) & 1) == 0) msg = 0x16;   // "Cannot save!"
        else if ((m.Rd(0x0710) & 0x0F) != 0) msg = 0x38;                    // player status
        if (msg != 0)
        {
            Equip.MenuSnd(m, 0x4E);
            foreach (var s in Equip.MenuPrint(m, 0x0F)) yield return s;
            foreach (var s in Equip.MenuPrint(m, msg)) yield return s;
            foreach (var s in Wait48(m)) yield return s;  // 9BFD
            m.Wr(R.PadNew, 0);
            m.Wr(SaveState, 0xAB);
            back.V = false;
            yield break;
        }
        foreach (var s in Equip.MenuPrint(m, 0x37)) yield return s;
        foreach (var s in Equip.MenuPrint(m, 0x0E)) yield return s;
        var no = new Ret<bool>();
        foreach (var s in Equip.Confirm(m, 0xA1, no)) yield return s;
        if (no.V)
        {
            foreach (var s in Equip.MenuPrint(m, 0x0F)) yield return s;
            back.V = true;
            yield break;
        }
        int sl = m.Rd(Slot) & 1;
        SaveGame(m);
        SaveSlotCopy(m, sl != 0 ? 6 : 4);
        SaveSlotCopy(m, sl != 0 ? 7 : 5);
        int c = m.Rd(0x70F5);
        m.Wr(sl != 0 ? 0x70F2 : 0x70F0, c); m.Wr(sl != 0 ? 0x70F3 : 0x70F1, c);
        foreach (var s in Busy(m, SaveLag)) yield return s;
        back.V = false;
    }

    // js: game/states/saveload.js saveLoadPage
    /// <summary>SaveLoadPage 16:9AEE + SaveLoadLoop 16:9B3A (returns at SaveLoadExit 16:9C7A)</summary>
    public static IEnumerable<Y> SaveLoadPage(Machine m)
    {
        m.Wr(SaveState, 0); m.Wr(Mode, 0); m.Wr(Slot, 0);
        foreach (var s in PageSetup(m)) yield return s;
        ModeCursor(m, 0);
        SlotCursor(m);
        foreach (var s in Equip.MenuPrint(m, 0x39)) yield return s;
        foreach (var s in Equip.MenuPrint(m, 0x17)) yield return s;
        m.Wr(0x19, 0);
        do
        {                                                 // 9B14: the SAVE / LOAD pictures
            int x = m.Rd(0x19);
            m.Wr(0x26, Prg.CpuRd(m, 0x9D8D + x)); m.Wr(0x27, Prg.CpuRd(m, 0x9D92 + x)); m.Wr(0x29, Prg.CpuRd(m, 0x9D97 + x));
            foreach (var s in SlotPicture(m)) yield return s;
            m.Wr(0x19, (m.Rd(0x19) + 1) & 0xFF);
        } while (((m.Rd(0x19) - 2) & 0x80) != 0);
        foreach (var s in Equip.MenuPrint(m, 0x14)) yield return s;
        foreach (var s in Equip.MenuPrint(m, 0x15)) yield return s;
        var back = new Ret<bool>();
        var no = new Ret<bool>();
        for (; ; )
        {                                                 // SaveLoadLoop
            m.Wr(SaveState, 0);
            foreach (var s in Death.WaitFrame(m)) yield return s;
            Pads.ReadPadsMenu(m);
            int p = m.Rd(R.PadNew);
            if ((p & 0x30) != 0) yield break;             // SELECT / START
            if ((p & 0x03) != 0)
            {                                             // LEFT / RIGHT: SAVE <-> LOAD
                m.Wr(Mode, (m.Rd(Mode) + 1) & 0xFF);
                ModeCursor(m, m.Rd(Mode) & 1);
                continue;
            }
            if ((p & 0x0C) != 0)
            {                                             // UP / DOWN: slot (re-print the old slot's row)
                foreach (var s in Equip.MenuPrint(m, (m.Rd(Slot) & 1) != 0 ? 0x39 : 0x3A)) yield return s;
                m.Wr(Slot, (m.Rd(Slot) + 1) & 0xFF);
                SlotCursor(m);
                continue;
            }
            if ((p & 0x40) != 0) { m.Wr(SaveState, 0xAB); yield break; }   // B
            if ((p & 0x80) == 0) continue;
            if ((m.Rd(Mode) & 1) == 0)
            {                                             // A on SAVE
                foreach (var s in MenuSave(m, back)) yield return s;
                if (back.V) continue;
                yield break;
            }
            foreach (var s in Equip.MenuPrint(m, 0x36)) yield return s;   // A on LOAD: "Load this game?"
            foreach (var s in Equip.MenuPrint(m, 0x0E)) yield return s;
            foreach (var s in Equip.Confirm(m, 0xA1, no)) yield return s;
            if (no.V)
            {
                foreach (var s in Equip.MenuPrint(m, 0x0F)) yield return s;
                continue;
            }
            SaveSlotCopy(m, (m.Rd(Slot) & 1) != 0 ? 2 : 0);
            foreach (var s in Busy(m, LoadLag)) yield return s;   // the NMI falls inside SaveSlotCopy
            LoadGame(m);
            m.Wr(SaveState, 0xFA);
            yield break;
        }
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
