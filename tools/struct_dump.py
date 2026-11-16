#!/usr/bin/env python3
"""Struct field tables for re-c (Ph5 structs 2/5, session 30).

usage:
  struct_dump.py obj [--sites] [--out f]
      Object slot struct: one row per object column (zp $70/$90/$B0/$D0 and the 32
      stride-$20 columns $0300-$06E0). For each column: labels.csv name, code-site
      reads/writes (indexed vs fixed-slot), banks, the routines that write / read it
      (nearest named label above the site in disasm/bank_NN.asm), and how many
      ObjTypeTable records initialise it (+ distinct values).  --sites lists every site.
  struct_dump.py objtype [col ...]
      Per-column value histogram over all 255 ObjTypeTable 13:AC00 records.
  struct_dump.py rec <layout> <BB:AAAA> [count]
      Decode fixed-size records with a pre-configured layout (see LAYOUTS).
      Variable-length formats keep their own tools: area records / sub-tables k0-k5
      -> area_dump.py, metasprites -> meta_dump.py, sound headers/streams ->
      sound_dump.py / sound_export.py, palettes -> palette_dump.py, object
      types -> objtype_dump.py.

Static RE only: counts are per code site (from disasm_all.py output), not per execution.
"""
import os
import re
import sys
from collections import defaultdict, Counter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nesrom  # noqa: E402
import ram_map  # noqa: E402
import objtype_dump  # noqa: E402

LABEL_LINE = re.compile(r'^([A-Za-z_][A-Za-z0-9_]*):')
CODE_LINE = re.compile(r';\s*([0-9A-F]{4})\s\s(?:[0-9a-f]{2} ?)+\s*$')

ZP_COLS = [0x70, 0x90, 0xB0, 0xD0]
ABS_COLS = [0x300 + i * 0x20 for i in range(32)]


