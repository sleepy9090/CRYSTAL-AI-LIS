// re-c C# port — the per-frame object loop (= web/src/game/obj/objects.js, s80). REVERSE.md "Object update + boss defeat"
// and "Per-frame order inside ObjUpdateAll" (session 30).
//
//   ObjUpdateAll(m)   ObjUpdateAll 30:CCCC (iterator: AI $1A's sequence 30:CD8A, obj/ai_thunder.js, and generator AIs
//                     wait on frames). Slots 0-30, each with SwitchBank16($0D) first: knockback, obj_freeze, then
//                     JSR $0010 = JMP ObjAiTable 27:A314[obj_active & $7F] (ids $6x run with SetR7($0F)).
//   HitSetFlag(m)     HitSetFlag 26:9011: param $13 in HitFlagParams 26:9045 -> story flag HitFlagIds 26:904F[i]
//   AiTable           id -> AiFn; ids not in it log "AI $xx not ported yet" once and do nothing.
//
// AI handler convention (C# s3): `AiFn(m, x, r)` returns null (plain JS function) or its iterator (JS generator);
// the JS return value goes into `r`: `true` (the handler popped ObjUpdateAll's return address) = r.Pop, a number
// (the X register it leaves) = r.X. Each AI module has a `static Dictionary<int, AiFn> Ai` (JS AI_xxx); AiTable merges
// them in the JS spread order (later modules override earlier ids).
namespace ReC.Game.Obj;

/// <summary>An AI handler's JS return value: true -> Pop, a number -> X (null = undefined)</summary>
public sealed class AiRet
{
    public bool Pop;
    public int? X;
}

/// <summary>ObjAiTable handler for slot x (null result = a plain function that did its work)</summary>
public delegate IEnumerable<Y>? AiFn(Machine m, int x, AiRet r);

public static class Objects
{
    private const int Flags2 = 0x0340, MetaDir = 0x0360, Timer = 0x0480, Active = 0x04A0, SpawnB2 = 0x0540, SpawnParam = 0x0560;
    private const int Freeze = 0x05A0;

    /// <summary>(objects.js AI_TABLE) = { ...AI_HANDLERS, ...AI_WALK, ...AI_FIELD, ...AI_SHOT, ...AI_EFFECT, ...AI_FINAL,
    /// ...AI_BOSS, ...AI_PLAYER }. C# s3: only the ported modules are merged; add each module's Ai here in that order.</summary>
    public static readonly Dictionary<int, AiFn> AiTable = Merge(AiStatic.Ai, AiShot.Ai, AiEffect.Ai, Player.Ai);

    private static Dictionary<int, AiFn> Merge(params Dictionary<int, AiFn>[] parts)
    {
        var d = new Dictionary<int, AiFn>();
        foreach (var p in parts) foreach (var kv in p) d[kv.Key] = kv.Value;
        return d;
    }

    // js: game/obj/objects.js hitSetFlag
    public static void HitSetFlag(Machine m)
    {
        int y = 0;
        for (; ; )
        {
            int p = Prg.CpuRd(m, 0x9045 + y);
            if (p == 0) return;
            if (p == m.Rd(0x13)) break;
            y = (y + 1) & 0xFF;
        }
        int id = Prg.CpuRd(m, 0x904F + y), bit = Prg.CpuRd(m, 0xC000 + (id & 7));
        m.Wr(0x11, bit ^ 0xFF);
        m.Wr(0x10, bit & m.Rd(0x12));
        int a = 0x6480 + (id >> 3);
        m.Wr(a, (m.Rd(a) & m.Rd(0x11)) | m.Rd(0x10));
    }

    // js: game/obj/objects.js unported
    private static void Unported(Machine m, string what)
    {
        if (m.Unported.Add(what)) m.Log($"{what} not ported yet");
    }

