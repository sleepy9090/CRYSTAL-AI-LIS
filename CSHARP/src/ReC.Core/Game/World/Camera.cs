// re-c C# port — camera + exits (= web/src/game/world/camera.js, s108). REVERSE.md "Camera" / "World interaction".
//
//   CameraTick(m, res)  CameraTick 31:E8F6 (boss_cam_lock 1-$7F: DEC + the shake 31:EADF; else CameraUpdate +
//                       ExitLookup). res.V = true when ExitLookup took an exit: the ROM then PLAs two return
//                       addresses (31:EB68), i.e. CameraTick's CALLER returns at once.
//   CameraUpdate(m)     CameraUpdate 31:E90A (CameraFollow, BgColRender / BgRowRender, PpuQueueAttrAll, cam_ofs_x ease)
//   CameraClampX(m)     CameraClampX 31:EA72
//   ExitLookup(m)       ExitLookup 31:EB09 (k3 records `x y area entry` at the player's 16 px cell) -> true = exit
using ReC.Game.Obj;
using ReC.Gen;

namespace ReC.Game.World;

public static class Camera
{
    private const int P = 0x34;                       // $34-$37 pos_x_lo .. pos_y_hi
    private const int Cam = 0x3C;                     // $3C-$3F camera target
    private const int AreaW = 0x62FC, AreaH = 0x62FD;
    private const int CamLock = 0x07D7, Shake = 0x07D6;

    // js: game/world/camera.js cameraTick
    /// <summary>CameraTick 31:E8F6</summary>
    public static IEnumerable<Y> CameraTick(Machine m, Ret<bool>? res = null)
    {
        int l = m.Rd(CamLock);
        if (l != 0 && (l & 0x80) == 0)
        {
            m.Wr(CamLock, l - 1);
            CameraShake(m);                           // JMP 31:EADF
            if (res != null) res.V = false;
            yield break;
        }
        foreach (var s in CameraUpdate(m)) yield return s;
        bool r = ExitLookup(m);
        if (res != null) res.V = r;
    }

    // js: game/world/camera.js scrollFromPos
    /// <summary>31:E993: $30/$31 = cam_ofs_x/y, 26:9824, $02-$05 = $34-$37</summary>
    private static void ScrollFromPos(Machine m)
    {
        m.Wr(0x30, m.Rd(0x3A));
        m.Wr(0x31, m.Rd(0x3B));
        Prg.SetR6(m, 0x1A);
        ObjUtil.TempAddOffset(m);
        for (int x = 3; x >= 0; x--) m.Wr(R.ScrollShadow0 + x, m.Rd(P + x));
    }

    // js: game/world/camera.js cameraUpdate
    /// <summary>CameraUpdate 31:E90A</summary>
    public static IEnumerable<Y> CameraUpdate(Machine m)
    {
        CameraFollow(m);
        m.Wr(0x2E, 0); m.Wr(0x2F, 0);
        for (int i = 0; i < 4; i++) m.Wr(P + i, m.Rd(Cam + i));
        Prg.SetR6(m, 0x1A);
        ObjUtil.TempAddOffset(m);
        CameraClampX(m);
        for (int x = 3; x >= 0; x--) m.Wr(0x28 + x, m.Rd(P + x));
        m.Wr(0x35, m.Rd(0x3D));
        m.Wr(0x34, m.Rd(0x3C));
        if (((m.Rd(0x3C) ^ m.Rd(0x28)) & 0xF8) != 0) foreach (var s in BgStream.BgColRender(m)) yield return s;
        m.Wr(0x34, m.Rd(0x28));
        m.Wr(0x35, m.Rd(0x29));
        m.Wr(0x37, m.Rd(0x3F));
        m.Wr(0x36, m.Rd(0x3E));
        if (((m.Rd(0x3E) ^ m.Rd(0x2A)) & 0xF8) != 0) foreach (var s in BgStream.BgRowRender(m)) yield return s;
        if (m.Rd(0x2F) != 0) PpuQueue.QueueAttrAll(m);
        if (m.Rd(AreaW) == 0) m.Wr(0x3A, 0);
        else
        {                                             // E971: ease cam_ofs_x toward +1 (step <= 0) / -8 (step > 0)
            int y = m.Rd(0x3A);
            int a = m.Rd(0x30);
            if (a == 0 || (a & 0x80) != 0) { if (y != 0x01) y = (y + 1) & 0xFF; }
            else if (y != 0xF8) y = (y - 1) & 0xFF;
            m.Wr(0x3A, y);
        }
        for (int x = 3; x >= 0; x--) { int v = m.Rd(0x28 + x); m.Wr(Cam + x, v); m.Wr(P + x, v); }
        ScrollFromPos(m);
    }

