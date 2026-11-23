// re-c C# port — the static / projectile enemy AIs (= web/src/game/obj/ai_static.js, s55). REVERSE.md "Enemies —
// catalogue + generic enemy AIs" + "Shots". Handlers take the slot x with banks 26/27 mapped (ObjAiTable 27:A314).
//
//   AI $10 AiStraightMover 27:B0B5   AI $11 AiDirShot 27:B0A8   AI $2C AiBurst 27:ACC0
//   AI $4C/$4D AiFanTurret 27:B595   AI $4E AiTurret 27:B5D9    + AiShootCheck 27:B714 (shared)
//   AI $50 AiS0DTransform 27:A660    AI $7F 27:A419 (RTS)
namespace ReC.Game.Obj;

public static class AiStatic
{
    private const int MetaId = 0x0300, MetaDir = 0x0360, Flags = 0x0380, ShotId = 0x0440, BgMask = 0x0460, Timer = 0x0480;
    private const int Active = 0x04A0, Anim = 0x04E0, Timer2 = 0x0620, ActTimer = 0x0640;

    // js: game/obj/ai_static.js aiShootCheck
    /// <summary>AiShootCheck 27:B714 -> true when it starts an attack ($0640 = $1F) and pops its caller (skip the walk)</summary>
    public static bool AiShootCheck(Machine m, int x)
    {
        if ((m.Rd(Flags + x) & 0x80) != 0) return false;
        if (((((x << 4) & 0xFF) + m.Rd(0x08)) & 0x1F) != 0) return false;
        if (ObjUtil.Rand64(m) != 0) return false;
        ObjUtil.DirToTarget(m, x, 1);
        if (m.Rd(0x21) >= 5) return false;
        m.Wr(ActTimer + x, 0x1F);
        return true;
    }