    // js: game/obj/objects.js knockback
    /// <summary>30:CD31 knockback; returns true when the slot's AI is skipped this frame</summary>
    private static bool Knockback(Machine m, int x)
    {
        int f = (m.Rd(Flags2 + x) + 0x10) & 0xFF;
        m.Wr(Flags2 + x, f);
        if ((f & 0xF0) != 0)
        {
            if (x == 1) return false;                             // CD42: the player handles its own knockback
            if ((m.Rd(Active + x) & 0x80) == 0)
            {
                int sf = m.Rd(Flags2 + x), sd = m.Rd(MetaDir + x);
                m.Wr(MetaDir + x, sd >> 4);
                m.Wr(Flags2 + x, 8);
                Prg.SetR6(m, 0x1A);
                Move.ObjMoveSlide(m, x);
                m.Wr(Timer + x, m.Rd(Timer + x) - 1);
                m.Wr(MetaDir + x, sd);
                m.Wr(Flags2 + x, sf);
                return true;
            }
        }
        m.Wr(MetaDir + x, m.Rd(MetaDir + x) & 0x0F);              // CD74: counter wrapped (or b7 object)
        return false;
    }

    // js: game/obj/objects.js objUpdateAll
    public static IEnumerable<Y> ObjUpdateAll(Machine m)
    {
        m.Wr(0x04BF, 0);                                          // CCCE STX obj_active slot $1F (X = 0)
        int rewinds = 0;
        var r = new AiRet();
        for (int x = 0; x < 0x1F; x++)
        {
            Prg.SwitchBank16(m, 0x0D);                            // CCD1
            if ((m.Rd(Flags2 + x) & 0xF0) != 0 && Knockback(m, x)) continue;
            if (m.Rd(Freeze + x) != 0)
            {                                                     // CCDD
                m.Wr(Freeze + x, m.Rd(Freeze + x) - 1);
                if (m.Rd(Freeze + x) != 0) continue;
                if (m.Rd(SpawnB2 + x) == 1)
                {
                    m.Wr(0x12, 0);
                    m.Wr(0x13, m.Rd(SpawnParam + x));
                    HitSetFlag(m);
                }
            }
            int act = m.Rd(Active + x), id = act & 0x7F;          // CCFA
            // s212: per-dispatch bucket (= tools.emu.pass_cost --slots spans, CCFA to CCFA)
            m.PassStage = act != 0 ? $"s{x}:{id}" : "upd";
            if (id == 0x1A)
            {                                                     // CD03 JMP ObjUpdateAi1A 30:CD7F
                foreach (var s in AiThunder.ThunderStep(m, x)) yield return s;   // CD7F JSR 30:CD8A (s197)
                Prg.SwitchBank16(m, 0x0D);                        // CD82
                continue;
            }
            m.Wr(0x10, 0x4C);
            m.Wr(0x11, Prg.CpuRd(m, 0xA314 + id * 2));
            m.Wr(0x12, Prg.CpuRd(m, 0xA315 + id * 2));
            if ((m.Rd(0x11) | m.Rd(0x12)) == 0) continue;
            if ((act & 0x70) == 0x60) Prg.SetR7(m, 0x0F);
            if (!AiTable.TryGetValue(id, out var h)) { Unported(m, $"AI ${id:X2}"); continue; }
            r.Pop = false; r.X = null;
            var g = h(m, x, r);
            if (g != null) foreach (var s in g) yield return s;   // s93: generator AIs (HudRedraw's queue spin)
            if (r.Pop) yield break;                               // popped ObjUpdateAll's return address
            if (r.X is int nx)
            {                                                     // s93: the AI left X changed (AI $04: X = $0C)
                // s165: X below the slot re-runs the slots after X in this pass (final boss setup 15:B301 returns X = 0)
                if (nx < x && ++rewinds > 0x40)
                {                                                 // ROM: an AI that always does it loops forever
                    Unported(m, $"AI at slot ${x:x} leaving X = ${nx:x} (the ROM loops forever)");
                    yield break;
                }
                x = nx;
            }
        }
    }
}
