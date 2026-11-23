// re-c C# port — the boss shot / effect AIs and other small AIs of bank 27 (= web/src/game/obj/ai_shot.js, s87).
// REVERSE.md "Boss shot / part AIs (session 86)". None pops its caller; all are plain functions (AiFn returns null).
//
//   AI $14 AiCountdown 27:B2C5     AI $16 AiCurveShot 27:B0F6    AI $17 AiHoverShot 27:B11B
//   AI $1B AiBounceShot 27:B12D    AI $1E AiDirFrameShot 27:B094 AI $1F AiShotAnimDec 27:B0A2
//   s166: $15 $18 $19 $1A $1C. s167: $0C $0D $0E $0F, $1D $2F $36. s168: $0A $33 $51-$55. s172: $0B AiPlayerDuel.
namespace ReC.Game.Obj;

public static class AiShot
{
    private const int MetaId = 0x0300, MetaDir = 0x0360, Flags = 0x0380, ShotId = 0x0440, BgMask = 0x0460, Timer = 0x0480;
    private const int Active = 0x04A0, Anim = 0x04E0, Phase = 0x0600, Timer2 = 0x0620;

    // js: game/obj/ai_shot.js dec
    private static void Dec(Machine m, int a) => m.Wr(a, m.Rd(a) - 1);

    // js: game/obj/ai_shot.js aiCountdown
    /// <summary>AI $14: obj_anim -= obj_phase; borrow -> ObjDespawn 26:98C7</summary>
    public static void AiCountdown(Machine m, int x)
    {
        int a = m.Rd(Anim + x), p = m.Rd(Phase + x);
        m.Wr(Anim + x, a - p);
        if (a < p) ObjUtil.ObjDespawn(m, x);
    }

    // js: game/obj/ai_shot.js aiDirFrameShot
    /// <summary>AI $1E: obj_anim = (dir &amp; 7) * 8 (fixed frame per direction)</summary>
    public static void AiDirFrameShot(Machine m, int x)
    {
        m.Wr(Anim + x, (m.Rd(MetaDir + x) & 7) << 3);
        AiStatic.AiStraightMoverTail(m, x);
    }

    // js: game/obj/ai_shot.js aiShotAnimDec
    /// <summary>AI $1F</summary>
    public static void AiShotAnimDec(Machine m, int x)
    {
        Dec(m, Anim + x);
        AiStatic.AiStraightMoverTail(m, x);
    }

    // js: game/obj/ai_shot.js aiCurveShot
    /// <summary>AI $16: every 16 obj_timer steps the 16-dir heading turns +1 (even slot) / -1 (odd slot)</summary>
    public static void AiCurveShot(Machine m, int x)
    {
        m.Wr(Anim + x, m.Rd(Anim + x) - 2);
        if ((m.Rd(Timer + x) & 0x0F) == 0)
            m.Wr(MetaDir + x, ((m.Rd(MetaDir + x) + ((x & 1) != 0 ? -1 : 1)) & 0xFF) & 0x0F);
        AiStatic.AiStraightMoverTail(m, x);
    }

    // js: game/obj/ai_shot.js aiHoverShot
    /// <summary>AI $17: obj_anim -= 2; when it goes negative: meta++ and obj_active = $10 (straight mover)</summary>
    public static void AiHoverShot(Machine m, int x)
    {
        m.Wr(Anim + x, m.Rd(Anim + x) - 2);
        if ((m.Rd(Anim + x) & 0x80) != 0)
        {
            m.Wr(MetaId + x, m.Rd(MetaId + x) + 1);
            m.Wr(Active + x, 0x10);
        }
        AiStatic.AiStraightMoverTail(m, x);
    }

