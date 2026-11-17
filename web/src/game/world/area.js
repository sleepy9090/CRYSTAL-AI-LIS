// re-c web port — area load: header sub-tables k0/k1/k2/k4 and the area record (Ph7 web 4/8, s53).
// REVERSE.md "Area record + header sub-tables" and "Area header sub-tables (session 9)". The tables come
// pre-decoded from web/data/areas.json (gen_web_data.py); every RAM/WRAM write below is the ROM's, in order.
//
//   loadAreaMap(m)      AreaLoadMap 31:E628 (k0) -> AreaRestoreVisited 31:E702 (k4) -> entry point 31:E686 (k2)
//                       -> AreaLoadGfx 31:E6B9 (k1) -> AreaLoadB 31:E148 -> AreaLoad 31:E14A / LoadAreaChrRecord 31:E16D
//   loadAreaChr(m, a)   LoadAreaChrRecord body: $07E6/$07E7, R4/R5 $07F4/$07F5, spawn entries from slot $0D
//
// Hooks the later tasks fill (Machine fields, no-ops until then):
//   m.playSound(id)             PlaySound 30:C125 (7/8)
//   m.spawnEntry(slot, entry)   AreaSpawnEntry 31:E1B6 -> SpawnHandlerTable 31:E218 (5/8)
//   m.entryFF(x)                bank 26 $98A8 (X=0), the $6D = $FF path (5/8: object-position code)

export function areaInfo(m, id = m.rd(0x6C)) {
  const a = m.data.areas.areas[id.toString(16).toUpperCase().padStart(2, '0')];   // keys '%02X'
  if (!a) throw new Error(`area $${id.toString(16)}: no record/header in areas.json (null area)`);
  return a;
}

/** AreaLoadMap 31:E628 and everything it chains into (k0, k4, k2, k1, record). */
export function loadAreaMap(m) {
  const a = areaInfo(m), k0 = a.header && a.header.k0;
  if (!k0 || k0.garbage) throw new Error(`area $${m.rd(0x6C).toString(16)}: no usable k0 header`);
  m.playSound(k0.snd);
  m.wr(0x62FC, k0.W - 1);                        // area_w (width - 1, screens)
  m.wr(0x62FD, k0.H - 1);                        // area_h
  m.wr(0x13, k0.H - 1);
  m.wr(0x62FE, k0.f62FE);
  m.wr(0x62FF, k0.f62FF);
  for (let i = 0; i < 0x80; i++) m.wr(0x6300 + i, 0x80);   // screen_grid prefill = page $80 (black)
  k0.grid.forEach((row, r) => row.forEach((s, c) => m.wr(0x6300 + r * 8 + c, s)));
  m.wr(0x12, 0xFF);                              // DEC tmp_12 / BPL leave both loop counters at $FF
  m.wr(0x13, 0xFF);
  for (let i = 0; i < 12; i++) m.wr(0x62F0 + i, 0);         // visited_map
  restoreVisited(m, a);
  placePlayer(m, a);
}

/** AreaRestoreVisited 31:E702: for each k4 `flag scr`, visited bit of screen scr = WRAM flag $64C0 bit. */
export function restoreVisited(m, a = areaInfo(m)) {
  for (const { flag, scr } of a.header.k4) {
    const set = (m.rd(0x64C0 + (flag >> 3)) & (1 << (flag & 7))) !== 0;
    const bit = 1 << (scr & 7), row = 0x62F0 + (scr >> 4);
    m.wr(row, (m.rd(row) & ~bit) | (set ? bit : 0));
  }
}

/** 31:E686: $6D = $FF -> bank 26 $98A8 (X=0); $6D&$E0 = $20 -> keep the position; else k2[$6D&$1F] -> $34-$37. */
export function placePlayer(m, a = areaInfo(m)) {
  const e = m.rd(0x6D);
  if (e === 0xFF) m.entryFF(0);
  else if ((e & 0xE0) !== 0x20) {
    const p = a.header.k2[e & 0x1F];
    if (!p) throw new Error(`area $${m.rd(0x6C).toString(16)}: no k2 entry ${e & 0x1F}`);
    m.wr(0x34, p.x & 0xFF); m.wr(0x35, p.x >> 8); m.wr(0x36, p.y & 0xFF); m.wr(0x37, p.y >> 8);
  }
  loadAreaGfx(m, a);
}

/** AreaLoadGfx 31:E6B9 (k1) then JMP AreaLoadB 31:E148 ($18 = 0) -> the area record. */
export function loadAreaGfx(m, a = areaInfo(m)) {
  const k1 = a.header.k1;
  m.wr(0x07E0, k1.pal[0]); m.wr(0x07E1, k1.pal[1]); m.wr(0x07E2, k1.pal[2]); m.wr(0x07E3, 0x7F);
  m.wr(0x66, 0); m.wr(0x68, 0);
  m.wr(0x67, k1.v67); m.wr(0x69, k1.v69);
  const sub = 0xBE00 + (k1.v67 & 0x3C) * 8;      // ASL x3 carry feeds ADC #$BE: a plain 16-bit sum
  m.wr(0x6A, sub & 0xFF); m.wr(0x6B, sub >> 8);
  m.wr(0x07F0, k1.R0); m.wr(0x07F1, k1.R1);
  areaLoad(m, 0, a);
}

/** AreaLoad 31:E14A: $18 = flag; AreaPtrTable 12:9201[$6C] (hi b7 clear = no record) -> LoadAreaChrRecord. */
export function areaLoad(m, flag18, a = areaInfo(m)) {
  m.wr(0x18, flag18);
  if (a.record && (a.record.ptr & 0x8000) && !('ignored' in a.record)) loadAreaChr(m, a);
}

/** LoadAreaChrRecord 31:E16D (record byte 0 != 0 -> RTS: areas.json marks those `ignored`). */
export function loadAreaChr(m, a = areaInfo(m)) {
  const r = a.record;
  m.wr(0x07E6, r.e6); m.wr(0x07E7, r.e7); m.wr(0x07F4, r.r4); m.wr(0x07F5, r.r5);
  for (const s of r.spawns) if (!s.skip) m.spawnEntry(s.slot, s);
}
