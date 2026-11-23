// re-c C# port — object-vs-object collision (= web/src/game/obj/collide.js, s113). REVERSE.md "Object-vs-object collision
// (session 30)", "Player damage — HitPlayer", "Hit handlers (s78)". Zero-page temps included.
//
//   ObjCollideAll(m)      ObjCollideAll 26:8F6E (iterator: hit handlers reach HudRedraw / HudHpBar / StatRecalc).
//                         CollPairTable 26:96F1 records 7..0 (even frames) or 14..8 (odd), each = ObjOverlapScan + the
//                         HitHandlerTable 26:8FBB handler on the first overlapping target ($2E/$2F loop counters).
//   ObjOverlapScan(m, x)  ObjOverlapScan 26:95F8 -> target slot or -1 (C = 0); scans $10+1 .. $11-1
//   handlers              0 HitByPlayerShot 26:90AA, 2 HitPlayer 26:9357, 4 TalkProbeHit 26:94B0, 6 HitHandler3 26:8FC4
// s213 / s216 (lag model): m.Cyc += the exact ROM cycles of the record loop, ObjOverlapScan, EnemyKilled and
// PickupGold (see collide.js for the breakdowns).
using ReC.Game.States;

namespace ReC.Game.Obj;

public static class Collide
{
    private const int Meta = 0x0300, Flags2 = 0x0340, MetaDir = 0x0360, Flags = 0x0380, Coll = 0x03A0, Hp = 0x03C0;
    private const int Attack = 0x03E0, Defense = 0x0400, HitFlags = 0x0420, Active = 0x04A0, DeathT = 0x04C0, Anim = 0x04E0;
    private const int Elem = 0x0500, XpVal = 0x0520, SpawnB2 = 0x0540, SpawnParam = 0x0560, Overlay = 0x0580;
    private const int Freeze = 0x05A0, ScrX = 0x05C0, ScrY = 0x05E0, Phase = 0x0600, Behav = 0x06E0;
    private const int FlashA = 0x0710, Level = 0x0421, Xp = 0x0704, XpNext = 0x0706, SubState = 0x41;
    private const int MsgId = 0x06C3, MsgGroup = 0x06E3, RideA = 0x0541, ProbeHit = 0x0643, ProbeNpc = 0x0623;

    // js: game/obj/collide.js rd
    private static int Rd(Machine m, int a) => m.Rd(a & 0xFFFF);
    // js: game/obj/collide.js box
    private static int Box(int i) => 0x9691 + i;                  // HitboxTable {dx w dy h}
    // js: game/obj/collide.js pgx
    /// <summary>HitboxTable byte k of box i crosses $9700</summary>
    private static int Pgx(int i, int k) => i + k >= 0x6F ? 1 : 0;

