// re-c C# port — the player's two objects (= web/src/game/obj/player.js, s111). REVERSE.md "Player — movement, jump,
// sword, magic" and "World interaction". Instruction by instruction (zero-page temps included).
//
//   AI $03 PlayerMove        PlayerMove 26:9D9A (slot 0, ground body)
//   AI $02 PlayerMoveLocked  PlayerMoveLocked 26:9D81 (pads zeroed; area $58 -> AI $03 + pad_dir 4)
//   AI $01 PlayerUpdate      PlayerUpdate 26:9B45 (slot 1, drawn body)
// SpendMp 26:9AE8 = SpendMp() below: res.V = false = MP short, sub_state $1C; every call site returns at once then.
using ReC.Game.States;

namespace ReC.Game.Obj;

public static class Player
{
    private const int Meta = 0x0300, Flags2 = 0x0340, MetaDir = 0x0360, Flags = 0x0380, Coll = 0x03A0, BgMask = 0x0460;
    private const int Timer = 0x0480, Active = 0x04A0, Overlay = 0x0580, Phase = 0x0600, Timer2 = 0x0620, ActTimer = 0x0640;
    private const int Aux = 0x0660, Param1 = 0x0680, Param2 = 0x06A0, Behav = 0x06E0;
    private const int FlashA = 0x0710, Sword = 0x0711, MagicSel = 0x0712, Shield = 0x0714, UseItem = 0x0715, Accessory = 0x0716;
    private const int Mp = 0x0708, HpMax = 0x03C0, Hp = 0x03C1, Orbit = 0x06C0, OrbitBurst = 0x06C1, SwordPower = 0x0719;
    private const int BgAttr = 0x0641, SpawnParamS01 = 0x0561, MetaS01 = 0x0301, Anim = 0x04E0, AnimS01 = 0x04E1;
    private const int PadPrev = 0x43, PadDir = 0x49, PadNew = 0x4B, PadAct = 0x4D, SubState = 0x41, AreaA = 0x6C;
    private const int JumpHeight = 0x9D4D;

    // js: game/obj/player.js dec
    private static void Dec(Machine m, int a) => m.Wr(a, (m.Rd(a) - 1) & 0xFF);
    // js: game/obj/player.js inc
    private static void Inc(Machine m, int a) => m.Wr(a, (m.Rd(a) + 1) & 0xFF);
    // js: game/obj/player.js status
    private static int Status(Machine m) => m.Rd(FlashA) & 0x0F;

    // js: game/obj/player.js spendMp
    /// <summary>SpendMp 26:9AE8 (A = cost); HudRedraw(4) keeps the caller's X. res.V = false: MP short.</summary>
    public static IEnumerable<Y> SpendMp(Machine m, int cost, int x, Ret<bool> res)
    {
        m.Wr(0x1F, cost);
        int mp = m.Rd(Mp);
        if (mp == 0 || mp < cost) { m.Wr(SubState, 0x1C); res.V = false; yield break; }
        m.Wr(Mp, mp - cost);
        foreach (var s in Death.HudRedraw(m, 4, x)) yield return s;
        res.V = true;
    }

    // js: game/obj/player.js status4Fail
    /// <summary>27:A1B0: status 4 -> PLA PLA + MagicFailSound (returns from the handler); true = blocked</summary>
    private static bool Status4Fail(Machine m, int y)
    {
        if (Status(m) != 4) return false;
        PlaySound.Play(m, 0x4E, y);
        return true;
    }

    // js: game/obj/player.js healStep
    /// <summary>27:A0B3 one heal step (run twice per frame by magic 1)</summary>
    private static IEnumerable<Y> HealStep(Machine m, int x)
    {
        int hp = m.Rd(Hp);
        if (hp == m.Rd(HpMax + x)) yield break;
        int n = (hp + 1) & 0xFF;
        m.Wr(Hp, n);
        if (n != m.Rd(HpMax + x) && (n & 3) != 0) yield break;
        foreach (var s in Death.HudHpBar(m)) yield return s;     // 26:8CC0
        var ok = new Ret<bool>();
        foreach (var s in SpendMp(m, 2, x, ok)) yield return s;
        if (!ok.V) yield break;
        CastShot(m, x, 0x12);
    }

