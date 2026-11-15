#!/usr/bin/env python3
"""List sound ids passed to PlaySound (30:C125) via `LDA #id / JSR $C125` (also `LDA #id / JMP`).

usage: sound_ids.py
Prints each id with kind (music <$20 / sfx) and call sites (BB:AAAA in default bank window).
"""
from collections import defaultdict
from nesrom import load_rom, bank_cpu_addr

_, PRG, _ = load_rom()
PLAY = (0x25, 0xC1)

hits = defaultdict(list)
for i in range(len(PRG) - 4):
    if PRG[i] == 0xA9 and PRG[i + 2] in (0x20, 0x4C) and (PRG[i + 3], PRG[i + 4]) == PLAY:
        bank, off = divmod(i, 0x2000)
        tag = '' if PRG[i + 2] == 0x20 else '(JMP)'
        hits[PRG[i + 1]].append(f"{bank:02d}:{bank_cpu_addr(bank, off):04X}{tag}")

for sid in sorted(hits):
    kind = 'music' if sid < 0x20 else ('music-force' if 0x80 <= sid < 0xA0 else 'sfx')
    print(f"${sid:02X} {kind:11s} x{len(hits[sid]):2d}  {' '.join(hits[sid])}")
print(f"-- {sum(len(v) for v in hits.values())} sites, {len(hits)} distinct ids")
