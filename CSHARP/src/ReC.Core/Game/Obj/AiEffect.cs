// re-c C# port — small effect AIs reached from ObjUpdateAll (= web/src/game/obj/ai_effect.js, s93). REVERSE.md
// "Enemies — catalogue + generic enemy AIs" (ObjAiTable 27:A314) + "Boss / effect AIs (s93)".
//
//   AI $04 AiPlayerFollowC 27:B2FC  slot $0C <- player x/y; r.X = $0C (ObjUpdateAll goes on at slot $0D)
//   AI $05 AiFollowFacing  27:A219  this slot <- slot 0 + 4 px in slot 1's facing; DEC $0603 (s110)
//   AI $06 / $08 / $7C     row redraw AIs (27:B93E / 27:B7F6), AI $74 ObjAiExplode, AI $7D DeathPuff, AI $5F AiFloorReveal
//   AI $76 / $77 AiMpDrain 27:A4DC / 27:A4E0 (iterator: HudRedraw spins in PpuQueueWaitSrc($7B))
using ReC.Game.States;
using ReC.Game.World;

namespace ReC.Game.Obj;

public static class AiEffect
{
    private const int Anim = 0x04E0, Active = 0x04A0, Timer2 = 0x0620, Phase = 0x0600, ActTimer = 0x0640;
    private const int MetaId = 0x0300, TileOfs = 0x0320, Flags = 0x0380, Drop = 0x0500;

    // js: game/obj/ai_effect.js aiPlayerFollowC
    /// <summary>AI $04 27:B2FC: ignores its own slot X; always moves / ages slot $0C, and returns with X = $0C</summary>
    public static IEnumerable<Y>? AiPlayerFollowC(Machine m, int x, AiRet r)
    {
        ObjUtil.ObjPosToTemp(m, 0);
        ObjUtil.TempToObjPos(m, 0x0C);
        int a = (m.Rd(Anim + 0x0C) - m.Rd(Timer2 + 0x0C)) & 0xFF;
        m.Wr(Anim + 0x0C, a);
        if ((a & 0x80) != 0) m.Wr(Active + 0x0C, 0);
        r.X = 0x0C;
        return null;
    }

    // js: game/obj/ai_effect.js aiMpDrain
    /// <summary>AI $76 27:A4DC (27:A4E0 = an unreferenced second entry with overlay $99)</summary>
    public static IEnumerable<Y> AiMpDrain(Machine m, int x, int overlay = 0xA3, int id = 0x76)
    {
        m.Wr(0x0581, overlay);                                    // obj_overlay_id slot 1
        if ((m.Rd(0x08) & 0x0F) == 0)
        {
            PlaySound.Play(m, 0x31, (id << 1) & 0xFF);            // Y = id*2 from ObjUpdateAll's TAY 30:CD07
            m.Wr(0x05A0, 2);                                      // obj_freeze slot 0
            if (m.Rd(0x0708) != 0)
            {                                                     // mp_cur
                m.Wr(0x0708, m.Rd(0x0708) - 1);
                foreach (var s in Death.HudRedraw(m, 4, x)) yield return s;   // A4FF JSR 26:8E46 (MP digits)
            }
        }
        int sum = m.Rd(Anim + x) + (m.Rd(0x4B) != 0 ? 0xFD : 0xFF);   // pad_new
        m.Wr(Anim + x, sum & 0xFF);
        if (sum < 0x100) m.Wr(Active + x, 0);
    }

    // js: game/obj/ai_effect.js aiFollowFacing
    /// <summary>AI $05 27:A219 (s110; slot 3 in the start room): DEC $0603 while != 0 (slot 3 fixed, not X), then this
    /// slot = player body (slot 0) + 4 px toward slot 1's facing (obj_meta_id_s01 &amp; 3 -> 27:A242 dx/dy)</summary>
    public static IEnumerable<Y>? AiFollowFacing(Machine m, int x, AiRet r)
    {
        if (m.Rd(0x0603) != 0) m.Wr(0x0603, m.Rd(0x0603) - 1);
        m.Wr(0x10, x);                                            // STX $10 (X saved around 26:98A8)
        ObjUtil.ObjPosToTemp(m, 0);
        int y = (m.Rd(0x0301) & 3) << 1;
        m.Wr(0x30, Prg.CpuRd(m, 0xA242 + y));
        m.Wr(0x31, Prg.CpuRd(m, 0xA243 + y));
        ObjUtil.TempAddOffset(m);                                 // 26:9824
        ObjUtil.TempToObjPos(m, x);                               // JMP 26:9897
        return null;
    }