    // js: game/obj/player.js castShot
    /// <summary>27:A0E6: SpawnShot(A, Y = slot 1 facing * 2) by JMP</summary>
    private static void CastShot(Machine m, int x, int id)
    {
        m.Wr(0x10, id);
        ObjUtil.SpawnShot(m, x, id, (m.Rd(MetaS01) & 3) << 1);
    }

    // js: game/obj/player.js outOfMp
    private static void OutOfMp(Machine m) => m.Wr(SubState, 0x1C);   // 27:A15C

    // js: game/obj/player.js playerMagic
    /// <summary>PlayerMagic 27:A022 (X = 0); res.V = the Y register at the RTS (approximate after a cast)</summary>
    private static IEnumerable<Y> PlayerMagic(Machine m, int x, Ret<int> res)
    {
        int y = m.Rd(MagicSel);
        if (y == 0 && m.Rd(Accessory) == 0x12) y = 9;
        m.Wr(0x10, y);
        y = Prg.CpuRd(m, 0xA092 + y);                             // MagicButtonSel: $08 pad_new / $00 pad_prev
        res.V = y;
        if ((m.Rd(PadPrev + y) & 0x80) == 0) yield break;
        if (Status(m) == 4) { PlaySound.Play(m, 0x4E, y); yield break; }   // A043 -> A01F JMP MagicFailSound
        int sel = m.Rd(0x10);
        if (sel != 8 && sel != 9 && m.Rd(Timer2) != 0) { PlaySound.Play(m, 0x4E, y); yield break; }
        if ((m.Rd(FlashA) & 0x80) != 0) { m.Wr(SubState, 0x19); res.V = m.Rd(FlashA); yield break; }   // A05B JMP A1AD
        y = (sel & 0x0F) << 1;
        res.V = y;
        m.Wr(0x10, Prg.CpuRd(m, 0xA072 + y));                     // JmpTable27_A072 -> JMP ($0010)
        m.Wr(0x11, Prg.CpuRd(m, 0xA073 + y));
        var ok = new Ret<bool>();
        switch (sel & 0x0F)
        {
            case 1:                                               // A0A3 heal
                if (Status4Fail(m, y)) yield break;
                if (m.Rd(Mp) < 2) { OutOfMp(m); yield break; }
                foreach (var s in HealStep(m, x)) yield return s;
                foreach (var s in HealStep(m, x)) yield return s;
                yield break;
            case 2:                                               // A0D7
                if (Status4Fail(m, y)) yield break;
                if (m.Rd(0x04AC) != 0) yield break;               // slot $0C busy
                foreach (var s in SpendMp(m, 4, x, ok)) yield return s;
                if (!ok.V) yield break;
                CastShot(m, x, 0x10);
                yield break;
            case 3:                                               // A18E telepathy
                if ((m.Rd(AreaA) & 0xF8) == 0x58) { PlaySound.Play(m, 0x4E, y); yield break; }
                if (m.Rd(Mp) < 8) { OutOfMp(m); yield break; }
                m.Wr(SubState, 0x15);
                yield break;
            case 4:                                               // A1A1 teleport
                if (Status4Fail(m, y)) yield break;
                if (m.Rd(Mp) < 0x14) { OutOfMp(m); yield break; }
                m.Wr(SubState, 0x14);
                yield break;
            case 5:                                               // A17D cure
                if (Status(m) == 0) yield break;
                foreach (var s in SpendMp(m, 0x18, x, ok)) yield return s;
                if (!ok.V) yield break;
                m.Wr(SubState, 0x18);
                yield break;
            case 6:                                               // A0F4
                if (m.Rd(Timer2) != 0) { PlaySound.Play(m, 0x4E, y); yield break; }
                if (m.Rd(Mp) == 0) { OutOfMp(m); yield break; }
                if ((m.Rd(0x08) & 7) == 0)
                {
                    foreach (var s in SpendMp(m, 1, x, ok)) yield return s;
                    if (!ok.V) yield break;
                }
                CastShot(m, x, 0x11);
                yield break;
            case 7:                                               // A161 change form
                if ((m.Rd(AreaA) & 0xF8) == 0x58) { PlaySound.Play(m, 0x4E, y); yield break; }
                if (Status4Fail(m, y)) yield break;
                if (m.Rd(Timer2 + x) != 0) yield break;
                if (m.Rd(Mp) < 0x14) { OutOfMp(m); yield break; }
                m.Wr(SubState, 0x17);
                yield break;
            case 8:                                               // MagicFlight A112
                if ((m.Rd(FlashA) & 0x40) != 0) yield break;
                if ((m.Rd(0x08) & 0x0F) == 0)
                {
                    foreach (var s in SpendMp(m, 1, x, ok)) yield return s;
                    if (!ok.V) yield break;
                    y = 0x18;                                     // HudRedraw(4) returns with Y = 4 * 6
                    res.V = y;
                }
                if (m.Rd(Timer2 + x) == 0)
                {
                    if (m.Rd(Mp) == 0) { OutOfMp(m); yield break; }
                    m.Wr(BgMask + x, 0x34);
                    JumpStart(m, x, y);
                    yield break;
                }
                if (m.Rd(Timer2 + x) == 0x12) Inc(m, Timer2 + x);  // hover at the peak
                m.Wr(ActTimer + x, m.Rd(PadDir));
                yield break;
            case 9:                                               // MagicJump A143
                if ((m.Rd(FlashA) & 0x40) != 0) yield break;
                if (m.Rd(Timer2 + x) != 0) yield break;
                JumpStart(m, x, y);
                yield break;
            default:                                              // A0A2 RTS (0, 10-15)
                yield break;
        }
    }