    // js: game/world/camera.js camStepClamp
    /// <summary>CamStepClamp 31:EA2F: signed step -> -3..+3</summary>
    private static int CamStepClamp(int a)
    {
        if (a == 0) return 0;
        if ((a & 0x80) == 0) return a < 3 ? a : 3;
        return a >= 0xFE ? a : 0xFD;
    }

    // js: game/world/camera.js cameraFollow
    /// <summary>CameraFollow 31:E9AD: $30/$31 camera step</summary>
    private static void CameraFollow(Machine m)
    {
        m.Wr(0x30, 0); m.Wr(0x31, 0); m.Wr(0x07D5, 0);
        if ((m.Rd(CamLock) & 0x80) != 0) { BossCamCenter(m); return; }
        int s = m.Rd(0xB0) - m.Rd(0x3E);              // E9BE: y in screen rows (240-line wrap: -$10)
        if (s < 0) s -= 0x10;
        int dy = s & 0xFF;
        m.Wr(0x10, dy);
        if (dy < 0x68) m.Wr(0x31, CamStepClamp((dy - 0x68) & 0xFF));
        else if (dy >= 0x78) m.Wr(0x31, CamStepClamp((dy - 0x78) & 0xFF));
        if ((m.Rd(0x0340) & 0x80) != 0 || (m.Rd(R.PadPrev) & 0x03) != 0)
        {                                             // E9E5: knockback or L/R held
            int ddx = (m.Rd(0x70) - m.Rd(0x3C)) & 0xFF;
            m.Wr(0x10, ddx);
            if (ddx < 0x68) m.Wr(0x30, CamStepClamp((ddx - 0x68) & 0xFF));
            else if (ddx >= 0xA0) m.Wr(0x30, CamStepClamp((ddx - 0xA0) & 0xFF));
            return;
        }
        // E9FE: re-centre 1 px on the 16 px cell (+8 when obj_screen_x < $88, ASL of the difference)
        int a = m.Rd(0x70) & 0xF0;
        if ((((m.Rd(0x05C0) - 0x88) & 0xFF) & 0x80) == 0) a = (a + 8) & 0xFF;
        int dx = (a - m.Rd(0x3C)) & 0xFF;
        m.Wr(0x10, dx);
        int r;
        if (dx < 0x68) r = (dx - 0x68) & 0xFF;
        else
        {
            if (dx < 0x98) return;
            r = (dx - 0x98) & 0xFF;
            if (r == 0) return;
        }
        m.Wr(0x30, (r & 0x80) != 0 ? 0xFF : 0x01);   // EA21: LDY #0 STY $30, BPL -> INC; else DEC DEC INC
    }

    // js: game/world/camera.js bossCamCenter
    /// <summary>31:EA41 (boss_cam_lock b7, odd frames): step $3C / $3E one px toward 0; $07D5 = 1 once both are 0</summary>
    private static void BossCamCenter(Machine m)
    {
        if ((m.Rd(0x08) & 1) == 0) return;
        // js: game/world/camera.js step
        void Step(int x, int y)
        {
            int a = m.Rd(Cam + y);
            if (a == 0) return;
            m.Wr(0x30 + x, (m.Rd(0x30 + x) + ((a & 0x80) != 0 ? 1 : -1)) & 0xFF);
            m.Wr(0x07D5, 0);
        }
        Step(0, 0);
        Step(1, 2);
        if ((m.Rd(0x3C) | m.Rd(0x3E)) == 0) m.Wr(0x07D5, 1);
    }

