// re-c C# port — the walker / chaser enemy AIs (= web/src/game/obj/ai_walk.js, s81). REVERSE.md "Enemies — catalogue +
// generic enemy AIs" (AiChase, AiWalkPause, the AI table). Ported instruction by instruction from bank 27 (every
// zero-page temp the ROM writes is written here too). Handlers take the slot x with banks 26/27 mapped.
//
//   AI $20 AiWalkPause 27:A915 (+ AiWalkPick 27:A94A)   AI $21 AiShooterWalk 27:A969   AI $22 AiDormant 27:AB42
//   AI $24 AiChase 27:AA57 (+ AiTryMove 27:AAFA, ChasePose 27:AB2D)   AI $25 AiChaseOnscreen 27:A9D2
//   AI $26 AiShooterChase 27:A9E4   AI $27 AiShooterChaseOnscreen 27:A9DB   AI $28 AiAimShooter 27:AB61
//   AI $29 AiBurrower 27:AB9E   AI $2A AiLunger 27:ABF8 (+ AiLungeCheck 27:AC40)   AI $2E AiSpawnChildLunger 27:A9B4
//   AI $41 AiShellOnHit 27:B339
// None of these pops ObjUpdateAll's return address: every AiShootCheck / AiLungeCheck here is reached by a JSR,
// so its PLA PLA only ends the AI handler itself (the callers simply return).
namespace ReC.Game.Obj;

public static class AiWalk
{
    private const int XLo = 0x70, XHi = 0x90, YLo = 0xB0, YHi = 0xD0;
    private const int MetaId = 0x0300, TileOfs = 0x0320, Flags2 = 0x0340, MetaDir = 0x0360, Flags = 0x0380, Coll = 0x03A0;
    private const int ShotId = 0x0440, Timer = 0x0480, Active = 0x04A0, Anim = 0x04E0, Freeze = 0x05A0, Phase = 0x0600;
    private const int Timer2 = 0x0620, ActTimer = 0x0640, Aux = 0x0660, Param1 = 0x0680, Param2 = 0x06A0;
    private const int WalkPose = 0x06C0, Behav = 0x06E0;

    // js: game/obj/ai_walk.js dec
    private static void Dec(Machine m, int a) => m.Wr(a, m.Rd(a) - 1);

    // js: game/obj/ai_walk.js aiWalkPick
    /// <summary>AiWalkPick 27:A94A: dir = Rand64 &amp; 6; walk obj_timer = Rand64*4 + $20; pause obj_phase = ($08 &amp; 7)*8</summary>
    private static void AiWalkPick(Machine m, int x)
    {
        m.Wr(MetaDir + x, ObjUtil.Rand64(m) & 6);
        m.Wr(Timer + x, ((ObjUtil.Rand64(m) << 2) + 0x20) & 0xFF);
        m.Wr(Phase + x, (m.Rd(0x08) & 7) << 3);
    }

    // js: game/obj/ai_walk.js aiWalkPause
    /// <summary>AI $20</summary>
    public static void AiWalkPause(Machine m, int x)
    {
        if ((m.Rd(Flags + x) & 0x80) != 0 && ((x + m.Rd(0x08)) & 3) != 0) return;   // offscreen: quarter rate
        if (m.Rd(Phase + x) != 0)
        {                                                                 // A923: pausing
            Dec(m, Phase + x);
            if ((m.Rd(Anim + x) & 7) != 0) Dec(m, Anim + x);
            return;
        }
        if (m.Rd(Timer + x) == 0) AiWalkPick(m, x);
        Dec(m, Anim + x);
        Dec(m, Timer + x);
        if (Move.ObjMoveSlide(m, x)) AiWalkPick(m, x);                    // A947 BCS: blocked
    }