    // js: game/obj/player.js jumpStart
    /// <summary>27:A14D</summary>
    private static void JumpStart(Machine m, int x, int y)
    {
        m.Wr(Timer2 + x, 0x20);
        m.Wr(ActTimer + x, m.Rd(PadDir));
        PlaySound.Play(m, 0x30, y);
    }

    // js: game/obj/player.js playerConveyor
    /// <summary>PlayerConveyor 26:9FD2</summary>
    private static void PlayerConveyor(Machine m, int x)
    {
        int sd = m.Rd(MetaDir + x), sf = m.Rd(Flags2 + x), st = m.Rd(Timer + x);
        m.Wr(Timer + x, m.Rd(0x08));
        m.Wr(Flags2 + x, m.Rd(Aux + x));
        m.Wr(MetaDir + x, m.Rd(Param1 + x) & 0x0F);
        if (Move.ObjMoveSlide(m, x)) m.Wr(Param1 + x, Prg.CpuRd(m, 0xA00F + (m.Rd(MetaDir + x) & 7)));   // 27:A00F
        m.Wr(Timer + x, st);
        m.Wr(Flags2 + x, sf);
        m.Wr(MetaDir + x, sd);
    }

    // js: game/obj/player.js playerMoveLocked
    /// <summary>AI $02 PlayerMoveLocked 26:9D81</summary>
    public static IEnumerable<Y>? PlayerMoveLocked(Machine m, int x, AiRet r) => MoveLockedIter(m, x);

    private static IEnumerable<Y> MoveLockedIter(Machine m, int x)
    {
        m.Wr(PadPrev, 0); m.Wr(PadNew, 0); m.Wr(PadAct, 0); m.Wr(PadDir, 0);
        if (m.Rd(AreaA) == 0x58)
        {
            m.Wr(Active + x, 3);
            m.Wr(PadDir, 4);
        }
        foreach (var s in MoveIter(m, x)) yield return s;
    }

    // js: game/obj/player.js playerMove
    /// <summary>AI $03 PlayerMove 26:9D9A</summary>
    public static IEnumerable<Y>? PlayerMove(Machine m, int x, AiRet r) => MoveIter(m, x);

