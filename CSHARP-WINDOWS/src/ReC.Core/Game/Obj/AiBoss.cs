// re-c C# port — bank 15 bosses (= web/src/game/obj/ai_boss.js, s100 ff.). REVERSE.md "Boss phase bodies, bank 15" +
// "Bank 15 bosses (web port, session 100)". ObjUpdateAll runs these ids with SetR7($0F).
//
//   BossPhaseDispatch(m, x, base, r)  15:A400: JMP (BossPhaseTable 15:A413[(base + obj_phase)*2]), $10/$11 = the handler
//                                     address. The table is shared, so BossPhases is keyed by handler address.
//   AI $60 BossAiE0 teleporter, $61 BossHomingShot, $62 BossAiE2 (area $1A walker), $63 BossAiE3 slam / sweep,
//   $64 BossRockShot, $65 AiTalkEventE5 (area $43), $66 BossAiE6 flyer, $67 AiBoss88 bouncer, $68 BossAiE8 waypoints,
//   $69 BossAiE9 statues, $6A BossAiEA patrol / dive, $6B AiBowOfTruth9E final boss (BG face), $6C-$6E AiReward6C,
//   $6F BossDefeated (+ hooks, BossFleeSpeech), $79 AiBank15Fall79, $3F AiBodyPart.
//
// Phase handlers: `Ph(m, x, c, r)` (c = the dispatch's ASL carry, r = the AI's JS return value). JS quirks kept for
// parity: e9Fire returns SpawnShot's bool, so a statue shot that spawns pops ObjUpdateAll (JS `true`); ebSetup
// returns X = 0 (s165); aiBank15Fall79 returns X = $2F.
using ReC.Game.States;
using ReC.Game.World;

namespace ReC.Game.Obj;

public static class AiBoss
{
    private const int XLo = 0x70, XHi = 0x90, YLo = 0xB0, YHi = 0xD0;
    private const int Meta = 0x0300, Flags2 = 0x0340, MetaDir = 0x0360, Flags = 0x0380, Coll = 0x03A0, Hp = 0x03C0;
    private const int ShotId = 0x0440, Timer = 0x0480, Active = 0x04A0, Anim = 0x04E0, Freeze = 0x05A0, Phase = 0x0600;
    private const int Timer2 = 0x0620, ActTimer = 0x0640, Aux = 0x0660, Param1 = 0x0680, Param2 = 0x06A0;
    private const int CamLock = 0x07D7, Overlay = 0x0580, Behav = 0x06E0;

    private delegate IEnumerable<Y>? Ph(Machine m, int x, int c, AiRet r);

    // js: game/obj/ai_boss.js unported
    private static void Unported(Machine m, string what)
    {
        if (m.Unported.Add(what)) m.Log($"{what} not ported yet");
    }

    // js: game/obj/ai_boss.js inc
    private static int Inc(Machine m, int a) { int v = (m.Rd(a) + 1) & 0xFF; m.Wr(a, v); return v; }

    // js: game/obj/ai_boss.js dec
    private static int Dec(Machine m, int a) { int v = (m.Rd(a) - 1) & 0xFF; m.Wr(a, v); return v; }

    // js: game/obj/ai_boss.js clearEnemySlots
    /// <summary>ClearEnemySlots 15:BB29: slots $0D-$1E except X and AIs $78/$7F/$30/$39 off, then slots 4-11 off</summary>
    public static void ClearEnemySlots(Machine m, int x)
    {
        for (int y = 0x0D; y != 0x1F; y++)
        {
            m.Wr(0x10, y);
            if (x == y) continue;
            int a = m.Rd(Active + y);
            if (a == 0x78 || a == 0x7F || a == 0x30 || a == 0x39) continue;
            m.Wr(Active + y, 0);
        }
        for (int y = 4; y < 0x0C; y++) m.Wr(Active + y, 0);
    }

    // js: game/obj/ai_boss.js introMusic
    /// <summary>the shared intro tail: once the camera is aligned ($07D5): player freeze 0 and boss music (Y = $0C)</summary>
    private static void IntroMusic(Machine m)
    {
        m.Wr(Freeze, 0);
        PlaySound.Play(m, 0x12, 0x0C);
    }

    // js: game/obj/ai_boss.js stepMove
    /// <summary>ObjStepVector(meta_dir) + 26:98A8 + 26:9824 + 26:9897 (the move tail at 15:AA37)</summary>
    private static void StepMove(Machine m, int x)
    {
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.TempAddOffset(m);
        ObjUtil.TempToObjPos(m, x);
    }

    // ---- AI $E0 BossAiE0 15:A477 ----

    // js: game/obj/ai_boss.js bossSmokeRing
    /// <summary>BossSmokeRing 15:A5D5: 8 x SpawnShot($3C, Y = $1F) with obj_freeze = $1F (8 ... 1)</summary>
    private static void BossSmokeRing(Machine m, int x)
    {
        m.Wr(0x1F, 8);
        do
        {
            if (!ObjUtil.SpawnShot(m, x, 0x3C, m.Rd(0x1F))) return;
            m.Wr(Freeze + m.Rd(0x10), m.Rd(0x1F));
        } while (Dec(m, 0x1F) != 0);
    }

