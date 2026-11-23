// re-c C# port — MainStateGameplay 30:CAB6 (main_state 1): the sub_state dispatcher (= web/src/game/states/play.js, s88).
// SubstateHandlerTable 30:CADE (ptr16 x 40) -> $10/$11, SubstateBankTable 30:CB2E -> SwitchBank16, JMP ($0010).
// Substates missing from Substates log once and do nothing. REVERSE.md "State Machine" + "Death / game over".
//
// s110: SubstPlay 30:CB62 (sub_state 8) = one gameplay frame:
//   ReadPads -> PlayDeathMenuCheck 30:CB84 (HP 0 -> sub 3; START -> PauseScreen 15:BC40; SELECT -> sub $12;
//   pad-2 debug pause / area cycler) -> PlayerStatusTick 31:EF55 -> ObjUpdateAll -> CameraTick (exit -> the
//   frame ends) -> sprite pass 28:800E -> ObjCollideAll 26:8F6E -> SubstPlayTail 31:E0B8.
// Handler convention (docs/architecture_cs.md §5.1): Func<Machine, IEnumerable<Y>?>; JS generator return values
// (the Y register) go through Ret<int> arguments.
using ReC.Game.Obj;
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class Play
{
    // js: game/states/play.js unported
    private static void Unported(Machine m, string what)
    {
        if (m.Unported.Add(what)) m.Log($"{what} not ported yet");
    }

    // js: game/states/play.js substPlay
    /// <summary>SubstPlay 30:CB62</summary>
    private static IEnumerable<Y> SubstPlay(Machine m)
    {
        m.LagStart = m.Frame;                                 // s215: lag model judges whole passes only
        int y = Pads.ReadPads(m);
        var r = new Ret<int?>();
        foreach (var s in PlayDeathMenuCheck(m, y, r)) yield return s;
        if (r.V == null) yield break;                         // PLA PLA: the frame ends here
        foreach (var s in SubstPlayBody(m, r.V.Value)) yield return s;
    }

    // js: game/states/play.js substPlayBody
    /// <summary>30:CB68-CB81: everything after PlayDeathMenuCheck; y = the Y register (PlaySound saves it in $010F)</summary>
    private static IEnumerable<Y> SubstPlayBody(Machine m, int y)
    {
        // s211: m.PassStage names the main-loop stage (= tools.emu.pass_cost's columns)
        foreach (var s in PlayerStatusTick(m, y)) yield return s;   // CB68
        m.PassStage = "upd";
        foreach (var s in Objects.ObjUpdateAll(m)) yield return s;   // CB6B
        m.PassStage = "cam";
        var exit = new Ret<bool>();
        foreach (var s in Camera.CameraTick(m, exit)) yield return s;   // CB6E (ExitLookup PLAs CameraTick's caller)
        if (exit.V) yield break;
        m.PassStage = "spr";
        Prg.SwitchBank16(m, 0x0E);                            // CB71
        Sprites.SpriteDraw(m);                                // CB76 JSR 28:800E
        Prg.SwitchBank16(m, 0x0D);                            // CB79
        m.PassStage = "col";
        foreach (var s in Collide.ObjCollideAll(m)) yield return s;   // CB7E JSR 26:8F6E
        SubstPlayTail(m);                                     // CB81 JMP 31:E0B8
        if (m.LagStart == m.Frame) m.LagPass = true;
    }

    // js: game/states/play.js playDeathMenuCheck
    /// <summary>PlayDeathMenuCheck 30:CB84. res.V = null when it PLAs SubstPlay's return address (the frame ends),
    /// else the Y register SubstPlay goes on with.</summary>
    private static IEnumerable<Y> PlayDeathMenuCheck(Machine m, int y0, Ret<int?> res)
    {
        res.V = y0;
        if (m.Rd(0x03C1) == 0) { m.Wr(R.SubState, 3); res.V = null; yield break; }   // player_hp = 0 -> death
        if ((m.Rd(R.PadAct) & 0x10) != 0)
        {                                                     // START
            PlayPaletteRestore(m);
            if (m.Rd(R.AreaId) == 0x5F) m.Wr(R.IrqMode, 0);
            Prg.SwitchBank16(m, 0x07);
            var py = new Ret<int>();
            foreach (var s in Pause.PauseScreen(m, py)) yield return s;   // CBA8 JSR 15:BC40
            if (m.Rd(R.AreaId) == 0x5F)
            {                                                 // CBB1 JMP (RTS -> CB68)
                var ry = new Ret<int>();
                foreach (var s in SaveLoad.SubstRestoreScreen(m, ry)) yield return s;
                res.V = ry.V;
                yield break;
            }
            res.V = py.V;
            yield break;
        }
        if ((m.Rd(R.PadNew) & 0x20) != 0) { m.Wr(R.SubState, 0x12); res.V = null; yield break; }   // SELECT -> menu
        if ((m.Rd(R.PadNew + 1) & 0x40) != 0)
        {                                                     // pad 2 B
            var dy = new Ret<int>();
            foreach (var s in SubstDebugPause(m, dy)) yield return s;
            res.V = dy.V;
            yield break;
        }
        if (m.Rd(R.PadPrev) == 0xC0 && (m.Rd(R.PadNew + 1) & 0x80) != 0)
        {                                                     // pad 1 A+B held + pad 2 A
            int i = m.Rd(0x0780) & 0x0F;
            m.Wr(R.AreaId, Prg.CpuRd(m, 0xCBEC + i));         // AreaByIndexTable (debug cycler)
            m.Wr(0x6D, 0);                                    // area_entry
            m.Wr(0x0780, (m.Rd(0x0780) + 1) & 0xFF);
            m.Wr(R.SubState, 1);
            res.V = null;
        }
    }

    // js: game/states/play.js substDebugPause
    /// <summary>SubstDebugPause 30:CBFC (pad 2 B freeze until pad 2 B again), then JMP ReadPads -> back into SubstPlay</summary>
    private static IEnumerable<Y> SubstDebugPause(Machine m, Ret<int> res)
    {
        do
        {
            foreach (var s in Death.WaitFrame(m)) yield return s;
            Prg.SwitchBank16(m, 0x0E); Sprites.SpriteDraw(m); Prg.SwitchBank16(m, 0x0D);   // FarCall_B14_800E
            Pads.ReadPadsMenu(m);
        } while ((m.Rd(R.PadNew + 1) & 0x40) == 0);
        m.Wr(R.SubState, 8);
        foreach (var s in Death.WaitFrames(m, 8)) yield return s;
        res.V = Pads.ReadPads(m);
    }

    private const int Obj1Flash = 0x0710, Accessory = 0x0716, Armor = 0x0713, Jump = 0x0620, Hp = 0x03C1, HpMax = 0x03C0;
    private const int Mp = 0x0708, MpMax = 0x0709, BgAttr = 0x0641, Ride = 0x0541, Invuln = 0x071A, FlagsS01 = 0x0381;

    // js: game/states/play.js playerStatusTick
    /// <summary>PlayerStatusTick 31:EF55-F0A3 (y = the Y register for PlaySound; X is $FF from ReadPads, 0 after damage)</summary>
    private static IEnumerable<Y> PlayerStatusTick(Machine m, int y)
    {
        int f = m.Rd(0x08);
        int x = 0xFF;
        // js: game/states/play.js status
        int Status() => m.Rd(Obj1Flash) & 0x0F;
        bool hit = false;
        if (((f << 1) & 0xFF) == 0 && Status() == 3) hit = (f & 0x1F) == 0;   // poison, every 128 frames
        else if (m.Rd(Accessory) != 0x13 && m.Rd(Jump) == 0 && ((m.Rd(0x0561) | m.Rd(BgAttr)) & 0x80) != 0)
            hit = (f & 0x1F) == 0;                            // damage floor
        else if (m.Rd(R.AreaId) == 0x1A && m.Rd(Accessory) != 0x0D) hit = (f & 7) == 0;
        if (hit)
        {                                                     // EF92
            int hp = m.Rd(Hp) - 4;
            m.Wr(Hp, hp < 0 ? 0 : hp);
            PlaySound.Play(m, 0x36, y);
            for (x = 3; x > 0; x--) m.Wr(0x6150 + x, 0x30);   // white BG flash; X ends 0
            foreach (var s in Death.WaitFrame(m)) yield return s;
            Prg.SetR6(m, 0x1A);
            foreach (var s in Death.HudHpBar(m)) yield return s;   // 26:8CC0 (keeps X)
            Death.PaletteBuildRom(m, 0);                      // 26:8C0E
        }
        Prg.SetR6(m, 0x1A);                                   // EFBC
        if (Status() == 1 && m.Rd(0x06C0) == 4)
        {                                                     // paralysis: no sword charge
            m.Wr(0x06C0, 0);
            PlaySound.Play(m, 0x31, y);
        }
        if (Status() == 2)
        {                                                     // EFDB stone
            if (m.Rd(0x05A0) == 0)
            {
                m.Wr(Obj1Flash, m.Rd(Obj1Flash) & 0xF0);
                m.Wr(0x07E4, 0);
                Death.PaletteBuildRom(m, 0);
            }
            else if (m.Rd(0x0712) == 5 && (m.Rd(R.PadNew) & 0x80) != 0 && m.Rd(R.SubState) == 8)
            {
                Prg.SetR6(m, 0x1A);
                // SpendMp 26:9AE8 ($18): MP 0 or short -> sub_state $1C and PLA PLA = return from PlayerStatusTick
                m.Wr(0x1F, 0x18);
                int mp = m.Rd(Mp);
                if (mp == 0 || mp < 0x18) { m.Wr(R.SubState, 0x1C); yield break; }
                m.Wr(Mp, mp - 0x18);
                foreach (var s in Death.HudRedraw(m, 4, x)) yield return s;
                m.Wr(R.SubState, 0x18);
            }
        }
        if (m.Rd(Accessory) == 0x11 && (m.Rd(R.PadDir) & 0x80) != 0 && (f & 0x3F) == 0 && m.Rd(Mp) != m.Rd(MpMax))
        {
            m.Wr(Mp, (m.Rd(Mp) + 1) & 0xFF);                  // F036 MP regen
            foreach (var s in Death.HudRedraw(m, 4, x)) yield return s;
        }
        if (m.Rd(Armor) == 8 && (m.Rd(R.PadDir) & 0x80) != 0 && m.Rd(HpMax) != m.Rd(Hp) && (f & 0x0F) == 0)
        {
            m.Wr(Hp, (m.Rd(Hp) + 1) & 0xFF);                  // F057 HP regen
            foreach (var s in Death.HudHpBar(m)) yield return s;
        }
        if (m.Rd(R.AreaId) == 0x52) m.Wr(0x04FE, (m.Rd(0x04FE) - 1) & 0xFF);
        if (m.Rd(Jump) == 0 && (m.Rd(BgAttr) & 1) != 0 && m.Rd(Ride) == 0)
        {                                                     // F068 pit fall
            m.Wr(R.SubState, 1);
            m.Wr(0x6D, (m.Rd(0x6D) & 0x1F) | 0x60);           // area_entry -> pit-fall entry
            m.Wr(BgAttr, 0);
        }
        int t = m.Rd(Invuln);                                 // F089
        if (t != 0)
        {
            m.Wr(Invuln, t - 1);
            m.Wr(FlagsS01, m.Rd(FlagsS01) | 0x04);
        }
        else m.Wr(FlagsS01, m.Rd(FlagsS01) & 0xFB);
    }

    // js: game/states/play.js substPlayTail
    /// <summary>SubstPlayTail 31:E0B8: BgChrAnimTick + every 16 frames slot $0D + ($08 >> 4): if empty, look for its area
    /// record entry with b1 b7 (respawning) -> AreaSpawnEntry, kept only when the new object is off screen.
    /// s213: m.Cyc += this path's exact ROM cycles (see play.js for the breakdown).</summary>
    private static void SubstPlayTail(Machine m)
    {
        if (m.Rd(0x07D7) != 0) return;                        // boss_cam_lock
        BgChrAnimTick(m);
        int f = m.Rd(0x08);
        if ((f & 0x0F) != 0) return;
        int slot = (f >> 4) + 0x0D;
        m.Wr(0x13, slot);
        if (m.Rd(0x04A0 + slot) != 0) { m.Cyc += 27; return; }
        Prg.SwitchBank16(m, 6);                               // E0D8 SwitchBank16_6
        int id = m.Rd(R.AreaId), t = (id & 0x80) != 0 ? 0x9301 : 0x9201, yy = (id << 1) & 0xFF;
        m.Cyc += 28 + 77 + 21 + (yy == 0xFE ? 1 : 0) + ((id & 0x80) != 0 ? 16 : 3) + 6;
        m.Wr(0x10, Prg.CpuRd(m, t + yy));
        m.Wr(0x11, Prg.CpuRd(m, t + yy + 1));
        if ((m.Rd(0x11) & 0x80) == 0) return;                 // no area record
        m.Cyc += 3;                                           // BPL not taken (-1) + LDY LDX 4
        int p = m.Rd(0x10) | m.Rd(0x11) << 8;
        // js: game/states/play.js pc
        int Pc(int yv) => ((p & 0xFF) + yv) > 0xFF ? 1 : 0;   // (zp),Y page cross
        for (int y = 5, x = 0x0D; x != 0x1D; y += 4, x++)
        {
            int b0 = Prg.CpuRd(m, p + y);
            if ((b0 & 0xF0) == 0xF0) { m.Cyc += 12 + Pc(y); return; }   // $Fx (FE/FF) ends the scan
            if (x != slot) { m.Cyc += (x == 0x1C ? 32 : 34) + Pc(y); continue; }
            int b1 = Prg.CpuRd(m, p + y + 1);
            if ((b1 & 0x80) == 0) { m.Cyc += 30 + Pc(y) + Pc(y + 1); return; }   // not a respawning entry
            m.PassStage = "colh";
            m.SpawnEntry(x, y);                               // E110 AreaSpawnEntry (s112)
            bool off = ObjOffscreen(m, x);
            m.PassStage = "col";
            m.Cyc += 29 + Pc(y) + Pc(y + 1) + 12 + (off ? 3 : 9);
            if (!off) m.Wr(0x04A0 + x, 0);                    // E113: on screen -> undo
            return;
        }
    }

    // js: game/states/play.js objOffscreen
    /// <summary>31:E12B: carry = slot x not on the camera's screen (x hi / y hi differ)</summary>
    private static bool ObjOffscreen(Machine m, int x)
    {
        int xb = m.Rd(0x70 + x) < m.Rd(0x02) ? 1 : 0;
        if (((m.Rd(0x90 + x) - m.Rd(0x03) - xb) & 0xFF) != 0) return true;
        int yb = m.Rd(0xB0 + x) < m.Rd(0x04) ? 1 : 0;
        return ((m.Rd(0xD0 + x) - m.Rd(0x05) - yb) & 0xFF) != 0;
    }

    // js: game/states/play.js bgChrAnimTick
    /// <summary>BgChrAnimTick 31:E75E: $62FE != 0 -> chr_r1_play = BgChrAnimTable 31:E779[$62FE*8 + (($08>>3)&amp;7)]</summary>
    private static void BgChrAnimTick(Machine m)
    {
        int b = (m.Rd(0x62FE) << 3) & 0xFF;
        m.Wr(0x10, b);
        if (b == 0) return;
        m.Wr(0x07F1, Prg.CpuRd(m, 0xE779 + (((m.Rd(0x08) >> 3) & 7) | b)));
    }

    // js: game/states/play.js substOutOfMp
    /// <summary>SubstOutOfMp 30:CC17 (sub_state 28): Area5FIrqOff, SFX $4E, sub_state $10 with pending message 32.14 (s151)</summary>
    public static IEnumerable<Y>? SubstOutOfMp(Machine m, int y = 0)
    {
        GiveItem.Area5FIrqOff(m);
        PlaySound.Play(m, 0x4E, y);                           // y = the Y register at the dispatch
        m.Wr(R.SubState, 0x10);
        m.Wr(0x06C3, 0x0E);
        m.Wr(0x06E3, 0x20);
        return null;
    }

    private static readonly Dictionary<int, Func<Machine, IEnumerable<Y>?>> Substates = new()
    {
        [0] = AreaEnter.SubstNewGameInit,                     // 30:CA5F (s108)
        [1] = AreaEnter.SubstAreaEnter,                       // 30:CA2E (s108)
        [3] = Death.PlayerDeath,                              // 19:B900
        [6] = GiveItem.ItemUseMenu,                           // 30:D497 ItemUseMenu + item hook 1 (s141)
        [7] = GiveItem.Give,                                  // 30:D3EB GiveItem + item hooks 0/2 (s139)
        [8] = SubstPlay,                                      // 30:CB62 (s110)
        [16] = GiveItem.ShowPendingMessage,                   // 30:D085 (s141)
        [12] = m => Pause.PauseScreen(m),                     // 15:BC40 (vestigial entry; play calls it directly, s94)
        [13] = m => Pause.PauseWaitEntry(m),                  // 15:BDDB (s142; unreachable)
        [17] = Talk.NpcTalk,                                  // 30:D0C8 NpcTalk + ShowMessage (s115)
        [18] = Equip.SubstEquipMenu,                          // 30:D8C7 SELECT menu (s98)
        [19] = Wake.SubstWakeCutscene,                        // 30:D9B6 -> 20:9400 new-game wake-up (s138)
        [20] = Teleport.SubstTeleportMenu,                    // 30:DB28 magic 4 teleport menu (s143)
        [21] = Telepathy.SubstTelepathySelect,                // 30:DD6B magic 3 telepathy + reply 30:DE04 (s145)
        [23] = ChangeForm.SubstChangeForm,                    // 30:DEC1 magic 7 change form (s146)
        [24] = Cure.SubstCureMagic,                           // 19:BB39 magic 5 / stone cure (s147)
        [25] = FormRevert.SubstFormRevert,                    // 19:BB9D magic press in a changed form (s148)
        [26] = SwordRaise.SwordRaisePose,                     // 19:BC04 event act 22 (s149)
        [27] = Mesia.SubstMesiaReunion,                       // 19:BC6B event act 30 (s150)
        [28] = m => SubstOutOfMp(m),                          // 30:CC17 SpendMp short of MP -> msg 32.14 (s151)
        [29] = Teleport.SubstTeleportMenu,                    // 30:DB28 2nd entry (never stored)
        [30] = FinalScene.SubstFinalSceneReset,               // 19:BDF2 AI $70 phase 10 -> ending (s152)
        [31] = m => SaveLoad.SubstRestoreScreen(m),           // 30:CC4D (s120; set by AI $70 phase 2)
        [32] = Shop.SubstArmorShop,                           // 30:D985 (s122; types $42 $48)
        [33] = Shop.SubstToolShop,                            // 30:D98E (s122; types $41 $44 $47)
        [34] = Shop.SubstInn,                                 // 30:D997 (s123; types $40 $45 $46)
        [35] = Shop.SubstPawnShop,                            // 30:D9AD (s121; keeper type $43)
        [36] = Shop.SubstShopEmpty,                           // 30:D9C1 (s124; AI $0E type $4E)
    };

    // js: game/states/play.js pauseFromPlay
    /// <summary>state entry at 15:BC40 from SubstPlay (30:CBA8 JSR, after PlayPaletteRestore / area $5F irq_mode = 0)</summary>
    private static IEnumerable<Y> PauseFromPlay(Machine m)
    {
        var y = new Ret<int>();
        foreach (var s in Pause.PauseScreen(m, y)) yield return s;
        if (m.Rd(R.AreaId) == 0x5F) foreach (var s in SaveLoad.SubstRestoreScreen(m, y)) yield return s;   // 30:CBB1 JMP
        foreach (var s in SubstPlayBody(m, y.V)) yield return s;   // 30:CBAF RTS -> SubstPlay goes on at CB68
    }

    /// <summary>a handler that may be a plain method (null) as a non-null iterator (ENTRY_POINTS values)</summary>
    private static IEnumerable<Y> Iter(IEnumerable<Y>? g)
    {
        if (g == null) yield break;
        foreach (var s in g) yield return s;
    }

    /// <summary>(play.js ENTRY_POINTS) Routines a tools/emu/web_state.py export may stop at that are NOT reached through
    /// the substate dispatch. Machine.LoadState starts `EntryPoints[s.Until](m)` as the pending iterator.</summary>
    public static readonly Dictionary<string, Func<Machine, IEnumerable<Y>>> EntryPoints = new()
    {
        ["15:BC40"] = PauseFromPlay,                          // START in SubstPlay: 30:CBA8 JSR PauseScreen
        // s98: a state stopped at a substate's first instruction is already past this frame's dispatch
        ["30:D8C7"] = m => Iter(Equip.SubstEquipMenu(m)),
        ["30:CC4D"] = m => SaveLoad.SubstRestoreScreen(m),    // s120 (restore5f.json)
        ["30:D9AD"] = m => Iter(Shop.SubstPawnShop(m)),       // s121
        ["30:D985"] = m => Iter(Shop.SubstArmorShop(m)),      // s122
        ["30:D98E"] = m => Iter(Shop.SubstToolShop(m)),       // s122
        ["30:D997"] = m => Iter(Shop.SubstInn(m)),            // s123
        ["30:D9C1"] = m => Iter(Shop.SubstShopEmpty(m)),      // s124
        ["30:D3EB"] = m => Iter(GiveItem.Give(m)),            // s139
        ["30:D497"] = m => Iter(GiveItem.ItemUseMenu(m)),     // s141
        ["30:D085"] = m => Iter(GiveItem.ShowPendingMessage(m)),   // s141
        ["30:DD6B"] = m => Iter(Telepathy.SubstTelepathySelect(m)),   // s145
        ["30:DEC1"] = m => Iter(ChangeForm.SubstChangeForm(m)),   // s146
        ["19:BB39"] = m => Iter(Cure.SubstCureMagic(m)),      // s147
        ["19:BB9D"] = m => Iter(FormRevert.SubstFormRevert(m)),   // s148
        ["19:BC04"] = m => Iter(SwordRaise.SwordRaisePose(m)),   // s149
        ["19:BC6B"] = m => Iter(Mesia.SubstMesiaReunion(m)),  // s150
        ["30:CC17"] = m => Iter(SubstOutOfMp(m)),             // s151 (one pass, no wait)
        ["19:BDF2"] = m => Iter(FinalScene.SubstFinalSceneReset(m)),   // s152
        // s203: event acts reached by ItemUseRun's JMP 30:D523; Y = $22 * 2
        ["30:D5A4"] = m => Iter(GiveItem.EventAction(m, 1, 2)),
        ["30:D5E6"] = m => Iter(GiveItem.EventAction(m, 7, 14)),
        // s204
        ["30:D654"] = m => Iter(GiveItem.EventAction(m, 8, 16)),
        ["30:D746"] = m => Iter(GiveItem.EventAction(m, 21, 42)),
        ["30:D791"] = m => Iter(GiveItem.EventAction(m, 23, 46)),
        // s205
        ["30:D5D6"] = m => Iter(GiveItem.EventAction(m, 2, 4)),
        ["30:D70D"] = m => Iter(GiveItem.EventAction(m, 15, 30)),
        ["30:D7FD"] = m => Iter(GiveItem.EventAction(m, 28, 56)),
        // s206
        ["30:D6D8"] = m => Iter(GiveItem.EventAction(m, 11, 22)),
        ["30:D6DD"] = m => Iter(GiveItem.EventAction(m, 12, 24)),
        ["30:D6E7"] = m => Iter(GiveItem.EventAction(m, 13, 26)),
        ["30:D718"] = m => Iter(GiveItem.EventAction(m, 19, 38)),
        ["30:D71F"] = m => Iter(GiveItem.EventAction(m, 20, 40)),
        // s207: talk acts entered through EventActionDispatch's JMP ($0010) redirected at 30:D560 (Y = act * 2)
        ["30:D1EB"] = m => Iter(Talk.TalkAction(m, 0x10, 32)),
        ["30:D18E"] = m => Iter(Talk.TalkAction(m, 0x14, 40)),
        ["30:D1B5"] = m => Iter(Talk.TalkAction(m, 0x13, 38)),
        ["30:D1CA"] = m => Iter(Talk.TalkAction(m, 0x16, 44)),
        ["30:D2AE"] = m => Iter(Talk.TalkAction(m, 0x19, 50)),
        ["30:D336"] = m => Iter(Talk.TalkAction(m, 0x1A, 52)),
    };

    // js: game/states/play.js playPaletteRestore
    /// <summary>PlayPaletteRestore 30:CC2E: slot 4 AI $1A (palette effect) -> clear it, PaletteBuild, irq_mode 2 -> 0</summary>
    public static void PlayPaletteRestore(Machine m)
    {
        if (m.Rd(0x04A4) != 0x1A) return;
        m.Wr(0x04A4, 0);
        Prg.SetR6(m, 0x1A);
        Death.PaletteBuildRom(m, 0);
        if (m.Rd(R.IrqMode) == 2) m.Wr(R.IrqMode, 0);
    }

    // js: game/states/play.js mainState1
    /// <summary>MainStateGameplay 30:CAB6</summary>
    public static IEnumerable<Y>? MainState1(Machine m)
    {
        int s = m.Rd(R.SubState);
        if (s != 8 && s != 7) PlayPaletteRestore(m);
        m.Wr(0x08, (m.Rd(0x08) - 1) & 0xFF);
        m.Wr(0x10, Prg.CpuRd(m, 0xCADE + s * 2));
        m.Wr(0x11, Prg.CpuRd(m, 0xCADF + s * 2));
        Prg.SwitchBank16(m, Prg.CpuRd(m, 0xCB2E + s));
        if (Substates.TryGetValue(s, out var fn)) return fn(m);
        Unported(m, $"sub_state {s} (30:CAB6)");
        return null;
    }
}