    private static IEnumerable<Y> MoveIter(Machine m, int x)
    {
        int fl = m.Rd(FlashA), y = 0x4C;
        if ((fl & 0x80) == 0 && (fl & 0x0F) != 4)
        {
            m.Wr(Behav + x, m.Rd(PadDir));
            y = 0xA7;
            if ((m.Rd(Flags + x) & 0x40) != 0 && m.Rd(Timer2 + x) == 0) y = 0x4C;   // on the ground in a sink cell
        }
        m.Wr(Meta + x, y);
        if (m.Rd(Timer2 + x) != 0)
        {                                                         // 9DBD jump timer
            Dec(m, Timer2 + x);
            int t = m.Rd(Timer2 + x);
            if (t == 0x11) PlaySound.Play(m, 0x31, y);
            if (t >= 0x1E && (m.Rd(PadDir) & 0x80) == 0) m.Wr(ActTimer + x, m.Rd(PadDir));
        }
        var my = new Ret<int>();
        foreach (var s in PlayerMagic(m, x, my)) yield return s;  // 9DE1
        y = my.V;
        fl = m.Rd(FlashA);
        if ((fl & 0x80) == 0 && (fl & 0x0F) != 4 && (m.Rd(PadPrev) & 0x40) != 0)
        {                                                         // 9DE4 sword charge
            int ph = m.Rd(Phase + x);
            if ((ph < 0x0C || ph == 0x11) && m.Rd(Sword) != 0)
            {
                m.Wr(Phase + x, 0x12);
                if ((m.Rd(0x08) & 3) == 0)
                {
                    Inc(m, Orbit);
                    int lv = m.Rd(Orbit);
                    if (lv == 8) { y = 0x0E; m.Wr(OrbitBurst, 0x0E); }
                    if ((lv & 7) == 0) PlaySound.Play(m, 0x26, y);
                    y = m.Rd(SwordPower);
                    int cap = Prg.CpuRd(m, 0x9D7E + y);           // SwordChargeCap
                    if (cap < lv) m.Wr(Orbit, cap);
                }
            }
        }
        y = m.Rd(Phase + x);                                      // 9E39 swing counter / pose
        int path = 2;                                             // 0 idle, 1 air, 2 swing
        if (y == 0) path = 0;
        else if (y == 0x12 && (m.Rd(PadPrev) & 0x40) != 0)
        {
            if ((m.Rd(PadDir) & 0x80) == 0) path = 0;
            else if (m.Rd(Timer2 + x) != 0) path = 1;
        }
        if (path == 0)
        {                                                         // 9E6F
            m.Wr(MetaS01, 0);
            if (m.Rd(Timer2 + x) == 0)
            {
                m.Wr(BgMask + x, 0x32);
                if ((m.Rd(PadDir) & 0x80) == 0) Dec(m, AnimS01);  // walk cycle
            }
        }
        else
        {
            if (path == 1) m.Wr(MetaS01, 0);                      // 9E4F
            else
            {                                                     // 9E57
                m.Wr(AnimS01, y);
                m.Wr(MetaS01, 4);
                Dec(m, Phase + x);
            }
            if (m.Rd(Timer2 + x) == 0) m.Wr(BgMask + x, 0x32);   // 9E62
        }
        if ((m.Rd(FlashA) & 0x40) != 0) m.Wr(BgMask, 0x34);      // 9E85 (absolute slot 0)
        y = m.Rd(Timer2 + x);                                     // 9E8F move
        bool move = true;
        if (y != 0)
        {
            m.Wr(PadDir, m.Rd(ActTimer + x));                     // the jump direction is latched
            if (Prg.CpuRd(m, JumpHeight + y) == 0) move = false;
        }
        if (move)
        {
            m.Wr(0x26, m.Rd(MetaDir + x));
            if ((m.Rd(PadDir) & 0x80) != 0) move = false;
        }
        if (move && (m.Rd(Param1 + x) & 0x80) != 0)
        {                                                         // 9EA7 forced move: DiagToCardinal 27:A017
            m.Wr(AnimS01, 0);
            int v = Prg.CpuRd(m, 0xA017 + m.Rd(PadDir));
            if ((v & 0x80) != 0) move = false;
            else m.Wr(PadDir, v);
        }
        if (move)
        {                                                         // 9EBA
            m.Wr(MetaDir + x, m.Rd(PadDir));
            Dec(m, Timer + x);
            if (!Move.ObjMoveSlide(m, x))
            {
                m.Wr(SpawnParamS01, m.Rd(0x25));
                m.Wr(MetaDir + x, m.Rd(0x23));
                m.Wr(Param2 + x, 8);
            }
            else if (m.Rd(Param2 + x) != 0)
            {                                                     // 9ED8 corner slide: keep the old direction
                Dec(m, Param2 + x);
                m.Wr(MetaDir + x, m.Rd(0x26));
                if (!Move.ObjMoveSlide(m, x)) m.Wr(SpawnParamS01, m.Rd(0x25));
            }
        }
        if ((m.Rd(0x08) & 3) == 0) { foreach (var s in SaveLoad.HudChargeMeter(m)) yield return s; x = 0; }   // 9EEF (LDX #0 after)
        ObjUtil.ObjPosToTemp(m, x);                               // 9EFA
        int t2 = m.Rd(Timer2 + x);                                // LDA #0 / LDY obj_timer2 / BNE: N = timer2 b7
        ObjUtil.ObjBgProbe(m, x, t2 != 0 ? 0 : 0x40, t2 & 0x80);
        if ((m.Rd(Timer2 + x) & 0xF0) == 0 && ((m.Rd(SpawnParamS01) | m.Rd(0x20)) & 2) != 0)
        {                                                         // 9F09 low obstacle
            m.Wr(ActTimer + x, m.Rd(Behav + x));
            if (m.Rd(Timer2) != 0)
            {
                Inc(m, Timer2);                                   // cancels the DEC: the jump timer is frozen
                if (m.Rd(Mp) != 0 && (m.Rd(0x08) & 0x0F) == 0)
                {
                    var ok = new Ret<bool>();
                    foreach (var s in SpendMp(m, 1, x, ok)) yield return s;
                    if (!ok.V) yield break;
                }
            }
        }
        if (m.Rd(Timer2 + x) == 0)
        {                                                         // 9F37 sink bit
            m.Wr(0x10, m.Rd(0x20) & 0x40);
            m.Wr(BgMask + x, (m.Rd(BgMask + x) & 0xBF) | m.Rd(0x10));
        }
        m.Wr(Flags + x, (m.Rd(BgMask + x) & 0x40) | m.Rd(Flags + x));
        int a20 = m.Rd(0x20);
        if ((a20 & 0x20) == 0)
        {                                                         // 9F5D off a conveyor
            int aux = m.Rd(Aux + x);
            if ((m.Rd(BgAttr) & 0x20) != 0 && ((aux - 7) & 0x80) != 0)
            {                                                     // just left one with obj_aux < 7 (signed CMP)
                m.Wr(Aux + x, 0);
                m.Wr(Param1 + x, 4);
                m.Wr(BgAttr, a20);
                yield break;
            }
            m.Wr(BgAttr, a20);                                    // 9F7B
            if (aux == 0) { m.Wr(Param1 + x, 4); m.Wr(BgAttr, a20); yield break; }
            if ((m.Rd(0x08) & 3) == 0) Dec(m, Aux + x);           // obj_aux decays
            m.Wr(Param1 + x, 0x84);
            PlayerConveyor(m, x);                                 // BNE PlayerConveyor (its RTS ends PlayerMove)
            yield break;
        }
        m.Wr(BgAttr, a20);                                        // 9F95 on a conveyor
        PlayerConveyor(m, x);
        int tt = m.Rd(Timer2 + x);
        bool down;
        if (tt == 1) down = true;
        else if (tt != 0) yield break;
        else if ((m.Rd(FlashA) & 0x40) != 0) down = false;
        else if ((m.Rd(0x08) & 0x0F) != 0) yield break;
        else down = true;
        if (down && m.Rd(PadDir) == 4 && m.Rd(Aux + x) <= 6) m.Wr(Aux + x, 6);   // 9FB4
        Inc(m, Aux + x);                                          // 9FC4
        if (m.Rd(Aux + x) > 9) m.Wr(Aux + x, 9);
    }

