#!/usr/bin/env python3
"""s127: can sound command $FF (music fade, 24:805F -> snd_fade_timer $BC -> SndFadeStep 24:8609) be queued?

Scans the two data-driven PlaySound sources (playsound_dynamic.py):
  - AreaLoadMap 31:E632: area sub-table 0 byte 0 (banks 10/11, AreaSubTable 30:C7A5) for areas 0-255
  - ObjInit 30:C289: first byte of the type record (bank 13 at $A000: ptr $AC00[t*2] / $AD00[(t-$80)*2])
usage: sound_ff_scan.py [id_hex]   (default FF) -> prints every area / type whose sound byte equals id
                                     plus a histogram of all values.
"""
import sys
from collections import Counter
from nesrom import load_rom
from area_dump import sub_header, rd10

_, PRG, _ = load_rom()
want = int(sys.argv[1], 16) if len(sys.argv) > 1 else 0xFF


def rd13(addr):
    """Bank 13 mapped at $A000-$BFFF (SetR7(13))."""
    return PRG[13 * 0x2000 + (addr & 0x1FFF)]


area_vals = Counter()
for area in range(256):
    _, ps = sub_header(area)
    v = rd10(ps[0])
    area_vals[v] += 1
    if v == want:
        print(f'area ${area:02X}: music byte ${v:02X} (sub0 {ps[0]:04X})')

type_vals = Counter()
for t in range(2, 256):
    base = 0xAC00 if t < 0x80 else 0xAD00
    y = (t * 2) & 0xFF
    p = rd13(base + y) | rd13(base + y + 1) << 8
    if not 0xA000 <= p < 0xC000:
        continue
    v = rd13(p)
    type_vals[v] += 1
    if v == want:
        print(f'type ${t:02X}: sound byte ${v:02X} (rec {p:04X})')

print('area music bytes:', ' '.join(f'{k:02X}x{n}' for k, n in sorted(area_vals.items())))
print('type sound bytes:', ' '.join(f'{k:02X}x{n}' for k, n in sorted(type_vals.items())))