def column_of(a):
    """RAM address -> (column base, slot) for object-table addresses, else None."""
    if 0x70 <= a < 0xF0:
        b = 0x70 + ((a - 0x70) // 0x20) * 0x20
        return b, a - b
    if 0x300 <= a < 0x700:
        b = a & ~0x1F
        return b, a & 0x1F
    return None


def routine_index():
    """(bank, pc) -> nearest preceding non-loc label in that bank's asm."""
    idx = {}
    asm = os.path.join(nesrom.ROOT, 'disasm')
    for bank in range(32):
        p = os.path.join(asm, f'bank_{bank:02d}.asm')
        if not os.path.exists(p):
            continue
        cur = '?'
        with open(p) as f:
            for line in f:
                m = LABEL_LINE.match(line)
                if m and not m.group(1).startswith('loc_'):
                    cur = m.group(1)
                m2 = CODE_LINE.search(line)
                if m2:
                    idx[(bank, int(m2.group(1), 16))] = cur
    return idx


def type_inits():
    """column -> Counter(value) over ObjTypeTable records (types 0..$FE, not 1)."""
    out = defaultdict(Counter)
    for t in range(0xFF):
        if t == 1:
            continue
        _, _, cols, _ = objtype_dump.decode(t)
        for c, v in cols.items():
            out[c][v] += 1
    return out


def cmd_obj(argv):
    show_sites = '--sites' in argv
    labels = nesrom.load_labels()
    refs = ram_map.scan()
    rix = routine_index()
    inits = type_inits()
    cols = defaultdict(lambda: {'r': 0, 'w': 0, 'fixed': Counter(), 'banks': Counter(),
                                'wr': Counter(), 'rd': Counter(), 'sites': []})
    for a, rf in refs.items():
        co = column_of(a)
        if co is None:
            continue
        base, slot = co
        c = cols[base]
        for bank, pc, mn, md in rf.sites:
            isw = mn in ram_map.WRITE_OPS or mn in ram_map.RMW_OPS
            isr = mn not in ram_map.WRITE_OPS
            rname = f'{rix.get((bank, pc), "?")}'
            if isw:
                c['w'] += 1
                c['wr'][f'{bank}:{rname}'] += 1
            if isr:
                c['r'] += 1
                c['rd'][f'{bank}:{rname}'] += 1
            c['banks'][bank] += 1
            if md not in ram_map.INDEX_REG:
                c['fixed'][slot] += 1
            c['sites'].append((bank, pc, mn, md, slot))

    def nm(base):
        v = labels.get(('*', base))
        return v[0] if v else ''

    def top(cn, n=6):
        items = cn.most_common()
        s = ' '.join(f'{k}({v})' for k, v in items[:n])
        return s + (f' +{len(items) - n}' if len(items) > n else '')

    print('; Object slot struct (struct_dump.py obj, session 30). 32 slots, stride $20.')
    print('; col | name | r w | fixed-slot refs | banks | types init (n, distinct values)')
    print(';   W: writer routines (bank:routine(sites))   R: reader routines')
    for base in ZP_COLS + ABS_COLS:
        c = cols[base]
        ini = inits.get(base, Counter())
        vals = ' '.join(f'{v:02X}' for v in sorted(ini)[:12]) + (' …' if len(ini) > 12 else '')
        fx = ' '.join(f's{s:02X}:{n}' for s, n in sorted(c['fixed'].items()))
        bk = ' '.join(f'{b}' for b in sorted(c['banks']))
        print(f'${base:04X} {nm(base):<18} r{c["r"]:<3} w{c["w"]:<3} fixed[{fx}] banks[{bk}] '
              f'init {sum(ini.values())}/{len(ini)}v [{vals}]')
        print(f'    W: {top(c["wr"])}')
        print(f'    R: {top(c["rd"])}')
        if show_sites:
            for bank, pc, mn, md, slot in sorted(c['sites']):
                print(f'      {bank:02d}:{pc:04X} {mn} {md} slot={slot:02X}')
    return 0


def cmd_objtype(argv):
    inits = type_inits()
    sel = [int(a, 16) for a in argv] or sorted(inits)
    for c in sel:
        h = inits.get(c, Counter())
        print(f'${c:04X}: {sum(h.values())} types, ' +
              ' '.join(f'{v:02X}x{n}' for v, n in sorted(h.items())))
    return 0


# Fixed-size record layouts: name -> (size, [(offset, field, fmt)]) fmt: b=byte, w=word LE
LAYOUTS = {
    'area_hdr':  (5, [(0, 'zero', 'b'), (1, 'pal_e6', 'b'), (2, 'pal_e7', 'b'),
                      (3, 'chr_r4', 'b'), (4, 'chr_r5', 'b')]),
    'spawn':     (4, [(0, 'slot_or_FF', 'b'), (1, 'b1', 'b'), (2, 'b2', 'b'), (3, 'param', 'b')]),
    'k1':        (7, [(0, 'pal_bg0', 'b'), (1, 'pal_bg1', 'b'), (2, 'pal_bg2', 'b'),
                      (3, 'tileset_67', 'b'), (4, 'v69', 'b'), (5, 'chr_r0', 'b'), (6, 'chr_r1', 'b')]),
    'k2':        (4, [(0, 'cam_x', 'w'), (2, 'cam_y', 'w')]),
    'k3':        (4, [(0, 'xcell', 'b'), (1, 'ycell', 'b'), (2, 'area', 'b'), (3, 'entry', 'b')]),
    'k4':        (2, [(0, 'flag', 'b'), (1, 'screen', 'b')]),
    'k5':        (4, [(0, 'area', 'b'), (1, 'a21', 'b'), (2, 'a22', 'b'), (3, 'a23', 'b')]),
    # session 30: HitboxTable 26:9691 (24 recs), CollPairTable 26:96F1 (15 recs)
    'hitbox':    (4, [(0, 'dx', 's'), (1, 'w', 's'), (2, 'dy', 's'), (3, 'h', 's')]),
    'collpair':  (4, [(0, 'slot', 'b'), (1, 'first', 'b'), (2, 'last', 'b'), (3, 'handler', 'b')]),
}


def cmd_rec(argv):
    if len(argv) < 2 or argv[0] not in LAYOUTS:
        print('layouts: ' + ' '.join(LAYOUTS))
        return 1
    size, fields = LAYOUTS[argv[0]]
    bank, addr = nesrom.parse_bank_addr(argv[1])
    n = int(argv[2], 0) if len(argv) > 2 else 1
    _, prg, _ = nesrom.load_rom()
    for i in range(n):
        a = addr + i * size
        off = bank * 0x2000 + (a & 0x1FFF)
        rec = prg[off:off + size]
        parts = []
        for o, f, fmt in fields:
            if fmt == 's':
                v = rec[o] - 256 if rec[o] >= 0x80 else rec[o]
                parts.append(f'{f}={v:+d}')
                continue
            v = rec[o] | (rec[o + 1] << 8 if fmt == 'w' else 0)
            parts.append(f'{f}={v:0{4 if fmt == "w" else 2}X}')
        print(f'{bank:02d}:{a:04X} [{i:2d}] ' + ' '.join(parts))
    return 0


def main(argv):
    if '--out' in argv:
        i = argv.index('--out')
        sys.stdout = open(argv[i + 1], 'w')
        argv = argv[:i] + argv[i + 2:]
    if not argv:
        print(__doc__)
        return 1
    cmd, rest = argv[0], argv[1:]
    return {'obj': cmd_obj, 'objtype': cmd_objtype, 'rec': cmd_rec}.get(
        cmd, lambda _: (print(__doc__), 1)[1])(rest)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
