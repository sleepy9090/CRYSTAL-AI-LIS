// re-c C# port — object sprite pass (= web/src/game/obj/sprites.js, s54): ObjSpriteDraw 28:800E, ObjDrawMeta 28:822E,
// ObjHideUnusedOam 28:80A5, OrbitSprites 28:80D6. REVERSE.md "Object sprite pass" + "Metasprite drawing".
//
//   SpriteDraw(m)   one call of 28:800E: OAM shadow $0200 (sprite 0 untouched), obj $05C0/$05E0, $0380 b7, $07EF
//
// The zero-page temps $10-$1E are written to RAM like the ROM does. ROM tables + metasprite records are read through
// CPU addresses from sprites_b28.bin (banks 28-29 = $8000-$BFFF). Quirks kept: see sprites.js's header.
// The JS module variable `regY` (the 6502 Y at the pass's RTS) is m.SpriteRegY.
namespace ReC.Game.Obj;

public static class Sprites
{
    private const int ObjXLo = 0x70, ObjXHi = 0x90, ObjYLo = 0xB0, ObjYHi = 0xD0;
    private const int ObjMetaId = 0x0300, ObjTileOfs = 0x0320, ObjFlags2 = 0x0340, ObjMetaDir = 0x0360, ObjFlags = 0x0380;
    private const int ObjActive = 0x04A0, ObjAnim = 0x04E0, ObjOverlay = 0x0580, ObjScreenX = 0x05C0, ObjScreenY = 0x05E0;
    private const int Oam = 0x0200;
    private const int Obj1Flash = 0x0710, Obj1OamOfs = 0x07EF, ObjDrawRot = 0x07FF;
    private const int OrbitLevel = 0x06C0, OrbitBurst = 0x06C1;
    private const int DrawOrderOfs = 0x83D8, DrawOrder = 0x83DC, MetaPtrA = 0x845C, MetaPtrB = 0x855C;
    private const int OrbitSine = 0x81BA, OrbitTile = 0x81DA, BurstDir = 0x81DE, BurstRadius = 0x81FE;
    private const int TOam = 0x10, T11 = 0x11, TSx = 0x12, TSy = 0x13, TPtr = 0x15, T17 = 0x17, T18 = 0x18, TPri = 0x19,
        TTile = 0x1A, TLeft = 0x1B, TMirror = 0x1C, T1D = 0x1D, T1E = 0x1E;

    // js: game/obj/sprites.js rom
    /// <summary>bank 28/29 byte at CPU address a ($8000-$BFFF). Meta id $9A's pointer is $0000, so its "record" is
    /// zero-page RAM, as on the 6502.</summary>
    private static int Rom(Machine m, int a)
    {
        a &= 0xFFFF;
        if (a < 0x2000) return m.Rd(a);
        if (a < 0x8000 || a >= 0xC000) throw new InvalidOperationException($"sprite pass: read ${a:x} outside banks 28/29 / RAM");
        m.RomRd++;                                                // s212: counted like prg.js cpuRd
        return m.Data.BinFile("spritesB28")[a - 0x8000];
    }

    // js: game/obj/sprites.js spriteDrawY
    /// <summary>Y at ObjSpriteDraw's last RTS (s198; AreaEnter's PlaySound($2F) stores it in snd_saved_y)</summary>
    public static int SpriteDrawY(Machine m) => m.SpriteRegY;

    // js: game/obj/sprites.js spriteDraw
    /// <summary>ObjSpriteDraw 28:800E. Returns the X register at its RTS (s172, AI $0B uses it): 0 after a full
    /// OrbitSprites loop, else ObjHideUnusedOam's X.</summary>
    public static int SpriteDraw(Machine m)
    {
        m.Wr(T1E, (m.Rd(0x08) & 2) != 0 ? 3 : 0);                // 800E (dead, see ObjDrawMeta)
        m.Wr(ObjDrawRot, m.Rd(ObjDrawRot) - 1);                   // 801A
        m.Wr(T11, Rom(m, DrawOrderOfs + (m.Rd(ObjDrawRot) & 3)));
        m.Wr(TLeft, 0x20);
        m.Wr(TOam, 4);
        do
        {
            m.SpriteRegY = m.Rd(T11);                             // 8030 LDY $11
            int x = Rom(m, DrawOrder + m.SpriteRegY);
            DrawSlot(m, x);
            m.Wr(T11, m.Rd(T11) + 1);                             // 808E
            m.Wr(TLeft, m.Rd(TLeft) - 1);
        } while (m.Rd(TLeft) != 0);
        int hx = HideUnusedOam(m);                                // 8094
        return OrbitSprites(m) ?? hx;                             // 8097 JMP
    }