    // js: game/obj/player.js playerSetSpeed
    /// <summary>PlayerSetSpeed 26:9BF6: slot 0 obj_flags2 low nibble</summary>
    private static void PlayerSetSpeed(Machine m, int a)
    {
        m.Wr(0x10, a);
        m.Wr(Flags2, (m.Rd(Flags2) & 0xF0) | a);
    }

    // js: game/obj/player.js playerPlaceBody
    /// <summary>PlayerPlaceBody 26:9CEF</summary>
    private static void PlayerPlaceBody(Machine m, int x)
    {
        int y = m.Rd(Timer2);
        int h = Prg.CpuRd(m, JumpHeight + y);
        m.Wr(0x31, h);
        if (h != 0)
        {
            m.Wr(Coll + x, 0x60);
            ObjUtil.ObjPosToTemp(m, x);
            ObjUtil.ObjBgProbe(m, x, 0x40);
            y = 0x1B;                                             // ObjBgProbe's lookup path ends with LDY #$1B
            m.Wr(Flags + x, m.Rd(Flags + x) & 0xBF);
        }
        else
        {
            m.Wr(Coll + x, 0x50);
            m.Wr(0x10, m.Rd(Flags + x) & 7);                      // keeps its own b0-2, takes slot 0's b3-7
            m.Wr(Flags + x, (m.Rd(Flags) & 0xF8) | m.Rd(0x10));
        }
        if (m.Rd(0x31) == 0xF9)
        {                                                         // landing from a pit fall
            PlaySound.Play(m, 0x56, y);
            m.Wr(0x07D7, 0x10);
            m.Wr(0x07D6, 0x10);
        }
        m.Wr(0x10, x);
        ObjUtil.ObjPosToTemp(m, 0);
        m.Wr(0x30, 0);
        ObjUtil.TempAddOffset(m);                                 // 26:9824
        ObjUtil.TempToObjPos(m, x);                               // JMP 26:9897
    }