    // js: game/world/camera.js cameraClampX
    /// <summary>CameraClampX 31:EA72 (Y = 0 throughout)</summary>
    public static void CameraClampX(Machine m)
    {
        int w = m.Rd(AreaW);
        if (w == 0) { m.Wr(0x34, 0); m.Wr(0x35, 0); }
        else if ((m.Rd(0x35) & 0x80) != 0)
        {
            if (m.Rd(0x34) < 0xF8) { m.Wr(0x30, 0); m.Wr(0x34, 0xF8); }
        }
        else if (m.Rd(0x35) >= w)
        {
            m.Wr(0x34, 0); m.Wr(0x35, w); m.Wr(0x30, 0);
        }
        int h = m.Rd(AreaH);
        if (h == 0)
        {                                             // EAA6
            m.Wr(0x37, 0); m.Wr(0x36, 0);
            m.Wr(0x3B, m.Rd(R.AreaId) == 0x5F ? 0 : 0xFC);
            return;
        }
        m.Wr(0x3B, 8);
        if ((m.Rd(0x37) & 0x80) != 0) { m.Wr(0x37, 0); m.Wr(0x36, 0); m.Wr(0x31, 0); return; }
        if (m.Rd(0x37) >= h && m.Rd(0x36) >= 0x28) { m.Wr(0x37, h); m.Wr(0x36, 0x28); m.Wr(0x31, 0); }
    }

    // js: game/world/camera.js cameraShake
    /// <summary>31:EADF: the boss-landing shake ($07D6 frames, cam_ofs_x from 31:EB05[$08 &amp; 3] = 0 -4 0 +4)</summary>
    private static void CameraShake(Machine m)
    {
        int s = m.Rd(Shake);
        if (s == 0) return;
        m.Wr(Shake, s - 1);
        if (s - 1 == 0) { m.Wr(0x3A, 0); return; }
        m.Wr(0x3A, Prg.CpuRd(m, 0xEB05 + (m.Rd(0x08) & 3)));
        for (int x = 3; x >= 0; x--) m.Wr(P + x, m.Rd(Cam + x));
        ScrollFromPos(m);
    }

    // js: game/world/camera.js areaSubTable
    /// <summary>AreaSubTable 30:C7A5 (A = k): SwitchBank16(5); ($10) = header 10:8300/8400[$6C][k]; Y = 0</summary>
    public static void AreaSubTable(Machine m, int k)
    {
        Prg.SwitchBank16(m, 5);
        int id = m.Rd(R.AreaId), b = (id & 0x80) != 0 ? 0x8400 : 0x8300, y = (id << 1) & 0xFF;
        m.Wr(0x10, Prg.CpuRd(m, b + y));
        m.Wr(0x11, Prg.CpuRd(m, b + y + 1));
        int lo = Prg.IndY(m, 0x10, k << 1), hi = Prg.IndY(m, 0x10, (k << 1) + 1);
        m.Wr(0x11, hi);
        m.Wr(0x10, lo);
    }

    // js: game/world/camera.js exitLookup
    /// <summary>ExitLookup 31:EB09: in SubstPlay a matching k3 record sets area_id / area_entry and sub_state 1 -> true</summary>
    public static bool ExitLookup(Machine m)
    {
        var (hit, y) = ExitScan(m);
        if (!hit || m.Rd(R.SubState) != 8) return false;
        m.Wr(R.AreaId, Prg.IndY(m, 0x10, y));
        m.Wr(0x6D, Prg.IndY(m, 0x10, y + 1));
        m.Wr(R.SubState, 1);
        return true;
    }

    // js: game/world/camera.js exitScan
    /// <summary>ExitLookup 31:EB09 up to its sub_state test: (matched, Y). s121: ShopWalkOut 30:DAA8 reads ($10),Y itself.</summary>
    public static (bool Hit, int Y) ExitScan(Machine m)
    {
        m.Wr(0x12, ((m.Rd(0x70) >> 4) | (m.Rd(0x90) << 4)) & 0xFF);
        m.Wr(0x13, ((m.Rd(0xB0) >> 4) | (m.Rd(0xD0) << 4)) & 0xFF);
        AreaSubTable(m, 3);
        int y = 0;
        for (; ; )
        {
            m.Wr(0x14, Prg.IndY(m, 0x10, y)); y = (y + 1) & 0xFF;
            m.Wr(0x15, Prg.IndY(m, 0x10, y)); y = (y + 1) & 0xFF;
            if (m.Rd(0x14) == 0xFF) return (false, y);
            if (m.Rd(0x15) == m.Rd(0x13) && m.Rd(0x14) == m.Rd(0x12)) return (true, y);
            y = (y + 2) & 0xFF;
            if (y == 0) return (false, y);
        }
    }
}
