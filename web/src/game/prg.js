// re-c web port — banked PRG reads + the MMC3 PRG bank setters (Ph7 web 6/8, s55).
//
//   prgByte(m, bank, addr)  byte of 8KB PRG bank `bank` at CPU address addr (any window)
//   cpuRd(m, a)             CPU read with the current mapping: RAM, WRAM, $8000 = m.prgR6, $A000 = m.prgR7,
//                           $C000 = bank 30, $E000 = bank 31 (MMC3 PRG mode 0, as set by SetR6 writing $8000 = 6)
//   setR6(m, v) / setR7(m, v)   SetR6 30:C418 / SetR7 30:C427: shadow $6E / $6F, $50 = 6 / 7, then the mapping
//   farRestore(m, r6, r7)   RestoreBanks 31:FFDB (SetR7 then SetR6, so $50 ends as 6)
//
// The data is web/data/prg.bin (all 32 banks, written by tools/gen_web_data.py). Routines that read ROM tables
// through (zp),Y pointers use cpuRd, so a pointer into RAM or another window reads what the 6502 would read.

export function prgByte(m, bank, addr) {
  return m.data.prg[(bank & 0x1F) * 0x2000 + (addr & 0x1FFF)];
}

export function cpuRd(m, a) {
  a &= 0xFFFF;
  m.romRd++;                                         // s211: counted for web/test/pass_count.mjs
  if (a < 0x2000) return m.mem.ram[a & 0x7FF];
  if (a >= 0x6000 && a < 0x8000) return m.mem.wram[a - 0x6000];
  if (a < 0x6000) throw new Error(`cpuRd $${a.toString(16)}: PPU/APU/open bus not modelled`);
  if (a < 0xA000) return prgByte(m, m.prgR6, a);
  if (a < 0xC000) return prgByte(m, m.prgR7, a);
  return prgByte(m, a < 0xE000 ? 30 : 31, a);
}

/** (zp),Y: 16-bit pointer at zp, zp+1 plus Y */
export function indY(m, zp, y) {
  return cpuRd(m, (m.rd(zp) | m.rd((zp + 1) & 0xFF) << 8) + (y & 0xFF));
}

export function setR6(m, v) {
  m.wr(0x6E, v); m.wr(0x50, 6); m.prgR6 = m.rd(0x6E);
}

export function setR7(m, v) {
  m.wr(0x6F, v); m.wr(0x50, 7); m.prgR7 = m.rd(0x6F);
}

/** SwitchBank16 30:C40E: $6E = 2n, SetR7(2n+1), SetR6(2n) ($50 ends as 6) */
export function switchBank16(m, n) {
  m.wr(0x6E, n << 1); setR7(m, (n << 1) | 1); setR6(m, n << 1);
}

export function farRestore(m, r6, r7) {
  setR7(m, r7); setR6(m, r6);
}