    // js: game/obj/ai_effect.js explodeStep
    /// <summary>27:B850 (ObjAiExplode's tail, also the head of AI $08): camera lock $10, DEC obj_phase; on borrow spawn
    /// shot $3E placed from the step record 27:B8E6[timer2*4]; dx / phase 0 = done -> ObjBecomeNext. y = the caller's Y.
    /// Returns true when a shot was spawned (JS `true`; the other exits are undefined).</summary>
    private static bool ExplodeStep(Machine m, int x, int y)
    {
        m.Wr(0x07D7, 0x10);                                       // boss_cam_lock
        int ph = (m.Rd(Phase + x) - 1) & 0xFF;
        m.Wr(Phase + x, ph);
        if ((ph & 0x80) == 0) return false;
        if ((m.Rd(Active + x) & 4) != 0)
        {                                                         // B85B
            PlaySound.Play(m, 0x54, y);
            m.Wr(0x07F5, 0x45);
            m.Wr(TileOfs + x, 0x80);
            m.Wr(0x07E7, 4);
            Death.PaletteBuildRom(m, 0);                          // 26:8C0E
            y = 4;                                                // PaletteBuild leaves Y = 4 (see collide.js)
        }
        int area = m.Rd(0x6C);
        bool skip = area == 0xE4 || area == 0x73 || ((area == 0x28 || area == 0x7C) && x == 0x0E);
        int t = m.Rd(Timer2 + x);
        if (!skip && ObjUtil.SpawnShot(m, x, 0x3E, y))
        {                                                         // B8A8
            m.Wr(0x11, x);
            m.Wr(Timer2 + x, (t + 1) & 0xFF);
            int r = (t << 2) & 0xFF;
            m.Wr(0x30, Prg.CpuRd(m, 0xB8E6 + r));
            if (m.Rd(0x30) == 0) { ObjUtil.ObjBecomeNext(m, x); return true; }
            m.Wr(0x31, Prg.CpuRd(m, 0xB8E7 + r));
            m.Wr(Phase + x, Prg.CpuRd(m, 0xB8E9 + r));
            ObjUtil.ObjPosToTemp(m, x);
            int n = m.Rd(0x10);
            m.Wr(MetaId + n, Prg.CpuRd(m, 0xB8E8 + r));
            m.Wr(TileOfs + n, m.Rd(TileOfs + x));
            ObjUtil.TempAddOffset(m);
            ObjUtil.TempToObjPos(m, n);
            return true;
        }
        m.Wr(Timer2 + x, (t + 1) & 0xFF);                         // B896
        int p = Prg.CpuRd(m, 0xB8E9 + ((t << 2) & 0xFF));
        if (p == 0) { ObjUtil.ObjBecomeNext(m, x); return false; }
        m.Wr(Phase + x, p);
        return false;
    }

    // js: game/obj/ai_effect.js aiExplode
    /// <summary>AI $74 ObjAiExplode 27:B83B (Y = $E8): area $5F -> irq_mode 0; area != $1A -> shake $10</summary>
    public static IEnumerable<Y>? AiExplode(Machine m, int x, AiRet r)
    {
        if (m.Rd(0x6C) == 0x5F) m.Wr(0x51, 0);
        if (m.Rd(0x6C) != 0x1A) m.Wr(0x07D6, 0x10);
        ExplodeStep(m, x, 0xE8);
        return null;
    }