    // js: game/obj/collide.js objOverlapScan
    /// <summary>ObjOverlapScan 26:95F8: attacker x vs targets $10+1 .. $11-1 -> first overlapping target or -1</summary>
    public static int ObjOverlapScan(Machine m, int x)
    {
        m.Wr(0x1B, x);
        if ((m.Rd(Flags + x) & 0x80) != 0) { m.Cyc += 21; return -1; }
        if ((m.Rd(Flags2 + x) & 0x80) != 0) { m.Cyc += 27; return -1; }
        int grp = m.Rd(Coll + x) & 0xF0;
        if (grp == 0) { m.Cyc += 35; return -1; }
        m.Wr(0x1F, grp);
        m.Wr(0x15, (m.Rd(Coll + x) & 0x0F) << 2);
        int i = (m.Rd(HitFlags + x) & 0x40) | m.Rd(0x15);
        m.Cyc += 97 + Pgx(i, 0) + Pgx(i, 1) + Pgx(i, 2) + Pgx(i, 3);
        m.Wr(0x15, (m.Rd(ScrY + x) + Prg.CpuRd(m, Box(i + 2))) & 0xFF);
        m.Wr(0x16, (m.Rd(0x15) + Prg.CpuRd(m, Box(i + 3))) & 0xFF);
        m.Wr(0x13, (m.Rd(ScrX + x) + Prg.CpuRd(m, Box(i))) & 0xFF);
        m.Wr(0x14, (m.Rd(0x13) + Prg.CpuRd(m, Box(i + 1))) & 0xFF);
        for (int y = (m.Rd(0x10) + 1) & 0xFF; y != m.Rd(0x11); y = (y + 1) & 0xFF)
        {
            if ((Rd(m, Flags + y) & 0x80) != 0) { m.Cyc += 14; continue; }
            if ((Rd(m, Coll + y) & m.Rd(0x1F)) == 0) { m.Cyc += 23; continue; }
            if ((Rd(m, Flags2 + y) & 0x80) != 0) { m.Cyc += 29; continue; }
            if (y == m.Rd(0x1B)) { m.Cyc += 34; continue; }
            m.Wr(0x10, (Rd(m, Coll + y) & 0x0F) << 2);
            int j = (Rd(m, HitFlags + y) & 0x40) | m.Rd(0x10);
            int a = Rd(m, ScrX + y) + Prg.CpuRd(m, Box(j));
            if ((a & 0xFF) >= m.Rd(0x14)) { m.Cyc += 73 + Pgx(j, 0); continue; }   // BCS (the ADC carry is dropped)
            a = ((a & 0xFF) + Prg.CpuRd(m, Box(j + 1))) & 0xFF;   // C = 0 after the BCS not taken
            if (a < m.Rd(0x13)) { m.Cyc += 82 + Pgx(j, 0) + Pgx(j, 1); continue; }
            a = (Rd(m, ScrY + y) + Prg.CpuRd(m, Box(j + 2))) & 0xFF;
            if (a >= m.Rd(0x16)) { m.Cyc += 97 + Pgx(j, 0) + Pgx(j, 1) + Pgx(j, 2); continue; }
            a = (a + Prg.CpuRd(m, Box(j + 3))) & 0xFF;
            int cx = Pgx(j, 0) + Pgx(j, 1) + Pgx(j, 2) + Pgx(j, 3);
            if (a < m.Rd(0x15)) { m.Cyc += 106 + cx; continue; }
            m.Cyc += 116 + cx;
            return y;
        }
        m.Cyc += 20;
        return -1;
    }

    // js: game/obj/collide.js hitKnockbackSet
    /// <summary>HitKnockbackSet 26:95C0: attacker $10, target $11 (saves X/Y in $12/$13)</summary>
    private static void HitKnockbackSet(Machine m, int x, int y)
    {
        m.Wr(0x12, x); m.Wr(0x13, y);
        int ax = m.Rd(0x10), ty = m.Rd(0x11);
        int f = Rd(m, Flags2 + ty);
        if ((f & 0x80) == 0) m.Wr(Flags2 + ty, (f & 0x0F) | 0x80);
        int d = Rd(m, MetaDir + ax);
        if (Rd(m, Flags2 + ax) >= 0x0B) d >>= 1;
        m.Wr(0x10, (d << 4) & 0xFF);
        m.Wr(MetaDir + ty, (Rd(m, MetaDir + ty) & 0x0F) | m.Rd(0x10));
    }

    // js: game/obj/collide.js isEnemyShot
    private static bool IsEnemyShot(Machine m, int y) => m.Rd(SpawnB2 + y) == 0xFF;   // IsEnemyShot 26:907C

    // js: game/obj/collide.js hitShotEater
    /// <summary>HitShotEater 26:906C</summary>
    private static void HitShotEater(Machine m, int y)
    {
        if (!IsEnemyShot(m, y)) return;
        m.Wr(Active + y, 0);
        PlaySound.Play(m, 0x3A, y);
    }

