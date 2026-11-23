// re-c C# port — the remaining field-enemy AIs of bank 27 (= web/src/game/obj/ai_field.js, s87). REVERSE.md "Enemies —
// catalogue + generic enemy AIs" (AI table rows $44 $45 $58 $5C $5D $5E, SteerToward). Ported instruction by
// instruction (every zero-page temp the ROM writes is written here too). Handlers take the slot x with banks 26/27
// mapped and never pop ObjUpdateAll (AiShootCheck is JSRed).
//
//   AI $44 AiDasher 27:B436          AI $45 AiHomingFlyer 27:B4BA (+ SteerToward 27:B73E, AiOffscreenHalf 27:B769)
//   AI $48 AiHomingShooter 27:B55A   AI $58 AiOrbiter 27:B6AA     AI $5C AiPatrolGunner 27:B626
//   AI $5D AiDrifter 27:B690         AI $5E AiWaitClear 27:B5F3   AI $2B AiTrapChest 27:AC60 (+ ChestHop 27:B79F)
//   AI $7B AiCoin 27:B778            AI $38 AiPitShuttle 27:AFA9      AI $3C AiCrumblePlatform 27:AFF5 (s117)
//   AI $3A AiNpcBossReveal 27:A555 (s133)   AI $30 AiNpcWander 27:ACD9 / $31 AiNpcStill 27:AE16 / $78 RTS (s140)
//   s170: AI $34 AiPeriodicShot 27:B318, $39 AiArenaLock 27:A519, $40 AiWander16 27:B39E
//   s171: AI $07 AiElementWall 27:A85F, $12 AiWiggleShot 27:B174, $13 AiSpiralShot 27:B235, $32 AiPathWalk 27:AE31
namespace ReC.Game.Obj;

public static class AiField
{
    private const int XLo = 0x70, YHi = 0xD0, Freeze = 0x05A0, RideSlot = 0x0541;
    private const int MetaId = 0x0300, Flags2 = 0x0340, MetaDir = 0x0360, Flags = 0x0380, Coll = 0x03A0, ShotId = 0x0440;
    private const int Timer = 0x0480, Active = 0x04A0, Anim = 0x04E0, Elem = 0x0500, SpawnParam = 0x0560, Phase = 0x0600;
    private const int Timer2 = 0x0620, ActTimer = 0x0640, Overlay = 0x0580, Param1 = 0x0680, Orbit = 0x06C0;

    // js: game/obj/ai_field.js dec
    private static void Dec(Machine m, int a) => m.Wr(a, m.Rd(a) - 1);

    // js: game/obj/ai_field.js offscreenSkip
    /// <summary>AiOffscreenHalf 27:B769: true = offscreen on an odd (X + $08) frame -> the caller returns (PLA PLA)</summary>
    private static bool OffscreenSkip(Machine m, int x) => (m.Rd(Flags + x) & 0x80) != 0 && ((x + m.Rd(0x08)) & 1) != 0;

    // js: game/obj/ai_field.js steerToward
    /// <summary>SteerToward 27:B73E: heading (0-15) one 22.5° step toward A; $10 = the turn (0 / $FF / 1); -> new heading</summary>
    private static int SteerToward(Machine m, int x, int a)
    {
        int t = (((a ^ 0xFF) + 1) + m.Rd(MetaDir + x)) & 0x0F;
        m.Wr(0x10, Prg.CpuRd(m, 0xB759 + t));
        int h = (m.Rd(0x10) + m.Rd(MetaDir + x)) & 0x0F;
        m.Wr(MetaDir + x, h);
        return h;
    }

    // js: game/obj/ai_field.js stepFree
    /// <summary>move one step in obj_meta_dir with no BG test: 26:98A8 / 26:8480 / 26:9861 / 26:9897</summary>
    private static void StepFree(Machine m, int x)
    {
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.TempAddStep(m);
        ObjUtil.TempToObjPos(m, x);
    }

    // js: game/obj/ai_field.js aiDasher
    /// <summary>AI $44</summary>
    public static void AiDasher(Machine m, int x)
    {
        if ((m.Rd(Flags2 + x) & 0xF0) == 0)
        {                                                                 // B43B: knockback nibble -> pick
            if (OffscreenSkip(m, x)) return;
            if (m.Rd(Phase + x) != 0)
            {                                                             // paused
                Dec(m, Phase + x);
                m.Wr(Anim + x, 0);
                return;
            }
            if (m.Rd(Timer + x) != 0)
            {
                if (m.Rd(Flags2 + x) == 6)
                {                                                         // dash: meta $92, double animation rate
                    m.Wr(MetaId + x, 0x92);
                    Dec(m, Anim + x);
                }
                Dec(m, Anim + x);
                Dec(m, Timer + x);
                ObjUtil.ObjPosToTemp(m, x);
                ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
                ObjUtil.TempAddStep(m);
                if (!Move.ObjMoveSlide(m, x)) return;                     // B477 BCC
            }
        }
        ObjUtil.ObjPosToTemp(m, x);                                       // B479: pick
        ObjUtil.ObjBgProbe(m, x, 0x40);
        m.Wr(MetaDir + x, ObjUtil.Rand64(m) & 6);
        m.Wr(MetaId + x, 0x65);
        int y = ObjUtil.Rand64(m);
        m.Wr(Timer + x, Prg.CpuRd(m, 0xB4AA + y));
        m.Wr(Flags2 + x, Prg.CpuRd(m, 0xB4B2 + y));
        m.Wr(Phase + x, (m.Rd(0x08) & 7) << 3);
    }

