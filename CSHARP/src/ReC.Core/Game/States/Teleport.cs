// re-c C# port — the teleport menu (= web/src/game/states/teleport.js, s143). REVERSE.md "ItemUseMenu" (Warp Boots,
// s135), substate table rows 20/29 and "TeleportAreaTable".
//
//   SubstTeleportMenu(m, y)  SubstTeleportMenu 30:DB28 (sub_state $14 = magic 4, $1D = never stored; also JSR'd by
//                            ItemUseMenu 30:D4DC for the Warp Boots with sub_state 6): refusals, the town menu, the pick
//                            30:DC70 and the warp 30:DCAE-DD33. See the JS. JS default `yIn = m.rd(0x010F)` -> overload.
using ReC.Game.Obj;
using ReC.Gen;

namespace ReC.Game.States;

public static class Teleport
{
    private const int Oam = 0x0200, AreaTable = 0xDC58;
    private const int Meta = 0x0300, MetaS01 = 0x0301, FlagsS01 = 0x0381, Active = 0x04A0, AnimS01 = 0x04E1, OverlayS01 = 0x0581;

    // js: game/states/teleport.js hideOam0
    /// <summary>30:C19F: OAM entry 0 = $F0 x 4</summary>
    private static void HideOam0(Machine m) { for (int i = 0; i < 4; i++) m.Wr(Oam + i, 0xF0); }

    // js: game/states/teleport.js testFlagF2F0
    /// <summary>TestFlagF2F0 30:DD58: flag F2F0 + a + 5; returns the AND result</summary>
    public static int TestFlagF2F0(Machine m, int a)
    {
        int n = (a + 5) & 0xFF;
        return m.Rd(0x64DE + (n >> 3)) & Prg.CpuRd(m, 0xC000 + (n & 7));
    }

    // js: game/states/teleport.js teleportMenuLocks
    /// <summary>TeleportMenuLocks 30:DD36: $6000-$6012 = $10, then PpuQueueBlock($20 + i) for every locked town i = 10..0</summary>
    private static IEnumerable<Y> TeleportMenuLocks(Machine m)
    {
        for (int x = 0x12; x >= 0; x--) m.Wr(0x6000 + x, 0x10);
        m.Wr(0x11, 0x0A);
        do
        {
            int i = m.Rd(0x11);
            if (TestFlagF2F0(m, i) == 0) foreach (var s in Pause.PpuQueueBlock(m, (i + 0x20) & 0xFF)) yield return s;
            m.Wr(0x11, (m.Rd(0x11) - 1) & 0xFF);
        } while ((m.Rd(0x11) & 0x80) == 0);
    }

    // js: game/states/teleport.js menuExit
    /// <summary>TeleportMenuExit 30:DC64</summary>
    private static void MenuExit(Machine m)
    {
        HideOam0(m);
        m.Wr(R.IrqMode, 0);
        m.Wr(R.SubState, 8);
    }

    /// <summary>(teleport.js MOVES) cursor moves 30:DBF4 / DC0C / DC26 / DC3E: OAM byte, temp, step, count, wrap</summary>
    private static readonly Dictionary<int, (int O, int T, int Step, int N, int Wrap)> Moves = new()
    {
        [4] = (0, 0x21, -0x10, 4, 0x40),                      // UP
        [5] = (0, 0x21, 0x10, 4, 0x40),                       // DOWN
        [6] = (3, 0x20, -0x50, 3, 0xF0),                      // LEFT
        [7] = (3, 0x20, 0x50, 3, 0xF0),                       // RIGHT
    };

    // js: game/states/teleport.js padNewButtonIndex
    /// <summary>PadNewButtonIndex 30:C210: index 0-7 (A B SEL START U D L R) of the single button in pad_new, else -1</summary>
    public static int PadNewButtonIndex(Machine m)
    {
        int p = m.Rd(R.PadNew);
        for (int y = 7; y >= 0; y--) if (p == Prg.CpuRd(m, 0xC222 + y)) return y;
        return -1;
    }

    public static IEnumerable<Y> SubstTeleportMenu(Machine m) => SubstTeleportMenu(m, m.Rd(0x010F));