    // js: game/obj/collide.js hitHandler3
    /// <summary>HitHandler3 26:8FC4 (attacker slot $0C)</summary>
    private static IEnumerable<Y> HitHandler3(Machine m, int x, int y)
    {
        if (m.Rd(Phase + x) == 1) { HitShotEater(m, y); yield break; }
        if (IsEnemyShot(m, y)) yield break;
        if (m.Rd(Active + y) == 0x78) yield break;
        m.Wr(Active + x, 0);
        if ((m.Rd(HitFlags + y) & 0x20) != 0) yield break;
        m.Wr(0x13, m.Rd(SpawnParam + y));
        m.Wr(0x12, 0xFF);
        m.Wr(Freeze + y, 0xFF);
        m.Wr(0x10, y);
        if (m.Rd(SpawnB2 + y) != 1) yield break;
        for (int i = 0x13; i >= 0; i--)
        {
            if (Prg.CpuRd(m, 0x9058 + i) == m.Rd(0x13)) { m.Wr(Freeze + m.Rd(0x10), 0); yield break; }   // HitImmuneParams
        }
        Objects.HitSetFlag(m);
    }

    // js: game/obj/collide.js thunderTransform
    /// <summary>ThunderTransform 26:9229: true = ObjInit(target, obj_behav) and PLA PLA (the hit handler ends)</summary>
    private static bool ThunderTransform(Machine m, int y)
    {
        if (m.Rd(0x0711) != 4 || m.Rd(SpawnB2 + y) != 0) return false;
        int p = m.Rd(SpawnParam + y);
        if (p != 0 && p != 3) return false;
        m.Wr(0x11, m.Rd(Behav + y));
        m.Wr(0x10, y);
        ObjInit.FarObjInit(m);
        return true;
    }

    // js: game/obj/collide.js hitByPlayerShot
    /// <summary>HitByPlayerShot 26:90AA (HitHandlerTable 0: player shot / sword slot x hits y)</summary>
    private static IEnumerable<Y> HitByPlayerShot(Machine m, int x, int y)
    {
        if (m.Rd(0x6C) == 0x5F) { m.Wr(0x04A4, 0); m.Wr(0x04A5, 0); m.Wr(0x04A6, 0); }
        if (ThunderTransform(m, y)) yield break;
        int act = m.Rd(Active + y);
        if (act == 0x52)
        {                                                         // 90C5: the sparring duel partner
            int v = (m.Rd(0x066D) - 1) & 0xFF;
            m.Wr(0x066D, v);
            if ((v & 3) == 0)
            {
                m.Wr(0xB0, (m.Rd(0xB0) - 8) & 0xFF);
                m.Wr(0xB0 + y, (m.Rd(0xB0 + y) - 8) & 0xFF);
            }
            PlaySound.Play(m, 0x21, y);
            yield break;
        }
        if (act == 0x10) yield break;
        if (act == 0x87)
        {                                                         // HitElemWall 26:9094
            if (((m.Rd(Elem + y) & m.Rd(Elem + x)) & 0x0F) != 0) yield break;
            if (m.Rd(HitFlags + x) == 1) yield break;
            foreach (var s in EnemyKilled(m, x, y)) yield return s;
            yield break;
        }
        if (m.Rd(Meta + y) == 0xA7)
        {                                                         // HitRevealA7 26:9082
            if (m.Rd(HitFlags + x) != 3) yield break;
            m.Wr(0x11, m.Rd(DeathT + y));
            m.Wr(0x10, y);
            ObjInit.FarObjInit(m);
            yield break;
        }
        m.Wr(0x10, m.Rd(HitFlags + y) & 0x1F);
        bool miss = m.Rd(Level) < m.Rd(0x10);
        if (!miss)
        {
            m.Wr(0x10, (m.Rd(0x03E1) + m.Rd(Attack + x)) & 0xFF);
            if (((m.Rd(Elem + y) & m.Rd(Elem + x)) & 0x0F) != 0) miss = true;
            else
            {
                int d = m.Rd(0x10) - m.Rd(Defense + y);
                m.Wr(0x10, d & 0xFF);
                miss = d < 0;
            }
        }
        if (miss)
        {                                                         // 9125
            m.Wr(Active + x, 0);
            m.Wr(0x10, x); m.Wr(0x11, y);
            if ((m.Rd(Active + y) & 0x80) == 0) HitKnockbackSet(m, x, y);
            PlaySound.Play(m, 0x3A, y);
            yield break;
        }
        int hp = m.Rd(Hp + y) - m.Rd(0x10);                      // 913B
        m.Wr(Hp + y, hp & 0xFF);
        if (hp < 0) { foreach (var s in EnemyKilled(m, x, y)) yield return s; yield break; }
        m.Wr(0x10, x); m.Wr(0x11, y);
        HitKnockbackSet(m, x, y);
        PlaySound.Play(m, 0x21, y);
    }