    // js: game/obj/sprites.js setOffscreen
    private static void SetOffscreen(Machine m, int x) => m.Wr(ObjFlags + x, m.Rd(ObjFlags + x) | 0x80);   // 809A

    // js: game/obj/sprites.js drawSlot
    /// <summary>loop body 8036-808B for slot x</summary>
    private static void DrawSlot(Machine m, int x)
    {
        if (m.Rd(ObjActive + x) == 0) { SetOffscreen(m, x); return; }
        int d = m.Rd(ObjXLo + x) - m.Rd(0x02);
        int sx = d & 0xFF;
        m.SpriteRegY = sx;                                        // 8040 TAY
        if (((m.Rd(ObjXHi + x) - m.Rd(0x03) - (d < 0 ? 1 : 0)) & 0xFF) != 0) { SetOffscreen(m, x); return; }
        m.Wr(TSx, sx);
        m.Wr(ObjScreenX + x, sx);
        int ylo = m.Rd(ObjYLo + x), cyl = m.Rd(0x04);             // 804D: 16-bit y - cam_y, hi must be 0
        if (((m.Rd(ObjYHi + x) - m.Rd(0x05) - (ylo < cyl ? 1 : 0)) & 0xFF) != 0) { SetOffscreen(m, x); return; }
        d = ylo - cyl;
        int sy = d & 0xFF;
        if (d < 0) sy = (sy - 0x0F - 1) & 0xFF;                   // 805E SBC #$0F with C = 0: 240-row wrap
        if (sy >= 0xE8) return;                                   // 8060: skipped, flags untouched
        if (sy < 0x10) { SetOffscreen(m, x); return; }
        m.Wr(TSy, sy);
        m.Wr(ObjScreenY + x, sy);
        int fl = m.Rd(ObjFlags + x) & 0x7F;
        m.Wr(ObjFlags + x, fl);
        m.Wr(T17, fl << 1);
        if (m.Rd(TOam) == 0) return;                              // OAM full
        if ((fl & 4) != 0 && ((x + m.Rd(ObjDrawRot)) & 1) != 0) return;   // 807F half-rate flicker
        DrawMeta(m, x);
    }

    // js: game/obj/sprites.js drawMeta
    /// <summary>ObjDrawMeta 28:822E (also reached through 28:8000 for slot $1F: states/pause.js objSlot1FDraw)</summary>
    public static void DrawMeta(Machine m, int x)
    {
        int t17 = m.Rd(T17);
        if ((t17 & 0x80) != 0) m.Wr(TSy, m.Rd(TSy) + 3);         // 8230 ASL -> C = $0380 b6; ADC #$02 + C
        if ((t17 & 4) == 0) { DrawMetaMain(m, x); return; }       // $0380 b1: overlay first
        int id = m.Rd(ObjMetaId + x), fl = m.Rd(ObjFlags + x);
        m.Wr(ObjFlags + x, (fl & 0xDF) | 1);
        m.Wr(ObjMetaId + x, m.Rd(ObjOverlay + x));
        DrawMetaMain(m, x);
        m.Wr(ObjFlags + x, fl);
        m.Wr(ObjMetaId + x, id);
        if (m.Rd(TOam) != 0) DrawMetaMain(m, x);
    }

