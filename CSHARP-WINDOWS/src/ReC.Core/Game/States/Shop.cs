// re-c C# port — shops (= web/src/game/states/shop.js, s121-s124): the walk in / walk out shared by sub_states 32-36,
// the pawn shop (35), armor / tool shops (32 / 33, buy loop 16:96CF), the inn (34) and the empty shop (36).
// REVERSE.md "Substates" rows 32-36 and the Verification Log rows "Pawn shop", "Armor / tool shops", "Inn", "Empty shop".
//
//   SubstPawnShop(m)   SubstPawnShop 30:D9AD = ShopWalkIn, 16:9509 (JMP 16:967A), ShopWalkOut
//   ShopWalkIn(m)      ShopWalkIn 30:DA0C   (player walks down to y = $98, then right to x = $78; SwitchBank16(8))
//   ShopWalkOut(m)     ShopWalkOut 30:DA72  (left to x = $68, up to y = $8F, ExitLookup -> area, sub_state 1)
// The pawn screen reuses the bank 16 menu (Equip.EquipMenu) with $07DD = $FF.
// The JS `pr` helper (two identical closures in buyLoop / innBody) is one static Pr here.
using ReC.Game.Obj;
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class Shop
{
    private const int Goods = 0x6470, Prices = 0x6478, Cur = 0x6427, Item = 0x646C;

    // js: game/states/shop.js inc
    private static void Inc(Machine m, int a) => m.Wr(a, (m.Rd(a) + 1) & 0xFF);

    // js: game/states/shop.js dec
    private static void Dec(Machine m, int a) => m.Wr(a, (m.Rd(a) - 1) & 0xFF);

    // js: game/states/shop.js spriteDrawFar
    /// <summary>FarCall_B14_800E 31:FF8C: SpriteDraw with banks 28/29, then the caller's banks back</summary>
    private static void SpriteDrawFar(Machine m)
    {
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);
        Prg.SwitchBank16(m, 0x0E);
        Sprites.SpriteDraw(m);
        Prg.FarRestore(m, r6, r7);
    }

    // js: game/states/shop.js clearOrbitSlots
    /// <summary>ClearOrbitSlots 30:DAFC</summary>
    private static void ClearOrbitSlots(Machine m)
    {
        m.Wr(0x06C0, 0);
        for (int x = 4; x < 0x0C; x++) m.Wr(0x04A0 + x, 0);
    }

    private static IEnumerable<Y> PlayerUpdate1(Machine m) => Player.PlayerUpdate(m, 1, new AiRet()) ?? [];

    // js: game/states/shop.js walkStep
    /// <summary>30:DABF: one walk frame: WaitFrame, PlayerUpdate (slot 1, banks 26/27), obj_anim_s01 -= 1,
    /// obj_flags[1] &amp;= $0B, sprites</summary>
    private static IEnumerable<Y> WalkStep(Machine m)
    {
        foreach (var s in Death.WaitFrame(m)) yield return s;
        Prg.SwitchBank16(m, 0x0D);
        foreach (var s in PlayerUpdate1(m)) yield return s;
        Dec(m, 0x04E1);
        m.Wr(0x0381, m.Rd(0x0381) & 0x0B);
        SpriteDrawFar(m);
    }

    // js: game/states/shop.js shopWalkIn
    /// <summary>ShopWalkIn 30:DA0C</summary>
    public static IEnumerable<Y> ShopWalkIn(Machine m)
    {
        // ShopDoorOpenStub 31:FE00 = RTS
        ClearOrbitSlots(m);
        Prg.SetR6(m, 0x1A);
        Death.PaletteBuildRom(m, 0);                          // JSR 26:8C0E
        m.Wr(0x58, 0x3C); m.Wr(0x59, 0x38);                   // chr_r0_alt / chr_r1_alt
        m.Wr(0x0360, 4);                                      // obj_meta_dir: down
        m.Wr(0x0301, 2);
        m.Wr(0x0600, 0); m.Wr(0x0620, 0); m.Wr(0x06C0, 0); m.Wr(0x0BB2, 0);
        Prg.SwitchBank16(m, 0x0D);
        foreach (var s in PlayerUpdate1(m)) yield return s;   // DA41 X = 1, JSR 26:9B45
        do
        {                                                     // DA46
            m.Wr(0x0600, 0); m.Wr(0x0620, 0);
            foreach (var s in WalkStep(m)) yield return s;
            Inc(m, 0xB1); Inc(m, 0xB0);
        } while (m.Rd(0xB0) != 0x98);
        m.Wr(0x0360, 2);                                      // DA5B: right
        do
        {
            foreach (var s in WalkStep(m)) yield return s;
            Inc(m, 0x71); Inc(m, 0x70);
        } while (m.Rd(0x70) != 0x78);
        Prg.SwitchBank16(m, 8);                               // banks 16/17
    }

    // js: game/states/shop.js shopWalkOut
    /// <summary>ShopWalkOut 30:DA72 (the x loop branches back to DA72, so the palette build + irq_mode 0 run each step)</summary>
    public static IEnumerable<Y> ShopWalkOut(Machine m)
    {
        do
        {
            // ShopDoorCloseStub 31:FE17 = RTS
            Prg.SetR6(m, 0x1A);
            Death.PaletteBuildRom(m, 0);
            m.Wr(R.IrqMode, 0);
            m.Wr(0x0360, 6);                                  // left
            foreach (var s in WalkStep(m)) yield return s;
            Dec(m, 0x71); Dec(m, 0x70);
        } while (m.Rd(0x70) != 0x68);
        m.Wr(0x0360, 0);                                      // DA93: up
        do
        {
            foreach (var s in WalkStep(m)) yield return s;
            Dec(m, 0xB1); Dec(m, 0xB0);
        } while (m.Rd(0xB0) != 0x8F);
        SpriteDrawFar(m);                                     // DAA5
        var (_, y) = Camera.ExitScan(m);                      // DAA8 ExitLookup (sub_state != 8: no load)
        m.Wr(R.AreaId, Prg.IndY(m, 0x10, y));
        m.Wr(0x6D, Prg.IndY(m, 0x10, (y + 1) & 0xFF));
        m.Wr(R.IrqMode, 0);
        m.Wr(R.PadNew, 0);
        m.Wr(R.SubState, 1);
    }

    // js: game/states/shop.js shopWindow
    /// <summary>16:9912: the shop window ($2800: frame rows 6, 9 x3, $0C) + its attribute bytes ($2BC0, all $FF)</summary>
    private static IEnumerable<Y> ShopWindow(Machine m)
    {
        m.Wr(0x20, 0); m.Wr(0x25, 0); m.Wr(0x21, 0x28); m.Wr(0x24, 0x40);
        foreach (var s in Equip.FrameRows(m, 6)) yield return s;
        m.Wr(0x19, 3);
        do
        {
            foreach (var s in Equip.FrameRows(m, 9)) yield return s;
            Dec(m, 0x19);
        } while (m.Rd(0x19) != 0);
        foreach (var s in Equip.FrameRows(m, 0x0C)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int x = 0x41; x > 0; x--) m.Wr(0x6000 + x, 0xFF);
        m.Wr(0x21, 0x2B); m.Wr(0x20, 0xC0); m.Wr(0x25, 1);
        Equip.MenuFlush(m);
    }

    // js: game/states/shop.js shopSprPalette
    /// <summary>16:9A12: sprite palette 1 $6154-$6157 = 16:8BFB[A*4] (tool shop, sub_state $21: 16:8C0B)</summary>
    private static void ShopSprPalette(Machine m, int a)
    {
        int @base = m.Rd(R.SubState) == 0x21 ? 0x8C0B : 0x8BFB;
        for (int y = 0; y < 4; y++) m.Wr(0x6154 + y, Prg.CpuRd(m, @base + ((a << 2) & 0xFF) + y));
    }

    // js: game/states/shop.js shopSetup
    /// <summary>16:950C: shop screen setup (inventory sort, CHR alt R2-R5 $58-$5B, window, irq_mode 1, $07DD = $FF)</summary>
    private static IEnumerable<Y> ShopSetup(Machine m)
    {
        Equip.SelToIds(m); Equip.SortInventory(m); Equip.IdsToSel(m);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        for (int i = 0; i < 4; i++) m.Wr(0x5A + i, 0x58 + i);
        m.Wr(0x5E, 0x3C); m.Wr(0x5F, 0x38);
        foreach (var s in ShopWindow(m)) yield return s;
        foreach (var s in Death.WaitFrame(m)) yield return s;
        m.Wr(R.IrqMode, 1);
        foreach (int a in new[] { 0x6424, 0x6425, 0x6426, 0x6427, 0x646E, 0x646F }) m.Wr(a, 0);
        m.Wr(0x07DD, 0xFF);
        ShopSprPalette(m, 0);
        Equip.SortInventory(m);                               // JMP InvSortAll
    }

    // js: game/states/shop.js shopIdForArea
    /// <summary>ShopIdForArea 16:9953: shop_id $646D = ShopAreaIds 16:9F75[index of area_id in 16:9F54], or $21</summary>
    private static void ShopIdForArea(Machine m)
    {
        int a = m.Rd(R.AreaId);
        for (int x = 0; x < 0x21; x++)
        {
            if (Prg.CpuRd(m, 0x9F54 + x) == a) { m.Wr(0x646D, Prg.CpuRd(m, 0x9F75 + x)); return; }
        }
        m.Wr(0x646D, 0x21);
    }

    // js: game/states/shop.js pr
    /// <summary>MenuPrint of each string in turn (the JS buyLoop / innBody `pr` closures)</summary>
    private static IEnumerable<Y> Pr(Machine m, params int[] strs)
    {
        foreach (int a in strs) foreach (var s in Equip.MenuPrint(m, a)) yield return s;
    }

    // js: game/states/shop.js pawnShop
    /// <summary>16:967A (via 16:9509): "PAWN SHOP" / "Is there anything you wish to sell?" YES NO; YES -> EquipMenu in
    /// shop mode; NO -> strings $2B $2A $20 + wait for any key (16:9878)</summary>
    private static IEnumerable<Y> PawnShop(Machine m)
    {
        Equip.SelToIds(m); Equip.SortInventory(m); Equip.IdsToSel(m);
        foreach (var s in ShopSetup(m)) yield return s;
        ShopIdForArea(m);
        foreach (var s in Pr(m, 0x1B, 0x27, 0x28, 0x29)) yield return s;
        var no = new Ret<bool>();
        foreach (var s in Equip.Confirm(m, 0x41, no)) yield return s;
        if (no.V)
        {                                                     // C set = NO
            Equip.MenuSnd(m, 0x4C);
            foreach (var s in Pr(m, 0x2B, 0x2A, 0x20)) yield return s;
            do
            {
                foreach (var s in Death.WaitFrame(m)) yield return s;
                Pads.ReadPadsMenu(m);
            } while (m.Rd(R.PadNew) == 0);                    // 16:9878 / 9884
            Equip.MenuSnd(m, 0x4B);
        }
        else
        {
            Equip.MenuSnd(m, 0x4C);                           // 96BE
            foreach (var s in Equip.EquipMenu(m)) yield return s;
        }
        foreach (var s in Equip.HideOam(m)) yield return s;   // 96C6 MenuHideOam
        m.Wr(0x07DD, 0);
    }

    // js: game/states/shop.js substPawnShop
    /// <summary>SubstPawnShop 30:D9AD (sub_state $23, keeper type $43)</summary>
    public static IEnumerable<Y> SubstPawnShop(Machine m)
    {
        foreach (var s in ShopWalkIn(m)) yield return s;
        foreach (var s in PawnShop(m)) yield return s;
        foreach (var s in ShopWalkOut(m)) yield return s;
    }

    // ---- armor / tool shops (session 122): 16:9558 / 16:957A, the buy loop 16:96CF ----

    // js: game/states/shop.js waitAnyKey
    /// <summary>MenuWaitAnyKey 16:9878: MenuWaitPad 16:9884 (WaitFrame + ReadPadsMenu) until pad_new != 0, MenuSfx($4B)</summary>
    private static IEnumerable<Y> WaitAnyKey(Machine m)
    {
        do
        {
            foreach (var s in Death.WaitFrame(m)) yield return s;
            Pads.ReadPadsMenu(m);
        } while (m.Rd(R.PadNew) == 0);
        Equip.MenuSnd(m, 0x4B);
    }

    // js: game/states/shop.js shopSetupGoods
    /// <summary>ShopSetupArmor 16:988A / ShopSetupTool 16:98E3: goods (4) + prices (8 bytes) for shop_id, then the four
    /// 3x3 pictures (16:98B6: column 16:9F96[i], row 3, tiles 16:8F5A[item] or $FF = blank)</summary>
    private static IEnumerable<Y> ShopSetupGoods(Machine m, int goodsTab, int priceTab)
    {
        ShopIdForArea(m);
        int id = m.Rd(0x646D);
        for (int y = 0; y < 4; y++) m.Wr(Goods + y, Prg.CpuRd(m, goodsTab + ((id << 2) & 0xFF) + y));
        for (int y = 0; y < 8; y++) m.Wr(Prices + y, Prg.CpuRd(m, priceTab + ((id << 3) & 0xFF) + y));
        for (int i = 0; i < 4; i++)
        {
            m.Wr(0x19, i);
            m.Wr(0x26, Prg.CpuRd(m, 0x9F96 + i)); m.Wr(0x27, 3);
            int a = m.Rd(Goods + i);
            m.Wr(0x29, a == 0xFF ? 0xFF : Prg.CpuRd(m, 0x8F5A + a));
            foreach (var s in SaveLoad.SlotPicture(m)) yield return s;
        }
    }

    // js: game/states/shop.js cursorSpr
    /// <summary>16:9815: the cursor arrow (shape 0) under goods slot $6427 (x = 16:9874[$6427], OAM slot $3B + 1)</summary>
    private static void CursorSpr(Machine m)
    {
        m.Wr(0x60FB, Prg.CpuRd(m, 0x9874 + m.Rd(Cur)));
        m.Wr(0x60F8, 0x21); m.Wr(0x12, 0); m.Wr(0x60F9, 0); m.Wr(0x60FA, 1);
        m.Wr(0x2E, 0x3B);
        Equip.MenuSprDraw(m);
    }

    // js: game/states/shop.js itemSpr
    /// <summary>16:9837: the item sprite (shape 2, tile 16:8FA5[item], OAM slot $37 + 1) + its palette; nothing for $FF</summary>
    private static void ItemSpr(Machine m)
    {
        int a = m.Rd(Goods + m.Rd(Cur));
        if (a == 0xFF) return;
        m.Wr(0x60F9, Prg.CpuRd(m, 0x8FA5 + a));
        m.Wr(0x60FB, Prg.CpuRd(m, 0x9874 + m.Rd(Cur)));
        m.Wr(0x60F8, 0x21); m.Wr(0x60FA, 1); m.Wr(0x12, 2); m.Wr(0x2E, 0x37);
        Equip.MenuSprDraw(m);
        ShopSprPalette(m, Prg.CpuRd(m, 0x8FF0 + a) & 3);
    }

    // js: game/states/shop.js shopPriceSel
    /// <summary>ShopPriceSel 16:9A37: $6474/75 = shop_prices[$6427] (0 for an empty slot)</summary>
    private static void ShopPriceSel(Machine m)
    {
        int c = m.Rd(Cur), x = (c << 1) & 0xFF;
        bool full = m.Rd(Goods + c) != 0xFF;
        m.Wr(0x6474, full ? m.Rd(Prices + x) : 0);
        m.Wr(0x6475, full ? m.Rd(Prices + x + 1) : 0);
    }

    // js: game/states/shop.js shopBuy
    /// <summary>16:9970: buy $646C for $6474/75: 1 = not enough gold, 2 = no free inventory slot, 0 = bought (see the JS)</summary>
    private static int ShopBuy(Machine m)
    {
        int g = m.Rd(0x0702) + (m.Rd(0x0703) << 8);
        int p = m.Rd(0x6474) + (m.Rd(0x6475) << 8);
        int d = (g - p) & 0xFFFF;
        m.Wr(0x6476, d & 0xFF); m.Wr(0x6477, d >> 8);
        if (g < p) return 1;
        int item = m.Rd(Item);
        int @base, n, fullX = -1;
        if (m.Rd(R.SubState) == 0x21)
        {
            if (item == 0x29) { @base = 0x18; n = 8; fullX = 7; } else { @base = 0x10; n = 8; }
        }
        else if (((item - 0x15) & 0x80) == 0) { @base = 4; n = 4; }
        else { @base = 8; n = 4; }
        m.Wr(0x2E, @base);
        int x = 0;
        while (x < n && m.Rd(0x6430 + @base + x) != 0xFF) x++;
        if (x == n) { if (fullX < 0) return 2; x = fullX; }
        m.Wr(0x2F, x);
        m.Wr(0x0702, m.Rd(0x6476)); m.Wr(0x0703, m.Rd(0x6477));
        m.Wr(0x6430 + ((@base + x) & 0xFF), item);
        return 0;
    }

    private enum Step { None, Move, Again, Ask, Price }

    // js: game/states/shop.js buyLoop
    /// <summary>16:96CF: "Buy something?" YES/NO, then the buy loop (see the JS)</summary>
    private static IEnumerable<Y> BuyLoop(Machine m)
    {
        // js: game/states/shop.js leave
        IEnumerable<Y> Leave()
        {                                                     // 97DD
            Equip.MenuSnd(m, 0x4C);
            foreach (var s in Pr(m, 0x2A, 0x26)) yield return s;
            foreach (var s in WaitAnyKey(m)) yield return s;
        }
        var no = new Ret<bool>();
        foreach (var s in Equip.Confirm(m, 0x41, no)) yield return s;
        if (no.V)
        {
            foreach (var s in Leave()) yield return s;
            yield break;
        }
        Equip.MenuSnd(m, 0x4C);
        CursorSpr(m); ItemSpr(m);
        foreach (var s in Pr(m, 0x2A)) yield return s;
        m.Wr(0x19, m.Rd(Goods));
        foreach (var s in Pr(m, 0x1D, 0x32)) yield return s;
        for (; ; )
        {
            foreach (var s in Death.WaitFrame(m)) yield return s;   // 96F8 MenuWaitPad
            Pads.ReadPadsMenu(m);
            m.Wr(Item, m.Rd(Goods + m.Rd(Cur)));
            int p = m.Rd(R.PadNew);
            var step = Step.None;                             // Move (97EF) / Again (973A) / Ask (97A4) / Price (97CD)
            if ((p & 0x02) != 0) { m.Wr(Cur, (m.Rd(Cur) - 1) & 3); step = Step.Move; }
            else if ((p & 0x01) != 0) { m.Wr(Cur, (m.Rd(Cur) + 1) & 3); step = Step.Move; }
            else if ((p & 0x20) != 0) yield break;            // SELECT: RTS
            else if ((p & 0x40) != 0) step = Step.Again;
            else if ((p & 0x80) == 0) step = Step.Price;
            else
            {
                Equip.MenuSnd(m, 0x4C);
                int r = ShopBuy(m);
                if ((m.Rd(0x6474) | m.Rd(0x6475)) == 0) step = Step.Again;
                else
                {
                    if (r == 1) foreach (var s in Pr(m, 0x2A, 0x22)) yield return s;
                    else if (r == 2) foreach (var s in Pr(m, 0x2A, 0x21)) yield return s;
                    else
                    {
                        foreach (var s in Pr(m, 0x2A, 0x2E)) yield return s;
                        foreach (var s in Equip.MenuFarHud(m, 1)) yield return s;
                    }
                    foreach (var s in WaitAnyKey(m)) yield return s;
                    step = Step.Ask;
                }
            }
            if (step == Step.Move)
            {                                                 // 97EF
                Equip.MenuSnd(m, 0x4B);
                CursorSpr(m); ItemSpr(m);
                foreach (var s in Pr(m, 0x2A)) yield return s;
                m.Wr(0x19, m.Rd(Goods + m.Rd(Cur)));
                foreach (var s in Pr(m, 0x1D, 0x32)) yield return s;
                continue;
            }
            if (step == Step.Again || step == Step.Ask)
            {
                foreach (var s in Pr(m, 0x2A, step == Step.Again ? 0x30 : 0x1F, 0x29)) yield return s;
                foreach (var s in Equip.Confirm(m, 0x41, no)) yield return s;
                if (no.V)
                {                                             // 97B3
                    foreach (var s in Leave()) yield return s;
                    yield break;
                }
                Equip.MenuSnd(m, 0x4C);
                foreach (var s in Pr(m, 0x2A, 0x1D)) yield return s;
                m.Wr(R.PadNew, 0);
            }
            ShopPriceSel(m);                                  // 97CD
            foreach (var s in Equip.MenuFarHud(m, 0x0B)) yield return s;
            foreach (var s in Pr(m, 0x32)) yield return s;
        }
    }

    // js: game/states/shop.js goodsShop
    /// <summary>16:9558 armor (sub_state $20, chr_r1_alt $3E, title $18) / 16:957A tool ($21, $3A, $19)</summary>
    private static IEnumerable<Y> GoodsShop(Machine m, bool tool)
    {
        foreach (var s in ShopSetup(m)) yield return s;
        foreach (var s in Death.WaitFrame(m)) yield return s;
        m.Wr(0x59, tool ? 0x3A : 0x3E);
        foreach (var s in Pr(m, tool ? 0x19 : 0x18, 0x1C, 0x29)) yield return s;
        if (tool) foreach (var s in ShopSetupGoods(m, 0x9E28, 0x9E54)) yield return s;
        else foreach (var s in ShopSetupGoods(m, 0x9DA4, 0x9DD0)) yield return s;
        foreach (var s in BuyLoop(m)) yield return s;
        foreach (var s in Equip.HideOam(m)) yield return s;   // 96C6
        m.Wr(0x07DD, 0);
    }

    // js: game/states/shop.js substArmorShop
    /// <summary>SubstArmorShop 30:D985 (sub_state $20)</summary>
    public static IEnumerable<Y> SubstArmorShop(Machine m)
    {
        foreach (var s in ShopWalkIn(m)) yield return s;
        foreach (var s in GoodsShop(m, false)) yield return s;
        foreach (var s in ShopWalkOut(m)) yield return s;
    }

    // js: game/states/shop.js substToolShop
    /// <summary>SubstToolShop 30:D98E (sub_state $21)</summary>
    public static IEnumerable<Y> SubstToolShop(Machine m)
    {
        foreach (var s in ShopWalkIn(m)) yield return s;
        foreach (var s in GoodsShop(m, true)) yield return s;
        foreach (var s in ShopWalkOut(m)) yield return s;
    }

    // ---- inn (session 123): 16:959C via 16:9506 ----

    // js: game/states/shop.js fadeSaveZpToWram
    /// <summary>FadeSaveZpToWram 30:C7D6: ZP $0A-$29 -> $6000, banks 26/27, fade out (A = 4), 30 x (LSR frame_ready /
    /// BCC), fade in (A = 4), banks 16/17, ZP restored from $6000</summary>
    private static IEnumerable<Y> FadeSaveZpToWram(Machine m)
    {
        for (int x = 0; x < 0x20; x++) m.Wr(0x6000 + x, m.Rd(0x0A + x));
        Prg.SwitchBank16(m, 0x0D);
        foreach (var s in Death.FadeOut(m, 4)) yield return s;
        for (int x = 0x1E; x > 0; x--)
        {
            for (; ; )
            {
                int v = m.Rd(R.FrameReady);
                m.Wr(R.FrameReady, v >> 1);
                if ((v & 1) != 0) break;
                yield return Y.Frame;
            }
        }
        foreach (var s in Death.FadeIn(m, 4)) yield return s;
        Prg.SwitchBank16(m, 8);
        for (int x = 0; x < 0x20; x++) m.Wr(0x0A + x, m.Rd(0x6000 + x));
    }

    // js: game/states/shop.js innBody
    /// <summary>16:959C: "INN" / "Need to rest? Please stay." + cost YES/NO (see the JS)</summary>
    private static IEnumerable<Y> InnBody(Machine m)
    {
        Equip.SelToIds(m); Equip.SortInventory(m); Equip.IdsToSel(m);
        foreach (var s in ShopSetup(m)) yield return s;
        ShopIdForArea(m);
        m.Wr(0x6474, 0); m.Wr(0x6475, 0);
        m.Wr(0x59, 0x38);
        foreach (var s in Pr(m, 0x1A, 0x23, 0x24, 0x29)) yield return s;
        int x = (m.Rd(0x646D) << 1) & 0xFF;
        m.Wr(0x6474, Prg.CpuRd(m, 0x9EAC + x)); m.Wr(0x6475, Prg.CpuRd(m, 0x9EAD + x));
        foreach (var s in Equip.MenuFarHud(m, 0x0C)) yield return s;
        var no = new Ret<bool>();
        foreach (var s in Equip.Confirm(m, 0x41, no)) yield return s;
        if (no.V)
        {                                                     // C set = NO
            Equip.MenuSnd(m, 0x4C);
            foreach (var s in Pr(m, 0x2B, 0x2A, 0x20)) yield return s;   // 9665
        }
        else
        {                                                     // 95F0
            int g = m.Rd(0x0702) | m.Rd(0x0703) << 8;
            int p = m.Rd(0x6474) | m.Rd(0x6475) << 8;
            int d = (g - p) & 0xFFFF;
            m.Wr(0x6474, d & 0xFF); m.Wr(0x6475, d >> 8);
            if (g < p)
            {
                foreach (var s in Pr(m, 0x2A, 0x2B, 0x2F)) yield return s;   // 964E
            }
            else
            {
                Equip.MenuSnd(m, 0x55);
                foreach (var s in FadeSaveZpToWram(m)) yield return s;
                foreach (var s in Pr(m, 0x2A, 0x2B, 0x25, 0x26)) yield return s;
                m.Wr(0x03C1, m.Rd(0x03C0));                   // player_hp = player_hp_max
                m.Wr(0x0708, m.Rd(0x0709));                   // mp_cur = mp_max
                m.Wr(0x0710, m.Rd(0x0710) & 0xF0);
                m.Wr(0x0702, m.Rd(0x6474)); m.Wr(0x0703, m.Rd(0x6475));
            }
            foreach (var s in Equip.MenuFarHud(m, 1)) yield return s;
        }
        foreach (var s in WaitAnyKey(m)) yield return s;      // 9674
        foreach (var s in Equip.HideOam(m)) yield return s;   // 96C6 (leaves X = 0)
        m.Wr(0x07DD, 0);
    }

    // js: game/states/shop.js substInn
    /// <summary>SubstInn 30:D997 (sub_state $22): walk in, 16:9506, SetR6($1A), HudRedraw(4) (MP), HudHpBar, walk out</summary>
    public static IEnumerable<Y> SubstInn(Machine m)
    {
        foreach (var s in ShopWalkIn(m)) yield return s;
        foreach (var s in InnBody(m)) yield return s;
        Prg.SetR6(m, 0x1A);
        foreach (var s in Death.HudRedraw(m, 4, 0)) yield return s;
        foreach (var s in Death.HudHpBar(m)) yield return s;
        foreach (var s in ShopWalkOut(m)) yield return s;
    }

    // js: game/states/shop.js substShopEmpty
    /// <summary>SubstShopEmpty 30:D9C1 (sub_state $24): walk in, message 32.17 through ShowMessage, sub_state = $24, walk out</summary>
    public static IEnumerable<Y> SubstShopEmpty(Machine m)
    {
        foreach (var s in ShopWalkIn(m)) yield return s;
        m.Wr(0x07DE, 0x11);                                   // text_msg_id
        m.Wr(0x07DF, 0x20);                                   // text_group
        foreach (var s in Talk.ShowMessage(m)) yield return s;
        m.Wr(R.SubState, 0x24);
        foreach (var s in ShopWalkOut(m)) yield return s;
    }
}