    // js: game/obj/collide.js playSoundSfxCycles
    /// <summary>s216: PlaySound 30:C125 cycles for an SFX id $20-$9F (queue full 70 / not 60), without the caller's JSR</summary>
    private static int PlaySoundSfxCycles(Machine m) => m.Rd(0x0100) == 5 ? 70 : 60;

    // js: game/obj/collide.js enemyKilled
    /// <summary>EnemyKilled 26:9152 (X = attacker, Y = the killed object); s216: passStage 'col' with exact cycles,
    /// ObjInit / level-up as 'colh' (fitted).</summary>
    private static IEnumerable<Y> EnemyKilled(Machine m, int x, int y)
    {
        string stage = m.PassStage;
        m.PassStage = "col";
        int v = m.Rd(XpVal + y);
        m.Cyc += 6 + ((v & 0x80) != 0 ? 66 : (m.Rd(Xp) + v > 0xFF ? 30 : 25));
        AddXp(m, y);
        if (m.Rd(DeathT + y) == 0) { m.Cyc += 86; EnemyDeathFx(m, y); }
        else
        {
            m.Cyc += 23;
            m.Wr(0x10, y);
            m.Wr(0x11, m.Rd(DeathT + y));
            m.PassStage = "colh";
            ObjInit.FarObjInit(m);
            m.PassStage = "col";
        }
        int xp = m.Rd(Xp) | m.Rd(Xp + 1) << 8, next = m.Rd(XpNext) | m.Rd(XpNext + 1) << 8;
        m.Cyc += xp >= next ? 20 + 16 : 21;
        if (xp >= next)
        {
            m.Wr(Xp, 0); m.Wr(Xp + 1, 0);
            if ((m.Rd(Level) & 0xF0) != 0) m.Cyc += 3;
            else
            {                                                     // 9188 level up
                m.Cyc += 2;
                m.PassStage = "colh";
                int lv = (m.Rd(Level) + 1) & 0xFF;
                m.Wr(Level, lv);
                m.Wr(0x03C0, Prg.CpuRd(m, 0x8B7F + lv));
                m.Wr(0x0709, Prg.CpuRd(m, 0x8B8F + lv));
                m.Wr(0x03C1, (Prg.CpuRd(m, 0x8B7F + lv) - Prg.CpuRd(m, 0x8B7E + lv) + m.Rd(0x03C1)) & 0xFF);
                m.Wr(0x0708, (Prg.CpuRd(m, 0x8B8F + lv) - Prg.CpuRd(m, 0x8B8E + lv) + m.Rd(0x0708)) & 0xFF);
                int yy = (lv << 1) & 0xFF;
                m.Wr(XpNext, Prg.CpuRd(m, 0x8B9E + yy));
                m.Wr(XpNext + 1, Prg.CpuRd(m, 0x8B9F + yy));
                foreach (var s in Death.HudHpBar(m)) yield return s;
                foreach (int e in new[] { 0, 3, 4, 5 }) foreach (var s in Death.HudRedraw(m, e, x)) yield return s;
                m.Wr(SubState, 0x10);
                m.Wr(MsgId, 0x0D);
                m.Wr(MsgGroup, 0x20);
                foreach (var s in Death.StatRecalc(m, x)) yield return s;
                m.PassStage = "col";
            }
        }
        m.Cyc += 8 + Death.HudRedrawCycles(m, 2);
        var ry = new Death.YOut();
        foreach (var s in Death.HudRedraw(m, 2, x, ry)) yield return s;   // 91EF
        m.Cyc += 8 + PlaySoundSfxCycles(m) + 6;
        PlaySound.Play(m, 0x21, ry.Y);                            // Y = HudRedraw's $18 (2*6) or the flush's Y
        m.PassStage = stage;                                      // back in the caller (HitByPlayerShot / HitElemWall)
    }

