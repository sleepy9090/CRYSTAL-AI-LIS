"""Areas whose AreaPtrTable record is null (ptr bit 15 clear: AreaLoad 31:E16B BPL -> RTS, no CHR / spawn record)
but whose AreaSubTable header (10:8300) is a real one (k0 map, k1 gfx, k3 exits ...), and which areas' exits lead to them.

usage: python3 tools/null_record_areas.py
"""
import sys
import os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_dump  # noqa: E402


def main():
    n = area_dump.table_len()
    found = []
    for a in range(n):
        if area_dump.decode(a) is not None:
            continue
        h, ps = area_dump.sub_header(a)
        if not h & 0x8000:
            print(f'${a:02X}  record null, header ${h:04X} (not a header)')
            continue
        w, hh = area_dump.rd10(ps[0] + 1), area_dump.rd10(ps[0] + 2)
        found.append(a)
        print(f'${a:02X}  record null ${area_dump.ptr(a):04X}, header ${h:04X}: k0 W={w + 1} H={hh + 1}'
              + ('' if w <= 7 and hh <= 11 else ' (k0 garbage)'))
    for a in range(n):
        if area_dump.decode(a) is None:
            continue
        h, ps = area_dump.sub_header(a)
        if not h & 0x8000:
            continue
        p = ps[3]
        to = set()
        while area_dump.rd10(p) != 0xFF and len(to) < 64:
            if area_dump.rd10(p + 2) in found:
                to.add(area_dump.rd10(p + 2))
            p += 4
        if to:
            print(f'  exits of ${a:02X} lead to ' + ' '.join(f'${t:02X}' for t in sorted(to)))
    return 0


if __name__ == '__main__':
    sys.exit(main())
