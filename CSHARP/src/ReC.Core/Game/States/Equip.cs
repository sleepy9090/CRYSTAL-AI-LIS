// re-c C# port — the SELECT equipment / item menu (= web/src/game/states/equip.js, s98): SubstEquipMenu 30:D8C7
// (sub_state $12) + the bank 16 menu 16:8000 (run with bank 23 at $A000). NMI side: MainLoop nmiMode6 (31:F7DB) +
// irqMenuChrSplit (IRQ handler 7, 31:F61A). REVERSE.md "Equipment menu (web port, session 98)".
//
//   SubstEquipMenu(m)   iterator = SubstEquipMenu 30:D8C7 (substate 18)
//   EquipMenu(m)        iterator = 16:8000 (START runs SaveLoadPage 16:9AEE, the $7000 = $FB/$AB exit runs 30:C80C)
// Shop mode ($07DD != 0, s109): price line 16:81B4 / MenuFarHud 16:8613, sell 16:835E + MenuSellGold 16:8631, 16:846C.
// JS 'exit' results (shop NO -> PLA PLA RTS) are a Ret<bool> set to true; MenuConfirmYesNo's C is a Ret<bool>.
using ReC.Game.Obj;
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class Equip
{
    private const int Buf = 0x6000;
    private const int Kind = 0x6424;          // 0 = Sword/Armor/Shield/Power grid, 1 = Item/Magic grid
    private const int NCol = 0x6426;          // columns of the current grid (4 / 8)
    private const int Cur = 0x6427;           // cursor cell (row * cols + col)
    private const int Sel = 0x6428;           // inv_select: selected column per row (b7 = none)
    private const int Inv = 0x6430;
    private const int Shop = 0x07DD;          // menu variant (0 = the SELECT menu)

    // js: game/states/equip.js unported
    private static void Unported(Machine m, string key, string what)
    {
        if (m.Unported.Add(key)) m.Log($"{what} {key} not ported yet");
    }

    // js: game/states/equip.js menuSnd
    /// <summary>16:8A9F: A -> snd_queue $0103, count $0100 = 1 under the lock $0101</summary>
    public static void MenuSnd(Machine m, int a)
    {
        m.Wr(0x0103, a); m.Wr(0x0101, 1); m.Wr(0x0100, 1); m.Wr(0x0101, 0);
    }

    // js: game/states/equip.js menuFlush
    /// <summary>MenuFlush 16:8AB0: queue ($21 $20 $24 $25) with NMI off; PpuQueueFlush 30:C67D now if rendering is off</summary>
    public static void MenuFlush(Machine m)
    {
        PpuQueue.PpuCtrlNmiOff(m);
        PpuQueue.QueuePush(m, m.Rd(0x21), m.Rd(0x20), m.Rd(0x24), m.Rd(0x25));
        if ((m.Rd(R.PpumaskShadow) & 0x18) == 0) PpuQueue.PpuQueueFlush(m);
        PpuQueue.PpuCtrlNmiOn(m);
    }

    // js: game/states/equip.js putc
    /// <summary>MenuPutc 16:8A02 (Y is the caller's)</summary>
    private static void Putc(Machine m, int a)
    {
        int x = m.Rd(0x28);
        m.Wr(Buf + x, a);
        m.Wr(0x24, (m.Rd(0x24) + 1) & 0xFF);
        m.Wr(0x28, (x + 1) & 0xFF);
    }

    // js: game/states/equip.js menuPrint
    /// <summary>MenuPrint 16:8933: record A of MenuStrings 16:8C1B (PPU lo, hi, text ptr); FF end, FE save-slot name,
    /// FD save-slot level, FC item name $19 ($FF -> $49). $7001 is the Y scratch of the FE/FD escapes.</summary>
    public static IEnumerable<Y> MenuPrint(Machine m, int a)
    {
        m.Wr(0x17, a);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        foreach (int z in new[] { 0x23, 0x22, 0x24, 0x25, 0x28 }) m.Wr(z, 0);
        int x = (a << 2) & 0xFF;
        m.Wr(0x20, Prg.CpuRd(m, 0x8C1B + x)); m.Wr(0x21, Prg.CpuRd(m, 0x8C1C + x));
        m.Wr(0x10, Prg.CpuRd(m, 0x8C1D + x)); m.Wr(0x11, Prg.CpuRd(m, 0x8C1E + x));
        int y = 0;
        for (; ; )
        {
            int c = Prg.IndY(m, 0x10, y);
            m.Wr(0x18, c);
            if (c == 0xFF) break;
            if (c == 0xFE)
            {                                                 // 8994: player name of save slot (b0)
                y = (y + 1) & 0xFF;
                m.Wr(0x7001, y);
                m.Wr(0x13, Prg.CpuRd(m, 0x89B6 + (Prg.IndY(m, 0x10, y) & 1)));
                m.Wr(0x12, 0x18);
                int yy = 0;
                do { Putc(m, Prg.IndY(m, 0x12, yy)); yy = (yy + 1) & 0xFF; } while (yy != 6);
                y = (m.Rd(0x7001) + 1) & 0xFF;
            }
            else if (c == 0xFD)
            {                                                 // 89B8: level of save slot
                y = (y + 1) & 0xFF;
                m.Wr(0x7001, y);
                int yy = ((Prg.IndY(m, 0x10, y) != 0 ? m.Rd(0x77F7) : m.Rd(0x71F7)) << 1) & 0xFF;
                for (int i = 0; i < 2; i++)
                {
                    int d = Prg.CpuRd(m, 0x89E0 + yy);
                    m.Wr(0x18, d);
                    Putc(m, d);
                    yy = (yy + 1) & 0xFF;
                }
                y = (m.Rd(0x7001) + 1) & 0xFF;
            }
            else if (c == 0xFC)
            {                                                 // 8A0D: item name
                y = (y + 1) & 0xFF;
                int id = m.Rd(0x19);
                if (id == 0xFF) id = 0x49;
                int xx = (id << 1) & 0xFF;
                m.Wr(0x12, Prg.CpuRd(m, 0x9086 + xx)); m.Wr(0x13, Prg.CpuRd(m, 0x9087 + xx));
                for (int yy = 0; ; yy = (yy + 1) & 0xFF)
                {
                    int d = Prg.IndY(m, 0x12, yy);
                    m.Wr(0x18, d);
                    if (d == 0xFF) break;
                    Putc(m, d);
                }
            }
            else
            {
                Putc(m, c);
                y = (y + 1) & 0xFF;
            }
        }
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        MenuFlush(m);
    }

    // js: game/states/equip.js menuSprDraw
    /// <summary>MenuSprDraw 16:86D3: shape $12 (MenuSprShapes 16:8B1F) at OAM slot $2E + 1, offsets $60F8-$60FB</summary>
    public static void MenuSprDraw(Machine m)
    {
        int s2 = (m.Rd(0x12) << 1) & 0xFF;
        m.Wr(0x12, Prg.CpuRd(m, 0x8B1F + s2)); m.Wr(0x13, Prg.CpuRd(m, 0x8B20 + s2));
        m.Wr(0x2F, Prg.IndY(m, 0x12, 0));
        int y = 1;
        int a1 = m.Rd(0x2E) << 1, a2 = (a1 & 0xFF) << 1;
        int x = ((a2 & 0xFF) + 4 + (a2 >> 8)) & 0xFF;           // ASL ASL ADC #4 (carry of the 2nd ASL)
        // js: game/states/equip.js put
        void Put(int v) { m.Wr(0x0200 + x, v & 0xFF); x = (x + 1) & 0xFF; y = (y + 1) & 0xFF; }
        do
        {
            Put(Prg.IndY(m, 0x12, y) + m.Rd(0x60F8));
            Put(Prg.IndY(m, 0x12, y) + m.Rd(0x60F9));
            Put(Prg.IndY(m, 0x12, y) | m.Rd(0x60FA));
            Put(Prg.IndY(m, 0x12, y) + m.Rd(0x60FB));
            m.Wr(0x2F, (m.Rd(0x2F) - 1) & 0xFF);
        } while (m.Rd(0x2F) != 0);
    }

    // js: game/states/equip.js hideOam
    /// <summary>16:8605: WaitFrame, then all of OAM y = $F0</summary>
    public static IEnumerable<Y> HideOam(Machine m)
    {
        foreach (var s in Death.WaitFrame(m)) yield return s;
        for (int x = 0; x < 0x100; x++) m.Wr(0x0200 + x, 0xF0);
    }

    // js: game/states/equip.js rowBase
    private static int RowBase(Machine m) => m.Rd(Kind) != 0 ? 4 : 0;   // LDA $6424 / BEQ / LDA #4

    // js: game/states/equip.js curItem
    /// <summary>16:85E6: [inventory[(grid ? $10 : 0) + cursor], X]</summary>
    private static (int A, int X) CurItem(Machine m)
    {
        int x = ((m.Rd(Kind) != 0 ? 0x10 : 0) + m.Rd(Cur)) & 0xFF;
        return (m.Rd(Inv + x), x);
    }

    // js: game/states/equip.js rowSlot
    /// <summary>16:85F6: equipment slot ($0711 + n) of row $2B (table 16:833A)</summary>
    private static int RowSlot(Machine m) => Prg.CpuRd(m, 0x833A + ((RowBase(m) + m.Rd(0x2B)) & 0xFF));

    // js: game/states/equip.js cellPos
    /// <summary>16:86AF: $60F8 / $60FB = position of the cursor cell (16:8B73 pairs + $30 / $38)</summary>
    private static void CellPos(Machine m)
    {
        int x = ((((m.Rd(Kind) != 0 ? 0x10 : 0) + m.Rd(Cur)) & 0xFF) << 1) & 0xFF;
        m.Wr(0x60F8, (Prg.CpuRd(m, 0x8B73 + x) + 0x30) & 0xFF);
        m.Wr(0x60FB, (Prg.CpuRd(m, 0x8B74 + x) + 0x38) & 0xFF);
    }

    // js: game/states/equip.js drawIcon
    /// <summary>16:8660: icon of the cursor item (tile 16:8FA5, flags 16:8FF0) at OAM slot row * 9 + 1</summary>
    private static void DrawIcon(Machine m)
    {
        var (a, _) = CurItem(m);
        if (a >= 0x4B) return;
        m.Wr(0x60F9, Prg.CpuRd(m, 0x8FA5 + a));
        m.Wr(0x2E, (m.Rd(0x2B) * 9) & 0xFF);
        int f = Prg.CpuRd(m, 0x8FF0 + a);
        m.Wr(0x60FA, f & 3);
        m.Wr(0x12, (f & 0x80) != 0 ? 3 : 2);
        CellPos(m);
        MenuSprDraw(m);
    }

    // js: game/states/equip.js drawCursor
    /// <summary>16:8699: the cursor frame (shape 0, palette 3) at OAM slot $39</summary>
    private static void DrawCursor(Machine m)
    {
        m.Wr(0x12, 0); m.Wr(0x60F9, 0); m.Wr(0x60FA, 3); m.Wr(0x2E, 0x38);
        CellPos(m);
        MenuSprDraw(m);
    }

    // js: game/states/equip.js confirmCursor
    /// <summary>16:850D: the YES/NO arrow (shape 1) at ($22, 16:8532[$23 &amp; 1])</summary>
    private static void ConfirmCursor(Machine m)
    {
        m.Wr(0x60FB, Prg.CpuRd(m, 0x8532 + (m.Rd(0x23) & 1)));
        m.Wr(0x60F8, m.Rd(0x22));
        m.Wr(0x60F9, 0); m.Wr(0x60FA, 1); m.Wr(0x2E, 0x36); m.Wr(0x12, 1);
        MenuSprDraw(m);
    }

    // js: game/states/equip.js confirm
    /// <summary>MenuConfirmYesNo 16:84DA: res = C (true = NO / cancel)</summary>
    public static IEnumerable<Y> Confirm(Machine m, int a, Ret<bool> res)
    {
        if (a == 0x41) a = 0x49;
        m.Wr(0x22, a);
        m.Wr(0x23, 1);
        for (; ; )
        {
            foreach (var s in Death.WaitFrame(m)) yield return s;
            ConfirmCursor(m);
            Pads.ReadPadsMenu(m);
            if ((m.Rd(R.PadNew) & 0x0F) != 0) { m.Wr(0x23, (m.Rd(0x23) + 1) & 0xFF); MenuSnd(m, 0x4B); }
            if ((m.Rd(R.PadNew) & 0xC0) != 0) break;
        }
        foreach (var s in Death.WaitFrame(m)) yield return s;
        m.Wr(0x02DC, 0xF0);
        int v = m.Rd(0x23);
        m.Wr(0x23, v >> 1);
        res.V = (v & 1) == 1;
    }

    // js: game/states/equip.js eraseRowIcon
    /// <summary>16:8A5A (X = caller's X): hide the 9 icon sprites of row $2B after a WaitFrame; the $60F8/$60FB
    /// writes index 16:8B73 with the stale X (dead values)</summary>
    private static IEnumerable<Y> EraseRowIcon(Machine m, int xIn)
    {
        m.Wr(0x60F8, (Prg.CpuRd(m, 0x8B73 + xIn) + m.Rd(0x2B)) & 0xFF);
        m.Wr(0x60FB, (Prg.CpuRd(m, 0x8B74 + xIn) + m.Rd(0x2A)) & 0xFF);
        int x = (m.Rd(0x2B) * 36 + 4) & 0xFF;
        foreach (var s in Death.WaitFrame(m)) yield return s;
        for (int i = 0; i < 36; i++) { m.Wr(0x0200 + x, 0xF8); x = (x + 1) & 0xFF; }
    }

    // js: game/states/equip.js sortInventory
    /// <summary>16:8534: sort each of the 8 inventory categories ascending (counts 16:859E, offsets 16:85A6) via $60E0</summary>
    public static void SortInventory(Machine m)
    {
        for (int c = 0; c < 8; c++)
        {
            m.Wr(0x2E, c);
            int n = Prg.CpuRd(m, 0x859E + c), o = Prg.CpuRd(m, 0x85A6 + c);
            for (int i = 0; i < n; i++) m.Wr(0x60E0 + i, m.Rd(Inv + o + i));
            do
            {
                m.Wr(0x2F, 0);
                int x = n;
                for (; ; )
                {
                    int a = m.Rd(0x60DF + x);
                    x--;
                    if (x == 0) break;
                    int b = m.Rd(0x60DF + x);
                    if (a >= b) continue;
                    m.Wr(0x60DF + x + 1, b);
                    m.Wr(0x60DF + x, a);
                    m.Wr(0x2F, (m.Rd(0x2F) + 1) & 0xFF);
                }
            } while (m.Rd(0x2F) != 0);
            for (int i = 0; i < n; i++) m.Wr(Inv + o + i, m.Rd(0x60E0 + i));
            m.Wr(0x2D, 0);
        }
        m.Wr(0x2E, 8);
    }

    // js: game/states/equip.js reselect
    /// <summary>16:85AE: after a drop, re-find item $26 in row $2B's category -> inv_select (>= 8 -> $FF)</summary>
    private static void Reselect(Machine m)
    {
        int t = m.Rd(0x26);
        if (t == 0xFF) return;
        int k = (RowBase(m) + m.Rd(0x2B)) & 0xFF;
        int x = Prg.CpuRd(m, 0x85A6 + k), y = 0;
        for (int g = 0; g < 0x10000; g++)
        {
            int a = m.Rd(Inv + x);
            y = (y + 1) & 0xFF; x = (x + 1) & 0xFF;
            if (a == t) break;
        }
        y = (y - 1) & 0xFF;
        m.Wr(Sel + k, ((y - 8) & 0x80) != 0 ? y : 0xFF);
    }

    // js: game/states/equip.js catPtr
    private static int CatPtr(Machine m, int i)
    {
        int p = Prg.CpuRd(m, 0x9ADE + 2 * i) | Prg.CpuRd(m, 0x9ADF + 2 * i) << 8;
        m.Wr(0x18, i); m.Wr(0x28, p & 0xFF); m.Wr(0x29, p >> 8);
        return p;
    }

    // js: game/states/equip.js selToIds
    /// <summary>16:9A62: $6000-$6007 = the selected item id of each row ($FF = none)</summary>
    public static void SelToIds(Machine m)
    {
        for (int i = 0; i < 8; i++)
        {
            int p = CatPtr(m, i);
            int s = m.Rd(Sel + i);
            m.Wr(Buf + i, (s & 0x80) != 0 ? 0xFF : m.Rd(p + s));
        }
        m.Wr(0x18, 8);
    }

    // js: game/states/equip.js idsToSel
    /// <summary>16:9A91 + 9ABD: inv_select = position of $6000+row in its category after the sort ($FF = none)</summary>
    public static void IdsToSel(Machine m)
    {
        for (int i = 0; i < 8; i++)
        {
            int p = CatPtr(m, i);
            int a = m.Rd(Buf + i);
            if (a != 0xFF)
            {
                m.Wr(0x22, a);
                int lim = i < 4 ? 4 : 8;
                m.Wr(0x19, lim);
                int y = 0;
                for (; ; )
                {
                    if (a == m.Rd(p + y)) break;
                    y++;
                    if (y == lim) { y = 0xFF; break; }
                }
                a = y;
            }
            m.Wr(Sel + i, a);
        }
        m.Wr(0x18, 8);
    }

    // js: game/states/equip.js menuPalette
    /// <summary>16:821F: menu palette $6150-$615F from 16:8BFB + 16:8236[X]</summary>
    private static void MenuPalette(Machine m, int x)
    {
        int o = Prg.CpuRd(m, 0x8236 + x);
        for (int y = 0; y < 16; y++) m.Wr(0x6150 + y, Prg.CpuRd(m, 0x8BFB + o + y));
    }

    // js: game/states/equip.js drawSelIcons
    /// <summary>16:826F: the icons of the selected item of rows 0-3 of the current grid</summary>
    private static void DrawSelIcons(Machine m)
    {
        m.Wr(0x2B, 0);
        int g = m.Rd(Kind);
        do
        {
            int r = m.Rd(0x2B);
            int s = m.Rd(Sel + (g != 0 ? 4 : 0) + r);
            if ((s & 0x80) == 0)
            {
                m.Wr(0x2A, s);
                m.Wr(Cur, ((g != 0 ? r * 8 : r * 4) + s) & 0xFF);
                DrawIcon(m);
            }
            m.Wr(0x2B, r + 1);
        } while (m.Rd(0x2B) < 4);
    }

    // js: game/states/equip.js setupGrid
    /// <summary>16:81FD: grid setup: palette, CHR R0 = $5E = $3C, R1 16:8232, columns 16:8238, $5F 16:8234; 16:826C</summary>
    private static IEnumerable<Y> SetupGrid(Machine m)
    {
        int k = m.Rd(Kind);
        MenuPalette(m, k);
        m.Wr(0x58, 0x3C); m.Wr(0x5E, 0x3C);
        m.Wr(0x59, Prg.CpuRd(m, 0x8232 + k));
        m.Wr(NCol, Prg.CpuRd(m, 0x8238 + k));
        m.Wr(0x5F, Prg.CpuRd(m, 0x8234 + k));
        // s121: entered from a mode-1 screen (the pawn shop 16:950C), this frame's IRQ handler 2 (IrqMsgWindowTop)
        // writes $5E/$5F after these stores in the port: re-apply them right after that IRQ (once).
        if (m.IrqSplits.Exists(s => s.Handler == 2))
        {
            var h = m.IrqHandlers[2];
            int e = m.Rd(0x5E), f = m.Rd(0x5F);
            m.IrqHandlers[2] = (mm, line) => { h(mm, line); mm.Wr(0x5E, e); mm.Wr(0x5F, f); mm.IrqHandlers[2] = h; };
        }
        foreach (var s in HideOam(m)) yield return s;
        DrawSelIcons(m);
    }

    private static readonly int[] CellOfs = [0, 1, 2, 0x20, 0x21, 0x22, 0x40, 0x41, 0x42];
    private static readonly int[] BlankTiles = [0xF5, 0xF6, 0xF7, 0xF8, 0x20, 0xF9, 0xFA, 0xFB, 0xFC];

    // js: game/states/equip.js cellTiles
    /// <summary>16:87B8: 3x3 item picture, tiles A..A+8, at $6000 + 16:8BF3[$29]</summary>
    private static void CellTiles(Machine m, int a)
    {
        int x = Prg.CpuRd(m, 0x8BF3 + m.Rd(0x29));
        for (int i = 0; i < 9; i++) m.Wr(Buf + x + CellOfs[i], (a + i) & 0xFF);
    }

    // js: game/states/equip.js cellBlank
    /// <summary>16:87EA: empty cell (frame tiles $F5-$FC, centre $20)</summary>
    private static void CellBlank(Machine m)
    {
        int x = Prg.CpuRd(m, 0x8BF3 + m.Rd(0x29));
        for (int i = 0; i < 9; i++) m.Wr(Buf + x + CellOfs[i], BlankTiles[i]);
    }

    // js: game/states/equip.js gridBand
    /// <summary>16:8795: flush one 3-tile-row band ($60 bytes) with irq_mode b7 (no PaletteUpload), $20 += $60</summary>
    private static IEnumerable<Y> GridBand(Machine m)
    {
        m.Wr(0x29, 0);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        m.Wr(R.IrqMode, m.Rd(R.IrqMode) | 0x80);
        MenuFlush(m);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        int v = m.Rd(0x20) + 0x60;
        m.Wr(0x20, v & 0xFF);
        m.Wr(0x21, (m.Rd(0x21) + (v >> 8)) & 0xFF);
        m.Wr(0x28, (m.Rd(0x28) + 1) & 0xFF);
    }

    // js: game/states/equip.js drawGrid
    /// <summary>16:871F: the 4 grid rows ($28C0 on, 16:8AFD setup, row template 16:8BD3, pictures 16:8F5A)</summary>
    private static IEnumerable<Y> DrawGrid(Machine m)
    {
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int i = 0; i < 16; i++) m.Wr(0x20 + i, Prg.CpuRd(m, 0x8AFD + i));
        for (int y = 0; y < 0x60; y++) m.Wr(Buf + y, Prg.CpuRd(m, 0x8BD3 + (y & 0x1F)));
        m.Wr(0x29, 0); m.Wr(0x28, 0); m.Wr(0x27, 0);
        do
        {
            foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
            do
            {
                m.Wr(R.FrameReady, 0);                        // 874D (no wait: the NMI does the OAM DMA)
                int x = m.Rd(Kind) != 0 ? (m.Rd(0x27) + 0x10) & 0xFF : m.Rd(0x27);
                int a = m.Rd(Inv + x);
                if (a >= 0x4B) CellBlank(m);
                else CellTiles(m, Prg.CpuRd(m, 0x8F5A + a));
                m.Wr(0x27, (m.Rd(0x27) + 1) & 0xFF);
                m.Wr(0x29, (m.Rd(0x29) + 1) & 0xFF);
            } while (((m.Rd(0x29) - m.Rd(NCol)) & 0x80) != 0);   // CMP $6426 / BMI
            foreach (var s in GridBand(m)) yield return s;
        } while (((m.Rd(0x28) - 4) & 0x80) != 0);
        m.Wr(R.IrqMode, m.Rd(R.IrqMode) & 0x7F);
    }

    // js: game/states/equip.js frameRows
    /// <summary>16:8864: two 32-tile frame rows (FE, left, 29 x fill, right; 3 bytes of 16:8B0D each), $20 += $40</summary>
    public static IEnumerable<Y> FrameRows(Machine m, int x)
    {
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        int y = 0;
        m.Wr(0x2F, 2);
        do
        {
            m.Wr(Buf + y++, 0xFE);
            m.Wr(Buf + y++, Prg.CpuRd(m, 0x8B0D + x)); x++;
            m.Wr(0x2E, 0x1D);
            do { m.Wr(Buf + y++, Prg.CpuRd(m, 0x8B0D + x)); m.Wr(0x2E, m.Rd(0x2E) - 1); } while (m.Rd(0x2E) != 0);
            x++;
            m.Wr(Buf + y++, Prg.CpuRd(m, 0x8B0D + x)); x++;
            m.Wr(0x2F, m.Rd(0x2F) - 1);
        } while (m.Rd(0x2F) != 0);
        MenuFlush(m);
        int v = m.Rd(0x20) + 0x40;
        m.Wr(0x20, v & 0xFF);
        m.Wr(0x21, (m.Rd(0x21) + (v >> 8)) & 0xFF);
    }

    // js: game/states/equip.js drawWindow
    /// <summary>16:881E: the window ($2800, 24 rows) + its attribute table ($2BC0 from $6001, all $FF)</summary>
    private static IEnumerable<Y> DrawWindow(Machine m)
    {
        int sv = m.Rd(R.IrqMode);
        m.Wr(R.IrqMode, sv | 0x80);
        foreach (var s in FrameRows(m, 0)) yield return s;
        foreach (var s in FrameRows(m, 6)) yield return s;
        m.Wr(0x19, 9);
        do
        {
            foreach (var s in FrameRows(m, 9)) yield return s;
            m.Wr(0x19, m.Rd(0x19) - 1);
        } while (m.Rd(0x19) != 0);
        foreach (var s in FrameRows(m, 0x0C)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int x = 0x41; x > 0; x--) m.Wr(Buf + x, 0xFF);
        m.Wr(0x21, 0x2B); m.Wr(0x20, 0xC0); m.Wr(0x25, 1);
        MenuFlush(m);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        m.Wr(R.IrqMode, sv);
    }

    // js: game/states/equip.js drawLabels
    /// <summary>16:823A: the row labels (string $11, $10, then 0-3 or 4-5)</summary>
    private static IEnumerable<Y> DrawLabels(Machine m)
    {
        foreach (var s in MenuPrint(m, 0x11)) yield return s;
        foreach (var s in MenuPrint(m, 0x10)) yield return s;
        foreach (int str in m.Rd(Kind) != 0 ? new[] { 4, 5 } : new[] { 0, 1, 2, 3 })
            foreach (var s in MenuPrint(m, str)) yield return s;
    }

    // js: game/states/equip.js menuFarHud
    /// <summary>MenuFarHud 16:8613: $10-$2F saved to $60EC, FarCall_B26_8E46 (HudRedraw A, X = $20), restored</summary>
    public static IEnumerable<Y> MenuFarHud(Machine m, int a)
    {
        for (int x = 0; x < 0x20; x++) m.Wr(0x60EC + x, m.Rd(0x10 + x));
        m.Wr(0x10, a);                                        // 31:FFA9 STA $10
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);
        Prg.SetR6(m, 0x1A);
        foreach (var s in Death.HudRedraw(m, a, 0x20)) yield return s;
        Prg.FarRestore(m, r6, r7);                            // RestoreBanks 31:FFDB
        for (int x = 0; x < 0x20; x++) m.Wr(0x10 + x, m.Rd(0x60EC + x));
    }

    // js: game/states/equip.js menuSellGold
    /// <summary>MenuSellGold 16:8631: gold += price 16:9EC2[item] (through $6474, saturates at $FFFF)</summary>
    private static void MenuSellGold(Machine m)
    {
        int x = (CurItem(m).A << 1) & 0xFF;
        int v = Prg.CpuRd(m, 0x9EC2 + x) + m.Rd(0x0702) + ((Prg.CpuRd(m, 0x9EC3 + x) + m.Rd(0x0703)) << 8);
        if (v > 0xFFFF) v = 0xFFFF;
        m.Wr(0x6474, v & 0xFF); m.Wr(0x6475, v >> 8);
        m.Wr(0x0702, v & 0xFF); m.Wr(0x0703, v >> 8);
    }

    // js: game/states/equip.js priceLine
    /// <summary>16:81BE / 16:84C4: shop price line: $6474 = 16:9EC2[item], "$" (string $33 / $34 by grid), digits =
    /// HUD element $0D</summary>
    private static IEnumerable<Y> PriceLine(Machine m, bool lookupFirst)
    {
        // js: game/states/equip.js lookup
        void Lookup()
        {
            int x = (CurItem(m).A << 1) & 0xFF;
            m.Wr(0x6474, Prg.CpuRd(m, 0x9EC2 + x)); m.Wr(0x6475, Prg.CpuRd(m, 0x9EC3 + x));
        }
        if (lookupFirst) Lookup();                            // 81BE: price, then "$"
        foreach (var s in MenuPrint(m, m.Rd(Kind) != 0 ? 0x34 : 0x33)) yield return s;
        if (!lookupFirst) Lookup();                           // 84B7: "$", then price
        foreach (var s in MenuFarHud(m, 0x0D)) yield return s;
    }

    // js: game/states/equip.js shopAgain
    /// <summary>16:847B: "YES NO"; NO -> "Please come again.", wait for any key, PLA PLA RTS = leave EquipMenu
    /// (exit.V = true); YES -> the cursor item's name + price again</summary>
    private static IEnumerable<Y> ShopAgain(Machine m, Ret<bool> exit)
    {
        var no = new Ret<bool>();
        foreach (var s in Confirm(m, 0xA0, no)) yield return s;
        if (no.V)
        {
            MenuSnd(m, 0x4C);
            foreach (var s in MenuPrint(m, 0x0F)) yield return s;
            foreach (var s in MenuPrint(m, 0x2C)) yield return s;
            do
            {
                foreach (var s in Death.WaitFrame(m)) yield return s;
                Pads.ReadPadsMenu(m);
            } while (m.Rd(R.PadNew) == 0);
            exit.V = true;
            yield break;
        }
        MenuSnd(m, 0x4C);                                     // 849E
        foreach (var s in MenuPrint(m, 0x0F)) yield return s;
        var (a, _) = CurItem(m);
        if (a == 0xFF) yield break;
        m.Wr(0x19, a);
        foreach (var s in MenuPrint(m, 6)) yield return s;
        foreach (var s in PriceLine(m, false)) yield return s;
    }

    // js: game/states/equip.js pressA
    /// <summary>16:82B7 (A): equip the cursor item in its row, or unequip it if it is the selected one</summary>
    private static IEnumerable<Y> PressA(Machine m, Ret<bool> exit)
    {
        if (m.Rd(Shop) != 0)
        {                                                     // 82BC JMP 835E (shop: A = sell)
            foreach (var s in PressB(m, exit, true)) yield return s;
            yield break;
        }
        var (a, _) = CurItem(m);
        if (a == 0xFF)
        {
            MenuSnd(m, 0x4E);
            foreach (var s in MenuPrint(m, 0x0F)) yield return s;
            yield break;
        }
        int x = (RowBase(m) + m.Rd(0x2B)) & 0xFF;
        int sl = m.Rd(Sel + x);
        if ((sl & 0x80) == 0 && sl == m.Rd(0x2A))
        {                                                     // 82E6: unequip
            m.Wr(Sel + x, sl | 0x80);
            MenuSnd(m, 0x4B);
            foreach (var s in EraseRowIcon(m, x)) yield return s;
            m.Wr(0x0711 + RowSlot(m), 0);
            foreach (var s in MenuPrint(m, 0x0F)) yield return s;
            yield break;
        }
        m.Wr(Sel + x, m.Rd(0x2A));                            // 8304: equip
        DrawIcon(m);
        MenuSnd(m, 0x4C);
        var (b, _) = CurItem(m);
        m.Wr(0x19, b);
        foreach (var s in MenuPrint(m, (Prg.CpuRd(m, 0x8FF0 + b) & 0x20) != 0 ? 9 : 0x0A)) yield return s;
        int slot = RowSlot(m);
        var (c, _) = CurItem(m);
        m.Wr(0x0711 + slot, Prg.CpuRd(m, 0x903B + c));
    }

    // js: game/states/equip.js pressB
    /// <summary>16:8342 (B): drop the cursor item after "YES/NO" (flag 16:8FF0 b6 = cannot drop)</summary>
    private static IEnumerable<Y> PressB(Machine m, Ret<bool> exit, bool fromA = false)
    {
        if (!fromA && m.Rd(Shop) != 0)
        {                                                     // 8347: shop B = "Anything else?" -> 847B
            MenuSnd(m, 0x4C);
            foreach (var s in MenuPrint(m, 0x0F)) yield return s;
            foreach (var s in MenuPrint(m, 0x31)) yield return s;
            foreach (var s in MenuPrint(m, 0x0E)) yield return s;
            foreach (var s in ShopAgain(m, exit)) yield return s;
            yield break;
        }
        var (a, xi) = CurItem(m);                             // 835E
        if (a == 0xFF) { MenuSnd(m, 0x4E); yield break; }
        m.Wr(0x2C, xi); m.Wr(0x19, a); m.Wr(0x646E, a);
        if ((Prg.CpuRd(m, 0x8FF0 + a) & 0x40) != 0)
        {
            MenuSnd(m, 0x4E);
            foreach (var s in MenuPrint(m, 0x0F)) yield return s;
            foreach (var s in MenuPrint(m, m.Rd(Shop) != 0 ? 0x0D : 8)) yield return s;
            yield break;
        }
        MenuSnd(m, 0x4C);
        foreach (var s in MenuPrint(m, 0x0F)) yield return s;
        foreach (var s in MenuPrint(m, m.Rd(Shop) != 0 ? 0x0B : 7)) yield return s;
        foreach (var s in MenuPrint(m, 0x0E)) yield return s;
        var no = new Ret<bool>();
        foreach (var s in Confirm(m, 0xA0, no)) yield return s;
        if (no.V)
        {                                                     // NO
            MenuSnd(m, 0x4B);
            foreach (var s in MenuPrint(m, 0x0F)) yield return s;
            yield break;
        }
        MenuSnd(m, 0x4C);
        if (m.Rd(Shop) != 0)
        {                                                     // 83D3: gold digits (HUD element 1)
            MenuSellGold(m);
            foreach (var s in MenuFarHud(m, 1)) yield return s;
        }
        int k = (RowBase(m) + m.Rd(0x2B)) & 0xFF;             // 83DB
        m.Wr(0x26, m.Rd(Inv + ((Prg.CpuRd(m, 0x85A6 + k) + m.Rd(Sel + k)) & 0xFF)));
        if ((m.Rd(Sel + k) & 7) == m.Rd(0x2A))
        {                                                     // dropping the equipped item
            m.Wr(Sel + k, 0xFF);
            m.Wr(0x26, 0xFF);
            int slot = RowSlot(m);
            m.Wr(0x0711 + slot, 0);
            foreach (var s in Death.WaitFrame(m)) yield return s;
            m.Wr(0x607F, m.Rd(Cur));
            foreach (var s in EraseRowIcon(m, slot)) yield return s;
            m.Wr(Cur, m.Rd(0x607F));
        }
        foreach (var s in MenuPrint(m, 0x0F)) yield return s;   // 8423
        if (m.Rd(Shop) == 0) foreach (var s in MenuPrint(m, 0x12)) yield return s;
        m.Wr(Inv + m.Rd(0x2C), 0xFF);
        m.Wr(0x14, m.Rd(0x2A)); m.Wr(0x15, m.Rd(0x2B));
        int cur = m.Rd(Cur);
        SortInventory(m);
        Reselect(m);
        m.Wr(R.FrameReady, m.Rd(R.FrameReady) & 0xFE);
        DrawSelIcons(m);
        foreach (var s in DrawGrid(m)) yield return s;
        m.Wr(Cur, cur);
        m.Wr(0x2A, m.Rd(0x14)); m.Wr(0x2B, m.Rd(0x15));
        foreach (var s in DrawLabels(m)) yield return s;
        if (m.Rd(Shop) == 0) yield break;
        foreach (var s in MenuPrint(m, 0x0F)) yield return s;   // 846C: "Anything else?" YES NO
        foreach (var s in MenuPrint(m, 0x0C)) yield return s;
        foreach (var s in MenuPrint(m, 0x0E)) yield return s;
        foreach (var s in ShopAgain(m, exit)) yield return s;
    }

    private enum Lbl { Grid, Moved, Loop, Ab, Flip }

    // js: game/states/equip.js equipMenu
    /// <summary>bank 16 menu entry 16:8000 (JS returns undefined on every exit)</summary>
    public static IEnumerable<Y> EquipMenu(Machine m)
    {
        foreach (var s in HideOam(m)) yield return s;
        foreach (var s in Death.WaitFrame(m)) yield return s;
        m.Wr(0x7000, 0);
        for (int i = 0; i < 16; i++) m.Wr(0x20 + i, Prg.CpuRd(m, 0x8AED + i));
        for (int i = 0; i < 4; i++) m.Wr(0x5A + i, 0x58 + i);
        m.Wr(0x5E, 0x3C); m.Wr(0x5F, 0x38);
        foreach (var s in DrawWindow(m)) yield return s;
        foreach (var s in Death.WaitFrame(m)) yield return s;
        m.Wr(R.IrqMode, 6);
        m.Wr(R.PpuctrlShadow, (m.Rd(R.PpuctrlShadow) & 0xFC) | 2);
        for (int a = 0x6140; a < 0x6160; a += 4) m.Wr(a, 0x0F);
        foreach (int a in new[] { 0x6424, 0x6425, 0x6426, 0x6427, 0x646E, 0x646F }) m.Wr(a, 0);
        m.Wr(R.PadNew, 0);
        SelToIds(m);
        SortInventory(m);
        IdsToSel(m);
        int grid = 0;
        var lbl = Lbl.Grid;
        var exit = new Ret<bool>();
        for (; ; )
        {
            if (lbl == Lbl.Grid)
            {                                                 // 807A / 807E
                m.Wr(Kind, grid);
                int sv = m.Rd(0x7000);
                if (sv == 0xFA)
                {
                    MenuSnd(m, 0x4C);
                    foreach (var s in HideOam(m)) yield return s;
                    yield break;
                }
                if (sv == 0xFB || sv == 0xAB)
                {
                    MenuSnd(m, sv == 0xFB ? 0x55 : 0x4C);
                    foreach (var s in HideOam(m)) yield return s;
                    foreach (var s in SaveLoad.MenuExitFade(m)) yield return s;   // JMP 30:C80C (RTS -> 30:D8E5)
                    yield break;
                }
                m.Wr(R.IrqMode, 0x86);
                int c2A = m.Rd(0x2A), c2B = m.Rd(0x2B), cur = m.Rd(Cur);
                foreach (var s in SetupGrid(m)) yield return s;
                foreach (var s in DrawGrid(m)) yield return s;
                m.Wr(Cur, cur); m.Wr(0x2B, c2B); m.Wr(0x2A, c2A);
                foreach (var s in DrawLabels(m)) yield return s;
                m.Wr(R.IrqMode, 6);
                lbl = Lbl.Moved;
            }
            else if (lbl == Lbl.Moved)
            {                                                 // 8171
                m.Wr(0x646E, CurItem(m).A);
                MenuSnd(m, 0x4B);
                int r = m.Rd(0x2B), c = m.Rd(0x2A);
                m.Wr(Cur, ((m.Rd(Kind) != 0 ? r * 8 : r * 4) + c) & 0xFF);
                DrawCursor(m);
                var (a, _) = CurItem(m);
                if (a == 0xFF)
                {
                    foreach (var s in MenuPrint(m, 0x0F)) yield return s;
                    lbl = Lbl.Loop; continue;
                }
                m.Wr(0x19, a);
                foreach (var s in MenuPrint(m, 0x0F)) yield return s;
                if (m.Rd(Shop) != 0)
                {                                             // 81B4: name + price
                    foreach (var s in MenuPrint(m, 0x0F)) yield return s;
                    foreach (var s in MenuPrint(m, 6)) yield return s;
                    foreach (var s in PriceLine(m, true)) yield return s;
                }
                else foreach (var s in MenuPrint(m, 6)) yield return s;
                lbl = Lbl.Ab;
            }
            else if (lbl == Lbl.Loop)
            {                                                 // 80D1
                foreach (var s in Death.WaitFrame(m)) yield return s;
                Pads.ReadPadsMenu(m);
                int p = m.Rd(R.PadNew);
                // js: game/states/equip.js clamp
                void Clamp() { if (((m.Rd(0x2A) - 4) & 0x80) == 0) m.Wr(0x2A, 3); }
                if ((p & 0x08) != 0)
                {                                             // UP
                    int r = (m.Rd(0x2B) - 1) & 0xFF;
                    m.Wr(0x2B, r);
                    if ((r & 0x80) == 0) { lbl = Lbl.Moved; continue; }
                    m.Wr(0x2B, 3); Clamp(); lbl = Lbl.Flip; continue;
                }
                if ((p & 0x04) != 0)
                {                                             // DOWN
                    int r = (m.Rd(0x2B) + 1) & 0xFF;
                    m.Wr(0x2B, r);
                    if (r < 4) { lbl = Lbl.Moved; continue; }
                    m.Wr(0x2B, 0); Clamp(); lbl = Lbl.Flip; continue;
                }
                if ((p & 0x02) != 0)
                {                                             // LEFT
                    int c = (m.Rd(0x2A) - 1) & 0xFF;
                    m.Wr(0x2A, (c & 0x80) != 0 ? 0 : c);
                    lbl = Lbl.Moved; continue;
                }
                if ((p & 0x01) != 0)
                {                                             // RIGHT
                    int c = m.Rd(0x2A);
                    m.Wr(0x2A, (c + 1) & 0xFF);
                    if (m.Rd(0x2A) >= m.Rd(NCol)) m.Wr(0x2A, c);
                    lbl = Lbl.Moved; continue;
                }
                if ((p & 0x20) != 0) yield break;             // SELECT: 81EC RTS
                if ((p & 0x10) != 0 && m.Rd(Shop) == 0)
                {                                             // START: 16:821F(0), JMP SaveLoadPage
                    MenuPalette(m, 0);
                    foreach (var s in SaveLoad.SaveLoadPage(m)) yield return s;   // s102
                    grid = m.Rd(Kind);                        // SaveLoadExit: JMP 807E if $6424 else 807A
                    lbl = Lbl.Grid;
                    continue;
                }
                lbl = Lbl.Ab;
            }
            else if (lbl == Lbl.Ab)
            {                                                 // 815C
                if ((m.Rd(R.PadNew) & 0x80) != 0)
                {
                    foreach (var s in PressA(m, exit)) yield return s;
                    if (exit.V) yield break;                  // shop PLA PLA RTS
                }
                if ((m.Rd(R.PadNew) & 0x40) != 0)
                {
                    foreach (var s in PressB(m, exit)) yield return s;
                    if (exit.V) yield break;
                }
                lbl = Lbl.Loop;
            }
            else
            {                                                 // Flip 81ED
                foreach (var s in MenuPrint(m, 0x0F)) yield return s;
                grid = m.Rd(Kind) != 0 ? 0 : 1;
                lbl = Lbl.Grid;
            }
        }
    }

    // js: game/states/equip.js selectedUseItem
    /// <summary>SelectedUseItem 30:DB0E: $0715 = selected consumable ($642C -> $6440) or key item ($642E -> $6450) - $1C</summary>
    private static void SelectedUseItem(Machine m)
    {
        int y = m.Rd(0x642C), a;
        if ((y & 0x80) == 0) a = m.Rd(0x6440 + y);
        else
        {
            y = m.Rd(0x642E);
            if ((y & 0x80) != 0) return;
            a = m.Rd(0x6450 + y);
        }
        m.Wr(0x0715, (a - 0x1C) & 0xFF);
    }

    // js: game/states/equip.js swordPowerLevel
    /// <summary>SwordPowerLevel 30:D9D8: Y = 0/1/2</summary>
    public static int SwordPowerLevel(Machine m)
    {
        int sw = m.Rd(0x0711);
        if (sw == 0) return 0;
        int ball = m.Rd(0x0718);
        if (ball == 0)
        {                                                 // D9FC
            int a = m.Rd(0x643B + sw);
            if (a == 0xFF) return 0;
            return (a & 1) != 0 ? 0 : 1;
        }
        m.Wr(0x10, sw - 1);
        if (((ball - 1) & 3) != sw - 1) return 0;
        return ((ball - 1) & 0x0C) != 0 ? 2 : 1;
    }

    // js: game/states/equip.js substEquipMenu
    /// <summary>SubstEquipMenu 30:D8C7 (sub_state $12, SELECT in play). EquipMenu returns undefined on every exit in
    /// the JS, so its `if (exit) for (;;) yield` stop is dead code and not carried over.</summary>
    public static IEnumerable<Y> SubstEquipMenu(Machine m)
    {
        if (m.Rd(R.AreaId) == 0x5F) m.Wr(R.IrqMode, 0);
        int sword = m.Rd(0x0711), im = m.Rd(R.IrqMode);
        Prg.SetR6(m, 0x10);
        Prg.SetR7(m, 0x17);
        foreach (var s in EquipMenu(m)) yield return s;
        foreach (var s in Death.WaitFrame(m)) yield return s;   // D8E7
        Pads.ReadPads(m);
        for (int x = 0; x < 0x100; x++) m.Wr(0x0200 + x, 0xF0);
        SelectedUseItem(m);
        m.Wr(0x0719, SwordPowerLevel(m));
        m.Wr(R.IrqMode, im);
        int xReg = 0;
        if (sword != m.Rd(0x0711))
        {                                                     // 30:DAFC: orbit level 0, slots 4-11 off
            m.Wr(0x06C0, 0);
            for (int x = 4; x < 0x0C; x++) m.Wr(0x04A0 + x, 0);
            xReg = 0x0C;
        }
        if (m.Rd(R.AreaId) == 0x5F) foreach (var s in SaveLoad.SubstRestoreScreen(m)) yield return s;   // D912 (s120)
        for (int y = 0x0D; y < 0x1F; y++)
        {                                                     // 30:DADA: drop the $EB/$EC objects with tile ofs 0
            if (m.Rd(0x04A0 + y) == 0) continue;
            int t = m.Rd(0x0300 + y);
            if ((t == 0xEB || t == 0xEC) && m.Rd(0x0320 + y) == 0) m.Wr(0x04A0 + y, 0);
        }
        if (m.Rd(0x7000) == 0xFA)
        {                                                     // D930: after a menu LOAD (s102)
            foreach (var s in SaveLoad.LoadTail(m)) yield return s;
            yield break;
        }
        foreach (var s in Death.StatRecalc(m, xReg)) yield return s;
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);                 // FarCall_B14_800E
        Prg.SwitchBank16(m, 0x0E);
        Sprites.SpriteDraw(m);
        Prg.FarRestore(m, r6, r7);
        Prg.SetR6(m, 0x1A);
        Death.PaletteBuildRom(m, 0);                          // JSR 26:8C0E
        m.Wr(R.PadNew, 0);                                    // 30:D8BE
        m.Wr(R.SubState, 8);
    }
}