    // js: game/obj/ai_static.js aiTurret
    /// <summary>AI $4E: aimed shot (16-direction units = dir*2) every 16 obj_anim steps</summary>
    public static IEnumerable<Y>? AiTurret(Machine m, int x, AiRet r)
    {
        m.Wr(Anim + x, m.Rd(Anim + x) - 1);
        if ((m.Rd(Anim + x) & 0x0F) != 0) return null;
        ObjUtil.DirToTarget(m, x, 1);
        ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), (m.Rd(0x20) << 1) & 0xFF);
        return null;
    }

    // js: game/obj/ai_static.js aiFanTurret
    /// <summary>AI $4C/$4D: idle = obj_anim-- + AiShootCheck; attacking = 3-shot fan at $0640 = $18 / $10 / $08.
    /// r.Pop when the idle path's JMP AiShootCheck PLA PLA pops ObjUpdateAll's own return address.</summary>
    public static IEnumerable<Y>? AiFanTurret(Machine m, int x, AiRet r)
    {
        if (m.Rd(ActTimer + x) == 0)
        {                                                         // B5D3
            m.Wr(Anim + x, m.Rd(Anim + x) - 1);
            r.Pop = AiShootCheck(m, x);
            return null;
        }
        m.Wr(ActTimer + x, m.Rd(ActTimer + x) - 1);
        int act = m.Rd(ActTimer + x), anim = (act << 1) & 0xFF;
        m.Wr(Anim + x, anim);
        if ((anim & 0xF0) == 0 || (anim & 0x0F) != 0) return null;
        ObjUtil.DirToTarget(m, x, 1);
        int ofs = Prg.CpuRd(m, 0xB5CF + ((act & 0x18) >> 3));    // 00 FF 00 01: $08 -> -1, $10 -> 0, $18 -> +1
        m.Wr(0x20, m.Rd(0x20) << 1);
        ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), (ofs + m.Rd(0x20)) & 0x0F);
        return null;
    }

    // js: game/obj/ai_static.js aiBurst
    /// <summary>AI $2C: 3 x SpawnShot(obj_shot_id, Rand64) then despawn (counter in $1F)</summary>
    public static IEnumerable<Y>? AiBurst(Machine m, int x, AiRet r)
    {
        m.Wr(0x1F, 3);
        do
        {
            int d = ObjUtil.Rand64(m);
            ObjUtil.SpawnShot(m, x, m.Rd(ShotId + x), d);
            m.Wr(0x1F, m.Rd(0x1F) - 1);
        } while (m.Rd(0x1F) != 0);
        m.Wr(Active + x, 0);
        return null;
    }

    // js: game/obj/ai_static.js aiDirShot
    /// <summary>AI $11: meta |= dir>>1 (directional sprite), obj_active-- (-> $10), then AI $10 this frame</summary>
    public static IEnumerable<Y>? AiDirShot(Machine m, int x, AiRet r)
    {
        m.Wr(MetaId + x, (m.Rd(MetaDir + x) >> 1) | m.Rd(MetaId + x));
        m.Wr(Active + x, m.Rd(Active + x) - 1);
        return AiStraightMover(m, x, r);
    }

    // js: game/obj/ai_static.js aiStraightMover
    /// <summary>AI $10: obj_anim -= 4; offscreen -> despawn; lifetime obj_timer -> death type; step; optional BG test</summary>
    public static IEnumerable<Y>? AiStraightMover(Machine m, int x, AiRet r)
    {
        m.Wr(Anim + x, m.Rd(Anim + x) - 4);
        AiStraightMoverTail(m, x);
        return null;
    }

    // js: game/obj/ai_static.js aiStraightMoverTail
    /// <summary>AiStraightMoverTail 27:B0BE (also the tail of AIs $16 $17 $1E $1F, obj/ai_shot.js; AI $57)</summary>
    public static void AiStraightMoverTail(Machine m, int x)
    {
        if ((m.Rd(Flags + x) & 0x80) != 0) { ObjUtil.ObjDespawn(m, x); return; }
        m.Wr(Timer + x, m.Rd(Timer + x) - 1);
        if (m.Rd(Timer + x) == 0) { ObjUtil.ObjBecomeNext(m, x); return; }
        ObjUtil.ObjPosToTemp(m, x);
        ObjUtil.ObjStepVector(m, x, m.Rd(MetaDir + x));
        ObjUtil.TempAddStep(m);
        if (m.Rd(BgMask + x) == 0) { ObjUtil.TempToObjPos(m, x); return; }
        ObjUtil.ObjBgProbe(m, x, 0xC0);
        if ((m.Rd(0x20) & m.Rd(BgMask + x) & 6) != 0) { ObjUtil.ObjBecomeNext(m, x); return; }   // B0E7: wall -> death type
        ObjUtil.TempToObjPos(m, x);
    }

    // js: game/obj/ai_static.js aiS0DTransform
    /// <summary>AI $50 AiS0DTransform 27:A660 (s112): obj_timer2 steps 0 (ObjInit slot $0D), 1 (countdown, then
    /// sub_state $11 talk), >= 2 (walk off, blocked -> x_hi = $FF)</summary>
    public static IEnumerable<Y>? AiS0DTransform(Machine m, int x, AiRet r)
    {
        int t2 = m.Rd(Timer2 + x);
        if (t2 == 0)
        {
            m.Wr(0x11, m.Rd(0x04CD));
            m.Wr(0x10, 0x0D);
            ObjInit.FarObjInit(m);
            m.Wr(ActTimer + x, 0x70);
            m.Wr(Timer2 + x, m.Rd(Timer2 + x) + 1);
            return null;
        }
        if (t2 == 1)
        {
            int v = (m.Rd(ActTimer + x) - 1) & 0xFF;
            m.Wr(ActTimer + x, v);
            if (v != 0) { m.Wr(0x05A0, 5); return null; }
            m.Wr(MetaId + x, 0xC0);
            m.Wr(Timer2 + x, m.Rd(Timer2 + x) + 1);
            m.Wr(0x41, 0x11);                                     // sub_state
            m.Wr(0x0623, x);
            m.Wr(ActTimer + x, 0x10);
            return null;
        }
        if ((m.Rd(Flags + x) & 0x80) != 0) return null;
        m.Wr(MetaId + x, 0x3B);
        m.Wr(Anim + x, (m.Rd(Anim + x) - 2) & 0xFF);
        m.Wr(Timer + x, (m.Rd(Timer + x) - 1) & 0xFF);
        m.Wr(MetaDir + x, 6);
        m.Wr(0x0340 + x, 7);                                      // obj_flags2
        if (Move.ObjMoveSlide(m, x)) m.Wr(0x90 + x, 0xFF);
        return null;
    }

    /// <summary>(ai_static.js AI_HANDLERS) ObjAiTable ids handled here</summary>
    public static readonly Dictionary<int, AiFn> Ai = new()
    {
        [0x10] = AiStraightMover, [0x11] = AiDirShot, [0x2C] = AiBurst, [0x4C] = AiFanTurret, [0x4D] = AiFanTurret,
        [0x4E] = AiTurret, [0x50] = AiS0DTransform,
        [0x7F] = (m, x, r) => null,                               // 27:A419 RTS (dormant boss, s112)
    };
}