    // js: game/obj/sprites.js drawMetaMain
    /// <summary>ObjDrawMetaMain 28:8268</summary>
    private static void DrawMetaMain(Machine m, int x)
    {
        if (x == 1) m.Wr(Obj1OamOfs, m.Rd(TOam));
        m.Wr(TTile, 0);
        m.Wr(TMirror, 0);
        int id = m.Rd(ObjMetaId + x);
        if ((m.Rd(ObjFlags + x) & 1) == 0) id = (id + m.Rd(ObjMetaDir + x)) & 0xFF;
        int tab = (id & 0x80) != 0 ? MetaPtrB : MetaPtrA, yi = (id << 1) & 0xFF;
        int ptr = Rom(m, tab + yi) | Rom(m, tab + yi + 1) << 8;
        int n;
        for (; ; )
        {                                                         // 829D
            n = Rom(m, ptr);
            if (n == 0) { m.Wr(TPtr, ptr); m.Wr(TPtr + 1, ptr >> 8); m.SpriteRegY = 0; return; }   // 82B8 RTS, Y = 0
            if (n < 0x80) break;
            ptr = Rom(m, ptr + 1) | Rom(m, ptr + 2) << 8;         // FF lo hi: mirrored redirect
            m.Wr(TMirror, 0x80);
        }
        // 82B9: frame f = ($04E0 >> 3) & mask; data += ((N * f) & $FF) * 4 (shift-add on 8-bit A)
        m.Wr(T17, n);
        int f = (m.Rd(ObjAnim + x) >> 3) & Rom(m, ptr + 1);
        int a = 0, t18 = n;                                       // $1D = f, LSR'd once per bit
        for (int b = 0; b < 4; b++)
        {
            if (((f >> b) & 1) != 0) a = (a + t18) & 0xFF;
            if (b < 3) t18 = (t18 << 1) & 0xFF;
        }
        m.Wr(T1D, f >> 4);
        ptr = (ptr + a * 4) & 0xFFFF;
        m.Wr(TPtr, ptr); m.Wr(TPtr + 1, ptr >> 8);
        int fl = m.Rd(ObjFlags + x);
        int t18f = (fl << 1) & 0xFF;                              // 8301: b7 = $0380 b6 (sink)
        m.Wr(T18, t18f);
        int pri = t18f & 0x20;                                    // $0380 b4 -> attr b5 (behind BG)
        m.Wr(TPri, pri);
        int tileAdd = (m.Rd(ObjTileOfs + x) + ((fl & 0x20) << 1)) & 0xFF;
        m.Wr(TTile, tileAdd);
        m.Wr(T1D, (m.Rd(ObjFlags2 + x) & 0x80) != 0 ? m.Rd(T1E) : 0);   // 8320: dead store
        int mirror = m.Rd(TMirror) & 0x80;
        int sx = m.Rd(TSx), sy = m.Rd(TSy);
        int o = m.Rd(TOam), y = 2, left = n;
        for (; ; )
        {                                                         // MetaEmitNormal 8331 / MetaEmitHFlip 8380
            int dx = Rom(m, ptr + y);
            if (dx == 0x80) break;
            int ex = mirror != 0 ? (((dx ^ 0xFF) - 8) & 0xFF) : dx;
            if (mirror != 0) m.Wr(TMirror, ex);
            int sum = ex + sx;
            m.Wr(Oam + 3 + o, sum);
            // ROR A puts C in b7; EOR dx: b7 = C ^ dx.b7 -> the entry wrapped horizontally, skip it
            if (((((sum & 0x100) != 0 ? 0x80 : 0) ^ ex) & 0x80) != 0)
            {
                left = (left - 1) & 0xFF;
                if (left == 0) break;
                y += 4;
                continue;
            }
            m.Wr(Oam + o, Rom(m, ptr + y + 1) + sy);
            int attr = Rom(m, ptr + y + 2) | pri;
            if (mirror != 0) attr ^= 0x40;
            m.Wr(Oam + 2 + o, attr);
            if ((attr & 4) != 0 && (t18f & 0x80) != 0) { y += 3; break; }   // 8379 BIT $18: lower part of a sinking object
            m.Wr(Oam + 1 + o, Rom(m, ptr + y + 3) + tileAdd);
            y += 4;
            o = (o + 4) & 0xFF;
            if (o == 0) break;                                    // OAM full
            left = (left - 1) & 0xFF;
            if (left == 0) break;
        }
        m.SpriteRegY = y & 0xFF;
        m.Wr(T17, left);
        m.Wr(TOam, o);
    }