    // js: game/obj/player.js playerFacing
    /// <summary>PlayerFacing 27:A1C9</summary>
    private static void PlayerFacing(Machine m, int x)
    {
        if (m.Rd(AreaA) == 0x5F && m.Rd(Phase) != 0)
        {
            m.Wr(MetaS01, m.Rd(MetaS01) & 0xFC);
            return;
        }
        int b = m.Rd(Behav);
        if (b != 0xFF)
        {                                                         // $FF: no direction always takes slot 0's direction
            int meta = m.Rd(Meta + x);
            if (Prg.CpuRd(m, 0xA211 + ((((b & 7) ^ 0xFF) + 1 + meta + meta) & 7)) != 0) return;   // FacingKeep
        }
        int d = m.Rd(MetaDir);
        m.Wr(MetaDir + x, d);
        m.Wr(0x10, (d >> 1) & 3);
        m.Wr(Meta + x, (m.Rd(Meta + x) & 0xFC) | m.Rd(0x10));
    }

    // js: game/obj/player.js playerUpdate
    /// <summary>AI $01 PlayerUpdate 26:9B45</summary>
    public static IEnumerable<Y>? PlayerUpdate(Machine m, int x, AiRet r) => UpdateIter(m, x);

    private static IEnumerable<Y> UpdateIter(Machine m, int x)
    {
        if ((m.Rd(PadNew) & 0x40) != 0 && m.Rd(UseItem) != 0 && m.Rd(SubState) == 8) m.Wr(SubState, 6);
        PlayerSetSpeed(m, m.Rd(Flags2 + x) & 0x0F);
        PlayerPlaceBody(m, x);
        PlayerFacing(m, x);
        if (Status(m) == 4)
        {                                                         // 9B68 status 4
            PlayerSetSpeed(m, 2);
            m.Wr(Meta + x, 0x53);
        }
        else if ((m.Rd(FlashA) & 0x40) != 0)
        {                                                         // 9B7D warp exit spin
            PlayerSetSpeed(m, 9);
            if (((m.Rd(SpawnParamS01) | m.Rd(BgAttr)) & 6) == 0) m.Wr(FlashA, m.Rd(FlashA) & 0xBF);
            else
            {
                m.Wr(Meta, Prg.CpuRd(m, 0x9B41 + (m.Rd(MetaS01) & 3)));   // WarpSpinPoseTable
                m.Wr(Flags + 1, m.Rd(Flags + 1) | 0x40);
                Dec(m, Anim);
                if (m.Rd(Phase) != 0)
                {
                    if ((m.Rd(MetaS01) & 3) == 2 && (m.Rd(PadDir) & 0x80) != 0) m.Wr(MetaS01, 0x96);
                }
                else m.Wr(AnimS01, m.Rd(Anim));
            }
        }
        if ((m.Rd(FlashA) & 0x80) != 0)
        {                                                         // 9BD2 status b7
            int yy = (m.Rd(FlashA) & 0x30) >> 4;
            m.Wr(0x10, Prg.CpuRd(m, 0x9BF2 + yy));
            m.Wr(Meta + x, (m.Rd(Meta + x) & 3) | m.Rd(0x10));
            PlayerSetSpeed(m, 5);
            yield break;
        }
        int meta = m.Rd(Meta + x);                                // 9C03 shield overlay
        if (m.Rd(Shield) == 0) m.Wr(Overlay + x, (meta & 4) != 0 ? 0x4C : (meta & 3) | 8);
        else m.Wr(Overlay + x, ((meta & 7) + 0x0C) & 0xFF);
        int y = (m.Rd(Meta + x) << 1) & 7;                        // 9C2A
        int ph = m.Rd(Phase);
        if (ph == 0x10) { foreach (var s in ChargedShot(m, x)) yield return s; yield break; }
        if (ph > 0x10 || ph < 4) yield break;
        if (m.Rd(Active) == 0x0B && ph != 0x0F) yield break;      // sparring duel: one slash only
        m.Wr(0x04A2, 0);                                          // 9C4C slash
        ObjUtil.SpawnShot(m, x, 0x19, y);
        int sw = m.Rd(Sword);
        m.Wr(0x0502, Prg.CpuRd(m, 0x9C66 + sw));                  // SwordElemBit
        m.Wr(0x03E2, Prg.CpuRd(m, 0x8BD2 + sw));                  // SwordAttackTable
    }