    // js: game/obj/collide.js enemyDeathFx
    /// <summary>EnemyDeathFx 26:91FA: the object becomes the AI $7D death puff</summary>
    private static void EnemyDeathFx(Machine m, int y)
    {
        m.Wr(Overlay + y, 0xAB);
        m.Wr(Flags + y, (m.Rd(Flags + y) | 0x02) & 0xEF);
        m.Wr(Flags2 + y, 0x08);
        m.Wr(Active + y, 0x7D);
        m.Wr(Anim + y, 0x1F);
        m.Wr(Phase + y, 0); m.Wr(Coll + y, 0); m.Wr(Freeze + y, 0);
        m.Wr(DeathT + y, 0x0B);
    }

    // js: game/obj/collide.js addXp
    /// <summary>AddXp 26:924B</summary>
    private static void AddXp(Machine m, int y)
    {
        int v = m.Rd(XpVal + y);
        int add;
        if ((v & 0x80) == 0) add = v;
        else
        {
            m.Wr(0x11, (v >> 4) & 7);
            add = (v & 0x7F) << 4;
        }
        int s = (m.Rd(Xp) | m.Rd(Xp + 1) << 8) + add;
        m.Wr(Xp, s & 0xFF);
        m.Wr(Xp + 1, (s >> 8) & 0xFF);
    }

    // js: game/obj/collide.js playerInvuln
    /// <summary>PlayerInvuln 26:9431</summary>
    private static void PlayerInvuln(Machine m)
    {
        m.Wr(0x071A, 0x1E);
        m.Wr(0x0381, m.Rd(0x0381) | 0x04);
    }

    // js: game/obj/collide.js playerPoisonRoll
    /// <summary>PlayerPoisonRoll 26:927E</summary>
    private static void PlayerPoisonRoll(Machine m, int x, int y)
    {
        if ((m.Rd(Flags + x) & 0x04) != 0) return;
        if (m.Rd(0x0713) == 7) return;
        if ((m.Rd(HitFlags + y) & 0x80) == 0) return;
        m.Wr(0x10, y);
        if (ObjUtil.Rand64(m) >= 4) return;
        m.Wr(FlashA, (m.Rd(FlashA) & 0xF0) | 0x03);
        m.Wr(SubState, 0x10);
        m.Wr(MsgId, 0x19);
        m.Wr(MsgGroup, 0x20);
        PlayerInvuln(m);
        PlaySound.Play(m, 0x36, y);
    }

    // js: game/obj/collide.js hitPlayerShot
    /// <summary>HitPlayerShot 26:92BF; res.V = true to go on with HitPlayer at 9361 (param 0)</summary>
    private static IEnumerable<Y> HitPlayerShot(Machine m, int x, int y, Ret<bool> res)
    {
        res.V = false;
        int p = m.Rd(SpawnParam + y);
        if (p == 0) { res.V = true; yield break; }
        int despawnSlot = y;                                      // 92F5
        // js: game/obj/collide.js despawn
        void Despawn() => m.Wr(Active + despawnSlot, 0);
        if (p == 1)
        {
            int sh = m.Rd(0x0714);
            if (sh != 6 && sh != 8)
            {
                PlaySound.Play(m, 0x44, y);
                m.Wr(SubState, 0x10);
                m.Wr(MsgId, 0x1A);
                m.Wr(MsgGroup, 0x20);
                m.Wr(FlashA, (m.Rd(FlashA) & 0xF0) | 0x01);
            }
            Despawn();
            yield break;
        }
        if (p == 2)
        {
            int sh = m.Rd(0x0714);
            if ((m.Rd(FlashA) & 0x0F) != 2 && sh != 4 && sh != 8)
            {
                m.Wr(SubState, 0x10);
                m.Wr(MsgId, 0x1B);
                m.Wr(MsgGroup, 0x20);
                PlaySound.Play(m, 0x45, y);
                m.Wr(0x05A0, 0xFF);
                m.Wr(0x07E4, 0x4D);
                Death.PaletteBuildRom(m, 0);                      // 26:8C0E
                // ROM quirk: PaletteBuild leaves Y = 4, so 92F5 clears slot 4's obj_active, not the stone shot's
                despawnSlot = 4;
                m.Wr(FlashA, (m.Rd(FlashA) & 0xF0) | 0x02);
            }
            Despawn();
            yield break;
        }
        if (p == 3)
        {
            m.Wr(0x10, y);
            m.Wr(0x11, 0x0B);
            ObjInit.FarObjInit(m);
            yield break;
        }
        Despawn();
        m.Wr(FlashA, 4);
        foreach (var s in Death.StatRecalc(m, x)) yield return s;
    }