    // js: game/states/teleport.js substTeleportMenu
    /// <summary>SubstTeleportMenu 30:DB28. yIn = Y at the dispatch (PlaySound($4E) saves it).</summary>
    public static IEnumerable<Y> SubstTeleportMenu(Machine m, int yIn)
    {
        if (m.Rd(0x03C1) == 0) { m.Wr(R.SubState, 8); yield break; }
        if (m.Rd(0x07D7) != 0 || (m.Rd(R.AreaId) & 0xF8) == 0x58 || (m.Rd(0x0710) & 0x0F) == 4)
        {
            PlaySound.Play(m, 0x4E, yIn);
            m.Wr(R.SubState, 8);
            yield break;
        }
        m.Wr(0x06C0, 0);                                      // DB4F orbit_level
        Prg.SetR6(m, 0x1A);
        Death.PaletteBuildRom(m, 0);                          // JSR 26:8C0E
        m.Wr(R.IrqMode, 0);
        m.Wr(0x07DE, 0x00);                                   // msg 33.0 (town list)
        m.Wr(0x07DF, 0x21);
        Prg.SetR6(m, 0x14);
        foreach (var s in Talk.TextWindowOpen(m)) yield return s;   // 20:8500
        foreach (var s in Talk.TextPrintBox(m)) yield return s;     // 20:8528
        foreach (var s in TeleportMenuLocks(m)) yield return s;     // 30:DD36
        foreach (var s in Death.WaitFrame(m)) yield return s;
        m.Wr(R.ChrR0Alt, 0x3C);
        m.Wr(R.ChrR0Alt + 1, 0x38);
        m.Wr(R.ChrR0Alt + 2, 0x5C);
        m.Wr(Oam + 3, 0x10);
        m.Wr(0x10, m.Rd(R.CamYLo) & 7);
        m.Wr(Oam, (0x1C - m.Rd(0x10)) & 0xFF);
        m.Wr(Oam + 2, 0);
        m.Wr(0x20, 0);
        m.Wr(0x21, 0);
        AreaEnter.FarSpriteDraw(m);
        m.Wr(R.IrqMode, 1);
        for (; ; )
        {                                                     // DBAC
            m.Wr(Oam + 1, (m.Rd(0x07FF) & 0x08) != 0 ? 0x3C : 0x3F);
            Pads.ReadPadsMenu(m);
            AreaEnter.FarSpriteDraw(m);
            Talk.OamClipTop(m);
            foreach (var s in Death.WaitFrame(m)) yield return s;
            int k = PadNewButtonIndex(m);
            if (k < 0) continue;
            if (k >= 1 && k <= 3) { MenuExit(m); yield break; }   // B / SELECT / START
            if (k >= 4)
            {
                var (o, t, step, n, wrap) = Moves[k];
                int a = m.Rd(Oam + o) + step;
                int y = m.Rd(t) + (step < 0 ? -1 : 1);
                if (y < 0) { y = n - 1; a += wrap; }
                else if (y == n) { y = 0; a -= wrap; }
                m.Wr(Oam + o, a & 0xFF);
                m.Wr(t, y);
                PlaySound.Play(m, 0x4B, y);                   // DBEC
                continue;
            }
            // TeleportMenuPick 30:DC70
            int idx = (m.Rd(0x21) * 3 + m.Rd(0x20)) & 0xFF;
            m.Wr(0x22, idx);
            if (idx == 0x0B) continue;                        // the cancel cell
            if (TestFlagF2F0(m, idx) == 0) continue;
            int area = Prg.CpuRd(m, AreaTable + idx);
            if (area == m.Rd(R.AreaId)) { MenuExit(m); yield break; }
            m.Wr(R.AreaId, area);
            m.Wr(0x6D, 0x40);                                 // area_entry $40 (SubstAreaEnter then runs SubstPlay at once)
            if (m.Rd(R.SubState) == 0x14)
            {                                                 // magic 4 (not the Warp Boots' sub_state 6)
                Prg.SetR6(m, 0x1A);
                foreach (var s in Player.SpendMp(m, 0x14, 0, new Ret<bool>())) yield return s;   // X = 0 (index 0 * 2)
            }
            // DCA9: LDY $56 / DEY / BNE = spin until the IRQ chain reached handler 1: one SPIN (frame_ready kept), see the JS
            yield return Y.Spin;
            foreach (var s in Warp(m)) yield return s;
            yield break;
        }
    }

    // js: game/states/teleport.js warp
    /// <summary>30:DCAE-DD33: warp-out, SubstAreaEnter, warp-in</summary>
    private static IEnumerable<Y> Warp(Machine m)
    {
        HideOam0(m);
        int meta = m.Rd(MetaS01), ovl = m.Rd(OverlayS01);
        m.Wr(MetaS01, 0xCE);
        m.Wr(OverlayS01, 0x4C);
        m.Wr(Meta, 0x4C);
        m.Wr(AnimS01, 0x3F);
        m.Wr(0x0BB1, 0);
        for (int x = 4; x < 0x20; x++) m.Wr(Active + x, 0);
        PlaySound.Play(m, 0x23, 0);                           // Y = 0 after the DCA9 loop
        m.Wr(0x07F2, 0x73);                                   // chr_r2_play
        m.Wr(R.IrqMode, 0);
        do
        {                                                     // DCE8: 64 frames
            AreaEnter.FarSpriteDraw(m);
            foreach (var s in Death.WaitFrame(m)) yield return s;
            m.Wr(AnimS01, (m.Rd(AnimS01) - 1) & 0xFF);
        } while ((m.Rd(AnimS01) & 0x80) == 0);
        m.Wr(MetaS01, 0x4C);
        m.Wr(Meta, 0x4C);
        AreaEnter.FarSpriteDraw(m);
        foreach (var s in AreaEnter.SubstAreaEnter(m)) yield return s;   // 30:CA2E (entry $40: one SubstPlay pass too)
        m.Wr(FlagsS01, m.Rd(FlagsS01) & 0xFB);
        m.Wr(OverlayS01, 0x4C);
        m.Wr(MetaS01, 0xCE);
        m.Wr(AnimS01, 0xC0);
        PlaySound.Play(m, 0x66, m.Rd(0x010F));                // Y after SubstAreaEnter: not tracked (approximation)
        do
        {                                                     // DD1D: 64 frames
            AreaEnter.FarSpriteDraw(m);
            foreach (var s in Death.WaitFrame(m)) yield return s;
            m.Wr(AnimS01, (m.Rd(AnimS01) + 1) & 0xFF);
        } while (m.Rd(AnimS01) != 0);
        m.Wr(OverlayS01, ovl);
        m.Wr(MetaS01, meta);
        foreach (var s in Death.StatRecalc(m)) yield return s;
        AreaEnter.FarSpriteDraw(m);                           // JMP FarCall_B14_800E
    }
}