    // js: game/obj/ai_walk.js aiShooterWalk
    /// <summary>AI $21</summary>
    public static void AiShooterWalk(Machine m, int x)
    {
        if (m.Rd(ActTimer + x) != 0)
        {
            Dec(m, Aux + x);
            if ((m.Rd(Aux + x) & 1) == 0) return;                         // every other frame
            Dec(m, ActTimer + x);
            m.Wr(Anim + x, m.Rd(ActTimer + x) >> 1);
            if (m.Rd(ActTimer + x) != 0x0F) return;
            ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), (m.Rd(MetaId + x) << 1) & 7);
            return;
        }
        if (AiStatic.AiShootCheck(m, x)) return;                          // A998: attack starts, skip the walk
        AiWalkPause(m, x);
        m.Wr(0x10, m.Rd(MetaId + x) & 0xFC);
        m.Wr(MetaId + x, (m.Rd(MetaDir + x) >> 1) | m.Rd(0x10));
        m.Wr(Phase + x, 0);
    }

    // js: game/obj/ai_walk.js aiSpawnChildLunger
    /// <summary>AI $2E: one linked child (the spawned slot's obj_param2 = this slot), then AI $2A from the next frame</summary>
    public static void AiSpawnChildLunger(Machine m, int x)
    {
        if (m.Rd(Param2 + x) == 0 && ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), 0))
        {
            m.Wr(Param2 + m.Rd(0x10), x);
            m.Wr(ShotId + x, 0);
            return;
        }
        AiLunger(m, x);
    }

    // js: game/obj/ai_walk.js aiChaseOnscreen
    /// <summary>AI $25</summary>
    public static void AiChaseOnscreen(Machine m, int x)
    {
        if ((m.Rd(Flags + x) & 0x80) == 0) AiChase(m, x);
    }

    // js: game/obj/ai_walk.js aiShooterChaseOnscreen
    /// <summary>AI $27</summary>
    public static void AiShooterChaseOnscreen(Machine m, int x)
    {
        if ((m.Rd(Flags + x) & 0x80) == 0) AiShooterChase(m, x);
    }

    // js: game/obj/ai_walk.js poseDir
    /// <summary>facing nibble used by the shooter / lunger poses: obj_flags b3 -> 0, else (dir &amp; 7) >> 1</summary>
    private static int PoseDir(Machine m, int x) => (m.Rd(Flags + x) & 8) != 0 ? 0 : (m.Rd(MetaDir + x) & 7) >> 1;

    // js: game/obj/ai_walk.js aiShooterChase
    /// <summary>AI $26</summary>
    public static void AiShooterChase(Machine m, int x)
    {
        if (m.Rd(ActTimer + x) != 0)
        {
            m.Wr(MetaId + x, PoseDir(m, x) | m.Rd(Behav + x));          // attack pose
            Dec(m, ActTimer + x);
            m.Wr(Anim + x, m.Rd(ActTimer + x) >> 1);
            if (m.Rd(ActTimer + x) != 0x0F) return;
            int y = (m.Rd(Flags + x) & 8) != 0 ? m.Rd(MetaDir + x) & 6 : (m.Rd(MetaId + x) & 3) << 1;
            ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), y);
            return;
        }
        m.Wr(MetaId + x, PoseDir(m, x) | m.Rd(WalkPose + x));            // AA31: walk pose
        m.Wr(TileOfs + x, m.Rd(TileOfs + x) & 3);
        if (AiStatic.AiShootCheck(m, x)) return;
        AiChase(m, x);
    }

    // js: game/obj/ai_walk.js chasePose
    /// <summary>27:AB2D: walk pose from the 4-way dir unless obj_flags b3</summary>
    private static void ChasePose(Machine m, int x)
    {
        if ((m.Rd(Flags + x) & 8) != 0) return;
        m.Wr(MetaId + x, ((m.Rd(MetaDir + x) & 6) >> 1) | m.Rd(WalkPose + x));
    }

    // js: game/obj/ai_walk.js turn
    /// <summary>27:AAAB (A = obj_phase ^ 1) / 27:AAB0 (A = obj_phase): dir += A b0 ? +1 : -1, &amp; 7</summary>
    private static void Turn(Machine m, int x, int a)
    {
        m.Wr(MetaDir + x, (m.Rd(MetaDir + x) + ((a & 1) != 0 ? 1 : -1)) & 7);
    }

    // js: game/obj/ai_walk.js turnAway
    private static void TurnAway(Machine m, int x) => Turn(m, x, m.Rd(Phase + x) ^ 1);    // AAAB

    // js: game/obj/ai_walk.js turnBack
    private static void TurnBack(Machine m, int x) => Turn(m, x, m.Rd(Phase + x));        // AAB0

    // js: game/obj/ai_walk.js aiTryMove
    /// <summary>AiTryMove 27:AAFA: ObjMoveSlide at speed $0A, position + obj_flags2 restored ($2B-$2F); -> C</summary>
    private static bool AiTryMove(Machine m, int x)
    {
        m.Wr(0x2B, m.Rd(XLo + x)); m.Wr(0x2C, m.Rd(XHi + x)); m.Wr(0x2D, m.Rd(YLo + x)); m.Wr(0x2E, m.Rd(YHi + x));
        m.Wr(0x2F, m.Rd(Flags2 + x));
        m.Wr(Flags2 + x, 0x0A);
        bool c = Move.ObjMoveSlide(m, x);
        m.Wr(XLo + x, m.Rd(0x2B)); m.Wr(XHi + x, m.Rd(0x2C)); m.Wr(YLo + x, m.Rd(0x2D)); m.Wr(YHi + x, m.Rd(0x2E));
        m.Wr(Flags2 + x, m.Rd(0x2F));
        return c;
    }

    // js: game/obj/ai_walk.js aiChase
    /// <summary>AI $24</summary>
    public static void AiChase(Machine m, int x)
    {
        m.Wr(0x10, (m.Rd(Flags + x) & 0x80) != 0 ? 7 : 1);
        if (((x + m.Rd(0x08)) & m.Rd(0x10)) != 0) return;                // every 2nd frame (8th offscreen)
        m.Wr(Anim + x, m.Rd(Anim + x) - 2);
        Dec(m, Timer + x);
        if (m.Rd(Timer2 + x) == 0)
        {
            if ((m.Rd(Timer + x) & 0x0F) == 0)
            {                                                             // re-aim at the player body (slot 0)
                ObjUtil.DirToTarget(m, x, 0);
                m.Wr(MetaDir + x, m.Rd(0x20) & 0xFE);
                if ((m.Rd(Flags + x) & 8) == 0) ChasePose(m, x);
            }
            if (!Move.ObjMoveSlide(m, x)) return;
            m.Wr(Timer2 + x, 0x60);                                       // AA9C: blocked -> 96-step detour
            m.Wr(Phase + x, (((x + m.Rd(0x08) + 1) & 0xFF) >> 2) & 1);   // ADC with C = 1 left by the BCS
            TurnAway(m, x);
            return;
        }
        Dec(m, Timer2 + x);                                               // AAC0: detour
        if ((m.Rd(Anim + x) & 7) == 0)
        {
            TurnBack(m, x);
            if (AiTryMove(m, x))
            {
                TurnAway(m, x);
                if (AiTryMove(m, x)) { TurnAway(m, x); return; }          // AADD JMP $AAAB
            }
            else
            {                                                             // AAEF: take the free way
                m.Wr(MetaDir + x, m.Rd(0x23));
                Move.ObjMoveSlide(m, x);
                ChasePose(m, x);
                return;
            }
        }
        Move.ObjMoveSlide(m, x);                                          // AAE0
        if (m.Rd(0x20) == 0) m.Wr(MetaDir + x, m.Rd(0x23));
        ChasePose(m, x);
    }

    // js: game/obj/ai_walk.js aiDormant
    /// <summary>AI $22</summary>
    public static void AiDormant(Machine m, int x)
    {
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        Dec(m, Anim + x);
        if (m.Rd(Anim + x) != 0) return;
        m.Wr(Active + x, m.Rd(Param2 + x));
        m.Wr(MetaId + x, m.Rd(WalkPose + x));
        m.Wr(Coll + x, m.Rd(Coll + x) | 0x10);
    }

    // js: game/obj/ai_walk.js aiAimShooter
    /// <summary>AI $28</summary>
    public static void AiAimShooter(Machine m, int x)
    {
        if (m.Rd(Aux + x) == 0)
        {                                                                 // start delay
            Dec(m, Anim + x);
            if (m.Rd(Anim + x) == 0) m.Wr(Aux + x, m.Rd(Aux + x) + 1);
            return;
        }
        m.Wr(MetaId + x, 0x65);
        if (m.Rd(ActTimer + x) != 0)
        {
            m.Wr(MetaId + x, 0x91);
            Dec(m, ActTimer + x);
            m.Wr(Anim + x, m.Rd(ActTimer + x));
            if (m.Rd(ActTimer + x) != 0x0F) return;
            ObjUtil.DirToTarget(m, x, 1);
            ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), m.Rd(0x20));
            return;
        }
        if (AiStatic.AiShootCheck(m, x)) return;
        AiChase(m, x);
    }

    // js: game/obj/ai_walk.js burrowerBody
    /// <summary>27:ABAC: the AiBurrower body (its AiShootCheck pop returns here, to AB9E)</summary>
    private static void BurrowerBody(Machine m, int x)
    {
        m.Wr(Coll + x, 0x41);
        m.Wr(MetaId + x, 0x68);
        if (m.Rd(ActTimer + x) == 0)
        {
            if (AiStatic.AiShootCheck(m, x)) return;
            AiChase(m, x);
            return;
        }
        m.Wr(Coll + x, 0x11);
        m.Wr(MetaId + x, 0x6B);
        if (m.Rd(ActTimer + x) == 0x10)
        {
            ObjUtil.DirToTarget(m, x, 1);
            ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), m.Rd(0x20));
        }
        Dec(m, Anim + x);
        Dec(m, Timer + x);
        if ((m.Rd(Timer + x) & 3) == 0) Dec(m, ActTimer + x);
    }

    // js: game/obj/ai_walk.js aiBurrower
    /// <summary>AI $29</summary>
    public static void AiBurrower(Machine m, int x)
    {
        BurrowerBody(m, x);
        if (m.Rd(Timer2) != 0) m.Wr(Coll + x, 0);                         // ABA1: LDA $0620 (slot 0, absolute)
    }

    // js: game/obj/ai_walk.js aiLungeCheck
    /// <summary>AiLungeCheck 27:AC40 -> true = attack started (PLA PLA: the caller's walk is skipped)</summary>
    private static bool AiLungeCheck(Machine m, int x)
    {
        ObjUtil.DirToTarget(m, x, 1);
        if (m.Rd(0x21) >= 2) return false;
        if (ObjUtil.Rand64(m) != 0) return false;
        m.Wr(MetaDir + x, m.Rd(0x20));
        m.Wr(ActTimer + x, 0x0F);
        m.Wr(Anim + x, 0x0F);
        return true;
    }

    // js: game/obj/ai_walk.js aiLunger
    /// <summary>AI $2A</summary>
    public static void AiLunger(Machine m, int x)
    {
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        if (m.Rd(ActTimer + x) == 0)
        {                                                                 // AC30
            m.Wr(MetaId + x, (m.Rd(MetaDir + x) >> 1) | m.Rd(WalkPose + x));
            if (AiLungeCheck(m, x)) return;
            AiChase(m, x);
            return;
        }
        m.Wr(MetaId + x, (m.Rd(MetaDir + x) >> 1) | m.Rd(Behav + x));
        Dec(m, Aux + x);
        if ((m.Rd(Aux + x) & 1) == 0) return;
        Dec(m, ActTimer + x);
        m.Wr(Anim + x, m.Rd(ActTimer + x));
        if (m.Rd(ActTimer + x) != 8) return;
        ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), (m.Rd(MetaId + x) & 3) << 1);
    }

    // js: game/obj/ai_walk.js aiShellOnHit
    /// <summary>AI $41</summary>
    public static void AiShellOnHit(Machine m, int x)
    {
        int p2 = m.Rd(Param2 + x);
        if (p2 == 0)
        {
            if ((m.Rd(Flags2 + x) & 0x80) == 0) { AiShooterChase(m, x); return; }
            m.Wr(Param1 + x, m.Rd(MetaId + x));                           // B346: hit -> close the shell
            m.Wr(Active + x, 0x41);
            m.Wr(Coll + x, m.Rd(Coll + x) & 0x0F);
            m.Wr(Param2 + x, 0x3F);
            m.Wr(MetaId + x, 0x97);
            return;
        }
        Dec(m, Param2 + x);                                               // B364 (A keeps the old value)
        int n = m.Rd(Param2 + x);
        if (n == 0)
        {                                                                 // B383: reopen
            m.Wr(Coll + x, m.Rd(Coll + x) | 0x10);
            m.Wr(Param2 + x, 0);
            m.Wr(Active + x, 0xC1);
            m.Wr(MetaId + x, m.Rd(Param1 + x));
            AiShooterChase(m, x);
            return;
        }
        if (p2 >= 0x1F)
        {
            m.Wr(Anim + x, n);
            if (n == 0x20) m.Wr(Freeze + x, 8);
        }
        else m.Wr(Anim + x, p2 ^ 0xFF);
    }

    private static AiFn P(Action<Machine, int> f) => (m, x, r) => { f(m, x); return null; };

    /// <summary>(ai_walk.js AI_WALK) ObjAiTable ids handled here</summary>
    public static readonly Dictionary<int, AiFn> Ai = new()
    {
        [0x20] = P(AiWalkPause), [0x21] = P(AiShooterWalk), [0x22] = P(AiDormant), [0x24] = P(AiChase),
        [0x25] = P(AiChaseOnscreen), [0x26] = P(AiShooterChase), [0x27] = P(AiShooterChaseOnscreen),
        [0x28] = P(AiAimShooter), [0x29] = P(AiBurrower), [0x2A] = P(AiLunger), [0x2E] = P(AiSpawnChildLunger),
        [0x41] = P(AiShellOnHit),
    };
}