    // js: game/obj/player.js chargedShot
    /// <summary>26:9C6C the release shot</summary>
    private static IEnumerable<Y> ChargedShot(Machine m, int x)
    {
        int sword = m.Rd(Sword);
        if (sword == 5)
        {                                                         // 9CDA Crystalis: only in area $5F
            if (m.Rd(AreaA) == 0x5F) ObjUtil.SpawnShot(m, x, 0x13, (m.Rd(Meta + x) & 3) << 1);
        }
        else if (sword != 0)
        {
            m.Wr(0x10, m.Rd(Orbit) >> 3);
            if (m.Rd(0x10) == 0 && Status(m) != 1 && m.Rd(Accessory) == 0x0F) Inc(m, 0x10);
            if (m.Rd(0x10) == 3 && Prg.CpuRd(m, 0x8BD8 + sword) >= m.Rd(Mp))
            {                                                     // level 3 needs MP > cost
                Dec(m, 0x10);
                Dec(m, Orbit);
            }
            int id = (((sword - 1) << 2) | m.Rd(0x10)) & 0xFF;
            if (ObjUtil.SpawnShot(m, x, id, (m.Rd(Meta + x) & 3) << 1) && (m.Rd(Orbit) & 0xF8) == 0x18)
            {
                var ok = new Ret<bool>();
                foreach (var s in SpendMp(m, Prg.CpuRd(m, 0x8BD8 + m.Rd(Sword)), x, ok)) yield return s;
                if (!ok.V) yield break;                           // PLA PLA: no reset
            }
        }
        m.Wr(Orbit, 0);                                           // 9CD1
        m.Wr(OrbitBurst, 0);
    }

    /// <summary>(player.js AI_PLAYER) ObjAiTable entries of this module (merged into Objects.AiTable)</summary>
    public static readonly Dictionary<int, AiFn> Ai = new()
    {
        [0x01] = PlayerUpdate, [0x02] = PlayerMoveLocked, [0x03] = PlayerMove,
    };
}