    // js: game/obj/ai_field.js homingSteer
    /// <summary>27:B4E5: re-aim (16-dir) and pick the speed level obj_phase 0-3 (B550 clamp) -> obj_flags2 = B54C[level]</summary>
    private static void HomingSteer(Machine m, int x)
    {
        ObjUtil.DirToTarget(m, x, 1);
        int h = SteerToward(m, x, (m.Rd(0x20) << 1) & 0xFF);
        m.Wr(Phase + x, m.Rd(Phase + x) + 1);
        if ((h & m.Rd(0x10)) != 0) m.Wr(Phase + x, m.Rd(Phase + x) - 2);   // B4F3 BIT $10 (A = the new heading)
        int p = Prg.CpuRd(m, 0xB550 + ((m.Rd(Phase + x) + 1) & 0xFF));
        m.Wr(Phase + x, p);
        m.Wr(Flags2 + x, Prg.CpuRd(m, 0xB54C + p));
    }

    // js: game/obj/ai_field.js homingMove
    /// <summary>27:B50E: obj_anim -= B556[level]; step; 1-in-64 aimed shot on ($08 &amp; 7) = 0 when obj_shot_id != 0</summary>
    private static void HomingMove(Machine m, int x)
    {
        m.Wr(Anim + x, m.Rd(Anim + x) - Prg.CpuRd(m, 0xB556 + m.Rd(Phase + x)));
        Dec(m, Timer + x);
        StepFree(m, x);
        if (m.Rd(ShotId + x) == 0 || (m.Rd(0x08) & 7) != 0 || ObjUtil.Rand64(m) != 0) return;
        ObjUtil.DirToTarget(m, x, 1);
        ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), m.Rd(0x20));
    }

    // js: game/obj/ai_field.js aiHomingFlyer
    /// <summary>AI $45</summary>
    public static void AiHomingFlyer(Machine m, int x)
    {
        if (m.Rd(SpawnParam + x) == 1 && (m.Rd(Active + 0x0D) & 0x80) == 0)
        {                                                                 // B4BA: param 1 dies with slot $0D's b7
            m.Wr(Active + x, 0);
            return;
        }
        if (OffscreenSkip(m, x)) return;
        m.Wr(Flags + x, m.Rd(Flags + x) & 0xEF);
        if ((m.Rd(Timer + x) & 0x0F) == 0) HomingSteer(m, x);
        HomingMove(m, x);
    }

    // js: game/obj/ai_field.js aiHomingShooter
    /// <summary>AI $48 (no object type uses it): attack = aimed shot at $0640 = $18; else AiShootCheck, AI $45, and a
    /// second steer + step when obj_timer reached 0 (B591 JMP $B4E5)</summary>
    public static void AiHomingShooter(Machine m, int x)
    {
        if (m.Rd(ActTimer + x) != 0)
        {
            Dec(m, ActTimer + x);
            m.Wr(Anim + x, m.Rd(Anim + x) - 2);
            if (m.Rd(ActTimer + x) != 0x18) return;
            ObjUtil.DirToTarget(m, x, 1);
            m.Wr(MetaDir + x, (m.Rd(0x20) << 1) & 0xFF);
            ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), m.Rd(0x20));
            return;
        }
        if (AiStatic.AiShootCheck(m, x)) return;
        AiHomingFlyer(m, x);
        if (m.Rd(Timer + x) != 0) return;
        HomingSteer(m, x);
        HomingMove(m, x);
    }

    // js: game/obj/ai_field.js aiWaitClear
    /// <summary>AI $5E: every 64 frames (per slot) look for a live spawn-param $50 object on the player's y_hi; none -> AI $2A</summary>
    public static void AiWaitClear(Machine m, int x)
    {
        if (((((x << 4) & 0xFF) + m.Rd(0x08)) & 0x3F) != 0) return;
        for (int y = 0x0D; y < 0x1F; y++)
        {
            if (m.Rd(Active + y) != 0 && m.Rd(YHi + y) == m.Rd(YHi) && m.Rd(SpawnParam + y) == 0x50) return;
        }
        m.Wr(Active + x, 0x2A);
        m.Wr(Coll + x, 0x10);
    }

    // js: game/obj/ai_field.js aiPatrolGunner
    /// <summary>AI $5C</summary>
    public static void AiPatrolGunner(Machine m, int x)
    {
        m.Wr(Elem + x, 0x0F);
        if (m.Rd(Timer2 + x) != 0)
        {                                                                 // B66E: attack burst, elem 0
            m.Wr(Elem + x, 0);
            m.Wr(Anim + x, 8);
            Dec(m, Timer2 + x);
            if ((m.Rd(Timer2 + x) & 7) != 0) return;
            ObjUtil.DirToTarget(m, x, 1);
            ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), m.Rd(0x20));
            return;
        }
        m.Wr(Anim + x, 0);
        int xl = m.Rd(XLo + x);
        if (xl < 0x36 || xl >= 0xE8) m.Wr(MetaDir + x, (m.Rd(MetaDir + x) + 4) & 7);
        Dec(m, Timer + x);
        if ((m.Rd(Timer + x) & 0x1F) == 0 && (ObjUtil.Rand64(m) & 1) != 0) m.Wr(Timer2 + x, 0x20);
        PatrolStep(m, x);
    }

    // js: game/obj/ai_field.js patrolStep
    /// <summary>27:B65F (shared by AI $5C / $5D): ObjStepVector before ObjPosToTemp, then step, no BG test</summary>
    private static void PatrolStep(Machine m, int x)
    {
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.TempAddStep(m);
        ObjUtil.TempToObjPos(m, x);
    }

    // js: game/obj/ai_field.js aiDrifter
    /// <summary>AI $5D</summary>
    public static void AiDrifter(Machine m, int x)
    {
        Dec(m, Anim + x);
        Dec(m, Timer + x);
        if ((m.Rd(Timer + x) & 0x3F) == 0)
        {
            ObjUtil.DirToTarget(m, x, 1);
            m.Wr(MetaDir + x, m.Rd(0x20));
        }
        PatrolStep(m, x);
    }

    // js: game/obj/ai_field.js aiOrbiter
    /// <summary>AI $58: position = slot $0D + circle table 27:B6F4 (radius 24); 1-in-64 aimed shot every 64 obj_anim steps</summary>
    public static void AiOrbiter(Machine m, int x)
    {
        int y = (((x << 3) & 0xFF) + m.Rd(0x08)) & 0x1F;
        m.Wr(0x30, Prg.CpuRd(m, 0xB6F4 + y));
        y = (y + 0xF8) & 0x1F;
        m.Wr(0x31, Prg.CpuRd(m, 0xB6F4 + y) - 8);
        m.Wr(0x10, x);
        ObjUtil.ObjPosToTemp(m, 0x0D);
        ObjUtil.TempAddOffset(m);
        ObjUtil.TempToObjPos(m, x);
        Dec(m, Anim + x);
        if ((m.Rd(Anim + x) & 0x3F) != 0 || ObjUtil.Rand64(m) != 0) return;
        ObjUtil.DirToTarget(m, x, 1);
        ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), m.Rd(0x20));
    }

    // js: game/obj/ai_field.js chestHop
    /// <summary>ChestHop 27:B79F (A = counter): dy = B7E6[nibble of B7C6[(A >> 1) &amp; $1F], low nibble on odd A]; y move only</summary>
    private static void ChestHop(Machine m, int x, int a)
    {
        m.Wr(0x10, a);
        int v = Prg.CpuRd(m, 0xB7C6 + ((a >> 1) & 0x1F));
        int c = m.Rd(0x10) & 1;
        m.Wr(0x10, m.Rd(0x10) >> 1);
        if (c == 0) v >>= 4;
        ObjUtil.ObjPosToTemp(m, x);
        m.Wr(0x31, Prg.CpuRd(m, 0xB7E6 + (v & 0x0F)));
        m.Wr(0x30, 0);
        ObjUtil.TempAddStep(m);
        ObjUtil.TempToObjPos(m, x);
    }

    // js: game/obj/ai_field.js aiTrapChest
    /// <summary>AI $2B (type $73): closed chest (meta $AA) until the player is in its cell, then hops around (meta $90)</summary>
    public static void AiTrapChest(Machine m, int x)
    {
        if (m.Rd(Phase + x) == 0)
        {                                                                 // ACAE
            m.Wr(MetaId + x, 0xAA);
            ObjUtil.DirToTarget(m, x, 1);
            if (m.Rd(0x21) == 0) m.Wr(Phase + x, m.Rd(Phase + x) + 1);
            return;
        }
        Dec(m, Timer + x);
        m.Wr(MetaId + x, 0x90);
        Dec(m, Anim + x);
        Dec(m, Timer2 + x);
        ChestHop(m, x, m.Rd(Timer2 + x));
        if ((m.Rd(Timer2 + x) & 0x3F) != 0)
        {                                                                 // AC80: step from the hopped position
            ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
            ObjUtil.TempAddStep(m);
            ObjUtil.TempToObjPos(m, x);
            return;
        }
        ObjUtil.ObjBgProbe(m, x, 0x40);                                   // AC8C: every 64 frames: wall -> reverse
        if ((m.Rd(0x20) & 6) != 0)
        {
            m.Wr(MetaDir + x, (m.Rd(MetaDir + x) + 4) & 7);
            return;
        }
        ObjUtil.DirToTarget(m, x, 1);                                     // else re-aim (8-dir)
        m.Wr(MetaDir + x, m.Rd(0x20));
    }

    // js: game/obj/ai_field.js aiCoin
    /// <summary>AI $7B coin: lifetime obj_timer -> ObjBecomeNext; blinks (obj_flags b2) below $30; hops while >= $C0</summary>
    public static void AiCoin(Machine m, int x)
    {
        m.Wr(Anim + x, m.Rd(Anim + x) - 2);
        Dec(m, Timer + x);
        if (m.Rd(Timer + x) == 0) { ObjUtil.ObjBecomeNext(m, x); return; }
        if (m.Rd(Timer + x) < 0x30)
        {
            Dec(m, Anim + x);
            m.Wr(Flags + x, m.Rd(Flags + x) | 4);
        }
        if (m.Rd(Timer + x) >= 0xC0) ChestHop(m, x, m.Rd(Timer + x));
    }

    // js: game/obj/ai_field.js aiPitShuttle
    /// <summary>AI $38 AiPitShuttle 27:AFA9 (types $7E/$7F, s117): DEC obj_timer; probe the point PlatformProbeOfs
    /// 27:AFED[dir&amp;6] ahead (26:9824, ObjBgProbe A = 0 -> no cache, no flag update); b0 (pit) set -> one free step in
    /// obj_meta_dir, else dir + 4 and obj_freeze $20</summary>
    public static void AiPitShuttle(Machine m, int x)
    {
        Dec(m, Timer + x);
        ObjUtil.ObjPosToTemp(m, x);
        int y = m.Rd(MetaDir + x) & 6;
        m.Wr(0x30, Prg.CpuRd(m, 0xAFED + y));
        m.Wr(0x31, Prg.CpuRd(m, 0xAFEE + y));
        ObjUtil.TempAddOffset(m);
        ObjUtil.ObjBgProbe(m, x, 0);
        if ((m.Rd(0x20) & 1) != 0) { StepFree(m, x); return; }
        m.Wr(MetaDir + x, (m.Rd(MetaDir + x) + 4) & 7);
        m.Wr(Freeze + x, 0x20);
    }

    // js: game/obj/ai_field.js aiCrumblePlatform
    /// <summary>AI $3C AiCrumblePlatform 27:AFF5 (type $9F, s117): 27:AFFB, then AiPitShuttle (also right after the
    /// despawn). Ridden (ride_platform_slot = X) -> obj_phase 1; else obj_anim $1E -> sound $64 (Y = $78 from
    /// ObjUpdateAll's TAY); phase != 0 -> obj_coll 0, meta $B7, DEC obj_anim, 0 -> obj_active 0</summary>
    public static void AiCrumblePlatform(Machine m, int x)
    {
        if (x == m.Rd(RideSlot)) m.Wr(Phase + x, 1);
        else
        {
            if (m.Rd(Anim + x) == 0x1E) PlaySound.Play(m, 0x64, 0x78);
            if (m.Rd(Phase + x) != 0)
            {
                m.Wr(Coll + x, 0);
                m.Wr(MetaId + x, 0xB7);
                Dec(m, Anim + x);
                if (m.Rd(Anim + x) == 0) m.Wr(Active + x, 0);
            }
        }
        AiPitShuttle(m, x);
    }

    // js: game/obj/ai_field.js aiNpcBossReveal
    /// <summary>AI $3A AiNpcBossReveal 27:A555 (s133; type $2A, area $6E slot $0E, hidden on the slot-$0D NPC): 3 sword
    /// hits -> messages $10.$10-$12, then slots X-1/X+1 off and ObjInit(X, $7D) (BossAiE6). Scenario `npc_reveal`.</summary>
    private static void AiNpcBossReveal(Machine m, int x)
    {
        if (m.Rd(Active - 1 + x) == 0) { m.Wr(Active + x, 0); return; }        // A555: the NPC in slot X-1 is gone
        if (m.Rd(Phase + x) == 3)
        {                                                                       // A561
            m.Wr(Active - 1 + x, 0); m.Wr(Active + 1 + x, 0);
            m.Wr(0x11, 0x7D); m.Wr(0x10, x);
            ObjInit.FarObjInit(m);                                              // A573 JMP FarCall_C25D
            return;
        }
        m.Wr(MetaDir + x, (((m.Rd(0x0301) + 2) & 3) << 1) & 0xFF);             // A576: face the player
        if (m.Rd(Timer2 + x) != 0) { Dec(m, Timer2 + x); return; }
        if ((m.Rd(Flags2 + x) & 0x80) == 0 || m.Rd(0xD1) != 1) return;         // A58B: hit, player on screen row 1
        int yl = m.Rd(0xB1);
        if (yl >= 0xB0 || yl < 0x28) return;
        m.Wr(0x03C0 + x, 0xFF);                                                 // A5A0: HP back to $FF
        m.Wr(Timer2 + x, 0x10);
        m.Wr(Phase + x, (m.Rd(Phase + x) + 1) & 0xFF);
        m.Wr(0x06C3, (m.Rd(Phase + x) + 0x0F) & 0xFF);                          // pending_msg_id
        m.Wr(0x06E3, 0x10);                                                     // pending_msg_group
        m.Wr(0x41, 0x10);                                                       // sub_state
    }

    // js: game/obj/ai_field.js cell16
    /// <summary>27:AE07: (Y:A) >> 4, low byte ($1F = scratch)</summary>
    private static int Cell16(Machine m, int lo, int hi)
    {
        int h = hi, a = lo;
        for (int i = 0; i < 4; i++) { int c = h & 1; h >>= 1; a = (a >> 1) | (c << 7); }
        m.Wr(0x1F, h);
        return a;
    }

    // js: game/obj/ai_field.js npcCellScan
    /// <summary>27:ADC8: slots $12..$13-1 (minus X): one active in the 16 px cell $10/$11 -> $20 |= 6; true = found (RTS)</summary>
    private static bool NpcCellScan(Machine m, int x)
    {
        do
        {
            int y = m.Rd(0x12);
            if (x != y && m.Rd(Active + y) != 0)
            {
                if (Cell16(m, m.Rd(XLo + y), m.Rd(0x90 + y)) == m.Rd(0x10)
                    && Cell16(m, m.Rd(0xB0 + y), m.Rd(YHi + y)) == m.Rd(0x11))
                {
                    m.Wr(0x20, m.Rd(0x20) | 6);
                    return true;
                }
            }
            m.Wr(0x12, m.Rd(0x12) + 1);
        } while (m.Rd(0x12) != m.Rd(0x13));
        return false;
    }

    // js: game/obj/ai_field.js npcCellBlocked
    /// <summary>27:ADA4: the look-ahead cell is blocked by another object (slots $0D-$1E, then slot 0)</summary>
    private static void NpcCellBlocked(Machine m, int x)
    {
        m.Wr(0x10, Cell16(m, m.Rd(0x34), m.Rd(0x35)));
        m.Wr(0x11, Cell16(m, m.Rd(0x36), m.Rd(0x37)));
        m.Wr(0x12, 0x0D); m.Wr(0x13, 0x1F);
        NpcCellScan(m, x);                                                      // ADBE (its RTS returns to ADC1)
        m.Wr(0x12, 0); m.Wr(0x13, 1);
        NpcCellScan(m, x);                                                      // falls into ADC8
    }

    // js: game/obj/ai_field.js aiNpcStill
    /// <summary>AiNpcStill 27:AE16 (AI $31, AiNpcWander with obj_behav b2): b0 -> obj_anim = $08>>3; $0603 = 0 -> meta = $06C0,X</summary>
    private static void AiNpcStill(Machine m, int x)
    {
        if ((m.Rd(0x06E0 + x) & 1) != 0) m.Wr(Anim + x, m.Rd(0x08) >> 3);
        if (m.Rd(0x0603) == 0) m.Wr(MetaId + x, m.Rd(0x06C0 + x));
    }

    // js: game/obj/ai_field.js aiNpcWander
    /// <summary>AI $30 AiNpcWander 27:ACD9 (s140; the town NPCs): pause timer $0440; obj_behav b2 -> AiNpcStill; moves on
    /// BitMask[$08&amp;7] &amp; obj_phase frames; every 16 steps a Rand64 dir (bits 0/3 clear) with a 16 px look-ahead
    /// (NpcLookAhead 27:AD9C, ObjBgProbe A=0 + the object cell scan 27:ADA4), blocked -> turn +2 (even slot) / -2 (odd),
    /// 4 tries; random 32-frame pause; step = 26:98A8 / 8480 / 9861 / 9A30($C0) / 9897, obj_anim--</summary>
    private static void AiNpcWander(Machine m, int x)
    {
        if (m.Rd(ShotId + x) != 0) { Dec(m, ShotId + x); return; }
        if ((m.Rd(0x06E0 + x) & 4) != 0) { AiNpcStill(m, x); return; }
        if ((Prg.CpuRd(m, 0xC000 + (m.Rd(0x08) & 7)) & m.Rd(Phase + x)) == 0) return;
        if ((m.Rd(Anim + x) & 0x0F) == 0)
        {
            m.Wr(0x21, 4);
            if ((m.Rd(Anim + x) & 0x10) == 0)
            {
                int r = ObjUtil.Rand64(m);
                m.Wr(0x10, r);
                if ((r & 9) == 0) m.Wr(MetaDir + x, r & 6);
            }
            for (; ; )
            {                                                                   // AD1E
                int d = m.Rd(MetaDir + x) & 6;
                m.Wr(MetaDir + x, d);
                m.Wr(0x30, Prg.CpuRd(m, 0xAD9C + d));
                m.Wr(0x31, Prg.CpuRd(m, 0xAD9D + d));
                ObjUtil.ObjPosToTemp(m, x);
                ObjUtil.TempAddOffset(m);
                ObjUtil.ObjBgProbe(m, x, 0);
                NpcCellBlocked(m, x);
                if ((m.Rd(0x20) & 6) == 0) break;
                m.Wr(0x21, m.Rd(0x21) - 1);
                if (m.Rd(0x21) == 0) return;
                m.Wr(MetaDir + x, (d + ((x & 1) != 0 ? -2 : 2)) & 6);           // AD4A
            }
            m.Wr(0x10, m.Rd(MetaDir + x) >> 1);                                 // AD60
            m.Wr(MetaId + x, (m.Rd(MetaId + x) & 0xFC) | m.Rd(0x10));
            if ((m.Rd(0x08) & 0xD6) == 0 && (((x + m.Rd(XLo)) ^ m.Rd(0xB0)) & 2) == 0) m.Wr(ShotId + x, 0x20);
        }
        ObjUtil.ObjPosToTemp(m, x);                                             // AD84
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.TempAddStep(m);
        ObjUtil.ObjBgProbe(m, x, 0xC0);
        ObjUtil.TempToObjPos(m, x);
        Dec(m, Anim + x);
    }

    // ---- s170: AI $34 $39 $40 (ai_missing.py) ----

    // js: game/obj/ai_field.js aiPeriodicShot
    /// <summary>AI $34 27:B318 (type $8F): obj_anim--; every 8th step, onscreen: SpawnShot(obj_shot_id, Y = x_lo b7 ? 6 : 2)
    /// (ROL x4 of the 9-bit A:C ring puts old b7 in b2, so the caller's carry never matters)</summary>
    public static void AiPeriodicShot(Machine m, int x)
    {
        Dec(m, Anim + x);
        if ((m.Rd(Anim + x) & 7) != 0) return;
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), (m.Rd(XLo + x) & 0x80) != 0 ? 6 : 2);   // B336 JMP 26:972D
    }

    // js: game/obj/ai_field.js aiArenaLock
    /// <summary>AI $39 AiArenaLock 27:A519 (type $29): slot X-1 inactive -> obj_active = 0, boss_cam_lock = 0. Phase 0:
    /// onscreen with screen y &lt; $C0 -> phase 1, boss_cam_lock = $FF. Phase != 0: $07D5 = 0 -> boss_cam_lock = $FF and
    /// slot 0 obj_freeze = 5; else boss_cam_lock = $10</summary>
    public static void AiArenaLock(Machine m, int x)
    {
        if (m.Rd(0x049F + x) == 0)
        {
            m.Wr(Active + x, 0);
            m.Wr(0x07D7, 0);
            return;
        }
        if (m.Rd(Phase + x) == 0)
        {
            if ((m.Rd(Flags + x) & 0x80) != 0) return;
            if (m.Rd(0x05E0 + x) >= 0xC0) return;
            m.Wr(Phase + x, 1);
            m.Wr(0x07D7, 0xFF);
            return;
        }
        if (m.Rd(0x07D5) == 0)
        {
            m.Wr(0x07D7, 0xFF);
            m.Wr(Freeze, 5);                                                    // A54B STA $05A0 (no ,X)
            return;
        }
        m.Wr(0x07D7, 0x10);
    }

    // js: game/obj/ai_field.js aiWander16
    /// <summary>AI $40 AiWander16 27:B39E (types $55 $7C): AiOffscreenHalf; phase 0: phase = $CF, heading = DirToTarget(Y = 1)*2
    /// + B41E[(x_lo + y_lo + timer of slot 0) &amp; 3]; phase $01-$7F: anim -= 2, every 8 anim steps heading = B40E[(x +
    /// anim >> 3) &amp; $F], flags2 $0C; phase >= $80: anim -= 3, phase--, flags2 $0E; both then timer--, one free step, phase--</summary>
    public static void AiWander16(Machine m, int x)
    {
        if (OffscreenSkip(m, x)) return;
        int p = m.Rd(Phase + x);
        if (p == 0)
        {
            m.Wr(Phase + x, 0xD0);
            ObjUtil.DirToTarget(m, x, 1);                                       // B3AD LDY #1, JSR 26:8409
            m.Wr(0x10, (m.Rd(0x20) << 1) & 0xFF);
            int y = (m.Rd(XLo) + m.Rd(0xB0) + m.Rd(Timer)) & 3;
            m.Wr(MetaDir + x, (Prg.CpuRd(m, 0xB41E + y) + m.Rd(0x10)) & 0x0F);
            Dec(m, Phase + x);
            return;
        }
        if ((p & 0x80) != 0)
        {                                                                       // B422
            m.Wr(Anim + x, m.Rd(Anim + x) - 3);
            Dec(m, Phase + x);
            m.Wr(Flags2 + x, 0x0E);
        }
        else
        {
            m.Wr(Anim + x, m.Rd(Anim + x) - 2);
            if ((m.Rd(Anim + x) & 6) == 0)
            {
                m.Wr(0x10, m.Rd(Anim + x) >> 3);
                m.Wr(MetaDir + x, Prg.CpuRd(m, 0xB40E + ((x + m.Rd(0x10)) & 0x0F)));
                m.Wr(Flags2 + x, 0x0C);
            }
        }
        Dec(m, Timer + x);                                                      // B3F9
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.TempAddStep(m);
        Dec(m, Phase + x);
        ObjUtil.TempToObjPos(m, x);                                             // B40B JMP 26:9897
    }

    // ---- s171: AI $07 $12 $13 $32 (ai_missing.py) ----

    // js: game/obj/ai_field.js swap
    /// <summary>(aiWiggleShot's local swap) exchange $30 / $31</summary>
    private static void Swap(Machine m) { int a = m.Rd(0x30); m.Wr(0x30, m.Rd(0x31)); m.Wr(0x31, a); }

    // js: game/obj/ai_field.js aiWiggleShot
    /// <summary>AI $12 27:B174 (type $12, the wind level-$18 shot): phase 0 -> phase 1, timer2 = B1E5[X], start offset
    /// ($30, $31) = (B1E9[X], B1ED[X]) swapped when meta_dir b1, TempAddOffset. Then anim -= 2, timer-- 0 -> despawn;
    /// wiggle (B1F5/B1F6[(timer2 &amp; $1F) * 2], same swap) timer2++, TempAddStep, then the meta_dir step</summary>
    public static void AiWiggleShot(Machine m, int x)
    {
        if (m.Rd(Phase + x) == 0)
        {                                                                       // B1BC
            m.Wr(Phase + x, 1);
            m.Wr(Timer2 + x, Prg.CpuRd(m, 0xB1E5 + x));
            m.Wr(0x30, Prg.CpuRd(m, 0xB1E9 + x));
            m.Wr(0x31, Prg.CpuRd(m, 0xB1ED + x));
            if ((m.Rd(MetaDir + x) & 2) != 0) Swap(m);
            ObjUtil.ObjPosToTemp(m, x);
            ObjUtil.TempAddOffset(m);
            ObjUtil.TempToObjPos(m, x);
            return;
        }
        m.Wr(Anim + x, m.Rd(Anim + x) - 2);
        Dec(m, Timer + x);
        if (m.Rd(Timer + x) == 0) { ObjUtil.ObjDespawn(m, x); return; }        // B184 JMP 26:98C7
        int y = (m.Rd(Timer2 + x) & 0x1F) << 1;
        m.Wr(0x30, Prg.CpuRd(m, 0xB1F5 + y));
        m.Wr(0x31, Prg.CpuRd(m, 0xB1F6 + y));
        if ((m.Rd(MetaDir + x) & 2) != 0) Swap(m);
        m.Wr(Timer2 + x, m.Rd(Timer2 + x) + 1);
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.TempAddStep(m);
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.TempAddStep(m);
        ObjUtil.TempToObjPos(m, x);                                             // B1B9 JMP 26:9897
    }

    // js: game/obj/ai_field.js aiSpiralShot
    /// <summary>AI $13 27:B235 (type $16, a spiral shot): timer |= $80; timer2++; (timer2 &amp; act_timer) = 0 -> orbit_level--
    /// (0 -> despawn), meta_dir = ((X &amp; 7) * 2 + phase) &amp; $0F, phase++, every 16 turns act_timer = act_timer &lt;&lt; 1 | 1;
    /// then one free step</summary>
    public static void AiSpiralShot(Machine m, int x)
    {
        m.Wr(Timer + x, m.Rd(Timer + x) | 0x80);
        m.Wr(Timer2 + x, m.Rd(Timer2 + x) + 1);
        if ((m.Rd(Timer2 + x) & m.Rd(ActTimer + x)) == 0)
        {
            Dec(m, Orbit + x);
            if (m.Rd(Orbit + x) == 0) { ObjUtil.ObjDespawn(m, x); return; }    // B24D JMP 26:98C7
            m.Wr(MetaDir + x, (((x & 7) << 1) + m.Rd(Phase + x)) & 0x0F);
            m.Wr(Phase + x, m.Rd(Phase + x) + 1);
            if ((m.Rd(Phase + x) & 0x0F) == 0) m.Wr(ActTimer + x, (m.Rd(ActTimer + x) << 1) | 1);   // SEC ROL
        }
        StepFree(m, x);
    }

    // js: game/obj/ai_field.js aiPathWalk
    /// <summary>AI $32 AiPathWalk 27:AE31 (type $38): slot 0 obj_freeze = 4; moves on BitMask[$08 &amp; 7] &amp; phase frames;
    /// anim--, every 16 steps the next byte of path AiPathTable[obj_param1] at timer2: &lt; $80 = new meta_dir (timer2++),
    /// $FC = slots $0D/$0E restart on path $0D, $FD = player AI $0A + this slot AI $30 (behav 6, orbit $2E), $FE = off,
    /// other = AI $30. meta_id low 2 bits = dir >> 1, one free step; area $D4 path $0D slot $0D carries the player</summary>
    public static void AiPathWalk(Machine m, int x)
    {
        m.Wr(Freeze, 4);
        if ((Prg.CpuRd(m, 0xC000 + (m.Rd(0x08) & 7)) & m.Rd(Phase + x)) == 0) return;
        Dec(m, Anim + x);
        if ((m.Rd(Anim + x) & 0x0F) == 0)
        {
            int y = m.Rd(Param1 + x) << 1 & 0xFF;
            m.Wr(0x10, Prg.CpuRd(m, 0xAF04 + y));
            m.Wr(0x11, Prg.CpuRd(m, 0xAF05 + y));
            int b = Prg.CpuRd(m, ((m.Rd(0x11) << 8 | m.Rd(0x10)) + m.Rd(Timer2 + x)) & 0xFFFF);
            if ((b & 0x80) != 0)
            {
                if (b == 0xFC)
                {                                                               // AE7B
                    m.Wr(Active + 0x0D, 0x32); m.Wr(Active + 0x0E, 0x32);
                    m.Wr(Param1 + 0x0D, 0x0D); m.Wr(Param1 + 0x0E, 0x0D);
                    m.Wr(Anim + 0x0D, 1); m.Wr(Anim + 0x0E, 1);
                    m.Wr(Timer2 + 0x0D, 0); m.Wr(Timer2 + 0x0E, 0);
                    return;
                }
                if (b == 0xFD)
                {                                                               // AE9C
                    m.Wr(Active, 0x0A);
                    m.Wr(Active + x, 0x30);
                    m.Wr(0x06E0 + x, 6);
                    m.Wr(Orbit + x, 0x2E);
                    return;
                }
                m.Wr(Active + x, b == 0xFE ? 0 : 0x30);
                return;
            }
            m.Wr(MetaDir + x, b);                                               // AEB1
            m.Wr(Timer2 + x, m.Rd(Timer2 + x) + 1);
        }
        m.Wr(0x10, m.Rd(MetaId + x) & 0xFC);                                    // AEB7
        m.Wr(MetaId + x, ((m.Rd(MetaDir + x) >> 1) & 3) | m.Rd(0x10));
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.TempAddStep(m);
        ObjUtil.TempToObjPos(m, x);
        if (m.Rd(0x6C) != 0xD4 || m.Rd(Param1 + x) != 0x0D || x != 0x0D) return;
        int yl = m.Rd(0xB0 + x);                                                // AEE9: the player rides slot $0D
        m.Wr(0xB1, yl); m.Wr(0xB0, yl);
        int xl = (m.Rd(XLo + x) + 0x10) & 0xFF;
        m.Wr(0x71, xl); m.Wr(XLo, xl);
        m.Wr(Timer2, 0); m.Wr(ActTimer, 0); m.Wr(Orbit, 0);
    }

    // js: game/obj/ai_field.js aiElementWall
    /// <summary>AI $07 27:A85F (types $D0-$D3, the element walls): shot_id != 0 -> outside areas $A8/$8F overlay = $4C;
    /// inside, behav != 0 -> behav--, anim = old behav >> 1, new behav $30 -> SpawnShot(shot_id, 0), spawned -> its
    /// meta_dir = A8A2[Rand64 &amp; 3]; behav 0 and onscreen -> behav $3E. Then: visited screen -> obj_active 0; meta_id =
    /// overlay; onscreen inside the box A8FC-A90B[phase] -> obj_coll |= $10</summary>
    public static void AiElementWall(Machine m, int x)
    {
        if (m.Rd(ShotId + x) != 0)
        {
            int a = m.Rd(0x6C);
            if (a != 0xA8 && a != 0x8F) m.Wr(Overlay + x, 0x4C);
            else if (m.Rd(0x06E0 + x) != 0)
            {                                                                   // A87A
                int b = m.Rd(0x06E0 + x);
                m.Wr(0x06E0 + x, b - 1);
                m.Wr(Anim + x, b >> 1);
                if (m.Rd(0x06E0 + x) == 0x30 && ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), 0))
                {
                    int d = Prg.CpuRd(m, 0xA8A2 + (ObjUtil.Rand64(m) & 3));
                    m.Wr(MetaDir + m.Rd(0x10), d);
                }
            }
            else if ((m.Rd(Flags + x) & 0x80) == 0) m.Wr(0x06E0 + x, 0x3E);   // A8A6
        }
        m.Wr(0x10, Prg.CpuRd(m, 0xC000 + m.Rd(0x90 + x)));                      // A8B0
        if ((m.Rd(0x62F0 + m.Rd(YHi + x)) & m.Rd(0x10)) != 0) m.Wr(Active + x, 0);
        m.Wr(MetaId + x, m.Rd(Overlay + x));                                    // A8C5
        if ((m.Rd(Flags + x) & 0x80) != 0) return;
        int p = m.Rd(Phase + x), sy = m.Rd(0x05E0 + x), sx = m.Rd(0x05C0 + x);
        if (sy < Prg.CpuRd(m, 0xA8FC + p) || sy >= Prg.CpuRd(m, 0xA900 + p)) return;
        if (sx < Prg.CpuRd(m, 0xA904 + p) || sx >= Prg.CpuRd(m, 0xA908 + p)) return;
        m.Wr(Coll + x, m.Rd(Coll + x) | 0x10);
    }

    private static AiFn P(Action<Machine, int> f) => (m, x, r) => { f(m, x); return null; };

    /// <summary>(ai_field.js AI_FIELD) ObjAiTable ids handled here</summary>
    public static readonly Dictionary<int, AiFn> Ai = new()
    {
        [0x07] = P(AiElementWall), [0x12] = P(AiWiggleShot), [0x13] = P(AiSpiralShot), [0x32] = P(AiPathWalk),   // s171
        [0x34] = P(AiPeriodicShot), [0x39] = P(AiArenaLock), [0x40] = P(AiWander16),                             // s170
        [0x30] = P(AiNpcWander), [0x31] = P(AiNpcStill), [0x78] = (m, x, r) => null,  // $78 = 27:A914 RTS (event triggers)
        [0x3A] = P(AiNpcBossReveal), [0x2B] = P(AiTrapChest), [0x44] = P(AiDasher), [0x45] = P(AiHomingFlyer),
        [0x48] = P(AiHomingShooter), [0x58] = P(AiOrbiter), [0x5C] = P(AiPatrolGunner), [0x5D] = P(AiDrifter),
        [0x5E] = P(AiWaitClear), [0x7B] = P(AiCoin), [0x38] = P(AiPitShuttle), [0x3C] = P(AiCrumblePlatform),
    };
}