    // js: game/obj/collide.js hitPlayer
    /// <summary>HitPlayer 26:9357 (HitHandlerTable 2: X = 1 the player, Y = the touching object)</summary>
    private static IEnumerable<Y> HitPlayer(Machine m, int x, int y)
    {
        if (IsEnemyShot(m, y))
        {
            var go = new Ret<bool>();
            foreach (var s in HitPlayerShot(m, x, y, go)) yield return s;
            if (!go.V) yield break;
        }
        PlayerPoisonRoll(m, x, y);                                // 9361
        if (m.Rd(Active) == 0x0B)
        {                                                         // the sparring duel
            int v = (m.Rd(0x0661) - 1) & 0xFF;
            m.Wr(0x0661, v);
            if ((v & 3) == 0)
            {
                m.Wr(0xB0, (m.Rd(0xB0) + 8) & 0xFF);
                m.Wr(0xBD, (m.Rd(0xBD) + 8) & 0xFF);
            }
            PlaySound.Play(m, 0x5F, y);
            yield break;
        }
        int id = m.Rd(Active + y) & 0x7F;
        if (id == 0x06 || id == 0x07) yield break;
        if (id == 0x7B) { foreach (var s in PickupGold(m, y)) yield return s; yield break; }
        if (id == 0x38 || id == 0x3C) { RidePlatform(m, y); yield break; }
        if ((m.Rd(0x0381) & 0x04) != 0) yield break;              // invulnerable
        m.Wr(0x11, x); m.Wr(0x10, y);
        HitKnockbackSet(m, x, y);
        if (m.Rd(0x6C) == 0x1A && (m.Rd(0x07D7) & 0x80) != 0) m.Wr(MetaDir + x, m.Rd(MetaDir + x) & 0x0F);
        m.Wr(MetaDir, m.Rd(MetaDir + x));
        m.Wr(Flags2, m.Rd(Flags2 + x));
        m.Wr(0x0640, 0xFF);
        m.Wr(0x10, m.Rd(Defense + x));
        if (IsEnemyShot(m, y)) m.Wr(0x10, m.Rd(Defense));
        int dmg = (((m.Rd(0x10) ^ 0xFF) + 1) + m.Rd(Attack + y)) & 0xFF;
        if ((dmg & 0x80) != 0) PlaySound.Play(m, 0x3A, y);
        else
        {
            m.Wr(0x10, dmg);
            if (m.Rd(Level) != 0x11)
            {
                int hp = m.Rd(Hp + x) - dmg;
                m.Wr(Hp + x, hp < 0 ? 0 : hp);
                PlaySound.Play(m, 0x5F, y);
                PlayerInvuln(m);
            }
        }
        if (IsEnemyShot(m, y)) m.Wr(Active + y, 0);               // 9422
        foreach (var s in Death.HudHpBar(m)) yield return s;
    }

    // js: game/obj/collide.js ridePlatform
    /// <summary>RidePlatform 26:943F</summary>
    private static void RidePlatform(Machine m, int y)
    {
        if (m.Rd(0x0620) != 0) return;
        m.Wr(RideA, y);
        if (m.Rd(Freeze + y) != 0) return;
        int d = m.Rd(MetaDir), f = m.Rd(Flags2);
        m.Wr(Flags2, 2);
        m.Wr(MetaDir, m.Rd(MetaDir + y));
        Move.ObjMoveSlide(m, 0);
        m.Wr(Flags2, f);
        m.Wr(MetaDir, d);
    }