    // js: game/obj/ai_effect.js aiScreenRedraw08
    /// <summary>AI $08 AiScreenRedraw08 27:B7F6 (Y = $10): ExplodeStep, then DEC obj_act_timer; below $0A each frame one
    /// BG row (27:B805 with sub $28)</summary>
    public static IEnumerable<Y> AiScreenRedraw08(Machine m, int x, int y = 0x10)
    {
        bool spawned = ExplodeStep(m, x, y);
        int t = (m.Rd(ActTimer + x) - 1) & 0xFF;
        m.Wr(ActTimer + x, t);
        if (t >= 0x0A) yield break;
        foreach (var s in RedrawRowTail(m, x, t, 0x28)) yield return s;
        // s114 lag-frame heuristic: a frame that spawns a shot AND renders a row with 12+ active slots runs past the NMI
        if (spawned && CountActive(m) >= 12) m.Overrun = true;
    }

    // js: game/obj/ai_effect.js redrawRowTail
    /// <summary>27:B805 (A = sub): $1F = sub, MarkScreenVisited, WaitPpuQueue, one BG row at the object's y + (t*8 - sub)
    /// (x = camera $3C/$3D, FarCall_EB91), PpuQueueAttrAll when t = 0. Shared by AI $08 (sub $28) and AI $06 (sub 8).</summary>
    private static IEnumerable<Y> RedrawRowTail(Machine m, int x, int t, int sub)
    {
        m.Wr(0x1F, sub);
        MarkScreenVisited(m, x);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        ObjUtil.ObjPosToTemp(m, x);
        m.Wr(0x31, ((t << 3) - sub) & 0xFF);                     // ASL x3, SEC SBC $1F ($30 as left)
        ObjUtil.TempAddOffset(m);
        m.Wr(0x30, 0xFF); m.Wr(0x31, 0xFF);
        m.Wr(0x34, m.Rd(0x3C)); m.Wr(0x35, m.Rd(0x3D));
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);                     // FarCall_EB91
        foreach (var s in BgStream.BgRowRender(m)) yield return s;
        Prg.FarRestore(m, r6, r7);
        if (t == 0) PpuQueue.QueueAttrAll(m);                     // PpuQueueAttrAll 30:C739
    }

    // js: game/obj/ai_effect.js aiCrumbleRedraw06
    /// <summary>AI $06 AiCrumbleRedraw06 27:B93E (type $D6; Y = $0C): camera lock $10; CrumbleStep; then act_timer--,
    /// below 4 the 27:B805 row redraw with sub = 8</summary>
    public static IEnumerable<Y> AiCrumbleRedraw06(Machine m, int x, int y = 0x0C)
    {
        m.Wr(0x07D7, 0x10);                                       // boss_cam_lock
        CrumbleStep(m, x, y);                                     // B943 JSR B955
        int t = (m.Rd(ActTimer + x) - 1) & 0xFF;
        m.Wr(ActTimer + x, t);
        if (t >= 4) yield break;
        foreach (var s in RedrawRowTail(m, x, t, 8)) yield return s;   // B950 LDA #8, JMP B805
    }

    // js: game/obj/ai_effect.js crumbleStep
    private static void CrumbleStep(Machine m, int x, int y)
    {
        m.Wr(Timer2 + x, (m.Rd(Timer2 + x) - 1) & 0xFF);
        if (ObjUtil.SpawnShot(m, x, 0x3D, y))
        {                                                         // B95A JSR 26:972D, BCC B980
            // timer2 read after the spawn: a shot spawned into this slot has it cleared
            int r = (m.Rd(Timer2 + x) & 0x0F) << 2;
            m.Wr(0x30, Prg.CpuRd(m, 0xB8E6 + r));
            m.Wr(0x31, Prg.CpuRd(m, 0xB8E7 + r));
            ObjUtil.ObjPosToTemp(m, x);
            m.Wr(0x11, x);
            ObjUtil.TempAddOffset(m);                             // LDX $10 (the new slot): 26:9824 / 26:9897
            ObjUtil.TempToObjPos(m, m.Rd(0x10));
        }
        if (m.Rd(Phase + x) == 0)
        {                                                         // B980
            int a = (m.Rd(Anim + x) + 1) & 0xFF;
            m.Wr(Anim + x, a);
            if (a == 0x17) m.Wr(Phase + x, m.Rd(Phase + x) + 1);
            return;
        }
        int b = (m.Rd(Anim + x) - 1) & 0xFF;                      // B993
        m.Wr(Anim + x, b);
        if (b == 0) m.Wr(Active + x, 0);
    }

    // js: game/obj/ai_effect.js aiDeathPuff
    /// <summary>AI $7D DeathPuff 27:B9AA: phase 0 = 16-frame jitter, then the puff record (+ drop SpawnShot($3F));
    /// phase != 0: anim -= 2, borrow -> off</summary>
    public static IEnumerable<Y>? AiDeathPuff(Machine m, int x, AiRet r)
    {
        if (m.Rd(Phase + x) != 0)
        {                                                         // B99E
            int a0 = (m.Rd(Anim + x) - 2) & 0xFF;
            m.Wr(Anim + x, a0);
            if ((a0 & 0x80) != 0) ObjUtil.ObjDespawn(m, x);       // BPL: N of the 2nd DEC; JMP 26:98C7
            return null;
        }
        ObjUtil.ObjPosToTemp(m, x);
        m.Wr(0x30, Prg.CpuRd(m, 0xBA2C + ((m.Rd(Anim + x) >> 1) & 1)));
        m.Wr(0x31, 0);
        ObjUtil.TempAddStep(m);                                   // 26:9861
        ObjUtil.TempToObjPos(m, x);
        int a = (m.Rd(Anim + x) - 2) & 0xFF;
        m.Wr(Anim + x, a);
        if ((a & 0x80) == 0) return null;
        m.Wr(Flags + x, (m.Rd(Flags + x) & 0xDD) | 1);            // B9D1
        m.Wr(Anim + x, 0x3F);
        m.Wr(MetaId + x, 0xAC);
        m.Wr(Phase + x, 0xAC);
        m.Wr(TileOfs + x, 0);
        if ((m.Rd(Drop + x) & 0xF0) == 0) return null;
        if (!ObjUtil.SpawnShot(m, x, 0x3F, 0))
        {                                                         // B9F9, BCS BA14
            m.Wr(Active + x, 0);
            int d0 = m.Rd(Drop + x);
            bool ok = ObjUtil.SpawnShot(m, x, 0x3F, 0);
            m.Wr(Drop + x, d0);
            if (!ok) return null;
        }
        int n = m.Rd(0x10), d = m.Rd(Drop + x);                   // BA14
        m.Wr(Drop + n, d);
        m.Wr(MetaId + n, Prg.CpuRd(m, 0x8BFE + (d >> 4)));
        return null;
    }

    // js: game/obj/ai_effect.js countActive
    private static int CountActive(Machine m)
    {
        int n = 0;
        for (int s = 0; s < 0x1F; s++) if (m.Rd(Active + s) != 0) n++;
        return n;
    }

    // js: game/obj/ai_effect.js markScreenVisited
    /// <summary>MarkScreenVisited 30:C75C: visited_map[y_hi] |= bit[x_hi]; the area's k4 list (flag, screen) pairs ->
    /// screen_flags $64C0 bit for the screen ($12 = y_hi&lt;&lt;4 | x_hi); ends SwitchBank16($0D)</summary>
    public static void MarkScreenVisited(Machine m, int x)
    {
        int xh = m.Rd(0x90 + x), yh = m.Rd(0xD0 + x);
        m.Wr(0x62F0 + yh, m.Rd(0x62F0 + yh) | Prg.CpuRd(m, 0xC000 + xh));
        m.Wr(0x12, ((yh << 4) | m.Rd(0x90 + x)) & 0xFF);
        Camera.AreaSubTable(m, 4);
        int y = 0;
        for (; ; )
        {
            int f = Prg.IndY(m, 0x10, y);
            if (f == 0xFF) break;
            m.Wr(0x13, f);
            int s = Prg.IndY(m, 0x10, y + 1);
            y = (y + 2) & 0xFF;
            if (s != m.Rd(0x12)) continue;
            int bit = Prg.CpuRd(m, 0xC000 + (f & 7));
            m.Wr(0x12, bit);
            m.Wr(0x64C0 + (f >> 3), m.Rd(0x64C0 + (f >> 3)) | bit);
            break;
        }
        Prg.SwitchBank16(m, 0x0D);
    }

    // js: game/obj/ai_effect.js floorRevealTail
    /// <summary>27:A459, AI $5F's reveal tail: off, visited row 0, player pos &lt;- camera, rendering off for a full redraw</summary>
    private static IEnumerable<Y> FloorRevealTail(Machine m, int x)
    {
        m.Wr(Active + x, 0);
        m.Wr(0x62F0, 0xFF);
        for (int i = 0; i < 4; i++) m.Wr(0x34 + i, m.Rd(0x3C + i));
        m.Wr(0x01, m.Rd(0x01) & 0xE7);                            // ppumask_shadow
        foreach (var s in Death.WaitFrame(m)) yield return s;
        foreach (var s in BgStream.FullScreenDraw(m, 1, BgStream.FsdLag)) yield return s;   // 30:C831
        m.Wr(0x01, m.Rd(0x01) | 0x18);
    }

    // js: game/obj/ai_effect.js aiFloorReveal
    /// <summary>AI $5F AiFloorReveal 27:A41A (s118; type $DA in tower areas $59-$5C)</summary>
    public static IEnumerable<Y> AiFloorReveal(Machine m, int x)
    {
        if (m.Rd(Phase + x) == 0)
        {
            m.Wr(Phase + x, (m.Rd(Phase + x) + 1) & 0xFF);
            int area = m.Rd(0x6C);
            int y = (0x5D - area) & 0xFF;
            if (area == 0x5C)
            {
                if (m.Rd(0x6430) == 4) foreach (var s in FloorRevealTail(m, x)) yield return s;
                m.Wr(Active + x, 0);
                yield break;                                      // PLA PLA RTS
            }
            if (m.Rd(0xB0) < 0x10) y = (y - 1) & 0xFF;
            while (y != 4)
            {                                                     // A4B9: INY wrapping to 0 also ends it (BNE)
                m.Wr(0x62F0 + y, 0xFF);
                y = (y + 1) & 0xFF;
                if (y == 0) break;
            }
        }
        int row = m.Rd(0xD0);
        for (int y = 0x0D; y < 0x1F; y++)
        {
            if (m.Rd(0xD0 + y) != row || m.Rd(Active + y) == 0) continue;
            int p = m.Rd(0x0560 + y);
            if (p == 0x51 || p == 0x50) yield break;
        }
        for (int i = 0; i < 6; i++) m.Wr(0x62F0 + i, 0xFF);
        PlaySound.Play(m, 0x5D, 0x1F);
        foreach (var s in FloorRevealTail(m, x)) yield return s;
    }

    /// <summary>(ai_effect.js AI_EFFECT) ObjAiTable ids handled here</summary>
    public static readonly Dictionary<int, AiFn> Ai = new()
    {
        [0x04] = AiPlayerFollowC, [0x05] = AiFollowFacing, [0x08] = (m, x, r) => AiScreenRedraw08(m, x),
        [0x57] = (m, x, r) => { AiStatic.AiStraightMoverTail(m, x); return null; },
        [0x5F] = (m, x, r) => AiFloorReveal(m, x),
        [0x06] = (m, x, r) => AiCrumbleRedraw06(m, x), [0x7D] = AiDeathPuff,   // s169
        [0x74] = AiExplode, [0x76] = (m, x, r) => AiMpDrain(m, x),
        [0x77] = (m, x, r) => AiMpDrain(m, x, 0x99, 0x77),        // s168: AiMpDrain99 27:A4E0 (no object type uses it)
        [0x7C] = (m, x, r) => AiScreenRedraw08(m, x, 0xF8),       // s166: same entry 27:B7F6, Y = $7C*2
    };
}
