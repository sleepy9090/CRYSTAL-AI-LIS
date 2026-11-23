// re-c C# port — GiveItem 30:D3EB (sub_state 7) + the item hooks it runs (= web/src/game/states/giveitem.js, s139).
// REVERSE.md "GiveItem / item-use dispatch", "Item hooks 0/1", "Event triggers — ItemHook2 14:80E3 / table 15:A17A".
//
//   Give(m)                GiveItem 30:D3EB: item id = obj_spawn_param[$0623] -> $07DC.
//                            id >= $80 -> 30:D539 (invisible event trigger: sub_state += 2, $08++, ItemEventHook(2),
//                                         EventActionDispatch, sub_state 8 and JMP MainStateGameplay = SubstPlay
//                                         runs in the same frame)
//                            id  = $70 -> TrapChest 30:D3DA (chest slot -> type $73, sub_state 8)
//                            else      -> ItemEventHook(0) (ItemGetHook 14:826F); full -> GiveItemFull 30:D47C;
//                                         else remap $50-$6F, SFX $3F / $93, msg 32.12, slot off, EventActionDispatch
//   ItemEventHook(m, n)    ItemEventHook 30:D834 (hooks 0-2; hook 1 = ItemUseHook 14:831E + handlers, s141)
//   ItemUseMenu(m)         ItemUseMenu 30:D497 (sub_state 6, s141; the Warp Boots' SubstTeleportMenu = Teleport)
//   ShowPendingMessage(m)  ShowPendingMessage 30:D085 (sub_state $10, s141; ItemUseFail "Nothing happens.")
//   EventActionDispatch(m) 30:D552 through EventActionTable 30:D563[$22] (all 32 entries)
//
// The SwordPowerLevel 30:D9D8 call after a pickup (30:D408) discards its Y result: $0719 is only written by the
// equip menu (30:D8FD). Its one lasting effect is $10 = sword - 1 (when a sword and a ball are equipped).
// JS PLA (act 30 drops the caller's return address) = the `pla` result flag of the event actions.
using ReC.Game.Obj;
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class GiveItem
{
    private const int TalkSlot = 0x0623, SpawnP = 0x0560, Active = 0x04A0, CurItem = 0x07DC, Msg = 0x07DE, Group = 0x07DF;
    private const int Inv = 0x6430;

    // js: game/states/giveitem.js unported
    private static void Unported(Machine m, string what)
    {
        if (m.Unported.Add(what)) m.Log($"{what} not ported yet");
    }

    // js: game/states/giveitem.js inc
    private static void Inc(Machine m, int a) => m.Wr(a, (m.Rd(a) + 1) & 0xFF);

    // ---------------------------------------------------------------- bank 14 hooks
    private static readonly int[] CrystalisClear = [0x6431, 0x6432, 0x6433, 0x643C, 0x643D, 0x643E, 0x643F];

    // js: game/states/giveitem.js invStoreItem
    /// <summary>InvStoreItem 14:82A0 ($24 = get record, $29 = store value). Returns the Y register.</summary>
    private static int InvStoreItem(Machine m)
    {
        int x = Prg.IndY(m, 0x24, 0), y = Prg.IndY(m, 0x24, 1);
        if ((y & 0x80) != 0)
        {                                                 // fixed equip slot
            m.Wr(0x23, m.Rd(0x23) & 0x7F);
            int v = m.Rd(0x29);
            if (v == 4)
            {                                             // 82B7 Crystalis
                m.Wr(Inv, 4);
                m.Wr(0x0711, 5);
                foreach (int a in CrystalisClear) m.Wr(a, 0xFF);
                m.Wr(0x6428, 0);
                return y;
            }
            m.Wr(Inv + x, v);                             // 82DE
            if (v == 6 || v == 8 || v == 0x0A || v == 0x0C)
            {
                if (((v - 4) >> 1) == m.Rd(0x0718)) { m.Wr(0x642B, 0xFF); m.Wr(0x0718, 0); }   // bracelet unequips the ball
            }
            return y;
        }
        do
        {                                                 // 8308: first $FF slot of start..start+count
            if (m.Rd(Inv + x) == 0xFF)
            {
                m.Wr(Inv + x, m.Rd(0x29));
                m.Wr(0x23, 0);
                return y;
            }
            x = (x + 1) & 0xFF;
            y = (y - 1) & 0xFF;
        } while (y != 0);
        return y;                                         // full: $23 b7 stays set
    }

    // js: game/states/giveitem.js itemGetHook
    /// <summary>ItemGetHook 14:826F ($23 = item; chains the record's `next` item). Returns the Y register.</summary>
    private static int ItemGetHook(Machine m)
    {
        for (; ; )
        {
            int item = m.Rd(0x23);
            m.Wr(0x23, item | 0x80);
            int x = (item << 1) & 0xFF;
            m.Wr(0x24, Prg.CpuRd(m, 0x9B00 + x));         // ItemGetTable
            m.Wr(0x25, Prg.CpuRd(m, 0x9B01 + x));
            m.Wr(0x29, Prg.CpuRd(m, 0x9D66 + item));      // ItemStoreValue
            int y = InvStoreItem(m);
            if ((m.Rd(0x23) & 0x80) != 0) return y;
            y = Talk.FlagApply(m, Talk.NpcResult(m, 2));
            int next = Prg.IndY(m, 0x24, y);
            if ((next & 0x80) != 0) return y;
            m.Wr(0x23, next);
        }
    }

    // js: game/states/giveitem.js itemHook2
    /// <summary>ItemHook2 14:80E3: EventTriggerTable 15:A17A[id &amp; $7F]; all conditions true -> result + set-list. Returns Y.</summary>
    private static int ItemHook2(Machine m)
    {
        int x = (m.Rd(0x23) << 1) & 0xFF;                 // the CMP #$86 / BCC is a no-op
        m.Wr(0x23, 0xFF);
        m.Wr(0x24, Prg.CpuRd(m, 0xA17A + x));
        m.Wr(0x25, Prg.CpuRd(m, 0xA17B + x));
        int y = 0;
        bool ok;
        do
        {
            (ok, y) = Talk.FlagTest(m, y);
            if (!ok) { m.Wr(0x20, 0); m.Wr(0x21, 0); return y; }
        } while ((m.Rd(0x26) & 0x80) == 0);
        m.Wr(0x23, 0);
        return Talk.FlagApply(m, Talk.NpcResult(m, y));   // 810F JSR NpcResult, falls into NpcFlagApply
    }

    // js: game/states/giveitem.js invRemoveItem
    /// <summary>InvRemoveItem 14:8354 ($23 = item). ROM quirk: the shift loop never INXes, so it only rewrites slot X
    /// (from X+1, Y-1 times) and then stores $FF there: the later slots are NOT shifted down. Returns Y.</summary>
    private static int InvRemoveItem(Machine m)
    {
        int it = m.Rd(0x23), t = (it << 1) & 0xFF;
        m.Wr(0x24, Prg.CpuRd(m, 0x9B00 + t));             // ItemGetTable
        m.Wr(0x25, Prg.CpuRd(m, 0x9B01 + t));
        int x = Prg.IndY(m, 0x24, 0), y = Prg.IndY(m, 0x24, 1);
        while (m.Rd(Inv + x) != it)
        {
            x = (x + 1) & 0xFF;
            y = (y - 1) & 0xFF;
            if (y == 0) return 0;                         // 8376: not found
        }
        for (; y != 1; y = (y - 1) & 0xFF) m.Wr(Inv + x, m.Rd(Inv + 1 + x));
        m.Wr(Inv + x, 0xFF);
        m.Wr(0x642A + (x >> 3), 0xFF);
        m.Wr(0x0715, 0);
        return y;
    }

    // ItemUseHandlers 14:8399[item] (hook 1). Each takes/returns Y (index into the ItemUseTable record at $24).

    // js: game/states/giveitem.js useFail
    private static int UseFail(Machine m, int y) { m.Wr(0x23, 0xFF); return y; }   // 84DB

    // js: game/states/giveitem.js useTouchMatch
    /// <summary>UseTouchMatch 14:8497: record param/b2 == the touched spawn ($0643) -> (match, Y)</summary>
    private static (bool Ok, int Y) UseTouchMatch(Machine m, int y)
    {
        int x = m.Rd(0x0643);
        int p = Prg.IndY(m, 0x24, y); y = (y + 1) & 0xFF;
        if (p != m.Rd(SpawnP + x)) return (false, y);
        int b = Prg.IndY(m, 0x24, y); y = (y + 1) & 0xFF;
        return (b == m.Rd(0x0540 + x), y);
    }

    // js: game/states/giveitem.js useTouchOrFail
    private static int UseTouchOrFail(Machine m, int y)
    {                                                     // 8491
        var (ok, y2) = UseTouchMatch(m, y);
        return ok ? y2 : UseFail(m, y2);
    }

    // js: game/states/giveitem.js useFindSlotObj
    /// <summary>UseFindSlotObj 14:8469: an on-screen object in 0D-1F with the record's param/b2</summary>
    private static int UseFindSlotObj(Machine m, int y)
    {
        m.Wr(0x26, Prg.IndY(m, 0x24, y)); y = (y + 1) & 0xFF;
        m.Wr(0x27, Prg.IndY(m, 0x24, y)); y = (y + 1) & 0xFF;
        for (int x = 0x0D; x < 0x20; x++)
        {
            if ((m.Rd(0x0380 + x) & 0x80) != 0) continue;
            if (m.Rd(SpawnP + x) == m.Rd(0x26) && m.Rd(0x0540 + x) == m.Rd(0x27)) return y;
        }
        return UseFail(m, y);
    }

    // js: game/states/giveitem.js useBowCheck
    private static int UseBowCheck(Machine m, int y)
    {                                                     // 8451 (UseFail inside UseFindSlotObj returns here)
        y = UseFindSlotObj(m, y);
        int l = m.Rd(0x07D7);
        return l == 0 || (l & 0x80) != 0 ? UseFail(m, y) : y;
    }

    // js: game/states/giveitem.js useStatus
    private static int UseStatus(Machine m, int y, int want)
    {                                                     // 8524/852F/853A/854A -> 8553
        if ((m.Rd(0x0710) & 0x0F) != want) return UseFail(m, y);
        if (want == 2) m.Wr(0x05A0, 0);                   // Fruit of Lime also clears obj_freeze
        m.Wr(0x0710, m.Rd(0x0710) & 0xF0);
        m.Wr(0x2F, y);
        PlaySound.Play(m, 0x25, y);
        return y;
    }

    // js: game/states/giveitem.js useTouchList
    private static int UseTouchList(Machine m, int[] ys)
    {                                                     // 84A9 / 859E: try records at Y = 0, 6, 12, ...
        for (int i = 0; i < ys.Length - 1; i++) { var (ok, y) = UseTouchMatch(m, ys[i]); if (ok) return y; }
        return UseTouchOrFail(m, ys[^1]);
    }

    private static readonly int[] TouchList4 = [0, 6, 0x0C, 0x12], TouchList2 = [0, 6];

    // USE_HANDLERS entries (anonymous JS arrows, no inventory entries)
    private static int UseHerb(Machine m, int y)
    {                                                     // Medical Herb: touch match, else HP +32
        var (ok, y2) = UseTouchMatch(m, y);
        if (ok) return y2;
        y = 6;
        int a = 0x20 + m.Rd(0x03C1);
        if (a > 0xFF) a = 0xFF;
        if (a >= m.Rd(0x03C0)) a = m.Rd(0x03C0);
        m.Wr(0x03C1, a);
        m.Wr(0x2F, y);
        PlaySound.Play(m, 0x25, y);
        return y;
    }

    private static int UsePower(Machine m, int y)
    {                                                     // Fruit of Power: MP +32
        int a = m.Rd(0x0708) + 0x20;
        if (a > 0xFF) a = 0xFF;
        if (a >= m.Rd(0x0709)) a = m.Rd(0x0709);
        m.Wr(0x0708, a);
        return y;
    }

    private static int UseInsectFlute(Machine m, int y)
    {
        y = (y + 1) & 0xFF;
        if (m.Rd(R.AreaId) != 0x1A || (m.Rd(0x038D) & 0x80) != 0 || m.Rd(0x04AD) == 0 || m.Rd(0x70) < 0x0A) return UseFail(m, y);
        m.Wr(0x04AD, 0xE2);
        return y;
    }

    private static int UseAreaByte(Machine m, int y)
    {                                                     // area byte
        int a = Prg.IndY(m, 0x24, y);
        m.Wr(0x26, a);
        y = (y + 1) & 0xFF;
        return a == m.Rd(R.AreaId) ? y : UseFail(m, y);
    }

    private static int UseShellFlute(Machine m, int y)
    {                                                     // Shell Flute: areas $60/$64/$68 + flag
        int area = m.Rd(R.AreaId);
        if (area != 0x60 && area != 0x64 && area != 0x68) return UseFail(m, y);
        var (ok, y2) = Talk.FlagTest(m, y);
        return ok ? y2 : UseFail(m, y2);
    }

    private static int UseLamp(Machine m, int y)
    {                                                     // Glowing Lamp: Broken Statue $38 -> Statue of Gold $3A
        for (int x = 0x18; x >= 0; x--)
        {
            if (m.Rd(0x643F + x) == 0x38) { m.Wr(0x643F + x, 0x3A); PlaySound.Play(m, 0x68, y); return y; }
        }
        return UseFail(m, y);
    }

    private static int Use84D0(Machine m, int y)
    {
        var (ok, y2) = UseTouchMatch(m, y);
        if (!ok) return UseFail(m, y2);
        m.Wr(0x62F2, 0xFF);
        return y2;
    }

    private static int UseBowOfTruth(Machine m, int y)
    {                                                     // Bow of Truth
        y = UseBowCheck(m, y);
        return m.Rd(0x060D) == 0 || m.Rd(R.AreaId) != 0xA6 ? UseFail(m, y) : y;
    }

    private static readonly Dictionary<int, Func<Machine, int, int>> UseHandlers = new()
    {
        [0x84DB] = UseFail,
        [0x8491] = UseTouchOrFail,
        [0x8564] = (m, y) => y,                           // UseWarpBoots: RTS (consumed)
        [0x8450] = (m, y) => y,                           // item $26: RTS
        [0x84E0] = UseHerb,
        [0x8507] = UsePower,
        [0x851D] = (m, y) => { m.Wr(0x0708, m.Rd(0x0709)); return y; },   // Magic Ring
        [0x8524] = (m, y) => UseStatus(m, y, 3),
        [0x852F] = (m, y) => UseStatus(m, y, 1),
        [0x853A] = (m, y) => UseStatus(m, y, 2),
        [0x854A] = (m, y) => UseStatus(m, y, 4),
        [0x8565] = (m, y) => UseTouchOrFail(m, y),        // 8565 (same as 8491: mismatch -> 8579 UseFail)
        [0x84B3] = UseInsectFlute,
        [0x859E] = (m, y) => UseTouchList(m, TouchList4),
        [0x84A9] = (m, y) => UseTouchList(m, TouchList2),
        [0x845F] = UseAreaByte,
        [0x856B] = UseShellFlute,
        [0x8585] = UseLamp,
        [0x84D0] = Use84D0,
        [0x8439] = (m, y) => m.Rd(R.AreaId) == 0x62 ? UseFail(m, y) : UseTouchOrFail(m, y),
        [0x8451] = UseBowCheck,
        [0x8442] = UseBowOfTruth,
    };

    // js: game/states/giveitem.js itemUseHook
    /// <summary>ItemUseHook 14:831E ($23 = item). Returns the Y register.</summary>
    private static int ItemUseHook(Machine m)
    {
        int it = m.Rd(0x23), x = (it << 1) & 0xFF;
        m.Wr(0x24, Prg.CpuRd(m, 0x9BE2 + x));             // ItemUseTable
        m.Wr(0x25, Prg.CpuRd(m, 0x9BE3 + x));
        m.Wr(0x26, Prg.CpuRd(m, 0x8399 + x));             // ItemUseHandlers
        m.Wr(0x27, Prg.CpuRd(m, 0x839A + x));
        int h = m.Rd(0x26) | m.Rd(0x27) << 8;
        int y;
        if (UseHandlers.TryGetValue(h, out var fn)) y = fn(m, 0);
        else { Unported(m, $"ItemUseHandler 14:{h:X}"); y = UseFail(m, 0); }
        if (m.Rd(0x23) == 0xFF) return y;
        y = Talk.FlagApply(m, Talk.NpcResult(m, y));
        if (m.Rd(0x23) == 0x36) return y;                 // Shell Flute is kept
        return InvRemoveItem(m);
    }

    private static readonly Func<Machine, int>[] Hooks = [ItemGetHook, ItemUseHook, ItemHook2];

    // js: game/states/giveitem.js area5FIrqOff
    /// <summary>Area5FIrqOff 30:C89E</summary>
    public static void Area5FIrqOff(Machine m) { if (m.Rd(R.AreaId) == 0x5F) m.Wr(R.IrqMode, 0); }

    // js: game/states/giveitem.js itemEventHook
    /// <summary>ItemEventHook 30:D834 (A = hook). res.V = the Y register after it.</summary>
    public static IEnumerable<Y> ItemEventHook(Machine m, int n, Ret<int> res)
    {
        m.Wr(0x23, m.Rd(SpawnP + m.Rd(TalkSlot)));
        Prg.SwitchBank16(m, 7);
        m.Wr(0x10, 0x4C);
        m.Wr(0x11, Prg.CpuRd(m, 0xD885 + n * 2));
        m.Wr(0x12, Prg.CpuRd(m, 0xD886 + n * 2));
        if (n >= Hooks.Length) { Unported(m, $"ItemEventHook({n})"); m.Wr(0x23, 0xFF); m.Wr(R.SubState, 8); res.V = 0; yield break; }
        int y = Hooks[n](m);
        if ((m.Rd(0x23) & 0x80) == 0 && (m.Rd(0x20) | m.Rd(0x21)) != 0)
        {
            int a22 = m.Rd(0x22), a23 = m.Rd(0x23);
            Play.PlayPaletteRestore(m);
            Area5FIrqOff(m);
            PlaySound.Play(m, 0x29, y);
            foreach (var s in Talk.ShowMessagePacked(m)) yield return s;
            foreach (var s in Death.StatRecalc(m, 0)) yield return s;   // X = 0 after ShowMessage's OamClipTop loop
            foreach (var s in SaveLoad.RestoreIfArea5F(m)) yield return s;
            m.Wr(0x23, a23);
            m.Wr(0x22, a22);
        }
        m.Wr(R.SubState, 8);
        res.V = y;
    }

    // ---------------------------------------------------------------- EventActionTable 30:D563

    // js: game/states/giveitem.js giveItemA
    /// <summary>GiveItemA 30:D22B: A -> $057F, GiveItem with $0623 = $1F (restored after)</summary>
    private static IEnumerable<Y> GiveItemA(Machine m, int a)
    {
        m.Wr(0x057F, a);
        int s = m.Rd(TalkSlot);
        m.Wr(TalkSlot, 0x1F);
        foreach (var q in Give(m)) yield return q;
        m.Wr(TalkSlot, s);
    }

    // js: game/states/giveitem.js talkGiveParam
    /// <summary>TalkGiveParam2 30:D21D (talk act 17): GiveItemA(obj_param2[$0623]) if non-zero, else TalkGiveParam1
    /// 30:D225 (talk act 3): GiveItemA(obj_param1[$0623])</summary>
    public static IEnumerable<Y> TalkGiveParam(Machine m, bool p2)
    {
        int y = m.Rd(TalkSlot);
        int a = p2 ? m.Rd(0x06A0 + y) : 0;
        foreach (var s in GiveItemA(m, a != 0 ? a : m.Rd(0x0680 + y))) yield return s;
    }

    // s201: talk acts 20 (D18E Flight + JMP TalkFullRestore), 25 (D2AE Shield Ring, slot 14 follower), 26 (D336)
    // (anonymous JS TALK_ACTIONS entries, used by Talk.TalkActions)
    public static IEnumerable<Y> TalkAct14(Machine m, int y)
    {
        foreach (var s in GiveItemA(m, 0x48)) yield return s;
        foreach (var s in Talk.Act17(m, y)) yield return s;
    }

    public static IEnumerable<Y> TalkAct19(Machine m, int y)
    {
        foreach (var s in GiveItemA(m, 0x30)) yield return s;
        m.Wr(Active + 0x0E, 0x32);
        m.Wr(0x03AE, 0); m.Wr(0x062E, 0); m.Wr(0x064E, 0);
        m.Wr(0x04EE, 1);
        m.Wr(0x060E, 0xFF);
        m.Wr(0x068E, 0x0D);
    }

    public static IEnumerable<Y> TalkAct1A(Machine m, int y)
    {
        foreach (var s in GiveItemA(m, 0x09)) yield return s;
        m.Wr(0x060D, 4);
    }

    /// <summary>s202: talk act 16 (D1EB): msg 11.1 "teach Recover", GiveItemA($45 Recover), SpawnFxAtSlot slot 14</summary>
    public static IEnumerable<Y> TalkAct10(Machine m, int y)
    {
        m.Wr(0x07DE, 1); m.Wr(0x07DF, 0x0B);
        foreach (var s in Talk.ShowMessage(m)) yield return s;
        foreach (var s in GiveItemA(m, 0x45)) yield return s;
        Talk.SpawnFxAtSlot(m, 0x0E);
    }

    // js: game/states/giveitem.js waitSfxDone
    /// <summary>WaitSfxDone 30:D88B: WaitFrame until snd_sfx_priority $0112 = $FF</summary>
    private static IEnumerable<Y> WaitSfxDone(Machine m)
    {
        do { foreach (var s in Death.WaitFrame(m)) yield return s; } while (m.Rd(0x0112) != 0xFF);
    }

    // js: game/states/giveitem.js evActClearSlot
    /// <summary>EvActClearSlot13/14 30:D718 / D71F (acts 19/20, s200): clear the slot, SFX $6B, WaitSfxDone; both 13
    /// and 14 empty -> boss_cam_lock $07D7 = $7F, ObjInit slot 15 type $04CF</summary>
    private static IEnumerable<Y> EvActClearSlot(Machine m, int slot, int y)
    {
        m.Wr(Active + slot, 0);
        PlaySound.Play(m, 0x6B, y);
        foreach (var s in WaitSfxDone(m)) yield return s;
        if ((m.Rd(Active + 0x0D) | m.Rd(Active + 0x0E)) != 0) yield break;
        m.Wr(0x07D7, 0x7F);
        m.Wr(0x11, m.Rd(0x04CF)); m.Wr(0x10, 0x0F);
        ObjInit.Init(m);
    }

    // js: game/states/giveitem.js nop
    private static IEnumerable<Y> Nop(Machine m, int y, Ret<bool> pla) { yield break; }

    private static IEnumerable<Y> Iter(IEnumerable<Y>? g)
    {
        if (g == null) yield break;
        foreach (var s in g) yield return s;
    }

    private static IEnumerable<Y> Once(Action a) { a(); yield break; }

    // EVENT_ACTIONS bodies (anonymous JS generators, no inventory entries)
    private static IEnumerable<Y> Ev01(Machine m, int y, Ret<bool> pla)
    {                                                     // D5A4 EvActThunderWarp (s203)
        m.Wr(0x0600, 0); m.Wr(0x0620, 0); m.Wr(0x06C0, 0);   // obj_phase / obj_timer2 / orbit_level
        Prg.SwitchBank16(m, 0x09);
        foreach (var s in Iter(SwordRaise.SwordRaisePose(m, y))) yield return s;   // JSR 19:BC04 (ends with sub_state 8)
        m.Wr(Msg, 0x11); m.Wr(Group, 0x1C);               // msg 28.17
        foreach (var s in Talk.ShowMessage(m)) yield return s;
        PlaySound.Play(m, 0x23, 0x0A);                    // ShowMessage leaves Y = $0A (s173 probe)
        m.Wr(R.AreaId, 0xF2);
        m.Wr(0x6D, 0x41);                                 // area_entry
        m.Wr(R.SubState, 0x01);                           // the caller's D54B stores 8 over it
    }

    private static IEnumerable<Y> Ev07(Machine m, int y, Ret<bool> pla)
    {                                                     // D5E6 EvActMesiaMessage (s203)
        m.Wr(Active + m.Rd(TalkSlot), 0);
        m.Wr(0x03AD, 0);                                  // obj_coll_s0D
        m.Wr(0x030D, 0xD2);                               // obj_meta_id_s0D
        m.Wr(0x04ED, 0x20);                               // obj_anim_s0D
        do
        {                                                 // D62E: 32 x (WaitFrame + sprite pass)
            foreach (var s in Death.WaitFrame(m)) yield return s;
            FormRevert.FarSpriteDraw(m);
            m.Wr(0x04ED, (m.Rd(0x04ED) - 1) & 0xFF);
        } while (m.Rd(0x04ED) != 0);
        m.Wr(0x030D, 0x2E); m.Wr(0x06CD, 0x2E);           // meta + orbit_level_s0D
        FormRevert.FarSpriteDraw(m);                      // D606 FarCall_B14_800E
        Prg.SetR6(m, 0x1A);
        ObjUtil.ObjPosToTemp(m, 0x0D);                    // 26:98A8 X = $0D
        m.Wr(0x10, 0x1E); m.Wr(0x11, 0xDE);
        ObjInit.FarObjInit(m);                            // FarCall_C25D (ObjInit keeps X = $1E)
        ObjUtil.TempToObjPos(m, 0x1E);                    // 26:9897
        m.Wr(Msg, 0x05); m.Wr(Group, 0x0E);               // msg 14.5, JMP ShowMessage
        foreach (var s in Talk.ShowMessage(m)) yield return s;
    }

    private static IEnumerable<Y> Ev09(Machine m, int y0, Ret<bool> pla)
    {                                                     // D659 EvActSpawnType38 (s200)
        if ((m.Rd(0x0710) & 0x40) != 0 || m.Rd(Active + 0x1E) != 0) yield break;
        int area = m.Rd(R.AreaId);
        int y = area == 0x64 ? 5 : area == 0x68 ? 10 : (m.Rd(0x6D) * 5) & 0xFF;
        int x = Prg.CpuRd(m, 0xD6A8 + y);                 // EvType38Pos: x lo, x hi, y lo, y hi, $069E
        if (x == 0xFF) yield break;
        m.Wr(0x8E, x);
        m.Wr(0x10, 0x1E); m.Wr(0x11, 0x38);
        ObjInit.Init(m);                                  // keeps Y
        m.Wr(0xAE, Prg.CpuRd(m, 0xD6A9 + y)); m.Wr(0xCE, Prg.CpuRd(m, 0xD6AA + y));
        m.Wr(0xEE, Prg.CpuRd(m, 0xD6AB + y)); m.Wr(0x069E, Prg.CpuRd(m, 0xD6AC + y));
        PlaySound.Play(m, 0x48, y);
    }

    private static IEnumerable<Y> Ev14(Machine m, int y, Ret<bool> pla)
    {                                                     // D6EC (s202): AreaLoadA, type $DF in $1E at slot 15
        Area.AreaLoad(m, 1);
        Prg.SetR6(m, 0x1A);
        ObjUtil.ObjPosToTemp(m, 0x0F);
        m.Wr(0x10, 0x1E);
        ObjUtil.TempToObjPos(m, 0x1E);
        m.Wr(0x11, 0xDF);
        ObjInit.Init(m);
        m.Wr(0x033E, 0xA0);
        yield break;
    }

    private static IEnumerable<Y> Ev21(Machine m, int y, Ret<bool> pla)
    {                                                     // D746 EvActSpawn9E (s200): SFX $6B, stop music, type $9E in 13
        PlaySound.Play(m, 0x6B, y);
        PlaySound.Play(m, 0x00, y);                       // PlaySound reloads Y from snd_saved_y
        foreach (var s in WaitSfxDone(m)) yield return s;
        m.Wr(0x11, 0x9E); m.Wr(0x10, 0x0D);
        ObjInit.Init(m);
    }

    private static IEnumerable<Y> Ev23(Machine m, int y, Ret<bool> pla)
    {                                                     // D791 EvActHistory (s200): sprite pass, msgs 22.0/2/4/6
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);             // FarCall_B14_800E
        Prg.SwitchBank16(m, 0x0E);
        Sprites.SpriteDraw(m);
        Prg.FarRestore(m, r6, r7);
        m.Wr(0x0381, m.Rd(0x0381) & 0xFB);
        m.Wr(Msg, 0); m.Wr(Group, 0x16);
        do
        {
            foreach (var s in Talk.ShowMessage(m)) yield return s;
            Inc(m, Msg); Inc(m, Msg);
        } while (m.Rd(Msg) != 8);
    }

    private static IEnumerable<Y> Ev02(Machine m, int y, Ret<bool> pla)
    {                                                     // D5D6 (s199): slot 13 gives its item, $030D = $88, follower
        m.Wr(TalkSlot, 0x0D);
        foreach (var s in TalkGiveParam(m, true)) yield return s;
        m.Wr(0x030D, 0x88);
        Talk.MakeFollower13(m);
    }

    private static readonly int[] TwoFollowersClear = [0x062D, 0x062E, 0x064D, 0x064E];

    private static IEnumerable<Y> Ev24(Machine m, int y, Ret<bool> pla)
    {                                                     // D75E EvActTwoFollowers (s199)
        m.Wr(0x068D, 0x0B); m.Wr(0x068E, 0x0A);
        m.Wr(Active + 0x0D, 0x32); m.Wr(Active + 0x0E, 0x32);
        foreach (int a in TwoFollowersClear) m.Wr(a, 0);
        m.Wr(Active + m.Rd(TalkSlot), 0);
        m.Wr(0x04ED, 1); m.Wr(0x04EE, 1);
        yield break;
    }

    private static IEnumerable<Y> Ev27(Machine m, int y0, Ret<bool> pla)
    {                                                     // D7CE EvActMakeFollowers (s199)
        m.Wr(Active + m.Rd(TalkSlot), 0);
        for (int y = 0x0D; y < 0x0F; y++)
        {
            int t = m.Rd(Active + y);
            if (t == 0 || t == 0x32 || m.Rd(0x05A0 + y) != 0) continue;
            m.Wr(0x0680 + y, y - 0x0B);
            m.Wr(Active + y, 0x32);
            m.Wr(0x04E0 + y, 1);
        }
        yield break;
    }

    private static IEnumerable<Y> Ev28(Machine m, int y, Ret<bool> pla)
    {                                                     // D7FD Gas Mask (s199)
        foreach (var s in GiveItemA(m, 0x29)) yield return s;
        Talk.MakeFollower13(m);
    }

    private static IEnumerable<Y> Ev12(Machine m, int y, Ret<bool> pla)
    {                                                     // D6DD Change, then D31F Y = $0E
        foreach (var s in GiveItemA(m, 0x47)) yield return s;
        Talk.NpcVanish(m, 0x0E);
    }

    private static IEnumerable<Y> Ev15(Machine m, int y, Ret<bool> pla)
    {                                                     // D70D Refresh into the trigger slot, JMP GiveItem
        m.Wr(SpawnP + m.Rd(TalkSlot), 0x41);
        foreach (var s in Give(m)) yield return s;
    }

    private static readonly int[] PushBackBytes = [0xB0, 0xB1];

    private static IEnumerable<Y> Ev25(Machine m, int y, Ret<bool> pla)
    {                                                     // D7B7 EvActPushBack (carry INCs the low byte: ROM quirk)
        foreach (int a in PushBackBytes)
        {
            int s = m.Rd(a) + 8;
            m.Wr(a, s & 0xFF);
            if (s > 0xFF) Inc(m, a);
        }
        yield break;
    }

    private static IEnumerable<Y> Ev29(Machine m, int y, Ret<bool> pla)
    {                                                     // D805 EvActConsume
        m.Wr(Active + m.Rd(TalkSlot), 0);
        if (m.Rd(R.AreaId) != 0xF2) yield break;
        m.Wr(0x7D, 0x80); m.Wr(0xBD, 0x40);
        m.Wr(0x10, 0x0D); m.Wr(0x11, 0x88);
        ObjInit.FarObjInit(m);
    }

    /// <summary>(giveitem.js EVENT_ACTIONS) EventActionTable 30:D563 acts -> handler(m, y, pla); pla.V = true is the JS
    /// PLA result (act 30 drops the caller's return address)</summary>
    public static readonly Dictionary<int, Func<Machine, int, Ret<bool>, IEnumerable<Y>>> EventActions = new()
    {
        [1] = Ev01,
        [7] = Ev07,
        [9] = Ev09,
        [6] = (m, y, p) => Once(() => Area.AreaLoad(m, 0)),   // D6D5 JMP AreaLoadB (s202)
        [17] = (m, y, p) => Once(() => Area.AreaLoad(m, 0)),  // D6D5
        [14] = Ev14,
        [19] = (m, y, p) => EvActClearSlot(m, 0x0D, y),   // D718
        [20] = (m, y, p) => EvActClearSlot(m, 0x0E, y),   // D71F
        [21] = Ev21,
        [23] = Ev23,
        [2] = Ev02,
        [24] = Ev24,
        [27] = Ev27,
        [28] = Ev28,
        [0] = Nop, [3] = Nop, [5] = Nop, [18] = Nop, [26] = Nop, [4] = Nop,   // D5A3 / D653 RTS
        [8] = (m, y, p) => GiveItemA(m, 0x42),            // D654 Paralysis
        [10] = (m, y, p) => Once(() => PlaySound.Play(m, 0x6E, y)),   // D63A
        [11] = (m, y, p) => GiveItemA(m, 0x46),           // D6D8 Barrier
        [12] = Ev12,
        [13] = (m, y, p) => GiveItemA(m, 0x3E),           // D6E7 Bow of Moon
        [15] = Ev15,
        [16] = (m, y, p) => Talk.TalkActions[8](m, y),    // D63F = talk act 8
        [22] = (m, y, p) => Once(() => m.Wr(R.SubState, 0x1A)),   // D78C
        [25] = Ev25,
        [29] = Ev29,
        [30] = (m, y, p) => Once(() => { m.Wr(R.SubState, 0x1B); p.V = true; }),   // D827 EvActEnding
        [31] = (m, y, p) => Once(() => m.Wr(0x04AD, 0x55)),   // D82E
    };

    /// <summary>EVENT_ACTIONS[act](m, y) for Play.EntryPoints (the PLA result is dropped there, as in the JS table call)</summary>
    public static IEnumerable<Y>? EventAction(Machine m, int act, int y) => EventActions[act](m, y, new Ret<bool>());

    // js: game/states/giveitem.js eventActionDispatch
    /// <summary>EventActionDispatch 30:D552 (JMP ($10) with $10/$11 = EventActionTable[$22]); pla.V = the act's PLA</summary>
    private static IEnumerable<Y> EventActionDispatch(Machine m, Ret<bool> pla)
    {
        int a = (m.Rd(0x22) << 1) & 0xFF;
        m.Wr(0x10, Prg.CpuRd(m, 0xD563 + a));
        m.Wr(0x11, Prg.CpuRd(m, 0xD564 + a));
        pla.V = false;
        if (EventActions.TryGetValue(a >> 1, out var fn))
        {
            foreach (var s in fn(m, a, pla)) yield return s;
            yield break;
        }
        Unported(m, $"EventActionTable {a >> 1} (30:{m.Rd(0x10) | m.Rd(0x11) << 8:X})");
    }

    // ---------------------------------------------------------------- ItemUseMenu (sub_state 6) / ShowPendingMessage (16)

    // js: game/states/giveitem.js itemUseRun
    /// <summary>30:D4F2: flute SFX, slot 3 param = item, ItemEventHook(1), EventActionDispatch or ItemUseFail 30:D526</summary>
    private static IEnumerable<Y> ItemUseRun(Machine m, int yIn)
    {
        int sel = m.Rd(0x0715);
        if (sel == 0x0B || sel == 0x0C || sel == 0x15) PlaySound.Play(m, 0x48, yIn);   // flutes
        m.Wr(SpawnP + 3, (sel + 0x1C) & 0xFF);
        m.Wr(TalkSlot, 3);
        Inc(m, 0x08);
        m.Wr(R.SubState, 8);
        var ry = new Ret<int>();
        foreach (var s in ItemEventHook(m, 1, ry)) yield return s;
        int y = ry.V;
        if ((m.Rd(0x23) & 0x80) == 0)
        {
            foreach (var s in EventActionDispatch(m, new Ret<bool>())) yield return s;
            yield break;
        }
        m.Wr(R.SubState, 0x10);                           // ItemUseFail: pending msg 32.19 "Nothing happens."
        m.Wr(0x06C3, 0x13);
        m.Wr(0x06E3, 0x20);
        PlaySound.Play(m, 0x4E, y);
    }

    // js: game/states/giveitem.js itemUseMenu
    /// <summary>ItemUseMenu 30:D497 (B with use_item_sel $0715 = item id - $1C); Y at the dispatch = $010F</summary>
    public static IEnumerable<Y>? ItemUseMenu(Machine m) => ItemUseMenu(m, m.Rd(0x010F));

    /// <summary>ItemUseMenu 30:D497 with the dispatch Y given</summary>
    public static IEnumerable<Y> ItemUseMenu(Machine m, int yIn)
    {
        int sel = m.Rd(0x0715);
        if (sel == 0x0A && m.Rd(0x0743) == 0)
        {                                                 // Opel Statue used by hand: refused, SubstPlay this frame
            Inc(m, 0x08);
            m.Wr(R.SubState, 8);
            foreach (var s in Iter(Play.MainState1(m))) yield return s;
            yield break;
        }
        m.Wr(0x0743, 0);
        int flash = m.Rd(0x0710);
        if (sel == 0x20 && (flash & 0x80) != 0 && (flash & 0x30) != 0x20) { m.Wr(R.SubState, 8); yield break; }   // Kirisa Plant
        if (sel != 0x08)
        {
            foreach (var s in ItemUseRun(m, yIn)) yield return s;
            yield break;
        }
        if ((flash & 0x80) != 0) { m.Wr(R.SubState, 8); yield break; }   // Warp Boots blocked
        int area = m.Rd(R.AreaId);
        foreach (var s in Iter(Teleport.SubstTeleportMenu(m, yIn))) yield return s;   // 30:D4DC (sub_state 6 -> no SpendMp)
        if (area == m.Rd(R.AreaId)) yield break;          // refused / cancelled / same town -> 30:D45B RTS
        foreach (var s in ItemUseRun(m, yIn)) yield return s;   // 30:D4E7
        Prg.SetR7(m, 0x17);                               // D4EA SetR7($17) (SaveAllowed reads 23:BF00), JMP 23:BC00
        if (SaveLoad.SaveGame(m)) { yield return Y.Frame; yield return Y.Spin; }
    }

    // js: game/states/giveitem.js showPendingMessage
    /// <summary>ShowPendingMessage 30:D085 (sub_state $10): message $06C3/$06E3 after a 32-frame wait</summary>
    public static IEnumerable<Y> ShowPendingMessage(Machine m)
    {
        Area5FIrqOff(m);
        int irq = m.Rd(R.IrqMode);
        foreach (var s in Death.WaitFrames(m, 0x20)) yield return s;   // Y = 0 after
        if (m.Rd(0x06C3) == 0x0D && m.Rd(0x06E3) == 0x20)
        {
            m.Wr(0x0102, m.Rd(0x0102) | 0x80);
            PlaySound.Play(m, 0x93, 0);
        }
        else PlaySound.Play(m, 0x29, 0);
        m.Wr(Msg, m.Rd(0x06C3));
        m.Wr(Group, m.Rd(0x06E3));
        foreach (var s in Talk.ShowMessage(m)) yield return s;
        m.Wr(R.IrqMode, irq);
        foreach (var s in SaveLoad.RestoreIfArea5F(m)) yield return s;
    }

    // ---------------------------------------------------------------- GiveItem

    // js: game/states/giveitem.js trapChest
    /// <summary>TrapChest 30:D3DA</summary>
    private static void TrapChest(Machine m)
    {
        m.Wr(0x10, m.Rd(TalkSlot));
        m.Wr(0x11, 0x73);
        ObjInit.Init(m);
        m.Wr(R.SubState, 8);
    }

    // js: game/states/giveitem.js giveItemFull
    /// <summary>GiveItemFull 30:D47C (Y = 0: InvStoreItem's count loop ran out)</summary>
    private static IEnumerable<Y> GiveItemFull(Machine m, int y)
    {
        if ((m.Rd(0x08) & 0x0F) != 0) yield break;
        Play.PlayPaletteRestore(m);
        m.Wr(Msg, 0x0F);
        m.Wr(Group, 0x20);
        PlaySound.Play(m, 0x29, y);
        foreach (var s in Talk.ShowMessage(m)) yield return s;
    }

    // js: game/states/giveitem.js giveTrigger
    /// <summary>30:D539: invisible event trigger</summary>
    private static IEnumerable<Y> GiveTrigger(Machine m)
    {
        Inc(m, R.SubState); Inc(m, R.SubState);           // 9: ShowMessage blinks the window
        Inc(m, 0x08);
        foreach (var s in ItemEventHook(m, 2, new Ret<int>())) yield return s;
        if ((m.Rd(0x23) & 0x80) == 0)
        {
            var pla = new Ret<bool>();
            foreach (var s in EventActionDispatch(m, pla)) yield return s;
            if (pla.V) yield break;
        }
        m.Wr(R.SubState, 8);
        foreach (var s in Iter(Play.MainState1(m))) yield return s;   // D54F JMP MainStateGameplay
    }

    // js: game/states/giveitem.js giveItem
    /// <summary>GiveItem 30:D3EB</summary>
    public static IEnumerable<Y> Give(Machine m)
    {
        int y = m.Rd(TalkSlot);
        int id = m.Rd(SpawnP + y);
        m.Wr(CurItem, id);
        if (id >= 0x80) { foreach (var s in GiveTrigger(m)) yield return s; yield break; }
        if (id == 0x70) { TrapChest(m); yield break; }
        var hy = new Ret<int>();
        foreach (var s in ItemEventHook(m, 0, hy)) yield return s;
        if ((m.Rd(0x23) & 0x80) != 0) { foreach (var s in GiveItemFull(m, hy.V)) yield return s; yield break; }
        Equip.SwordPowerLevel(m);                         // D408: Y result unused
        y = m.Rd(TalkSlot);
        int raw = m.Rd(SpawnP + y);
        if (raw >= 0x50) { y = raw; m.Wr(CurItem, Prg.CpuRd(m, 0xD40C + raw)); }   // ItemRemapTable (name only)
        if ((m.Rd(CurItem) & 0xF0) == 0x40)
        {
            m.Wr(0x0102, m.Rd(0x0102) | 0x80);
            PlaySound.Play(m, 0x93, y);
        }
        else PlaySound.Play(m, 0x3F, y);
        m.Wr(Group, 0x20);
        m.Wr(Msg, 0x0C);
        int a22 = m.Rd(0x22);
        foreach (var s in Talk.ShowMessage(m)) yield return s;
        Play.PlayPaletteRestore(m);
        m.Wr(0x22, a22);
        m.Wr(Active + m.Rd(TalkSlot), 0);
        var pla = new Ret<bool>();
        foreach (var s in EventActionDispatch(m, pla)) yield return s;
        if (pla.V) Unported(m, "EventActionTable 30 after an item pickup");
    }
}