    // js: game/obj/ai_boss.js bossE0Fire
    /// <summary>BossE0Fire 15:A5A2: obj_aux &amp; 3 = 0 -> 2 shots with param1 = +2 / -2, param2 = -4 above the player else +4</summary>
    private static void BossE0Fire(Machine m, int x)
    {
        if ((m.Rd(Aux + x) & 3) != 0) return;
        m.Wr(0x2F, 2);
        do
        {
            if (!ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), m.Rd(0x2F))) return;
            int n = m.Rd(0x10);
            m.Wr(Param1 + n, ((m.Rd(0x2F) << 2) - 6) & 0xFF);
            m.Wr(Param2 + n, m.Rd(YLo + x) < m.Rd(YLo) ? 0xFC : 0x04);
        } while (Dec(m, 0x2F) != 0);
    }

    // js: game/obj/ai_boss.js e0Intro
    private static void E0Intro(Machine m, int x)
    {                                                             // A48B
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        ClearEnemySlots(m, x);
        m.Wr(0x07E6, 0x21); m.Wr(0x07E7, 0x21);
        m.Wr(0x07F4, 0x6C); m.Wr(0x07F5, 0x6C);
        m.Wr(Freeze, 0x10);
        m.Wr(CamLock, 0xFF);
        if (m.Rd(0x07D5) == 0) return;
        IntroMusic(m);
        m.Wr(Phase + x, (m.Rd(Phase + x) + 2) & 0xFF);
    }

    // js: game/obj/ai_boss.js e0Wait
    private static void E0Wait(Machine m, int x)
    {                                                             // A4C3 (unused phase 1)
        if (Dec(m, Timer + x) == 0) Inc(m, Phase + x);
    }

    // js: game/obj/ai_boss.js e0Smoke
    private static void E0Smoke(Machine m, int x)
    {                                                             // A4CC
        BossSmokeRing(m, x);
        m.Wr(Anim + x, 0);
        Inc(m, Phase + x);
    }

    // js: game/obj/ai_boss.js e0Vanish
    private static void E0Vanish(Machine m, int x)
    {                                                             // A4D8
        if ((Inc(m, Anim + x) & 0x0F) != 0) return;
        m.Wr(Anim + x, 0);
        m.Wr(Coll + x, m.Rd(Coll + x) & 0x0F);
        m.Wr(Flags + x, 1);
        m.Wr(Meta + x, 0x4C);
        Inc(m, Phase + x);
    }

    // js: game/obj/ai_boss.js e0Teleport
    private static void E0Teleport(Machine m, int x)
    {                                                             // A50B
        int a = (m.Rd(Aux + x) + 1) & 0x0F;
        m.Wr(Aux + x, a);
        if (a < 0x0C && Inc(m, Anim + x) != 0x80) return;
        Inc(m, Phase + x);                                        // A524
        m.Wr(Timer2 + x, 0);
        int s = Prg.CpuRd(m, 0xA4FB + (m.Rd(0x08) & 0x0F));       // BossE0Spots
        m.Wr(XLo + x, s & 0xF0);
        m.Wr(YLo + x, (s << 4) & 0xFF);
        m.Wr(XHi + x, m.Rd(XHi));
        m.Wr(YHi + x, m.Rd(YHi));
    }

    // js: game/obj/ai_boss.js e0Appear
    private static void E0Appear(Machine m, int x)
    {                                                             // A54A
        m.Wr(Meta + x, 0x65);
        m.Wr(Coll + x, m.Rd(Coll + x) | 0x30);
        BossSmokeRing(m, x);
        m.Wr(Anim + x, 0);
        Inc(m, Phase + x);
        BossE0Fire(m, x);
    }

    // js: game/obj/ai_boss.js e0Hold
    private static void E0Hold(Machine m, int x)
    {                                                             // A565
        if ((Inc(m, Anim + x) & 0x0F) != 0) return;
        m.Wr(Anim + x, ((m.Rd(Hp + x) << 2) + 0x10) & 0xFF);
        Inc(m, Phase + x);
    }

    // js: game/obj/ai_boss.js e0Linger
    private static void E0Linger(Machine m, int x)
    {                                                             // A581
        if ((m.Rd(Flags2 + x) & 0x80) == 0)
        {
            if (Dec(m, Anim + x) != 0) return;
        }
        ObjUtil.DirToTarget(m, x, 1);                             // A592
        m.Wr(MetaDir + x, m.Rd(0x20));
        m.Wr(Phase + x, 2);
    }

    // js: game/obj/ai_boss.js bossAiE0
    public static IEnumerable<Y>? BossAiE0(Machine m, int x, AiRet r)
    {
        AiFinal.HitFlashPal(m, x, 0xC0);
        if (m.Rd(Phase + x) != 0) m.Wr(CamLock, 0x10);
        return BossPhaseDispatch(m, x, 0x00, r);
    }

    // ---- AI $61 BossHomingShot 15:A5EE ----

    // js: game/obj/ai_boss.js clampVel3
    /// <summary>ClampVel3 15:A665</summary>
    private static int ClampVel3(int a)
    {
        if ((a & 0x80) != 0) return a >= 0xFE ? a : 0xFD;
        return a < 3 ? a : 3;
    }

    // js: game/obj/ai_boss.js homingMove
    /// <summary>HomingMove 15:A602: velocity (param1, param2) nudged +-1 toward $34-$37 every 8 obj_anim steps</summary>
    public static void HomingMove(Machine m, int x)
    {
        m.Wr(MetaDir + x, (m.Rd(MetaDir) + 4) & 7);
        m.Wr(0x30, m.Rd(Param1 + x));
        m.Wr(0x31, m.Rd(Param2 + x));
        if ((m.Rd(Anim + x) & 7) == 0)
        {
            int sx = ((m.Rd(0x34) < m.Rd(XLo + x) ? 1 : 0) + m.Rd(XHi + x)) & 0xFF;   // SBC hi with the lo borrow
            int hx = (m.Rd(0x35) - sx) & 0xFF;
            int vx = ClampVel3((((hx & 0x80) != 0 ? 0xFF : 1) + m.Rd(Param1 + x)) & 0xFF);
            m.Wr(Param1 + x, vx); m.Wr(0x30, vx);
            int sy = ((m.Rd(0x36) < m.Rd(YLo + x) ? 1 : 0) + m.Rd(YHi + x)) & 0xFF;
            int hy = (m.Rd(0x37) - sy) & 0xFF;
            int vy = ClampVel3((((hy & 0x80) != 0 ? 0xFF : 1) + m.Rd(Param2 + x)) & 0xFF);
            m.Wr(Param2 + x, vy); m.Wr(0x31, vy);
        }
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.TempAddOffset(m);
        ObjUtil.TempToObjPos(m, x);
        Dec(m, Anim + x);
    }

    // js: game/obj/ai_boss.js bossHomingShot
    public static void BossHomingShot(Machine m, int x)
    {
        m.Wr(0x10, x);
        ObjUtil.ObjPosToTemp(m, 1);
        if ((m.Rd(Flags + x) & 0x80) != 0) { m.Wr(Active + x, 0); return; }
        HomingMove(m, x);
    }

    // ---- AI $E3 BossAiE3 15:A95D ----

    // js: game/obj/ai_boss.js e3NewDir
    /// <summary>BossE3NewDir 15:AA89: meta_dir = 15:AA9F[Rand64], phase - 1</summary>
    private static void E3NewDir(Machine m, int x)
    {
        m.Wr(MetaDir + x, Prg.CpuRd(m, 0xAA9F + ObjUtil.Rand64(m)));
        Dec(m, Phase + x);
    }

    // js: game/obj/ai_boss.js turnToward16
    /// <summary>TurnToward16 15:AB25: obj_act_timer one 16-dir step toward A (sign table 15:AD86)</summary>
    private static void TurnToward16(Machine m, int x, int a)
    {
        int t = m.Rd(ActTimer + x);
        int y = (((a ^ 0xFF) + 1) + t) & 0x0F;
        m.Wr(ActTimer + x, (Prg.CpuRd(m, 0xAD86 + y) + t) & 0x0F);
    }

    // js: game/obj/ai_boss.js e3Intro
    private static void E3Intro(Machine m, int x)
    {                                                             // A971
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        m.Wr(Freeze, 0x10);
        m.Wr(CamLock, 0xFF);
        m.Wr(0x07F4, 0x77);
        m.Wr(0x07E7, 0x1C);
        Death.PaletteBuildRom(m, 0);                              // JSR 26:8C0E
        ClearEnemySlots(m, x);
        if (m.Rd(0x07D5) == 0) return;
        IntroMusic(m);
        m.Wr(0x06C3, 0);                                          // pending message 6.0 (area $28) or 24.0
        m.Wr(0x06E3, m.Rd(0x6C) == 0x28 ? 0x06 : 0x18);
        m.Wr(0x41, 0x10);
        m.Wr(Flags + x, 9);
        m.Wr(Coll + x, 0x30);
        Inc(m, Phase + x);
    }

    // js: game/obj/ai_boss.js e3Approach
    private static void E3Approach(Machine m, int x)
    {                                                             // A9C6
        if ((m.Rd(Flags + x) & 0x80) == 0 && (m.Rd(0x08) & 7) == 0 && ObjUtil.Rand64(m) == 0)
        {
            m.Wr(Phase + x, 3);
            m.Wr(Timer + x, 0x4F);
            m.Wr(Anim + x, 0x4F);
            if (m.Rd(0x6C) == 0x28) return;
            ObjUtil.DirToTarget(m, x, 1);
            m.Wr(ActTimer + x, (m.Rd(0x20) << 1) & 0xFF);
            Inc(m, Phase + x);
            return;
        }
        m.Wr(Meta + x, 0xD3);                                     // A9FA
        int t = Dec(m, Timer + x);
        Dec(m, Anim + x);
        if ((t & 7) == 0)
        {
            if (m.Rd(YLo + x) < 0x48) { Inc(m, Phase + x); E3NewDir(m, x); return; }
            ObjUtil.DirToTarget(m, x, 0);
            m.Wr(MetaDir + x, m.Rd(0x20));
            if (m.Rd(0x08) < 0xA0 && m.Rd(0x21) < 2)
            {
                Inc(m, Phase + x);
                m.Wr(Timer + x, 0x30);
            }
        }
        StepMove(m, x);
    }

    // js: game/obj/ai_boss.js e3BackOff
    private static void E3BackOff(Machine m, int x)
    {                                                             // AA46
        Dec(m, Anim + x);
        int t = Dec(m, Timer + x);
        if (t == 0)
        {
            Dec(m, Phase + x);
            m.Wr(MetaDir + x, m.Rd(MetaDir + x) | 4);
            return;
        }
        if ((t & 7) != 0) { StepMove(m, x); return; }
        ObjUtil.DirToTarget(m, x, 0);
        m.Wr(MetaDir + x, Prg.CpuRd(m, 0xAA97 + ((m.Rd(0x20) + 4) & 7)));
        if (m.Rd(YLo + x) >= 0x48)
        {
            int xl = m.Rd(XLo + x);
            if (xl >= 0x30 && xl < 0xD0) { StepMove(m, x); return; }
            m.Wr(MetaDir + x, 0);                                 // AA84
        }
        E3NewDir(m, x);
    }

    // js: game/obj/ai_boss.js e3Slam
    private static void E3Slam(Machine m, int x)
    {                                                             // AAA7
        if (Dec(m, Timer + x) == 0) { Dec(m, Phase + x); E3NewDir(m, x); return; }
        m.Wr(Meta + x, 0xD5);
        m.Wr(MetaDir + x, ((m.Rd(0x0301) << 1) + 4) & 7);
        if ((m.Rd(0x08) & 1) != 0) Dec(m, Anim + x);
        if (((m.Rd(Anim + x) ^ 7) & 0x0F) != 0) return;
        m.Wr(0x07D6, 8);
        PlaySound.Play(m, 0x3D, ((0x0A + m.Rd(Phase + x)) << 1) & 0xFF);   // Y from BossPhaseDispatch
        ObjUtil.SpawnShot(m, x, 0x38, 0);
    }

    // js: game/obj/ai_boss.js e3Sweep
    private static void E3Sweep(Machine m, int x)
    {                                                             // AAE6
        m.Wr(Meta + x, 0xD3);
        int t = Dec(m, Timer + x);
        if (t == 0) { m.Wr(Phase + x, 2); E3NewDir(m, x); return; }
        if (t >= 0x20) return;
        m.Wr(Meta + x, 0xD4);
        if ((t & 1) != 0) return;
        if ((t & 6) == 0)
        {
            ObjUtil.DirToTarget(m, x, 1);
            TurnToward16(m, x, (m.Rd(0x20) << 1) & 0xFF);
        }
        ObjUtil.SpawnShot(m, x, 0x36, m.Rd(ActTimer + x));
    }

    // js: game/obj/ai_boss.js bossAiE3
    public static IEnumerable<Y>? BossAiE3(Machine m, int x, AiRet r)
    {
        if (m.Rd(Phase + x) != 0) m.Wr(CamLock, 4);
        AiFinal.HitFlashPal(m, x, 0xC0);
        return BossPhaseDispatch(m, x, 0x0A, r);
    }

    // ---- AI $64 BossRockShot 15:AB3E ----

    // js: game/obj/ai_boss.js rockByte
    /// <summary>15:AB8A: low 6 bits, sign from b7</summary>
    private static int RockByte(int v) => (v & 0x3F) | ((v & 0x80) != 0 ? 0xC0 : 0);

    // js: game/obj/ai_boss.js bossRockShot
    public static void BossRockShot(Machine m, int x)
    {
        int p = m.Rd(Phase + x);
        if (p == 0)
        {
            Inc(m, Phase + x);
            int r = ObjUtil.Rand64(m);
            m.Wr(0x10, r);
            int saved = m.Rd(0x6E);
            Prg.SetR6(m, ObjUtil.Rand64(m));
            int y = ((((r << 1) & 0xFF) + m.Rd(XLo + x) + (r >> 7)) & 0xFF) ^ m.Rd(YLo + x) ^ m.Rd(0x08);
            m.Wr(0x10, 0); m.Wr(0x11, 0x80);
            m.Wr(0x30, Prg.CpuRd(m, 0x8000 + y));
            m.Wr(0x31, Prg.CpuRd(m, 0x8000 + ((y + 1) & 0xFF)));
            Prg.SetR6(m, saved);
            ObjUtil.ObjPosToTemp(m, x);
            m.Wr(0x10, m.Rd(0x30)); m.Wr(0x30, RockByte(m.Rd(0x30)));
            m.Wr(0x10, m.Rd(0x31)); m.Wr(0x31, RockByte(m.Rd(0x31)));
            ObjUtil.TempAddOffset(m);
            ObjUtil.TempToObjPos(m, x);
            return;
        }
        if (p == 1)
        {
            if (Dec(m, Anim + x) != 0) return;
            m.Wr(Anim + x, 0x20);
            m.Wr(Meta + x, 0x9E);
            Inc(m, Phase + x);
            return;
        }
        Dec(m, YLo + x);                                          // ABAD
        if (Dec(m, Anim + x) != 0) return;
        m.Wr(Anim + x, 0x80);
        m.Wr(Active + x, 0x10);
        ObjUtil.DirToTarget(m, x, 0);
        m.Wr(0x20, (m.Rd(0x20) << 1) & 0xFF);
        m.Wr(MetaDir + x, (Prg.CpuRd(m, 0xABD6 + ((x + m.Rd(0x08)) & 3)) + m.Rd(0x20)) & 0xFF);
    }

    // ---- shared by $E6 / $E7 / $E8 (session 104) ----

    // js: game/obj/ai_boss.js introMessage
    /// <summary>the intro's "aligned" tail with a pending message: group/id, sub_state $10 (15:ACE6 / AE26 / AF8A)</summary>
    private static void IntroMessage(Machine m, int group, int id)
    {
        m.Wr(0x06E3, group);
        m.Wr(0x06C3, id);
        m.Wr(0x41, 0x10);
    }

    // js: game/obj/ai_boss.js stepMove16
    /// <summary>ObjPosToTemp + ObjStepVector(meta_dir) + TempAddStep + TempToObjPos (BossE7Step 15:AEBA, tail 15:AD73)</summary>
    private static void StepMove16(Machine m, int x)
    {
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.TempAddStep(m);
        ObjUtil.TempToObjPos(m, x);
    }

    // ---- AI $E5 AiTalkEventE5 15:ABDA (area $43 talk event, session 157) ----

    // js: game/obj/ai_boss.js e5Rise
    private static void E5Rise(Machine m, int x)
    {                                                             // ABE4
        m.Wr(Overlay + x, 0xBA);
        if (Inc(m, Anim + x) != 0x1F) return;
        m.Wr(Meta + x, 0xBB);
        Inc(m, Phase + x);
    }

    // js: game/obj/ai_boss.js e5Settle
    private static void E5Settle(Machine m, int x)
    {                                                             // ABFC
        if (Dec(m, Anim + x) != 0) return;
        Inc(m, Phase + x);
        m.Wr(Meta + x, 0xB0);
        m.Wr(Overlay + x, 0x4C);
    }

    // js: game/obj/ai_boss.js e5Talk
    private static void E5Talk(Machine m, int x)
    {                                                             // AC0F: NpcTalk with obj_timer2_s03 = this slot
        m.Wr(0x41, 0x11);
        m.Wr(0x0623, x);
        Inc(m, Phase + x);
    }

    // js: game/obj/ai_boss.js e5Wait
    private static void E5Wait(Machine m, int x) { Dec(m, Anim + x); }   // AC1A

    // js: game/obj/ai_boss.js e5Leave
    private static void E5Leave(Machine m, int x)
    {                                                             // AC1E
        m.Wr(Meta + x, 0xBB);
        m.Wr(Overlay + x, 0xBA);
        m.Wr(Anim + x, 0);
        Inc(m, Phase + x);
    }

    // js: game/obj/ai_boss.js e5Sink
    private static void E5Sink(Machine m, int x)
    {                                                             // AC31
        if (Inc(m, Anim + x) != 0x1F) return;
        m.Wr(Active + x, 0);
    }

    // js: game/obj/ai_boss.js e5ThrowStart
    private static void E5ThrowStart(Machine m, int x)
    {                                                             // AC41: SpawnShot($0E, dir 4), 8-frame wait
        m.Wr(Meta + x, 0xB1);
        m.Wr(Overlay + x, 0x4C);
        ObjUtil.SpawnShot(m, x, 0x0E, 4);
        m.Wr(Anim + x, 8);
        Inc(m, Phase + x);
    }

    // js: game/obj/ai_boss.js e5Throw
    /// <summary>AC5B: after the wait, slot 0's y + 8 -> slots 0 and 1 every frame until >= $B0; then $0640 = $FF, phase 4</summary>
    private static void E5Throw(Machine m, int x)
    {
        if (Dec(m, Anim + x) != 0) return;
        int y = (m.Rd(YLo) + 8) & 0xFF;
        m.Wr(YLo, y); m.Wr(YLo + 1, y);
        Inc(m, Anim + x);
        if (y < 0xB0) return;
        m.Wr(ActTimer, 0xFF);
        m.Wr(Phase + x, 4);
    }

    // js: game/obj/ai_boss.js aiTalkEventE5
    public static IEnumerable<Y>? AiTalkEventE5(Machine m, int x, AiRet r)
    {
        m.Wr(Freeze, 0x10);
        return BossPhaseDispatch(m, x, 0x0F, r);
    }

    // ---- AI $E6 BossAiE6 15:AC7B ----

    // js: game/obj/ai_boss.js e6Intro
    private static void E6Intro(Machine m, int x)
    {                                                             // AC8F
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        m.Wr(CamLock, 0xFF);
        m.Wr(Freeze, 0x10);
        ClearEnemySlots(m, x);
        m.Wr(0x07F4, 0x75);
        if (m.Rd(0x07D5) == 0) return;
        IntroMusic(m);                                            // every frame until obj_timer2 reaches $40
        Inc(m, Timer2 + x);
        m.Wr(Freeze, 0);
        if (m.Rd(Timer2 + x) != 0x40) return;
        Inc(m, Phase + x);
        m.Wr(Flags + x, 0x0B);
        m.Wr(Coll + x, 0x30);
        m.Wr(Overlay + x, 0x4C);
        m.Wr(MetaDir + x, 8);
        m.Wr(Timer2 + x, 0x50);
        if (m.Rd(0x6C) == 0xAC) IntroMessage(m, 0x18, 0x02);
    }

    // js: game/obj/ai_boss.js e6Fire
    /// <summary>15:AD96: the fire fan (meta $DC + overlay $DD), a shot every 8 obj_timer2 steps in (timer2>>4) - 2 + dir</summary>
    private static void E6Fire(Machine m, int x)
    {
        m.Wr(Overlay + x, 0xDD);
        m.Wr(Meta + x, 0xDC);
        if (Dec(m, Timer2 + x) == 0) { m.Wr(Meta + x, 0xDB); m.Wr(Overlay + x, 0x4C); }
        Inc(m, Anim + x); Inc(m, Anim + x);
        int t = m.Rd(Timer2 + x);
        if ((t & 7) != 0) return;
        ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), ((t >> 4) - 2 + m.Rd(MetaDir + x)) & 0x0F);
    }

    // js: game/obj/ai_boss.js e6Fight
    private static void E6Fight(Machine m, int x)
    {                                                             // ACF5
        if ((m.Rd(0x08) & 0x3F) == 0 && m.Rd(0x6C) == 0xAC) ObjUtil.SpawnShot(m, x, 0x35, 0);
        if (m.Rd(Timer2 + x) != 0) { E6Fire(m, x); return; }
        if ((Dec(m, Anim + x) & 7) == 0)
        {
            int py = m.Rd(YLo);                                   // a fake player y of $30 / $70
            m.Wr(YLo, ((m.Rd(0x08) & 0x80) >> 1) | 0x30);
            ObjUtil.DirToTarget(m, x, 0);
            m.Wr(YLo, py);
            int d = m.Rd(MetaDir + x);
            int y = ((((m.Rd(0x20) << 1) & 0xFF) ^ 0xFF) + 1 + d) & 0x0F;
            m.Wr(MetaDir + x, (Prg.CpuRd(m, 0xAD86 + y) + d) & 0x0F);
            if (ObjUtil.Rand64(m) == 0)
            {
                ObjUtil.DirToTarget(m, x, 1);
                m.Wr(Timer2 + x, 0x50);
                m.Wr(MetaDir + x, (m.Rd(0x20) << 1) & 0xFF);
            }
        }
        int h = m.Rd(Hp + x) >> 4;                                // AD5E: speed $0E $0D $0F once HP < $30
        if (h < 3) m.Wr(Flags2 + x, Prg.CpuRd(m, 0xAD82 + h));
        Dec(m, Timer + x);
        StepMove16(m, x);
    }

    // js: game/obj/ai_boss.js bossAiE6
    public static IEnumerable<Y>? BossAiE6(Machine m, int x, AiRet r)
    {
        if (m.Rd(Phase + x) != 0) m.Wr(CamLock, 4);
        AiFinal.HitFlashPal(m, x, 0xC0);
        return BossPhaseDispatch(m, x, 0x17, r);
    }

    // ---- AI $E7 AiBoss88 15:ADD4 ----

    // js: game/obj/ai_boss.js e7Intro
    private static void E7Intro(Machine m, int x)
    {                                                             // ADE8
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        ClearEnemySlots(m, x);
        m.Wr(0x07F4, 0x76);
        m.Wr(0x07E6, 0x1E);
        m.Wr(Freeze, 0x10);
        m.Wr(CamLock, 0xFF);
        if (m.Rd(0x07D5) == 0) return;
        IntroMusic(m);
        Inc(m, Phase + x);
        m.Wr(Coll + x, 0x30);
        m.Wr(Flags + x, 9);
        if (m.Rd(0x6C) == 0xB9) IntroMessage(m, 0x18, 0x04);
    }

    // js: game/obj/ai_boss.js e7Pose
    private static void E7Pose(Machine m, int x)
    {                                                             // AE35
        Inc(m, Phase + x);
        m.Wr(Meta + x, 0xD8);
        m.Wr(Anim + x, 0x0F);
    }

    // js: game/obj/ai_boss.js e7Wait
    private static void E7Wait(Machine m, int x)
    {                                                             // AE43
        if (Dec(m, Anim + x) != 0) return;
        Inc(m, Phase + x);
        m.Wr(Meta + x, 0xD7);
        m.Wr(ActTimer + x, (m.Rd(Hp + x) + 0x20) & 0xFF);
    }

    // js: game/obj/ai_boss.js e7Bounce
    private static void E7Bounce(Machine m, int x)
    {                                                             // AE5A
        Dec(m, Timer + x);
        Dec(m, Anim + x); Dec(m, Anim + x);
        StepMove16(m, x);
        int yl = m.Rd(YLo + x), xl = m.Rd(XLo + x);
        int edge = -1;
        if (yl < 0x15) edge = 0;
        else if (yl >= 0xD0) edge = 0x10;
        else if (xl < 0x20) edge = 0x18;
        else if (xl >= 0xE0) edge = 0x08;
        if (edge >= 0)
        {                                                         // AE82
            m.Wr(0x10, edge);
            PlaySound.Play(m, 0x3D, edge);
            m.Wr(0x07D6, 8);
            int r = ObjUtil.Rand64(m);
            m.Wr(MetaDir + x, Prg.CpuRd(m, 0xAF28 + ((r + m.Rd(0x10)) & 0xFF)));   // BossE7BounceDirs
            StepMove16(m, x);
        }
        if ((Dec(m, Timer2 + x) & 1) == 0) return;                // AE9E
        if (Dec(m, ActTimer + x) != 0) return;
        Inc(m, Phase + x);
        m.Wr(Meta + x, 0xD8);
        m.Wr(Anim + x, 0);
    }

    // js: game/obj/ai_boss.js e7Aim
    private static void E7Aim(Machine m, int x)
    {                                                             // AEC9
        if (Inc(m, Anim + x) != 0x0F) return;
        Inc(m, Phase + x);
        m.Wr(Anim + x, 0x28);
        ObjUtil.DirToTarget(m, x, 1);
        m.Wr(ActTimer + x, (m.Rd(0x20) << 1) & 0xFF);
        m.Wr(Meta + x, 0xD9);
    }

    // js: game/obj/ai_boss.js e7Volley
    private static void E7Volley(Machine m, int x)
    {                                                             // AEEC
        if (m.Rd(Anim + x) == 0) { m.Wr(Phase + x, 1); return; }
        int a = Dec(m, Anim + x);
        if (((m.Rd(0x6C) == 0xB9 ? 3 : 7) & a) != 0) return;
        int d = (m.Rd(ActTimer + x) - 2) & 0x0F;                  // the shifted masked value is always 0: aim - 2
        m.Wr(0x1F, d);
        if (!ObjUtil.SpawnShot(m, x, 0x5C, d >> 1)) return;
        m.Wr(MetaDir + m.Rd(0x10), m.Rd(0x1F));
    }

    // js: game/obj/ai_boss.js aiBoss88
    public static IEnumerable<Y>? AiBoss88(Machine m, int x, AiRet r)
    {
        if (m.Rd(Phase + x) != 0) m.Wr(CamLock, 4);
        AiFinal.HitFlashPal(m, x, 0xC0);
        return BossPhaseDispatch(m, x, 0x1B, r);
    }

    // ---- AI $E8 BossAiE8 15:AF48 ----

    // js: game/obj/ai_boss.js e8Intro
    private static void E8Intro(Machine m, int x)
    {                                                             // AF5C
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        m.Wr(CamLock, 0xFF);
        m.Wr(0x07E7, 0x1F);
        m.Wr(0x07F4, 0x74);
        m.Wr(Freeze, 0x10);
        ClearEnemySlots(m, x);
        if (m.Rd(0x07D5) == 0) return;
        IntroMusic(m);
        Inc(m, Phase + x);
        IntroMessage(m, 0x18, 0x08);
        m.Wr(Flags2 + x, 0x0D);
        m.Wr(Coll + x, 0x30);
        m.Wr(Flags + x, 0x2B);
        m.Wr(Overlay + x, 0x4C);
    }

    // js: game/obj/ai_boss.js e8Route
    private static void E8Route(Machine m, int x)
    {                                                             // AFAD
        m.Wr(Overlay + x, 0x4C);
        m.Wr(Meta + x, 0xDB);
        m.Wr(MetaDir + x, (m.Rd(0x0301) << 2) & 6);
        int y = (m.Rd(Timer2 + x) << 1) & 0xFF;
        int a = Prg.CpuRd(m, 0xB02B + y);                         // BossE8Route
        if (a == 0xFF)
        {
            Inc(m, Phase + x);
            m.Wr(ActTimer + x, 0x50);
            m.Wr(Aux + x, ObjUtil.Rand64(m));
            return;
        }
        if (a == 0xFE)
        {
            m.Wr(Anim + x, 0); m.Wr(Timer2 + x, 0);
            y = 0; a = Prg.CpuRd(m, 0xB02B);
        }
        int b = Prg.CpuRd(m, 0xB02C + y);
        m.Wr(0x34, (a << 4) & 0xFF); m.Wr(0x35, 0);
        m.Wr(0x36, (b << 4) & 0xFF); m.Wr(0x37, b >> 4);
        m.Wr(0x1F, x);
        ObjUtil.TempToObjPos(m, 0x1F);                            // the waypoint as a dummy object in slot $1F
        HomingMove(m, x);
        ObjUtil.DirToTarget(m, x, 0x1F);
        if (Dec(m, Anim + x) != 0 && m.Rd(0x21) != 0) return;
        Inc(m, Timer2 + x);
        m.Wr(Anim + x, 0);
    }

    // js: game/obj/ai_boss.js e8AttackPose
    /// <summary>15:B0A5</summary>
    private static void E8AttackPose(Machine m, int x)
    {
        m.Wr(Overlay + x, 0xF1);
        m.Wr(Meta + x, 0xDE);
        Dec(m, Anim + x); Dec(m, Anim + x);
    }

    // js: game/obj/ai_boss.js e8LegEnd
    /// <summary>15:B077: past the FF, dir 8, phase 1</summary>
    private static void E8LegEnd(Machine m, int x)
    {
        Inc(m, Timer2 + x);
        m.Wr(Phase + x, 1);
        m.Wr(MetaDir + x, 8);
        m.Wr(Anim + x, 0);
    }

    // js: game/obj/ai_boss.js e8Attack
    private static void E8Attack(Machine m, int x)
    {                                                             // B051
        E8AttackPose(m, x);
        int t = Dec(m, ActTimer + x);
        if (t == 0) { E8LegEnd(m, x); return; }
        if (m.Rd(Aux + x) < 2)
        {                                                         // B08A: one aimed shot at act_timer $20
            if (t != 0x20) return;
            ObjUtil.DirToTarget(m, x, 1);
            ObjUtil.SpawnShot(m, x, 0x46, m.Rd(0x20));
            return;
        }
        if ((t & 0x0F) != 0) return;
        ObjUtil.SpawnShot(m, x, 0x33, ((t >> 2) + 2) & 0x0F);
    }

    // js: game/obj/ai_boss.js bossAiE8
    public static IEnumerable<Y>? BossAiE8(Machine m, int x, AiRet r)
    {
        if (m.Rd(Phase + x) != 0) m.Wr(CamLock, 4);
        AiFinal.HitFlashPal(m, x, 0xC0);
        return BossPhaseDispatch(m, x, 0x21, r);
    }

    // ---- AI $E9 BossAiE9 15:B0B6 (statues, session 156) ----

    // js: game/obj/ai_boss.js e9Intro
    private static void E9Intro(Machine m, int x)
    {                                                             // B0C5
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        m.Wr(CamLock, 0xFF);
        m.Wr(Freeze, 0x10);
        if (m.Rd(0x07D5) == 0) return;
        m.Wr(Freeze, 0);
        Inc(m, Phase + x);
        if (x == 0x0D) IntroMessage(m, 0x1B, 0x03);
    }

    // js: game/obj/ai_boss.js e9Fire
    /// <summary>B105 (also phase 2): obj_anim -= 4; at $20 one shot in DirToTarget*2 + 15:B12C[Rand64 &amp; 3].
    /// Returns SpawnShot's result (JS `true` pops ObjUpdateAll; undefined / false = no pop).</summary>
    private static bool E9Fire(Machine m, int x)
    {
        int a = (m.Rd(Anim + x) - 4) & 0xFF;
        m.Wr(Anim + x, a);
        if (a != 0x20) return false;
        ObjUtil.DirToTarget(m, x, 1);
        m.Wr(0x20, (m.Rd(0x20) << 1) & 0xFF);
        int y = (Prg.CpuRd(m, 0xB12C + (ObjUtil.Rand64(m) & 3)) + m.Rd(0x20)) & 0xFF;
        return ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), y);
    }

    // js: game/obj/ai_boss.js e9Watch
    private static bool E9Watch(Machine m, int x)
    {                                                             // B0F4
        if (m.Rd(Anim + x) != 0) return E9Fire(m, x);
        if (((m.Rd(XLo + x) ^ m.Rd(0x71)) & 0x80) != 0) return false;   // EOR $71 (slot 1's x lo)
        m.Wr(Anim + x, 0x24);
        return false;
    }

    // js: game/obj/ai_boss.js bossAiE9
    public static IEnumerable<Y>? BossAiE9(Machine m, int x, AiRet r)
    {
        if (m.Rd(Phase + x) != 0) m.Wr(CamLock, 0x7F);
        return BossPhaseDispatch(m, x, 0x24, r);
    }

    // ---- AI $EA BossAiEA 15:B130 (session 156) ----

    // js: game/obj/ai_boss.js eaMove
    /// <summary>15:B203: obj_timer--, then BossE7Step's move (98A8 / 8480 / 9861 / 9897)</summary>
    private static void EaMove(Machine m, int x)
    {
        Dec(m, Timer + x);
        StepMove16(m, x);
    }

    // js: game/obj/ai_boss.js eaFire
    /// <summary>15:B278 fire: wind-up above $20, then every 8 frames a 4-way fan (dir, dy) from 15:B2D2</summary>
    private static void EaFire(Machine m, int x)
    {
        if (Dec(m, Timer2 + x) >= 0x20)
        {
            m.Wr(Meta + x, 0xE0); m.Wr(Overlay + x, 0xE1);
            Dec(m, Anim + x);
            return;
        }
        m.Wr(Meta + x, 0xE2); m.Wr(Overlay + x, 0xF2);
        m.Wr(Anim + x, (m.Rd(Anim + x) - 4) & 0xFF);
        if ((m.Rd(0x08) & 7) != 0) return;
        m.Wr(0x1E, 3);
        for (; ; )
        {
            int y = (m.Rd(0x1E) << 1) & 0xFF;
            m.Wr(0x1F, Prg.CpuRd(m, 0xB2D3 + y));
            if (!ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), Prg.CpuRd(m, 0xB2D2 + y))) return;
            int n = m.Rd(0x10);
            m.Wr(YLo + n, (m.Rd(YLo + n) + m.Rd(0x1F)) & 0xFF);
            if ((Dec(m, 0x1E) & 0x80) != 0) return;
        }
    }

    // js: game/obj/ai_boss.js eaIntro
    private static void EaIntro(Machine m, int x)
    {                                                             // B14F
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        m.Wr(Freeze, 0x10);
        m.Wr(0x07F4, 0x79);
        m.Wr(0x07E6, 0x41); m.Wr(0x07E7, 0x40);
        m.Wr(CamLock, 0xFF);
        if (m.Rd(0x07D5) == 0) return;
        m.Wr(Flags + x, 0x0B);
        m.Wr(Overlay + x, 0x4C);
        m.Wr(Coll + x, 0x30);
        m.Wr(Freeze, 0);
        PlaySound.Play(m, 0x12, 0x4E);                            // Y = BossPhaseDispatch's ($27 + 0) * 2
        m.Wr(0x06C3, m.Rd(0x6C) == 0xA6 ? 0x04 : 0x00);
        m.Wr(0x06E3, 0x1B);
        m.Wr(0x41, 0x10);
        Inc(m, Phase + x);
    }

    // js: game/obj/ai_boss.js eaPatrol
    private static void EaPatrol(Machine m, int x)
    {                                                             // B1A5
        if (m.Rd(Timer2 + x) != 0) { EaFire(m, x); return; }
        m.Wr(Meta + x, 0xDB); m.Wr(Overlay + x, 0x4C);
        m.Wr(Flags2 + x, 4);
        int a = Dec(m, Anim + x);
        bool face = true;
        if (a < 0xC0)
        {
            if ((a & 0x1F) != 0) face = false;
            else if ((ObjUtil.Rand64(m) & 1) != 0) m.Wr(Timer2 + x, 0x40);
        }
        if (face)
        {                                                         // B1D5: x >= player x (16 bit) -> left
            int c = m.Rd(XLo + x) >= m.Rd(XLo) ? 1 : 0;
            bool ge = m.Rd(XHi + x) - m.Rd(XHi) - (1 - c) >= 0;
            m.Wr(MetaDir + x, ge ? 6 : 2);
        }
        int lo = (m.Rd(XLo) - m.Rd(XLo + x)) & 0xFF;              // B1E6
        m.Wr(0x10, lo);
        int b = m.Rd(XLo) < m.Rd(XLo + x) ? 1 : 0;
        int hi = (m.Rd(XHi) - m.Rd(XHi + x) - b) & 0xFF;
        int d = (hi & 0x80) != 0 ? ((lo ^ 0xFF) - 1) & 0xFF : lo;  // EOR #$FF / SEC / SBC #1 (= -dx - 2)
        if (d < 8) { Inc(m, Phase + x); return; }
        EaMove(m, x);
    }

    // js: game/obj/ai_boss.js eaDive
    private static void EaDive(Machine m, int x)
    {                                                             // B215
        if (m.Rd(YLo + x) >= 0xD0) { Inc(m, Phase + x); return; }
        m.Wr(Flags2 + x, 8);
        m.Wr(MetaDir + x, 4);
        EaMove(m, x);
    }

    // js: game/obj/ai_boss.js eaRise
    private static void EaRise(Machine m, int x)
    {                                                             // B22C
        Dec(m, Anim + x);
        m.Wr(Meta + x, 0xE0); m.Wr(Overlay + x, 0xE1);
        if (m.Rd(Timer2 + x) != 0) { EaFire(m, x); return; }
        int yl = m.Rd(YLo + x);
        if (yl < 0xA0)
        {
            if ((((yl - 0x20) & 0xFF) & 0x3F) == 0x20) m.Wr(Timer2 + x, 0x40);
            if (yl < 0x30)
            {
                m.Wr(Phase + x, 1);
                m.Wr(MetaDir + x, (ObjUtil.Rand64(m) & 1) != 0 ? 6 : 2);
                return;
            }
        }
        m.Wr(Flags2 + x, 2);                                      // B26B
        m.Wr(MetaDir + x, 0);
        EaMove(m, x);
    }

    // js: game/obj/ai_boss.js bossAiEA
    public static IEnumerable<Y>? BossAiEA(Machine m, int x, AiRet r)
    {
        AiFinal.HitFlashPal(m, x, 0xC0);
        if (m.Rd(0x6C) == 0xA6) m.Wr(Hp + x, 0xFF);
        if (m.Rd(Phase + x) != 0) m.Wr(CamLock, 0x10);
        return BossPhaseDispatch(m, x, 0x27, r);
    }

    // ---- AI $6C-$6E AiReward6C 15:B728 (the boss reward drop, type $0F; obj_phase = BossAreaList index) ----

    private static bool RewardIdle(int a) => a == 0x78 || a == 0x70 || a == 0x7F || a == 0x30 || a == 0x39 || a == 0x00;

    // js: game/obj/ai_boss.js aiReward6C
    public static void AiReward6C(Machine m, int x)
    {
        if ((m.Rd(0x0560 + x) & 0x80) != 0)
        {                                                         // B72B: becomes type $B3 (AI $70 phase 1)
            m.Wr(0x10, x); m.Wr(0x11, 0xB3);
            ObjInit.FarObjInit(m);
            return;
        }
        if (m.Rd(Timer2 + x) == 0)
        {
            bool busy = false;
            for (int y = 0x0D; y != 0x1F; y++)
            {                                                     // B740: $10 = Y each pass
                m.Wr(0x10, y);
                if (x == y) continue;
                if (!RewardIdle(m.Rd(Active + y))) { busy = true; break; }
            }
            if (!busy)
            {
                int ph = m.Rd(Phase + x);
                if (ph != 3)
                {
                    Inc(m, Timer2 + x);
                    int mus = m.Rd(0x0102);
                    m.Wr(0x0102, 0);
                    PlaySound.Play(m, mus, 0);                    // B778
                }
                m.Wr(Coll + x, 0x8D);
                m.Wr(Meta + x, 0xAA);
                int r = 0xB7C1 + ((ph * 5) & 0xFF);
                if (Prg.CpuRd(m, r) != 0)
                {
                    m.Wr(0x07E7, Prg.CpuRd(m, r));
                    m.Wr(0x07F5, Prg.CpuRd(m, r + 1));
                    m.Wr(0x0320 + x, Prg.CpuRd(m, r + 2));
                    m.Wr(0x06E3, Prg.CpuRd(m, r + 3));
                    m.Wr(0x06C3, Prg.CpuRd(m, r + 4));
                    if ((Prg.CpuRd(m, r + 3) | Prg.CpuRd(m, r + 4)) != 0) m.Wr(0x41, 0x10);
                    Death.PaletteBuildRom(m, 0);                  // B7B8 JSR 26:8C0E
                }
            }
        }
        m.Wr(CamLock, 0x10);                                      // B7BB
    }

    // ---- AI $62 (= $E2) BossAiE2 15:A675 (s163) ----

    // js: game/obj/ai_boss.js bossBuildParts
    /// <summary>BossBuildParts 15:A73C: Y = index into 15:A814 (x lo, y lo, meta; $FF ends): slots $0E+ become type $C2 at
    /// slot $0D's hi bytes; then every later slot up to $1E is turned off unless its AI is $7F.</summary>
    private static void BossBuildParts(Machine m, int y)
    {
        int x = 0x0E;
        for (; ; )
        {
            int a = Prg.CpuRd(m, 0xA814 + y);
            if (a == 0xFF) break;
            m.Wr(ZpX(XLo, x), a);
            m.Wr(ZpX(YLo, x), Prg.CpuRd(m, 0xA815 + y));
            m.Wr(ZpX(XHi, x), m.Rd(0x9D));
            m.Wr(ZpX(YHi, x), m.Rd(0xDD));
            ObjInitAt(m, x, 0xC2);
            m.Wr(Meta + x, Prg.CpuRd(m, 0xA816 + y));
            y = (y + 3) & 0xFF;
            x = (x + 1) & 0xFF;
        }
        do
        {                                                         // A76A
            if (m.Rd(Active + x) != 0x7F) m.Wr(Active + x, 0);
            x = (x + 1) & 0xFF;
        } while (x != 0x1F);
    }

    // js: game/obj/ai_boss.js zpX
    private static int ZpX(int @base, int x) => (@base + x) & 0xFF;

    // js: game/obj/ai_boss.js partAnims
    /// <summary>the leg animation tail shared by BossEBMove and BossE2Walk: slots $0E/$0F/$10/$11 from obj_anim_s13</summary>
    private static void PartAnims(Machine m)
    {
        int a = m.Rd(0x04F3);
        m.Wr(0x04EF, a);
        m.Wr(0x04EE, (a + 0x10) & 0xFF);
        int b = ((a ^ 0xFF) + 9) & 0xFF;
        m.Wr(0x04F1, b);
        m.Wr(0x04F0, (b + 0x10) & 0xFF);
    }

    // js: game/obj/ai_boss.js stepSlot
    private static void StepSlot(Machine m, int x) { ObjUtil.ObjPosToTemp(m, x); ObjUtil.TempAddStep(m); ObjUtil.TempToObjPos(m, x); }

    // js: game/obj/ai_boss.js bossEBMove
    /// <summary>BossEBMove 15:A8FF: one ObjStepVector step for the boss and slots $0E-$12, the negated step for slot $13,
    /// then the part animations</summary>
    private static void BossEBMove(Machine m, int x)
    {
        m.Wr(0x2F, x);
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.TempAddStep(m);
        ObjUtil.TempToObjPos(m, x);
        for (int s = 0x0E; s != 0x13; s++) StepSlot(m, s);
        m.Wr(0x30, (-m.Rd(0x30)) & 0xFF);
        m.Wr(0x31, (-m.Rd(0x31)) & 0xFF);
        StepSlot(m, 0x13);
        PartAnims(m);
    }

    // js: game/obj/ai_boss.js aiBank15Fall79
    /// <summary>AI $79 27:A90C (no object type uses it; s169): SetR7($0F) swaps out the running window, so the RTS lands
    /// on 15:A911 mid-BossEBMove (ASL $A820 = vertical mirroring); see the JS. Returns X = $2F.</summary>
    public static int AiBank15Fall79(Machine m, int x)
    {
        Prg.SetR7(m, 0x0F);
        m.Ppu.Mirroring = 0x32 & 1;                               // ASL $A820: $99 (dummy write), then $32
        ObjUtil.TempAddStep(m);                                   // A915 JSR 26:9861
        ObjUtil.TempToObjPos(m, x);                               // A918 JSR 26:9897
        for (x = (x + 1) & 0xFF; x != 0x13; x = (x + 1) & 0xFF) StepSlot(m, x);
        m.Wr(0x30, (-m.Rd(0x30)) & 0xFF);
        m.Wr(0x31, (-m.Rd(0x31)) & 0xFF);
        StepSlot(m, 0x13);
        PartAnims(m);
        return m.Rd(0x2F);
    }

    // js: game/obj/ai_boss.js e2Intro
    /// <summary>phase 0 15:A684: intro + BG redraw (see the JS)</summary>
    private static IEnumerable<Y> E2Intro(Machine m, int x)
    {                                                             // A684
        m.Wr(Freeze, 0x10);
        if (m.Rd(0x07D5) == 0) yield break;
        PlaySound.Play(m, 0x12, 0x10);                            // Y = BossPhaseDispatch's (8 + 0) * 2
        Inc(m, Phase + x);
        m.Wr(0x62FE, 0);
        m.Wr(0x07F1, 0x2C); m.Wr(0x07F4, 0x78); m.Wr(0x07F5, 0x6F);
        m.Wr(0x07E0, 0xAF); m.Wr(0x07E1, 0xAF); m.Wr(0x07E2, 0xAF);
        m.Wr(0x07E6, 0x4F); m.Wr(0x07E7, 0x4F);
        Death.PaletteBuildRom(m, 0);                              // A6C0 JSR 26:8C0E
        BossBuildParts(m, 0);
        m.Wr(0x07E6, 0x16); m.Wr(0x07E7, 0x17);
        m.Wr(0x07E8, 0x38);                                       // pal_nofade_mask
        FarSpritePass(m);
        yield return Y.Spin;                                      // s163: CPU overrun, the NMI hits in this sprite pass
        foreach (var s in Death.FadeIn(m, 0x10)) yield return s;  // A6DB JSR 26:8C49
        foreach (var s in Death.FadeOut(m, 0x10)) yield return s; // A6E0 JSR 26:8C3D
        BossBuildParts(m, 0x13);
        m.Wr(0x030D, 0xC8);
        m.Wr(0x7D, 0x88);
        m.Wr(0xBD, 0x60);
        FarSpritePass(m);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int i = 0; i < 0x90; i++) m.Wr(0x6000 + i, Prg.CpuRd(m, 0xA77C + i));
        for (int i = 0x17; i >= 0; i--) m.Wr(0x6100 + i, m.Rd(0x6078 + i));
        foreach (var s in Pause.PpuQueueBlock(m, 0x2C)) yield return s;
        foreach (var s in Pause.PpuQueueBlock(m, 0x2D)) yield return s;
        m.Wr(0x07E0, 0x88); m.Wr(0x07E1, 0x89); m.Wr(0x07E2, 0x8A);
        foreach (var s in Death.FadeIn(m, 0x20)) yield return s;
        m.Wr(0x07E8, 0);
        m.Wr(Coll + x, 0x13);                                     // PLA TAX: the boss slot again
    }

    // js: game/obj/ai_boss.js e2Attack
    /// <summary>BossE2Attack 15:A84D (orbit_level != 0): see the JS</summary>
    private static void E2Attack(Machine m, int x)
    {
        m.Wr(Anim + x, 8);
        if ((Dec(m, 0x06C0 + x) & 7) != 0) return;
        if (ObjUtil.Rand64(m) != 0) { ObjUtil.SpawnShot(m, x, 0x3A, 4); return; }
        if (!ObjUtil.SpawnShot(m, x, 0x39, m.Rd(0x0E))) return;
        m.Wr(0x11, x);
        int n = m.Rd(0x10);
        m.Wr(0x0320 + n, 0x40);
        m.Wr(ZpX(YHi, n), (m.Rd(ZpX(YHi, n)) - 1) & 0xFF);
    }

    // js: game/obj/ai_boss.js e2Walk
    /// <summary>phase 1 15:A87E (c = BossPhaseDispatch's ASL carry): see the JS</summary>
    private static void E2Walk(Machine m, int x, int c)
    {                                                             // A87E
        m.Wr(Anim + x, 0);
        m.Wr(0x51, 7);
        if (m.Rd(0x06C0 + x) != 0) { E2Attack(m, x); return; }
        if (Dec(m, Timer + x) == 0)
        {
            m.Wr(Timer + x, (((ObjUtil.Rand64(m) + m.Rd(0x70) + c) & 0x0F) + 0x20) & 0xFF);
            if (ObjUtil.Rand64(m) >= 2) m.Wr(0x06C0 + x, 0x80);
            int lo = m.Rd(XLo + x) < m.Rd(0x71) ? 1 : 0;           // CMP / SBC 16-bit vs slot 1
            m.Wr(MetaDir + x, (m.Rd(XHi + x) - m.Rd(0x91) - lo) >= 0 ? 6 : 2);
        }
        int xl = m.Rd(XLo + x);                                   // A8BC
        int a = 0;
        if (xl < 0x30) a = 2;
        if (xl >= 0xD8) a = 6;
        if (a != 0) m.Wr(MetaDir + x, a);
        BossEBMove(m, x);
        m.Wr(0x04F3, (m.Rd(0x04F3) - ((m.Rd(0x30) << 1) & 0xFF)) & 0xFF);
        PartAnims(m);
    }

    // js: game/obj/ai_boss.js aiBodyPart
    /// <summary>AI $3F AiBodyPart 27:A414 (type $C2): irq_mode = 7 (BossEBMove moves them)</summary>
    public static void AiBodyPart(Machine m) { m.Wr(0x51, 7); }

    // js: game/obj/ai_boss.js bossAiE2
    public static IEnumerable<Y>? BossAiE2(Machine m, int x, AiRet r)
    {
        AiFinal.HitFlashPal(m, x, 0xC7);
        m.Wr(CamLock, 0xFF);
        return BossPhaseDispatch(m, x, 0x08, r);
    }

    // ---- AI $6B (= $EB) AiBowOfTruth9E 15:B2DA, the final boss (type $9E, area $A6) (s164) ----

    // js: game/obj/ai_boss.js bossBgStrip
    /// <summary>BossBgStrip 15:B6C1: A = strip n; $C0 bytes from bank 7 BossBgStripPtrs[n] into $6000 (page wrap quirk,
    /// see the JS); PpuQueueBlock($2F + n)</summary>
    private static IEnumerable<Y> BossBgStrip(Machine m, int x, int n)
    {
        m.Wr(0x2F, n);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        m.Wr(0x2E, x);
        int y = (n << 1) & 0xFF;
        m.Wr(0x10, Prg.CpuRd(m, 0xB709 + y));
        m.Wr(0x11, Prg.CpuRd(m, 0xB70A + y));
        m.Wr(0x12, 0xC0);
        Prg.SetR6(m, 7);
        int i = 0;
        do
        {
            m.Wr(0x6000 + i, Prg.IndY(m, 0x10, 0));
            i = (i + 1) & 0xFF;
            int lo = (m.Rd(0x10) + 1) & 0xFF;
            m.Wr(0x10, lo);
            if (lo == 0)
            {
                m.Wr(0x10, 0xF0);
                m.Wr(0x11, (m.Rd(0x11) + 1) & 0xFF);
                i = (i - 2) & 0xFF;
            }
        } while (Dec(m, 0x12) != 0);
        Prg.SetR6(m, 0x1A);
        foreach (var s in Pause.PpuQueueBlock(m, (m.Rd(0x2F) + 0x2F) & 0xFF)) yield return s;
    }

    // js: game/obj/ai_boss.js bossBgCycle
    /// <summary>BossBgCycle 15:B4C8: BossBgStrip(obj_behav), obj_behav = (obj_behav + 1) mod 10</summary>
    private static IEnumerable<Y> BossBgCycle(Machine m, int x)
    {
        foreach (var s in BossBgStrip(m, x, m.Rd(Behav + x))) yield return s;
        int y = (m.Rd(Behav + x) + 1) & 0xFF;
        if (y == 0x0A) y = 0;
        m.Wr(Behav + x, y);
    }

    // js: game/obj/ai_boss.js ebHandsIdle
    /// <summary>BossEBHandsIdle 15:B71D</summary>
    private static void EbHandsIdle(Machine m) { m.Wr(0x0310, 0xE9); m.Wr(0x0311, 0xE7); }

    // js: game/obj/ai_boss.js ebSetElem
    /// <summary>BossEBSetElem 15:B618: A -> obj_elem_drop of slots $13 ... $0D</summary>
    private static void EbSetElem(Machine m, int a) { for (int y = 0x13; y != 0x0C; y--) m.Wr(0x0500 + y, a); }

    // js: game/obj/ai_boss.js ebEdgeBounce
    /// <summary>BossEBEdgeBounce 15:B6A3: heading left (dir b3) at x &lt; $38 or right at x >= $D8 -> mirror via 15:B50C</summary>
    private static void EbEdgeBounce(Machine m, int x)
    {
        int d = m.Rd(MetaDir + x), xl = m.Rd(XLo + x);
        if ((d & 8) != 0 ? xl >= 0x38 : xl < 0xD8) return;
        m.Wr(MetaDir + x, Prg.CpuRd(m, 0xB50C + d));
    }

    // js: game/obj/ai_boss.js ebFlameOne
    /// <summary>15:B642: one flame, dir (15:B672[a &amp; 7] + obj_act_timer) &amp; $0F, SpawnShot($2E zp); x -$12 (+$24 when
    /// $2F b7), y + $2D</summary>
    private static void EbFlameOne(Machine m, int x, int a)
    {
        int dir = (Prg.CpuRd(m, 0xB672 + (a & 7)) + m.Rd(ActTimer + x)) & 0x0F;
        if (!ObjUtil.SpawnShot(m, x, m.Rd(0x2E), dir)) return;
        int n = m.Rd(0x10);
        int v = (m.Rd(XLo + n) - 0x12) & 0xFF;
        if ((m.Rd(0x2F) & 0x80) != 0) v = (v + 0x24) & 0xFF;
        m.Wr(XLo + n, v);
        m.Wr(YLo + n, (m.Rd(YLo + n) + m.Rd(0x2D)) & 0xFF);
    }

    // js: game/obj/ai_boss.js ebFlame
    /// <summary>BossEBFlame 15:B623: every 4 frames two flames, wobble index $08 >> 5 and the same + 4 ($2F = 0, then $FF)</summary>
    private static void EbFlame(Machine m, int x)
    {
        if ((m.Rd(0x08) & 3) != 0) return;
        m.Wr(0x2F, 0);
        int a = m.Rd(0x08) >> 5;
        EbFlameOne(m, x, a);
        m.Wr(0x2F, (m.Rd(0x2F) - 1) & 0xFF);
        EbFlameOne(m, x, (a + 4) & 0xFF);
    }

    // js: game/obj/ai_boss.js ebSetup
    /// <summary>phase 0 15:B301 (one call, no camera wait): see the JS. Ends with r.X = 0 (s165: RTS with X = 0 left by the
    /// sprite pass, ObjUpdateAll's INX re-runs slots 1-$1E this pass).</summary>
    private static IEnumerable<Y> EbSetup(Machine m, int x, AiRet r)
    {                                                             // B301
        m.Wr(0x1F, x);
        FarSpritePass(m);
        m.Wr(0x07E8, 0x38);
        foreach (var s in Death.FadeOut(m, 0x10)) yield return s;
        PlaySound.Play(m, 0x0C, 4);                               // Y = 4, left by PaletteLoadOne's colour loop
        m.Wr(0x07F0, 0x2E); m.Wr(0x07F1, 0x2E);
        m.Wr(0x07F4, 0x7A); m.Wr(0x07F5, 0x75);
        m.Wr(0x07E0, 0xA0);
        m.Wr(0x07E1, 0xA1); m.Wr(0x07E1, 0x07);
        m.Wr(0x07E6, 0x43); m.Wr(0x07E7, 0x42);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int i = 0; i < 0x100; i++) m.Wr(0x6000 + i, 0xFF);  // 15:B2F8
        for (int b = 0x39; b != 0x3C; b++)
        {
            foreach (var s in Pause.PpuQueueBlock(m, b)) yield return s;
            m.Wr(0x09, 0);
        }
        for (int i = 0x3F; i >= 0; i--) { m.Wr(0x6100 + i, 0); m.Wr(0x60C0 + i, 0); }
        m.Wr(0x60D3, 0x44); m.Wr(0x6113, 0x44);
        foreach (var s in Pause.PpuQueueBlock(m, 0x3C)) yield return s;
        m.Wr(0x1D, 4);
        do
        {
            foreach (var s in BossBgStrip(m, x, m.Rd(0x1D))) yield return s;
        } while ((Dec(m, 0x1D) & 0x80) == 0);
        BossBuildParts(m, 0x26);
        x = m.Rd(0x1F);
        m.Wr(XLo + x, 0x78); m.Wr(YLo + x, 0x58);
        m.Wr(Meta + x, 0x4C); m.Wr(MetaDir + x, 2);
        Inc(m, Phase + x);
        FarSpritePass(m);
        m.Wr(0x03B0, 0x62); m.Wr(0x0430, 0x40);
        m.Wr(0x03B1, 0x63); m.Wr(0x0431, 0x40);
        foreach (var s in Death.FadeIn(m, 0x10)) yield return s;
        m.Wr(0x07E8, 0);
        r.X = 0;
    }

    // js: game/obj/ai_boss.js ebMouthStart
    /// <summary>15:B541: phase 5 (mouth open) with obj_act_timer = DirToTarget(slot 1) * 2</summary>
    private static void EbMouthStart(Machine m, int x)
    {
        m.Wr(Phase + x, 5);
        ObjUtil.DirToTarget(m, x, 1);
        m.Wr(ActTimer + x, (m.Rd(0x20) << 1) & 0xFF);
    }

    // js: game/obj/ai_boss.js ebHandsEnd
    /// <summary>15:B493 (hand attack over): hands idle; odd Rand64 -> phase 2, dir 15:B4BB[Rand64], speed 15:B4C4[..]</summary>
    private static void EbHandsEnd(Machine m, int x)
    {
        EbHandsIdle(m);
        if ((ObjUtil.Rand64(m) & 1) == 0) return;
        m.Wr(Phase + x, 2);
        m.Wr(MetaDir + x, Prg.CpuRd(m, 0xB4BB + ObjUtil.Rand64(m)));
        int y = (((m.Rd(XLo + x) + m.Rd(0x71)) & 0xFF) >> 1) & 3;
        m.Wr(Flags2 + x, Prg.CpuRd(m, 0xB4C4 + y));
    }

    // js: game/obj/ai_boss.js ebHands
    /// <summary>15:B425 hand attack (obj_timer2 != 0): see the JS</summary>
    private static IEnumerable<Y> EbHands(Machine m, int x)
    {
        if (m.Rd(Behav + x) != 9) foreach (var s in BossBgCycle(m, x)) yield return s;
        if (Dec(m, Timer2 + x) == 0) { EbHandsEnd(m, x); yield break; }
        int t = m.Rd(Timer2 + x);
        if ((t & 7) != 0) yield break;
        int y = (t & 0x18) >> 2;
        m.Wr(0x0310, Prg.CpuRd(m, 0xB48B + y));
        m.Wr(0x0311, Prg.CpuRd(m, 0xB48C + y));
        if ((t & 8) != 0) yield break;
        if (!ObjUtil.SpawnShot(m, x, 0x2E, (ObjUtil.Rand64(m) + 4) & 0x0F)) yield break;
        int n = m.Rd(0x10);
        m.Wr(YLo + n, (m.Rd(YLo + n) - 0x1C) & 0xFF);
        int xl = m.Rd(XLo + n);
        int a = (xl - 0x18) & 0xFF;
        int c = xl >= 0x18 ? 1 : 0;
        int f = ((m.Rd(Timer2 + x) & 0x10) - 1) & 0xFF;           // STA $10 / DEC $10
        m.Wr(0x10, f);
        if ((f & 0x80) == 0) a = (a + 0x30 + c) & 0xFF;           // ADC #$30 with SBC's carry
        m.Wr(XLo + n, a);
        Dec(m, Anim + x);
    }

    // js: game/obj/ai_boss.js ebSway
    /// <summary>phase 1 15:B3BC sway: see the JS</summary>
    private static IEnumerable<Y> EbSway(Machine m, int x)
    {                                                             // B3BC
        m.Wr(Flags2 + x, 1);
        Dec(m, Timer + x);
        if (m.Rd(Timer2 + x) != 0)
        {
            foreach (var s in EbHands(m, x)) yield return s;
            yield break;
        }
        if ((Dec(m, Anim + x) & 0x1F) == 0 && (ObjUtil.Rand64(m) & 1) != 0)
        {
            m.Wr(Timer2 + x, 0xC0);
            if ((m.Rd(0x08) & 0x80) == 0)
            {
                if (((m.Rd(0x71) ^ m.Rd(0xB1)) & 1) == 0) { EbMouthStart(m, x); yield break; }
                m.Wr(Phase + x, 6);
                m.Wr(Timer2 + x, 0x80);
                yield break;
            }
        }
        m.Wr(0x04EE, 0);                                          // B3F7
        EbHandsIdle(m);
        int d = m.Rd(MetaDir + x);
        int xl = m.Rd(XLo + x);
        if (xl < 0x40) d = 2;
        if (xl >= 0xC0) d = 6;
        m.Wr(MetaDir + x, d);
        Dec(m, 0x04EF); Dec(m, 0x04F2);
        BossEBMove(m, x);
        if (m.Rd(Behav + x) != 0) foreach (var s in BossBgCycle(m, x)) yield return s;
    }

    // js: game/obj/ai_boss.js ebDive
    /// <summary>phase 2 15:B4DD dive (to y >= $C0 -> timer2 $20, dir 15:B51C[Rand64], phase 4)</summary>
    private static IEnumerable<Y> EbDive(Machine m, int x)
    {                                                             // B4DD
        Dec(m, 0x04F2); Dec(m, Timer + x);
        if ((m.Rd(Hp + x) & 0x80) != 0) foreach (var s in BossBgCycle(m, x)) yield return s;
        EbEdgeBounce(m, x);
        if (m.Rd(YLo + x) < 0xC0) { BossEBMove(m, x); yield break; }
        m.Wr(Timer2 + x, 0x20);
        m.Wr(MetaDir + x, Prg.CpuRd(m, 0xB51C + ObjUtil.Rand64(m)));
        m.Wr(Phase + x, 4);
    }

    // js: game/obj/ai_boss.js ebRise
    /// <summary>phase 3 15:B524 rise (to y &lt; $50 -> obj_anim = y, phase 5)</summary>
    private static IEnumerable<Y> EbRise(Machine m, int x)
    {                                                             // B524
        Dec(m, 0x04F2); Dec(m, Timer + x);
        if ((m.Rd(Hp + x) & 0x80) != 0) foreach (var s in BossBgCycle(m, x)) yield return s;
        EbEdgeBounce(m, x);
        int yl = m.Rd(YLo + x);
        if (yl >= 0x50) { BossEBMove(m, x); yield break; }
        m.Wr(Anim + x, yl);
        EbMouthStart(m, x);
    }

    // js: game/obj/ai_boss.js ebHold
    /// <summary>phase 4 15:B552 hold: BG cycle unless obj_behav 9; timer2 $10 -> aimed SpawnShot($46); 0 -> phase 3</summary>
    private static IEnumerable<Y> EbHold(Machine m, int x)
    {                                                             // B552
        if (m.Rd(Behav + x) != 9) foreach (var s in BossBgCycle(m, x)) yield return s;
        if (Dec(m, Timer2 + x) == 0x10)
        {
            ObjUtil.DirToTarget(m, x, 1);
            ObjUtil.SpawnShot(m, x, 0x46, m.Rd(0x20));
            yield break;
        }
        m.Wr(0x04EE, 8);
        if (m.Rd(Timer2 + x) != 0) yield break;
        m.Wr(0x04EE, 0);
        m.Wr(Phase + x, 3);
    }

    // js: game/obj/ai_boss.js ebBackToSway
    /// <summary>15:B599: random dir 2 / 6, phase 1, elem $0F (immune again)</summary>
    private static void EbBackToSway(Machine m, int x)
    {
        m.Wr(MetaDir + x, (ObjUtil.Rand64(m) & 1) != 0 ? 6 : 2);
        m.Wr(Phase + x, 1);
        EbSetElem(m, 0x0F);
    }

    // js: game/obj/ai_boss.js ebMouth
    /// <summary>phase 5 15:B587 mouth open: see the JS</summary>
    private static IEnumerable<Y> EbMouth(Machine m, int x)
    {                                                             // B587
        EbSetElem(m, 0);
        if (m.Rd(Behav + x) != 0) foreach (var s in BossBgCycle(m, x)) yield return s;
        if (Dec(m, Timer2 + x) == 0) { EbBackToSway(m, x); yield break; }
        m.Wr(0x2E, 0x30); m.Wr(0x2D, 8);
        EbFlame(m, x);
        if ((m.Rd(Timer2 + x) & 0x0F) != 0) yield break;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        int y = Prg.CpuRd(m, 0xB5F9 + (m.Rd(Timer2 + x) >> 4));
        m.Wr(0x030F, Prg.CpuRd(m, 0xB609 + y));
        int z = (y << 2) & 0xFF;
        for (int i = 0; i < 4; i++) m.Wr(0x6000 + i, Prg.CpuRd(m, 0xB60C + z + i));
        foreach (var s in Pause.PpuQueueBlock(m, 0x3D)) yield return s;
    }

    // js: game/obj/ai_boss.js ebRain
    /// <summary>phase 6 15:B67A rain: every 16 frames SpawnShot($2F) straight down, 16 px up; timer2 0 -> B599</summary>
    private static void EbRain(Machine m, int x)
    {                                                             // B67A
        m.Wr(0x04EE, 8);
        if (Dec(m, Timer2 + x) == 0) { EbBackToSway(m, x); return; }
        if ((m.Rd(Timer2 + x) & 0x0F) != 0) return;
        if (!ObjUtil.SpawnShot(m, x, 0x2F, 4)) return;
        int n = m.Rd(0x10);
        m.Wr(YLo + n, (m.Rd(YLo + n) - 0x10) & 0xFF);
    }

    // js: game/obj/ai_boss.js aiBowOfTruth9E
    public static IEnumerable<Y>? AiBowOfTruth9E(Machine m, int x, AiRet r)
    {
        AiFinal.HitFlashPal(m, x, 0xC7);
        if (m.Rd(Phase + x) != 0)
        {
            m.Wr(CamLock, 0x10);
            if (m.Rd(0x51) != 2) m.Wr(0x51, 7);
        }
        return BossPhaseDispatch(m, x, 0x2B, r);
    }

    // ---- AI $6F BossDefeated 15:B807 (s159) ----

    // js: game/obj/ai_boss.js farSpritePass
    /// <summary>FarCall_B14_800E: one sprite pass with banks $0E mapped</summary>
    private static void FarSpritePass(Machine m)
    {
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);
        Prg.SwitchBank16(m, 0x0E);
        Sprites.SpriteDraw(m);
        Prg.FarRestore(m, r6, r7);
    }

    // js: game/obj/ai_boss.js objInitAt
    private static void ObjInitAt(Machine m, int slot, int type) { m.Wr(0x10, slot); m.Wr(0x11, type); ObjInit.FarObjInit(m); }

    // js: game/obj/ai_boss.js hookB8CF
    /// <summary>15:B8CF (BossRecs index 1, area $1A): WRAM $6000-$607F = $7E, PpuQueueBlock($2C), slots $0E-$13 active $73</summary>
    private static IEnumerable<Y> HookB8CF(Machine m)
    {
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int y = 0x7F; y >= 0; y--) m.Wr(0x6000 + y, 0x7E);
        foreach (var s in Pause.PpuQueueBlock(m, 0x2C)) yield return s;
        for (int y = 0x0E; y != 0x14; y++) m.Wr(Active + y, 0x73);
    }

    // js: game/obj/ai_boss.js hookB8EE
    /// <summary>15:B8EE (index 10, area $9F): the drop's y lo = $60, slot $1E = type $B2</summary>
    private static void HookB8EE(Machine m, int x)
    {
        m.Wr(YLo + x, 0x60);
        ObjInitAt(m, 0x1E, 0xB2);
    }

    // js: game/obj/ai_boss.js bossHookA6
    /// <summary>BossHookA6 15:B8FD (index 11, area $A6): irq_mode 0, slots $0D-$13 off, sprite pass, player pos &lt;- camera,
    /// slot $14 = type ($04D4), slot $15 = type $B3, rendering off, WaitFrame, FullScreenDraw(1), rendering on</summary>
    private static IEnumerable<Y> BossHookA6(Machine m)
    {
        m.Wr(0x51, 0);
        for (int y = 0x0D; y != 0x14; y++) m.Wr(Active + y, 0);
        FarSpritePass(m);
        for (int y = 3; y >= 0; y--) m.Wr(0x34 + y, m.Rd(0x3C + y));
        ObjInitAt(m, 0x14, m.Rd(0x04D4));
        ObjInitAt(m, 0x15, 0xB3);
        m.Wr(0x01, m.Rd(0x01) & 0xE7);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        foreach (var s in BgStream.FullScreenDraw(m, 1, BgStream.FsdLagWf)) yield return s;   // s161: NMIs after rows 9 15 21 27
        m.Wr(0x01, m.Rd(0x01) | 0x18);
    }

    // js: game/obj/ai_boss.js hookB947
    /// <summary>15:B947 (index 13, area $5F): slots $0D-$0F off (the drop too when it sits there), slot $14 = type $B5</summary>
    private static void HookB947(Machine m)
    {
        m.Wr(0x04AD, 0); m.Wr(0x04AE, 0); m.Wr(0x04AF, 0);
        ObjInitAt(m, 0x14, 0xB5);
    }

    // js: game/obj/ai_boss.js bossFleeSpeech
    /// <summary>BossFleeSpeech 15:BA98 (s160; indices 2/4/5 = areas $28/$6E/$F2): see the JS</summary>
    private static IEnumerable<Y> BossFleeSpeech(Machine m, int x)
    {
        foreach (var s in Death.WaitFrames(m, 0x20)) yield return s;
        foreach (int a in new[] { 0x0710, 0x0600, 0x0620, 0x06C0 }) m.Wr(a, 0);
        foreach (var s in Death.StatRecalc(m, x)) yield return s;
        PlaySound.Play(m, 0x80, 5);                               // BAB0: Y = 5 after StatRecalc (tools.emu)
        m.Wr(0x1F, x);
        ObjUtil.ObjPosToTemp(m, x);                               // BAB5 JSR 26:98A8
        ObjInitAt(m, 0x1F, 0x2B);
        ObjUtil.TempToObjPos(m, 0x1F);                            // BAC3 JSR 26:9897 (X = $1F)
        int yy = (m.Rd(Phase + m.Rd(0x1F)) << 1) & 0xFF;
        m.Wr(0x10, Prg.CpuRd(m, 0xBAFE + yy));
        m.Wr(0x11, Prg.CpuRd(m, 0xBAFF + yy));
        m.Wr(0x031F, Prg.IndY(m, 0x10, 1));                       // obj_meta_id_s1F
        m.Wr(0x033F, Prg.IndY(m, 0x10, 2));                       // obj_tile_ofs_s1F
        m.Wr(0x07DE, Prg.IndY(m, 0x10, 3));
        m.Wr(0x07DF, Prg.IndY(m, 0x10, 4));
        ClearEnemySlots(m, m.Rd(0x1F));
        PlaySound.Play(m, 0x29, 0x0C);                            // Y = $0C after ClearEnemySlots
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);                     // FarCall_D354
        foreach (var s in Talk.ShowMessage(m)) yield return s;
        Prg.FarRestore(m, r6, r7);
        do
        {                                                         // BossFleeWalkOff 15:BB5B
            ObjUtil.ObjPosToTemp(m, 0x1F);
            m.Wr(0x31, 0xFD);
            m.Wr(Active + 0x1F, 0xFF);
            m.Wr(0x30, Prg.CpuRd(m, 0xBB8F + (Dec(m, Anim + 0x1F) & 0x1F)));
            ObjUtil.TempAddStep(m);
            ObjUtil.TempToObjPos(m, 0x1F);
            foreach (var s in Death.WaitFrame(m)) yield return s;
            FarSpritePass(m);
        } while (m.Rd(YLo + 0x1F) < 0xC0);
        m.Wr(Active + 0x1F, 0);
    }

    /// <summary>(ai_boss.js BOSS_REC_HOOKS) hook address -> handler(m, x): iterator or null</summary>
    private static readonly Dictionary<int, Func<Machine, int, IEnumerable<Y>?>> BossRecHooks = new()
    {
        [0xB8CF] = (m, x) => HookB8CF(m),
        [0xB8EE] = (m, x) => { HookB8EE(m, x); return null; },
        [0xB8FD] = (m, x) => BossHookA6(m),
        [0xB947] = (m, x) => { HookB947(m); return null; },
        [0xBA98] = BossFleeSpeech,
    };

    private static bool D5Keep(int a) => a == 0 || a == 0x78 || a == 0x7F || a == 0x30 || a == 0x39 || a == 0x70;

    // js: game/obj/ai_boss.js bossDefeated
    /// <summary>AI $6F BossDefeated 15:B807 (type $CF, the end of the boss death chain): see the JS</summary>
    public static IEnumerable<Y> BossDefeated(Machine m, int x)
    {
        ObjInitAt(m, x, 0x0F);                                    // B807
        m.Wr(XLo + x, 0x80);
        m.Wr(YLo + x, 0x90);
        int y = 0;
        while (Prg.CpuRd(m, 0xB95D + y) != m.Rd(0x6C))
        {                                                         // B81A
            y = (y + 1) & 0xFF;
            if (y == 0) break;
        }
        m.Wr(Phase + x, y);
        int yy = (y << 1) & 0xFF;
        m.Wr(0x18, Prg.CpuRd(m, 0xB96B + yy));
        m.Wr(0x19, Prg.CpuRd(m, 0xB96C + yy));
        int r18 = m.Rd(0x18), r19 = m.Rd(0x19);                   // PHA PHA (TXA PHA keeps the slot)
        m.Wr(0x10, 0x4C);
        m.Wr(0x11, Prg.IndY(m, 0x18, 0));
        m.Wr(0x12, Prg.IndY(m, 0x18, 1));
        int hook = m.Rd(0x12) << 8 | m.Rd(0x11);
        if (hook != 0)
        {                                                         // B84B JSR $0010
            if (BossRecHooks.TryGetValue(hook, out var h))
            {
                var g = h(m, x);
                if (g != null) foreach (var s in g) yield return s;
            }
            else Unported(m, $"BossDefeated hook 15:{hook:X4}");
        }
        m.Wr(0x19, r19); m.Wr(0x18, r18);                         // B84E
        m.Wr(0x0102, Prg.IndY(m, 0x18, 3));
        m.Wr(0x0560 + x, Prg.IndY(m, 0x18, 4));
        int i = 5;
        for (m.Wr(0x10, 8); m.Rd(0x10) != 0; Dec(m, 0x10), i++)
        {                                                         // B868: pal ids -> $07E0-$07E7
            int v = Prg.IndY(m, 0x18, i);
            if (v != 0) m.Wr(0x07DB + i, v);
        }
        for (m.Wr(0x10, 6); m.Rd(0x10) != 0; Dec(m, 0x10), i++)
        {                                                         // B878: CHR -> $07F0-$07F5
            int v = Prg.IndY(m, 0x18, i);
            if (v != 0) m.Wr(0x07E3 + i, v);
        }
        m.Wr(0x62FE, Prg.IndY(m, 0x18, 19));
        if (Prg.IndY(m, 0x18, 20) != 0)
        {                                                         // B88A
            for (int s = 0x0E; s != 0x1F; s++)
            {
                if (D5Keep(m.Rd(Active + s) & 0x7F)) continue;
                ObjInitAt(m, s, 0xD5);
                m.Wr(0x0320 + s, 0x80);
                m.Wr(Meta + s, 0xEB);
                m.Wr(Freeze + s, ((s - 0x0E) << 2) & 0xFF);
            }
        }
        Death.PaletteBuildRom(m, 0);                              // B8CC JMP 26:8C0E
    }

    // ---- BossPhaseDispatch 15:A400 ----

    private static Ph P(Action<Machine, int> f) => (m, x, c, r) => { f(m, x); return null; };
    private static Ph G(Func<Machine, int, IEnumerable<Y>> f) => (m, x, c, r) => f(m, x);

    /// <summary>(ai_boss.js BOSS_PHASES) handler address -> phase handler</summary>
    private static readonly Dictionary<int, Ph> BossPhases = new()
    {
        [0xA48B] = P(E0Intro), [0xA4C3] = P(E0Wait), [0xA4CC] = P(E0Smoke), [0xA4D8] = P(E0Vanish),
        [0xA50B] = P(E0Teleport), [0xA54A] = P(E0Appear), [0xA565] = P(E0Hold), [0xA581] = P(E0Linger),
        [0xA684] = G(E2Intro), [0xA87E] = (m, x, c, r) => { E2Walk(m, x, c); return null; },
        [0xA971] = P(E3Intro), [0xA9C6] = P(E3Approach), [0xAA46] = P(E3BackOff), [0xAAA7] = P(E3Slam), [0xAAE6] = P(E3Sweep),
        [0xABE4] = P(E5Rise), [0xABFC] = P(E5Settle), [0xAC0F] = P(E5Talk), [0xAC1A] = P(E5Wait), [0xAC1E] = P(E5Leave),
        [0xAC31] = P(E5Sink), [0xAC41] = P(E5ThrowStart), [0xAC5B] = P(E5Throw),
        [0xAC8F] = P(E6Intro), [0xACF5] = P(E6Fight),
        [0xADE8] = P(E7Intro), [0xAE35] = P(E7Pose), [0xAE43] = P(E7Wait), [0xAE5A] = P(E7Bounce), [0xAEC9] = P(E7Aim),
        [0xAEEC] = P(E7Volley),
        [0xAF5C] = P(E8Intro), [0xAFAD] = P(E8Route), [0xB051] = P(E8Attack),
        [0xB0C5] = P(E9Intro),
        [0xB0F4] = (m, x, c, r) => { r.Pop = E9Watch(m, x); return null; },
        [0xB105] = (m, x, c, r) => { r.Pop = E9Fire(m, x); return null; },
        [0xB14F] = P(EaIntro), [0xB1A5] = P(EaPatrol), [0xB215] = P(EaDive), [0xB22C] = P(EaRise),
        [0xB301] = (m, x, c, r) => EbSetup(m, x, r), [0xB3BC] = G(EbSway), [0xB4DD] = G(EbDive), [0xB524] = G(EbRise),
        [0xB552] = G(EbHold), [0xB587] = G(EbMouth), [0xB67A] = P(EbRain),
    };

    // js: game/obj/ai_boss.js bossPhaseDispatch
    private static IEnumerable<Y>? BossPhaseDispatch(Machine m, int x, int @base, AiRet r)
    {
        int i = (@base + m.Rd(Phase + x)) & 0xFF;
        int y = (i << 1) & 0xFF, c = i >> 7;                      // c = the ASL carry (BossE2Walk's ADC reads it)
        m.Wr(0x10, Prg.CpuRd(m, 0xA413 + y));
        m.Wr(0x11, Prg.CpuRd(m, 0xA414 + y));
        int a = m.Rd(0x11) << 8 | m.Rd(0x10);
        if (!BossPhases.TryGetValue(a, out var h)) { Unported(m, $"AI boss phase handler 15:{a:X4}"); return null; }
        return h(m, x, c, r);
    }

    /// <summary>(ai_boss.js AI_BOSS)</summary>
    public static readonly Dictionary<int, AiFn> Ai = new()
    {
        [0x60] = BossAiE0, [0x61] = (m, x, r) => { BossHomingShot(m, x); return null; }, [0x62] = BossAiE2,
        [0x63] = BossAiE3, [0x64] = (m, x, r) => { BossRockShot(m, x); return null; }, [0x65] = AiTalkEventE5,
        [0x66] = BossAiE6, [0x67] = AiBoss88, [0x68] = BossAiE8, [0x69] = BossAiE9, [0x6A] = BossAiEA,
        [0x6B] = AiBowOfTruth9E,
        [0x6C] = (m, x, r) => { AiReward6C(m, x); return null; }, [0x6D] = (m, x, r) => { AiReward6C(m, x); return null; },
        [0x6E] = (m, x, r) => { AiReward6C(m, x); return null; },
        [0x6F] = (m, x, r) => BossDefeated(m, x),
        [0x79] = (m, x, r) => { r.X = AiBank15Fall79(m, x); return null; },
        [0x3F] = (m, x, r) => { AiBodyPart(m); return null; },   // s163 (bank 27, but only the bosses' parts use it)
    };
}