    // js: game/obj/collide.js pickupGold
    /// <summary>PickupGold 26:946D (s216: passStage 'col' with exact ROM cycles from HitPlayer's JMP on)</summary>
    private static IEnumerable<Y> PickupGold(Machine m, int y)
    {
        string stage = m.PassStage;
        m.PassStage = "col";
        int x = (m.Rd(Elem + y) >> 4) << 1;
        int g = (m.Rd(0x0702) | m.Rd(0x0703) << 8) + (Prg.CpuRd(m, 0x8BDE + x) | Prg.CpuRd(m, 0x8BDF + x) << 8);
        m.Cyc += 3 + (g > 0xFFFF ? 54 : 45) + 15;
        m.Wr(0x0702, g > 0xFFFF ? 0xFF : g & 0xFF);
        m.Wr(0x0703, g > 0xFFFF ? 0xFF : g >> 8);
        m.Wr(Active + y, 0);
        m.Cyc += PlaySoundSfxCycles(m);
        PlaySound.Play(m, 0x24, y);
        m.Cyc += 5 + Death.HudRedrawCycles(m, 1);
        foreach (var s in Death.HudRedraw(m, 1, x)) yield return s;
        m.PassStage = stage;
    }

    // js: game/obj/collide.js talkProbeHit
    /// <summary>TalkProbeHit 26:94B0 (HitHandlerTable 4: the slot 3 probe touched y)</summary>
    private static IEnumerable<Y> TalkProbeHit(Machine m, int x0, int y)
    {
        m.Wr(ProbeHit, y);
        int id = m.Rd(Active + y) & 0x7F;
        if (id == 0x78 || id == 0x6E)
        {                                                         // TalkProbeItemUse 26:94A2
            if (m.Rd(SubState) == 8) { m.Wr(ProbeNpc, y); m.Wr(SubState, 7); }
            yield break;
        }
        int x = y;
        if (((m.Rd(Active + x) << 1) & 0xFF) != 0x60) yield break;   // AI $30 NPC
        if ((m.Rd(0x49) & 0x80) != 0) yield break;                // pad_dir: no direction held
        if ((m.Rd(Behav + x) & 0x04) != 0)
        {
            NpcPushBack(m, x);
            TalkStart(m, x);
            yield break;
        }
        if (m.Rd(0x0620) != 0) yield break;
        if (m.Rd(0x0603) != 0 && x == m.Rd(ProbeNpc)) yield break;
        int c = m.Rd(Coll + x);
        m.Wr(Coll + x, c | 0x10);
        m.Wr(0x10, 0); m.Wr(0x11, 2);
        bool hit = ObjOverlapScan(m, x) >= 0;                     // the NPC against the player body (slot 1)
        m.Wr(Coll + x, c);
        if (hit) yield break;
        TalkStart(m, x);
    }

    // js: game/obj/collide.js talkStart
    /// <summary>TalkStart 26:9508</summary>
    private static void TalkStart(Machine m, int x)
    {
        m.Wr(0x0603, 0x10);
        m.Wr(ProbeNpc, x);
        m.Wr(SubState, 0x11);
        if (m.Rd(Freeze + x) != 0) return;
        if ((m.Rd(Behav + x) & 0x02) == 0) return;
        m.Wr(0x10, (m.Rd(0x0301) + 2) & 3);
        m.Wr(Meta + x, (m.Rd(Meta + x) & 0xFC) | m.Rd(0x10));
    }