    // js: game/obj/sprites.js hideUnusedOam
    /// <summary>ObjHideUnusedOam 28:80A5</summary>
    public static int HideUnusedOam(Machine m)
    {
        int o = m.Rd(TOam);
        if (o != 0) for (; o < 0x100; o += 4) m.Wr(Oam + o, 0xF0);
        if ((m.Rd(Obj1Flash) & 0x80) == 0) return 0;              // X = 0 (BEQ, or the INX loop wrapped)
        int n = (m.Rd(ObjFlags + 1) & 0x40) != 0 ? 4 : 6;         // $0381 ASL -> N = b6
        int x = m.Rd(Obj1OamOfs);
        do
        {
            m.Wr(Oam + 2 + x, m.Rd(Oam + 2 + x) & 0xFC);
            x = (x + 4) & 0xFF;
        } while (--n != 0);
        m.SpriteRegY = 0;                                         // 80D2 DEY loop ends at 0
        return x;
    }

    // js: game/obj/sprites.js orbitSprites
    /// <summary>OrbitSprites 28:80D6 (null = JS undefined: returned before the loops)</summary>
    private static int? OrbitSprites(Machine m)
    {
        int cx = m.Rd(ObjScreenX + 1), cy = m.Rd(ObjScreenY + 1);
        // js: game/obj/sprites.js wraps
        // signed add with wrap check (CLC ADC; ROR; EOR v; BMI = skip): true if base + v (v signed) leaves 0-255
        static bool Wraps(int v, int b) => (((((v + b) & 0x100) != 0 ? 0x80 : 0) ^ v) & 0x80) != 0;
        int burst = m.Rd(OrbitBurst);
        if (burst == 0)
        {
            if ((m.Rd(OrbitLevel) & 0xF8) == 0) return null;
            int o = 0xE0;
            for (int k = 7; k >= 0; k--)
            {
                m.Wr(TOam, k);
                int yi = (((k & 6) << 2) + m.Rd(0x08)) & 0x1F;
                m.SpriteRegY = yi;                                // 80F3 TAY
                int s = Rom(m, OrbitSine + yi);
                if ((k & 1) == 0) s = ((s ^ 0xFF) + 1) & 0xFF;
                int v = (s - 4) & 0xFF;
                m.Wr(T11, v);
                m.Wr(TSx, v + cx);
                if (!Wraps(v, cx))
                {
                    m.Wr(Oam + 3 + o, v + cx);
                    yi = (yi + 2) & 0x1F;
                    m.Wr(Oam + o, Rom(m, OrbitSine + yi) + cy - 0x0C);
                    m.SpriteRegY = m.Rd(OrbitLevel) >> 3;         // 8130 TAY
                    m.Wr(Oam + 1 + o, Rom(m, OrbitTile + m.SpriteRegY));
                    m.Wr(Oam + 2 + o, 1);
                }
                o += 4;
            }
            m.Wr(TOam, 0xFF);
            return 0;                                             // X = $E0 + 8 * 4 wrapped
        }
        m.Wr(OrbitBurst, burst - 1);                              // 814A DEC; CMP uses the old value
        if (burst >= 0x0E) return null;
        burst = m.Rd(OrbitBurst);
        // js: game/obj/sprites.js radius
        int Radius(int yi)
        {                                                         // BurstRadius 28:81A5
            m.SpriteRegY = (Rom(m, BurstDir + yi) | burst) & 0xFF;   // 81AF TAY
            int r = Rom(m, BurstRadius + m.SpriteRegY);
            return (Rom(m, BurstDir + 1 + yi) & 0x80) != 0 ? ((r ^ 0xFF) + 1) & 0xFF : r;
        }
        int oo = 0xE0;
        for (int k = 7; k >= 0; k--)
        {
            m.Wr(TOam, k);
            int vx = (Radius(k * 4) - 4) & 0xFF;
            m.Wr(T11, vx);
            m.Wr(TSx, vx + cx);
            if (!Wraps(vx, cx))
            {
                m.Wr(Oam + 3 + oo, vx + cx);
                int vy = (Radius(k * 4 + 2) - 0x0C) & 0xFF;
                m.Wr(TSx, vy);
                m.Wr(T11, vy + cy);
                if (!Wraps(vy, cy))
                {
                    m.Wr(Oam + oo, vy + cy);
                    m.Wr(Oam + 1 + oo, 0x29);
                    m.Wr(Oam + 2 + oo, 1);
                }
            }
            oo += 4;
        }
        m.Wr(TOam, 0xFF);
        return 0;
    }
}