    // js: game/obj/ai_shot.js aiBounceShot
    /// <summary>AI $1B: step, ObjBgProbe($40); wall -> heading += 4 (of 16) and retry, 5 tries; obj_anim reaching 0
    /// clears obj_bg_mask</summary>
    public static void AiBounceShot(Machine m, int x)
    {
        if ((m.Rd(Flags + x) & 0x80) != 0) { ObjUtil.ObjDespawn(m, x); return; }
        m.Wr(0x1F, 5);
        for (; ; )
        {
            Dec(m, Timer + x);
            Dec(m, Anim + x);
            if (m.Rd(Anim + x) == 0) { m.Wr(BgMask + x, 0); return; }
            ObjUtil.ObjPosToTemp(m, x);
            ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
            ObjUtil.TempAddStep(m);
            ObjUtil.TempToObjPos(m, x);
            ObjUtil.ObjBgProbe(m, x, 0x40);
            if ((m.Rd(0x20) & m.Rd(BgMask + x) & 6) == 0) return;
            m.Wr(MetaDir + x, (m.Rd(MetaDir + x) + 4) & 0x0F);
            Dec(m, 0x1F);
            if (m.Rd(0x1F) == 0) return;
        }
    }

    // js: game/obj/ai_shot.js aiTrailShot
    /// <summary>AI $15 27:B27A (type $19): obj_timer--, 0 -> ObjDespawn; on even timers SpawnShot(obj_shot_id, dir);
    /// obj_anim--, then one step along obj_meta_dir with no BG test</summary>
    public static void AiTrailShot(Machine m, int x)
    {
        Dec(m, Timer + x);
        if (m.Rd(Timer + x) == 0) { ObjUtil.ObjDespawn(m, x); return; }   // B27F JMP 26:98C7
        if ((m.Rd(Timer + x) & 1) == 0) ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), m.Rd(MetaDir + x));
        Dec(m, Anim + x);
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.TempAddStep(m);
        ObjUtil.TempToObjPos(m, x);                               // B2A1 JMP 26:9897
    }

    // js: game/obj/ai_shot.js aiRingSpawner
    /// <summary>AI $1C 27:B2A4 (type $1A): every other obj_timer2 step SpawnShot($17, dir = obj_phase), obj_phase++;
    /// the spawn at obj_phase 7 clears obj_active first</summary>
    public static void AiRingSpawner(Machine m, int x)
    {
        Dec(m, Timer2 + x);
        if ((m.Rd(Timer2 + x) & 1) != 0) return;                  // B2AD RTS
        int p = m.Rd(Phase + x);
        if (p == 7) m.Wr(Active + x, 0);
        m.Wr(Phase + x, p + 1);
        ObjUtil.SpawnShot(m, x, 0x17, p);                         // B2C2 JMP 26:972D
    }

    // js: game/obj/ai_shot.js aiShotTurnMeta
    /// <summary>AI $18 27:B2D5 (types $1C $1D): obj_timer2++, obj_meta_id |= dir >> 1, then AI $19</summary>
    public static void AiShotTurnMeta(Machine m, int x)
    {
        m.Wr(Timer2 + x, m.Rd(Timer2 + x) + 1);
        m.Wr(MetaId + x, m.Rd(MetaId + x) | (m.Rd(MetaDir + x) >> 1));
        AiShotTurn(m, x);
    }

    // js: game/obj/ai_shot.js aiShotTurn
    /// <summary>AI $19 27:B2E2 (type $1E): dir = (dir * 2 + ROM[$B2F1 + X]) &amp; $0F, obj_active = $10 (the "table" at
    /// B2F1 is this routine's own code bytes, indexed by slot)</summary>
    public static void AiShotTurn(Machine m, int x)
    {
        m.Wr(MetaDir + x, (((m.Rd(MetaDir + x) << 1) & 0xFF) + Prg.CpuRd(m, 0xB2F1 + x)) & 0x0F);
        m.Wr(Active + x, 0x10);
    }

    // js: game/obj/ai_shot.js aiShopEmpty
    /// <summary>AI $0E 27:A5C0 (type $4E): sub_state = $24</summary>
    public static void AiShopEmpty(Machine m, int x) => m.Wr(0x41, 0x24);

    // js: game/obj/ai_shot.js aiSetSubstate
    /// <summary>AI $0F 27:A5C5 (shop types $40-$48): sub_state = obj_timer2</summary>
    public static void AiSetSubstate(Machine m, int x) => m.Wr(0x41, m.Rd(Timer2 + x));

    // js: game/obj/ai_shot.js aiFollowPlayerWait
    /// <summary>AI $0C 27:A5CB: $0561 | $0641 = 0 -> obj_active++ (AI $0D); else obj_timer2 = $10, meta $4C, on the player</summary>
    public static void AiFollowPlayerWait(Machine m, int x)
    {
        if ((m.Rd(0x0561) | m.Rd(0x0641)) == 0) { m.Wr(Active + x, m.Rd(Active + x) + 1); return; }
        m.Wr(Timer2 + x, 0x10);
        m.Wr(MetaId + x, 0x4C);
        m.Wr(0x10, x);
        ObjUtil.ObjPosToTemp(m, 0);
        ObjUtil.TempToObjPos(m, x);                               // A5EA JMP 26:9897
    }

    // js: game/obj/ai_shot.js aiMessageAfterWait
    /// <summary>AI $0D 27:A5ED: meta $8A; obj_timer2 counts down, at 0: obj_active = $30, pending message 12.4 (12.5 in
    /// area $68), sub_state = $10</summary>
    public static void AiMessageAfterWait(Machine m, int x)
    {
        m.Wr(MetaId + x, 0x8A);
        if (m.Rd(Timer2 + x) != 0) { Dec(m, Timer2 + x); return; }
        m.Wr(Active + x, 0x30);
        m.Wr(0x06C3, m.Rd(0x6C) == 0x68 ? 5 : 4);                 // pending_msg_id
        m.Wr(0x06E3, 0x0C);                                       // pending_msg_group
        m.Wr(0x41, 0x10);
    }

    // js: game/obj/ai_shot.js aiShotEmitter
    /// <summary>AI $1D 27:B02C (type $BE): obj_anim--, 0 -> obj_active = 0; on odd obj_anim SpawnShot(obj_shot_id, dir)</summary>
    public static void AiShotEmitter(Machine m, int x)
    {
        Dec(m, Anim + x);
        if (m.Rd(Anim + x) == 0) { m.Wr(Active + x, 0); return; }
        if ((m.Rd(Anim + x) & 1) != 0) ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), m.Rd(MetaDir + x));   // B044 JMP 26:972D
    }

    // js: game/obj/ai_shot.js aiFallingObj
    /// <summary>AI $2F 27:B048 (type $B9): falls with dy = obj_timer2 (capped at 8); y_lo >= $C8 -> obj_active = 0</summary>
    public static void AiFallingObj(Machine m, int x)
    {
        ObjUtil.ObjPosToTemp(m, x);
        m.Wr(0x30, 0);
        int t = m.Rd(Timer2 + x);
        m.Wr(0x31, t);
        m.Wr(Timer2 + x, t == 8 ? 8 : t + 1);
        ObjUtil.TempAddStep(m);
        ObjUtil.TempToObjPos(m, x);
        if (m.Rd((0xB0 + x) & 0xFF) >= 0xC8) m.Wr(Active + x, 0);
    }

    // js: game/obj/ai_shot.js aiDropSpawner
    /// <summary>AI $36 27:B070 (type $33): dir = 0; first frame: SpawnShot($14, 0), the new object 24 px lower;
    /// then AiStraightMoverTail</summary>
    public static void AiDropSpawner(Machine m, int x)
    {
        m.Wr(MetaDir + x, 0);
        if (m.Rd(Timer2 + x) == 0)
        {
            m.Wr(Timer2 + x, 1);
            if (ObjUtil.SpawnShot(m, x, 0x14, 0))
            {
                int y = m.Rd(0x10);
                m.Wr(0xB0 + y, m.Rd(0xB0 + y) + 0x18);
            }
        }
        AiStatic.AiStraightMoverTail(m, x);
    }

    // js: game/obj/ai_shot.js aiPhaseUnhide
    /// <summary>AI $33 27:A4C6 (type $31): obj_phase++; at $40 obj_timer2++ and obj_flags b2 cleared</summary>
    public static void AiPhaseUnhide(Machine m, int x)
    {
        m.Wr(Phase + x, m.Rd(Phase + x) + 1);
        if (m.Rd(Phase + x) != 0x40) return;
        m.Wr(Timer2 + x, m.Rd(Timer2 + x) + 1);
        m.Wr(Flags + x, m.Rd(Flags + x) & 0xFB);
    }

    // js: game/obj/ai_shot.js aiCarryLeft
    /// <summary>AI $55 27:A616 (type $3A): only in area $51 or area $60 entry 0. Walks 1 px left, carries slots 0 and 1;
    /// at x = $0680: player y_lo = $88, obj_active = $FF</summary>
    public static void AiCarryLeft(Machine m, int x)
    {
        int a = m.Rd(0x6C);
        if (!(a == 0x51 || (a == 0x60 && m.Rd(0x6D) == 0))) return;
        if (m.Rd(0x70 + x) == 0) m.Wr(0x90 + x, m.Rd(0x90 + x) - 1);
        m.Wr(0x70 + x, m.Rd(0x70 + x) - 1);
        Dec(m, Anim + x);
        m.Wr(0x10, x);
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.TempToObjPos(m, 0);
        ObjUtil.TempToObjPos(m, 1);
        m.Wr(0x0600, 0); m.Wr(0x0620, 0); m.Wr(0x06C0, 0);
        if (m.Rd(0x70 + x) != 0x80 || m.Rd(0x90 + x) != 0x06) return;
        m.Wr(0xB0, 0x88);
        m.Wr(Active + x, 0xFF);
    }

    // js: game/obj/ai_shot.js flagGate
    /// <summary>27:A6FB: $10 = story_flags[$06C0,X]; false (PLA PLA: the AI returns) when $10 &amp; BitMask[obj_behav,X] = 0</summary>
    private static bool FlagGate(Machine m, int x)
    {
        m.Wr(0x10, m.Rd(0x6480 + m.Rd(0x06C0 + x)));
        return (m.Rd(0x10) & Prg.CpuRd(m, 0xC000 + m.Rd(0x06E0 + x))) != 0;
    }

    // js: game/obj/ai_shot.js aiFlagGatedSpawn
    /// <summary>AI $51 27:A6C5 (types $B0 $B1 $D8 $D9): story flag gate; obj_anim--; on screen in area 3 with $62F1 = 0 and
    /// slot $0D not AI 8 -> slot $0D = ObjInit(its death type); every 32 obj_anim steps PlaySound($37)</summary>
    public static void AiFlagGatedSpawn(Machine m, int x)
    {
        if (!FlagGate(m, x)) return;
        Dec(m, Anim + x);
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        if (m.Rd(0x62F1) == 0 && m.Rd(0x6C) == 0x03 && m.Rd(0x04AD) != 0x08)
        {
            m.Wr(0x11, m.Rd(0x04CD));
            m.Wr(0x10, 0x0D);
            ObjInit.FarObjInit(m);                                // ObjInit keeps X / Y (stack)
        }
        if ((m.Rd(Anim + x) & 0x1F) == 0) PlaySound.Play(m, 0x37, m.Rd(0x06E0 + x));   // Y = obj_behav from 27:A703
    }

    // js: game/obj/ai_shot.js aiDuelStart
    /// <summary>AI $0A AiDuelStart 27:A70E (slot 0)</summary>
    public static void AiDuelStart(Machine m, int x)
    {
        if (m.Rd(0x0660 + x) == 0)
        {                                                         // obj_aux
            m.Wr(0x0660 + x, m.Rd(0x0660 + x) + 1);
            m.Wr(0x06C3, 3); m.Wr(0x06E3, 3);                     // pending message 3.3
            m.Wr(0x41, 0x10);
            return;
        }
        Dec(m, 0xB0); Dec(m, 0xB1); Dec(m, 0x04E1);
        m.Wr(0x0301, 0);
        if (m.Rd(0xB0) != 0x86) return;
        m.Wr(0x03AD, 0x60);                                       // obj_coll slot $0D
        m.Wr(0x04AD, 0x52);
        m.Wr(0x04A0, 0x0B);
        m.Wr(0xB0, 0x86);
        m.Wr(0x07F5, 0x4E);                                       // chr_r5_play
        m.Wr(0x657F, m.Rd(0x657F) + 1);                           // story_flags_F7F8
    }

    // js: game/obj/ai_shot.js duelEndCheck
    /// <summary>27:A2B4 (AI $0B's duel-end test): player y >= $98 = lost, y &lt; $58 = won; either end -> sub_state $10,
    /// slot $0D AI $30, slot 0 AI $03, FarCall_B14_800E. Returns X after it (the sprite pass clobbers X)</summary>
    private static int DuelEndCheck(Machine m, int x)
    {
        int y = m.Rd(0xB0 + x);
        if (y < 0x98)
        {
            if (y >= 0x58) return x;
            m.Wr(0x038E, m.Rd(0x038E) | 4);
            m.Wr(0x04AE, 0x53);
            m.Wr(0x06C3, 1); m.Wr(0x06E3, 3);
            m.Wr(0x03AD, 0x80);
            m.Wr(0x06ED, 5);
            m.Wr(0x06CD, 0x57);
        }
        else
        {
            m.Wr(0x06C3, 2); m.Wr(0x06E3, 3);
            m.Wr(0x07F5, 0x4D);                                   // chr_r5_play
            m.Wr(0x03AD, 0x80);
        }
        m.Wr(0x41, 0x10);                                         // A2FC
        m.Wr(0x030D, m.Rd(0x06CD));
        m.Wr(0x04A0, 0x03);
        m.Wr(0x04AD, 0x30);
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);                     // FarCall_B14_800E
        Prg.SwitchBank16(m, 0x0E);
        int nx = Sprites.SpriteDraw(m);
        Prg.FarRestore(m, r6, r7);
        return nx;
    }

    // js: game/obj/ai_shot.js aiPlayerDuel
    /// <summary>AI $0B AiPlayerDuel 27:A24A (slot 0 in the sparring duel)</summary>
    public static void AiPlayerDuel(Machine m, int x)
    {
        x = DuelEndCheck(m, x);
        m.Wr(Timer2 + x, 0);
        m.Wr(0x06C0 + x, 0);
        m.Wr(MetaId + x, 0xA7);
        m.Wr(MetaDir + x, 0);
        if ((m.Rd(0x4B) & 0x40) != 0)
        {
            int p0 = m.Rd(Phase + x);
            if (p0 < 0x0C || p0 == 0x11) m.Wr(Phase + x, 0x12);
        }
        m.Wr(0x0301, 0);
        int p = m.Rd(Phase + x);
        if (p != 0)
        {
            m.Wr(0x04E1, p);
            m.Wr(0x0301, 4);
            m.Wr(Phase + x, p - 1);
            return;
        }
        int d = m.Rd(0x49);
        if ((d & 0x80) != 0) return;
        int v = (Prg.CpuRd(m, 0xA2AC + d) + m.Rd(0x70 + x)) & 0xFF;
        m.Wr(0x70 + x, v);
        if (v < 0x27) v += 2;                                     // A299 INY INY
        if (v >= 0x98) v -= 2;                                    // A29F DEY DEY
        m.Wr(0x70 + x, v & 0xFF);
        Dec(m, 0x04E1);
    }

    // js: game/obj/ai_shot.js aiDuelOpponent
    /// <summary>AI $52 AiDuelOpponent 27:A753 (slot $0D): swing or follow the player's x 1 px a frame; every 8th frame a
    /// Rand64 roll within 8 px of the player starts a swing (timer2 = $1E)</summary>
    public static void AiDuelOpponent(Machine m, int x)
    {
        int t2 = m.Rd(Timer2 + x);
        if (t2 != 0)
        {
            m.Wr(Anim + x, t2);
            m.Wr(Timer2 + x, t2 - 2);
            m.Wr(MetaId + x, 0x56);
            if (m.Rd(Anim + x) == 0x0E) ObjUtil.SpawnShot(m, x, 0x5B, 4);   // A771 JMP 26:972D
            return;
        }
        m.Wr(MetaId + x, 0x55);
        int px = m.Rd(0x71), ox = m.Rd(0x70 + x);
        if (px != ox)
        {
            int v = (ox + (px < ox ? 0xFF : 0x01)) & 0xFF;
            m.Wr(0x70 + x, v);
            if (v == 0x27) m.Wr(0x70 + x, m.Rd(0x70 + x) + 1);
            if (v == 0x98) m.Wr(0x70 + x, m.Rd(0x70 + x) - 1);
            Dec(m, Anim + x);
        }
        if ((m.Rd(0x08) & 7) != 0) return;
        m.Wr(0x10, 2);
        m.Wr(0x10, ((((m.Rd(0x657F) << 1) & 0xFF) + 6) & 7) | 1);
        int r = ObjUtil.Rand64(m);
        if (((r - m.Rd(0x10)) & 0x80) != 0) return;               // CMP $10 / BMI
        int d = (m.Rd(0x71) - m.Rd(0x70 + x)) & 0xFF;
        if ((d & 0x80) != 0) d = ((d ^ 0xFF) + 1) & 0xFF;
        if (d < 8) m.Wr(Timer2 + x, 0x1E);
    }

    // js: game/obj/ai_shot.js aiHideAfterEvent
    /// <summary>AI $53 27:A7CD (type $2B): obj_aux 0 waits for sub_state 8, then $0575 = $43, $0623 = $15, sub_state 7;
    /// after that obj_flags b2 set and obj_aux counts to $40 -> obj_active = 0</summary>
    public static void AiHideAfterEvent(Machine m, int x)
    {
        if (m.Rd(0x0660 + x) != 0)
        {
            m.Wr(Flags + x, m.Rd(Flags + x) | 4);
            m.Wr(0x0660 + x, m.Rd(0x0660 + x) + 1);
            if (m.Rd(0x0660 + x) == 0x40) m.Wr(Active + x, 0);
            return;
        }
        if (m.Rd(0x41) != 0x08) return;
        m.Wr(0x0660 + x, m.Rd(0x0660 + x) + 1);
        m.Wr(0x0575, 0x43);
        m.Wr(0x0623, 0x15);
        m.Wr(0x41, 0x07);
    }

    // js: game/obj/ai_shot.js aiChasePlayer
    /// <summary>AI $54 27:A802: idle while slot 0 flags2 b7 clear and pad_dir b7 set; every 4th frame aims at the player;
    /// then timer--, anim--, one step (9824 keeps the y wrap)</summary>
    public static void AiChasePlayer(Machine m, int x)
    {
        if ((m.Rd(0x0340) & 0x80) == 0 && (m.Rd(0x49) & 0x80) != 0) return;
        if ((m.Rd(0x08) & 3) == 0)
        {
            m.Wr(Flags + x, m.Rd(Flags + x) | 0x10);
            ObjUtil.DirToTarget(m, x, 0);
            m.Wr(MetaDir + x, m.Rd(0x20));
            int a = m.Rd(0x21) < 2 ? 5 : 7;
            if ((m.Rd(0x0340) & 0x80) != 0) a = 8;
            m.Wr(0x0340 + x, a);
            m.Wr(0x10, (m.Rd(MetaDir + x) >> 1) & 3);
            m.Wr(MetaId + x, (m.Rd(MetaId + x) & 0xFC) | m.Rd(0x10));
        }
        Dec(m, Timer + x);
        Dec(m, Anim + x);
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.TempAddOffset(m);
        ObjUtil.TempToObjPos(m, x);                               // A85B JMP 26:9897
    }

    private static AiFn P(Action<Machine, int> f) => (m, x, r) => { f(m, x); return null; };

    /// <summary>(ai_shot.js AI_SHOT) ObjAiTable ids handled here</summary>
    public static readonly Dictionary<int, AiFn> Ai = new()
    {
        [0x0A] = P(AiDuelStart), [0x0B] = P(AiPlayerDuel), [0x33] = P(AiPhaseUnhide), [0x51] = P(AiFlagGatedSpawn),
        [0x52] = P(AiDuelOpponent), [0x53] = P(AiHideAfterEvent), [0x54] = P(AiChasePlayer), [0x55] = P(AiCarryLeft),
        [0x0C] = P(AiFollowPlayerWait), [0x0D] = P(AiMessageAfterWait), [0x0E] = P(AiShopEmpty), [0x0F] = P(AiSetSubstate),
        [0x14] = P(AiCountdown), [0x15] = P(AiTrailShot), [0x16] = P(AiCurveShot), [0x17] = P(AiHoverShot),
        [0x18] = P(AiShotTurnMeta), [0x19] = P(AiShotTurn),
        [0x1A] = (m, x, r) => null,                               // 27:B2F4 RTS (ObjUpdateAll takes $1A to 30:CD8A first)
        [0x1B] = P(AiBounceShot), [0x1C] = P(AiRingSpawner), [0x1D] = P(AiShotEmitter), [0x1E] = P(AiDirFrameShot),
        [0x1F] = P(AiShotAnimDec), [0x2F] = P(AiFallingObj), [0x36] = P(AiDropSpawner),
    };
}