    // js: game/obj/collide.js npcPushBack
    /// <summary>NpcPushBack 26:9535: up to 4 passes; while the player body touches the solid NPC, slots 3..0 slide one
    /// step away from the player's facing, then a sprite pass (FarCall_B14_800E)</summary>
    private static void NpcPushBack(Machine m, int x)
    {
        m.Wr(0x28, 4);
        do
        {
            int c = m.Rd(Coll + x);
            m.Wr(Coll + x, c | 0xF0);
            m.Wr(0x10, (x - 1) & 0xFF);
            m.Wr(0x11, (x + 1) & 0xFF);
            int y = ObjOverlapScan(m, 1);
            m.Wr(Coll + x, c);
            if (y < 0)
            {                                                     // 9558
                m.Wr(0x0603, 0);
                if (m.Rd(0x0620) != 0) m.Wr(0x0640, 0xFF);
                return;
            }
            if (m.Rd(SpawnB2 + y) == 1 && m.Rd(SpawnParam + y) == 0x3A && (m.Rd(0x0301) & 3) == 0)
                m.Wr(0x0301, m.Rd(0x0301) & 0xFC);
            for (int s = 3; s >= 0; s--)
            {
                int f = m.Rd(Flags2 + s), d = m.Rd(MetaDir + s);
                m.Wr(MetaDir + s, (((m.Rd(0x0301) & 3) << 1) + 4) & 6);
                Move.ObjMoveSlide(m, s);
                m.Wr(MetaDir + s, d);
                m.Wr(Flags2 + s, f);
            }
            int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);                 // FarCall_B14_800E
            Prg.SwitchBank16(m, 0x0E);
            Sprites.SpriteDraw(m);
            Prg.FarRestore(m, r6, r7);
            m.Wr(0x28, (m.Rd(0x28) - 1) & 0xFF);
        } while (m.Rd(0x28) != 0);
    }

    private delegate IEnumerable<Y> HitFn(Machine m, int x, int y);
    private static readonly HitFn[] Handlers = { HitByPlayerShot, HitPlayer, TalkProbeHit, HitHandler3 };

    /// <summary>(collide.js HIT_HANDLERS) the HitHandlerTable entries by name (routine checks)</summary>
    public static IEnumerable<Y> RunHitHandler(Machine m, string name, int x, int y) => name switch
    {
        "hitByPlayerShot" => HitByPlayerShot(m, x, y),
        "hitPlayer" => HitPlayer(m, x, y),
        "talkProbeHit" => TalkProbeHit(m, x, y),
        "hitHandler3" => HitHandler3(m, x, y),
        _ => throw new ArgumentException(name),
    };

    // js: game/obj/collide.js objCollideAll
    /// <summary>ObjCollideAll 26:8F6E</summary>
    public static IEnumerable<Y> ObjCollideAll(Machine m)
    {
        m.Wr(RideA, 0);
        m.Wr(ProbeHit, 0);
        int odd = m.Rd(0x08) & 1;
        m.Wr(0x2F, odd != 0 ? 0x0E : 0x07);
        m.Wr(0x2E, odd != 0 ? 0x07 : 0x08);
        m.Cyc += odd != 0 ? 31 : 28;                              // s213: 8F6E-8F85
        for (; ; )
        {
            int r = (m.Rd(0x2F) << 2) & 0xFF;
            // s213: record 8F87-8FAC 60 cycles + CollPairTable $96F1+r+k crossing $9700, DEC DEC 10
            int cross = 0;
            for (int k = 0; k < 4; k++) cross += r + k >= 0x0F ? 1 : 0;
            m.Cyc += 60 + cross + 10;
            int x = Prg.CpuRd(m, 0x96F1 + r);
            m.Wr(0x10, Prg.CpuRd(m, 0x96F2 + r));
            m.Wr(0x11, Prg.CpuRd(m, 0x96F3 + r));
            int h = Prg.CpuRd(m, 0x96F4 + r);
            m.Wr(0x20, 0x4C);
            m.Wr(0x21, Prg.CpuRd(m, 0x8FBB + h));
            m.Wr(0x22, Prg.CpuRd(m, 0x8FBC + h));
            int y = ObjOverlapScan(m, x);
            if (y >= 0)
            {
                if ((h & 1) != 0 || (h >> 1) >= Handlers.Length)
                    throw new InvalidOperationException($"ObjCollideAll: handler index {h} not in HitHandlerTable");
                m.Cyc += 11;
                m.PassStage = "colh";
                foreach (var s in Handlers[h >> 1](m, x, y)) yield return s;
                m.PassStage = "col";
            }
            else m.Cyc += 3;
            m.Wr(0x2F, (m.Rd(0x2F) - 1) & 0xFF);
            int n = (m.Rd(0x2E) - 1) & 0xFF;
            m.Wr(0x2E, n);
            m.Cyc += n != 0 ? 3 : 8;
            if (n == 0) break;
        }
    }
}
