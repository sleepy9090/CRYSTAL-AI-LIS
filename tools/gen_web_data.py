#!/usr/bin/env python3
"""Export every ROM table the web port needs to web/data/ (Ph7 web 1/8, session 49).

usage: gen_web_data.py [--out-dir web/data] [--skip-sound] [--check] [--only NAME]
  (default)     regenerate all files + manifest.json
  --skip-sound  do not re-run sound_export.py (slow, 2.7 MB); keeps the existing sound.json
  --check       do not write: recompute everything and compare with manifest.json sha1s
  --only NAME   write just NAME (e.g. areas.json) and update its manifest.json entry; other files untouched

Design: docs/architecture_web.md section 5. The port must not hand-copy table values; it reads
these files. Binary blobs keep ROM byte order and ROM CPU addressing (offsets documented in the
manifest), JSON holds decoded records produced by the existing decoders (area_dump, objtype_dump,
meta_dump, palette_dump, shot_dump) so there is one decoder per format.
"""
import csv
import hashlib
import json
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nesrom import load_rom, load_labels, ROOT, ROM_PATH  # noqa: E402
import area_dump  # noqa: E402
import objtype_dump  # noqa: E402
import meta_dump  # noqa: E402
import palette_dump  # noqa: E402
import shot_dump  # noqa: E402

ROM, PRG, CHR = load_rom()
LABELS = load_labels()
SESSION = 49


def bank_bytes(bank, addr, n):
    """n bytes of 8KB bank `bank` starting at CPU addr (must not cross the bank)."""
    o = addr & 0x1FFF
    assert o + n <= 0x2000, (bank, hex(addr), n)
    b = bank * 0x2000 + o
    return PRG[b:b + n]


def label_name(bank, addr, default):
    lb = LABELS.get((bank, addr))
    return lb[0] if lb else default


# ---------------------------------------------------------------- named fixed-size tables
# (name if unlabelled, bank, addr, count, fmt). Count/format from labels.csv comments + REVERSE.md.
# fmt: u8 | s8 | u16 (little-endian) | ptr16
TABLES = [
    ('LevelHpTable', 26, 0x8B7F, 16, 'u8'),
    ('LevelMpTable', 26, 0x8B8F, 16, 'u8'),       # entry 15 overlaps LevelXpTable[0] lo (ROM layout)
    ('LevelXpTable', 26, 0x8B9E, 16, 'u16'),
    ('SwordAttackTable', 26, 0x8BD2, 6, 'u8'),
    ('ObjStepSpeedTables', 26, 0x84BC, 48, 'u8'),  # 3 x 16 read by ObjStepVector 26:8480 (84BC/84CC/84DC)
    ('HudElemTable', 26, 0x8EC5, 42, 'u8'),         # 7 x 6 bytes
    ('HitboxTable', 26, 0x9691, 96, 's8'),          # 24 x {dx w dy h}
    ('CollPairTable', 26, 0x96F1, 60, 'u8'),        # 15 x {slot first last handler}
    ('Rand64Table', 26, 0x97E4, 64, 'u8'),          # Rand64 26:97D7 reads [++$0E & $3F]
    ('BgProbePoints', 26, 0x9907, 64, 's8'),        # 8 dirs x 8 bytes
    ('JumpHeightTable', 26, 0x9D4D, 49, 's8'),
    ('AiDirTable_A8A2', 27, 0xA8A2, 4, 'u8'),
    ('AiPathTable', 27, 0xAF04, 16, 'ptr16'),
    ('AiPathData', 27, 0xAF24, 0x82, 'u8'),         # AF24-AFA5 direction-byte paths
    ('OrbitSineTable', 28, 0x81BA, 32, 's8'),
    ('OrbitTileTable', 28, 0x81DA, 4, 'u8'),
    ('BurstDirTable', 28, 0x81DE, 32, 'u8'),
    ('BurstRadiusTable', 28, 0x81FE, 48, 'u8'),
    ('ObjDrawOrderTable', 28, 0x83DC, 128, 'u8'),   # 4 x 32 slot permutations
    ('ObjMetaPtrTableA', 28, 0x845C, 128, 'ptr16'),
    ('ObjMetaPtrTableB', 28, 0x855C, 128, 'ptr16'),
    ('ObjAiTable', 27, 0xA314, 128, 'ptr16'),
    ('BossPhaseTable', 15, 0xA413, 50, 'ptr16'),    # A413-A476 (boss_phase_dump TABLE_END)
    ('BossAreaList', 15, 0xB95D, 14, 'u8'),
    ('TelepathyClass', 14, 0x821A, 8, 'u8'),
    ('TelepathyRandSeq', 14, 0x822F, 64, 'u8'),
    ('NewSaveTemplate', 23, 0xBE00, 256, 'u8'),
    ('Wram6000InitTable', 11, 0xBE00, 256, 'u8'),
    ('MainStateTable', 30, 0xC91F, 6, 'ptr16'),
    ('SubstateHandlerTable', 30, 0xCADE, 40, 'ptr16'),
    ('SubstateBankTable', 30, 0xCB2E, 40, 'u8'),
    ('AreaByIndexTable', 30, 0xCBEC, 16, 'u8'),
    ('SpawnHandlerTable', 31, 0xE218, 5, 'ptr16'),
    ('AreaEntryTable', 31, 0xE3D9, 8, 'ptr16'),
    ('MetatileAttrPtrTable', 31, 0xEEEF, 12, 'ptr16'),
    ('AttrRowOffset', 31, 0xEF07, 16, 'u8'),
    ('IrqHandlerTable', 31, 0xF433, 8, 'ptr16'),
    ('NmiModeTable', 31, 0xF6C2, 10, 'ptr16'),
    ('PadDirTable', 31, 0xFED0, 16, 'u8'),
]


def decode_table(bank, addr, count, fmt):
    size = {'u8': 1, 's8': 1, 'u16': 2, 'ptr16': 2}[fmt]
    raw = bank_bytes(bank, addr, count * size)
    if fmt == 'u8':
        return list(raw)
    if fmt == 's8':
        return [v - 256 if v & 0x80 else v for v in raw]
    return [raw[i] | raw[i + 1] << 8 for i in range(0, len(raw), 2)]


def tables_json():
    out = {}
    for name, bank, addr, count, fmt in TABLES:
        name = label_name(bank, addr, name)
        out[name] = {'bank': bank, 'addr': addr, 'fmt': fmt, 'count': count,
                     'values': decode_table(bank, addr, count, fmt)}
    return out


# ---------------------------------------------------------------- areas
def sub_ptrs_sorted(h, ps):
    return sorted(set(p for p in ps if 0x8000 <= p < 0xC000) | {h})


def next_after(p, ps_sorted, cap):
    """First header/sub-table pointer above p (bounds variable-length tables), else p+cap."""
    hi = [q for q in ps_sorted if q > p]
    return min(hi[0], p + cap) if hi else p + cap


def area_json(a):
    rd = area_dump.rd10
    d = area_dump.decode(a)
    if d is None:
        # null AreaPtrTable record (AreaLoad 31:E16B BPL -> RTS: no CHR / spawn record, $07E6/$07E7/R4/R5 kept)
        # but the area can still have a real AreaSubTable header (k0 map, k1 gfx, k3 exits): $16 $6F $A7 $B7
        # (tools/null_record_areas.py; $A7 = the room above the final-boss hall $A6, entered by its top exits)
        h, ps = area_dump.sub_header(a)
        if not h & 0x8000 or not 0x8000 <= ps[0] < 0xC000 or rd(ps[0] + 1) > 7 or rd(ps[0] + 2) > 11:
            return None                      # no header, or $BD $CA $CC $DB $E6 $EA: garbage (unused) headers
        return area_header({'record': {'ptr': area_dump.ptr(a), 'null': True}}, a)
    rec = {'ptr': d['ptr']}
    if 'ignored' in d:
        rec['ignored'] = d['ignored']
        return {'record': rec}
    rec.update({'e6': d['e6'], 'e7': d['e7'], 'r4': d['r4'], 'r5': d['r5'], 'spawns': []})
    for slot, e in d['ents']:
        if e is None:
            rec['spawns'].append({'slot': slot, 'skip': 'FE'})
        elif e == 'skip':
            rec['spawns'].append({'slot': slot, 'skip': 'b1'})
        else:
            y, x, h, prm, b2 = e
            rec['spawns'].append({'slot': slot, 'x': x, 'y': y, 'handler': h, 'param': prm, 'b2': b2})
    return area_header({'record': rec}, a)


def area_header(out, a):
    """add AreaSubTable 10:8300[a]'s header sub-tables k0-k5 to out (when the header pointer is real)"""
    rd = area_dump.rd10
    h, ps = area_dump.sub_header(a)
    if not h & 0x8000:
        return out
    srt = sub_ptrs_sorted(h, ps[:5] + ([ps[5]] if ps[0] != h + 10 else []))
    p = ps[0]
    snd, w, hh, fe, ff = (rd(p + i) for i in range(5))
    hdr = {'ptr': h, 'k': ps[:5] + ([ps[5]] if ps[0] != h + 10 else [])}
    if w <= 7 and hh <= 11:
        hdr['k0'] = {'snd': snd, 'W': w + 1, 'H': hh + 1, 'f62FE': fe, 'f62FF': ff,
                     'grid': [[rd(p + 5 + r * (w + 1) + c) for c in range(w + 1)] for r in range(hh + 1)]}
    else:
        hdr['k0'] = {'garbage': True}
    k1 = [rd(ps[1] + i) for i in range(7)]
    hdr['k1'] = {'pal': k1[0:3], 'v67': k1[3], 'v69': k1[4], 'R0': k1[5], 'R1': k1[6]}
    p = ps[2]
    n = max(1, min(32, (next_after(p, srt, 128) - p) // 4))
    hdr['k2'] = [{'x': rd(p + 4 * e) | rd(p + 4 * e + 1) << 8, 'y': rd(p + 4 * e + 2) | rd(p + 4 * e + 3) << 8}
                 for e in range(n)]
    ex, p = [], ps[3]
    while rd(p) != 0xFF and len(ex) < 64:
        ex.append({'xcell': rd(p), 'ycell': rd(p + 1), 'area': rd(p + 2), 'entry': rd(p + 3)})
        p += 4
    hdr['k3'] = ex
    fl, p = [], ps[4]
    while rd(p) != 0xFF and len(fl) < 64:
        fl.append({'flag': rd(p), 'scr': rd(p + 1)})
        p += 2
    hdr['k4'] = fl
    if ps[0] != h + 10:   # 6-pointer header -> k5 present; no terminator (31:E573), bound by next ptr
        p = ps[5]
        n = max(1, min(16, (next_after(p, srt, 64) - p) // 4))
        hdr['k5'] = [[rd(p + 4 * i + j) for j in range(4)] for i in range(n)]
        hdr['k5_note'] = 'raw 4-byte records [area a21 a22 a23]; count bounded by the next pointer (no terminator)'
    out['header'] = hdr
    return out


def areas_json():
    n = area_dump.table_len()
    areas = {}
    used_p = set()
    for a in range(n):
        j = area_json(a)
        if j is None:
            continue
        areas['%02X' % a] = j
        for s in j['record'].get('spawns', []):
            if s.get('handler') == 1:
                used_p.add(s['param'])
    indirect = {}
    for p in sorted(used_p):
        addr, r = area_dump.indirect_rec(p)
        indirect['%02X' % p] = {'bank': 4 + (addr >= 0xA000), 'addr': addr, 'bytes': list(r)}
    return {'source': 'AreaPtrTable 12:9201 + AreaHeaderTable 10:8300 (tools/area_dump.py)',
            'count': n, 'areas': areas, 'indirect': indirect}


# ---------------------------------------------------------------- objects, sprites, palettes, shots
def objtypes_json():
    out = {}
    for t in range(256):
        p, snd, cols, end = objtype_dump.decode(t)
        ai = cols.get(0x4A0)
        out['%02X' % t] = {'ptr': p, 'snd': snd, 'cols': {'%03X' % k: v for k, v in sorted(cols.items())},
                           'ai': ai, 'aiHandler': objtype_dump.ai_handler(ai) if ai is not None else None}
    return {'source': 'ObjTypeTable 13:AC00/AD00, ObjAiTable 27:A314 (tools/objtype_dump.py)',
            'note': 'cols: column base address -> value written at base+slot by ObjInit 30:C25D',
            'types': out}


def shots_json():
    recs = []
    for i in list(range(0x40)) + list(range(0x40, 0x60)):
        base, r = shot_dump.rec(i)
        recs.append({'id': i, 'addr': base, 'first': r[0], 'end': r[1], 'type': r[2], 'count': r[3]})
    offs = [v - 256 if v & 0x80 else v for v in bank_bytes(20, 0xBD80, 0x80)]
    return {'source': 'ShotTable 20:BC00/BD00, ShotOffsets 20:BD80 (tools/shot_dump.py)',
            'records': recs, 'offsets': {'addr': 0xBD80, 'values': offs,
                                         'index': '(shooter $06E0 << 4) | ((dir*2) & $0F) -> dx, dy'}}


def palettes_json():
    return {'source': 'palette records banks 2/3 page tails (tools/palette_dump.py)',
            'nesRgb': palette_dump.NES_RGB, 'slots': palette_dump.read_slots(PRG),
            'bgSlot': 'p', 'sprSlot': '(p + 0xB0) & 0xFF', 'backdropByte': 0xFF}


def ram_labels_json():
    out = {}
    with open(os.path.join(ROOT, 'labels.csv'), newline='') as f:
        for row in csv.reader(f):
            if len(row) >= 3 and row[0].strip() == '*':
                try:
                    out[row[2].strip()] = int(row[1], 16)
                except ValueError:
                    pass
    return out


# ---------------------------------------------------------------- driver
def dmc_bytes():
    import sound_export  # noqa: E402 (heavy import, only here)
    rec = sound_export.DMC_RECS[1]
    addr, n = 0xC000 + rec[2] * 64, rec[3] * 16 + 1
    return addr, bank_bytes(31, addr, n)


def sha1(b):
    return hashlib.sha1(b).hexdigest()


def build():
    """-> list of (filename, bytes, meta dict)."""
    files = []

    def add_bin(name, data, src, desc):
        files.append((name, bytes(data), {'kind': 'bin', 'source': src, 'desc': desc}))

    def add_json(name, obj, src, desc):
        data = json.dumps(obj, separators=(',', ':'), sort_keys=False).encode()
        files.append((name, data, {'kind': 'json', 'source': src, 'desc': desc}))

    add_bin('chr.bin', CHR, 'CHR ROM file 0x40010-0x6000F',
            '128 x 1KB CHR banks, NES 2bpp planar tiles (16 B/tile); R0-R5 index 1KB banks')
    add_bin('screens.bin', PRG[0:8 * 0x2000], 'PRG banks 0-7',
            'screen page S at S*256 (16x15 metatile ids + 16-byte tail); ScreenPagePtr 31:EF36')
    add_bin('screens_b10.bin', PRG[10 * 0x2000:11 * 0x2000], 'PRG bank 10',
            '$62FF b7 areas: page S&$1F at (S&$1F)*256 when S&$3F < $20 (row render 31:EBEA only)')
    add_bin('metatiles.bin', PRG[8 * 0x2000:10 * 0x2000], 'PRG banks 8-9 as $8000-$BFFF',
            'index = cpu_addr - $8000: quadrant pages ((v67&$FC)|q)<<8|M, attrs $B000+i*$40, '
            'BG collision ($69<<8)|M, visited subst $BE00+(v67&$3C)*8+M')
    add_bin('sprites_b28.bin', PRG[28 * 0x2000:30 * 0x2000], 'PRG banks 28-29 as $8000-$BFFF',
            'index = cpu_addr - $8000: sprite pass tables (ObjDrawOrderOfs $83D8, ObjDrawOrderTable $83DC, '
            'OrbitSineTable $81BA, OrbitTileTable $81DA, Burst* $81DE/$81FE), ObjMetaPtrTableA/B $845C/$855C '
            '+ metasprite records (ObjDrawMeta 28:822E reads them through ($15),Y)')
    add_bin('sound_b24.bin', PRG[24 * 0x2000:25 * 0x2000], 'PRG bank 24 at $8000',
            'sound driver tables + music streams (SndMusicTable $8C0C)')
    add_bin('sound_b25.bin', PRG[25 * 0x2000:26 * 0x2000], 'PRG bank 25 at $A000', 'music stream data')
    add_bin('sound_b12.bin', PRG[12 * 0x2000:13 * 0x2000], 'PRG bank 12 at $A000 (SetR7(12))',
            'SfxPtrTable at $A000 + SFX streams')
    add_bin('prg.bin', PRG, 'PRG ROM file 0x10-0x4000F (s55)',
            '32 x 8KB PRG banks; byte at bank:addr = [bank*$2000 + (addr & $1FFF)] (web/src/game/prg.js). '
            'Used by the object code (ObjInit 13:AC00, ShotTable 20:BC00, banks 26/27 tables) so routines '
            'can read ROM tables by CPU address the way the 6502 does')
    daddr, dbytes = dmc_bytes()
    add_bin('dmc.bin', dbytes, f'31:{daddr:04X}', 'the single DMC sample (SndDmcTable 24:8BD8)')
    add_json('palettes.json', palettes_json(), 'banks 2/3', 'NES master palette + 256 palette slots')
    add_json('areas.json', areas_json(), 'banks 4/5, 10/11, 12/13', 'area records, spawns, header k0-k5')
    add_json('objtypes.json', objtypes_json(), '13:AC00, 27:A314', 'ObjInit records per object type')
    add_json('metasprites.json', meta_dump.meta_json(PRG), 'banks 28/29', 'metasprite frames per id')
    add_json('shots.json', shots_json(), '20:BC00-BDFF', 'SpawnShot records + offsets')
    add_json('tables.json', tables_json(), 'see each entry', 'named fixed-size gameplay tables')
    add_json('ram_labels.json', ram_labels_json(), 'labels.csv bank *', 'RAM/WRAM name -> address')
    return files


def spot_checks(files):
    """Cross-check exported values against facts already recorded in labels.csv / REVERSE.md."""
    import render_screen as rs  # noqa: E402
    byname = {n: d for n, d, _ in files}
    tab = json.loads(byname['tables.json'])
    areas = json.loads(byname['areas.json'])
    scr, mt = byname['screens.bin'], byname['metatiles.bin']
    checks = [
        ('MainStateTable', tab['MainStateTable']['values'] == [0xC957, 0xCAB6, 0xC931, 0xC939, 0xC944, 0xC94C]),
        ('IrqHandlerTable', tab['IrqHandlerTable']['values'] ==
         [0xF455, 0xF488, 0xF4E5, 0xF54B, 0xF5E1, 0xF60E, 0xF691, 0xF61A]),
        ('OrbitTileTable', tab['OrbitTileTable']['values'] == [0x29, 0x29, 0x2A, 0x32]),
        ('AiDirTable_A8A2', tab['AiDirTable_A8A2']['values'] == [8, 8, 9, 7]),
        ('BossAreaList', tab['BossAreaList']['values'][:4] == [0x0A, 0x1A, 0x28, 0xFF]),
        ('MetatileAttrPtrTable', tab['MetatileAttrPtrTable']['values'] == [0xB000 + i * 0x40 for i in range(12)]),
        ('AttrRowOffset', tab['AttrRowOffset']['values'][:4] == [0, 0, 8, 8]),
        ('SubstateBankTable', [i for i, v in enumerate(tab['SubstateBankTable']['values']) if v == 9]
         == [3, 24, 25, 26, 27, 30]),
        ('area count 252', areas['count'] == 252),
        ('screens.bin == page_bytes', all(scr[s * 256:s * 256 + 256] == rs.page_bytes(s) for s in range(256))),
        ('screens_b10 == page_bytes(b10)', all(byname['screens_b10.bin'][(s & 0x1F) * 256:][:256] ==
                                               rs.page_bytes(s, True) for s in range(0x20))),
        ('metatiles.bin == mt_byte', all(mt[a - 0x8000] == rs.mt_byte(a) for a in range(0x8000, 0xC000, 7))),
    ]
    # every area render_level can draw has a k0 grid equal to render_screen.area_bg
    n_bg = 0
    ok = True
    for a in range(areas['count']):
        bg = rs.area_bg(a)
        if bg is None:
            continue
        n_bg += 1
        k0 = areas['areas'].get('%02X' % a, {}).get('header', {}).get('k0', {})
        k1 = areas['areas']['%02X' % a]['header']['k1']
        ok &= k0.get('grid') == bg['grid'] and k1['v67'] == bg['v67'] and k1['R0'] == bg['R0']
    checks.append((f'k0/k1 == area_bg for {n_bg} areas', ok))
    k5 = sorted(k for k, v in areas['areas'].items() if 'k5' in v.get('header', {}))
    checks.append((f'k5 areas {" ".join(k5)}', k5 == '6A 6D 6E 8A 9F A2 A5 AE AF B4'.split()))
    bad = 0
    for name, good in checks:
        print(('ok   ' if good else 'FAIL ') + name)
        bad += not good
    return bad


def run_text_sound(out_dir, skip_sound):
    py = sys.executable
    tools = os.path.dirname(os.path.abspath(__file__))
    subprocess.run([py, os.path.join(tools, 'text_dump.py'), 'none', '--json',
                    os.path.join(out_dir, 'text.json')], check=True, stdout=subprocess.DEVNULL)
    if not skip_sound:
        subprocess.run([py, os.path.join(tools, 'sound_export.py'), '--out',
                        os.path.join(out_dir, 'sound.json')], check=True, stdout=subprocess.DEVNULL)


def main(argv):
    out_dir = os.path.join(ROOT, 'web', 'data')
    if '--out-dir' in argv:
        out_dir = argv[argv.index('--out-dir') + 1]
    files = build()
    mpath = os.path.join(out_dir, 'manifest.json')
    if '--check' in argv:
        man = json.load(open(mpath))
        bad = 0
        for name, data, _ in files:
            ent = man['files'].get(name)
            ok = ent is not None and ent['sha1'] == sha1(data)
            bad += not ok
            if not ok:
                print('STALE', name)
        for name in ('text.json', 'sound.json'):
            p = os.path.join(out_dir, name)
            ok = os.path.exists(p) and man['files'][name]['sha1'] == sha1(open(p, 'rb').read())
            bad += not ok
            if not ok:
                print('STALE', name)
        if man['rom_sha1'] != sha1(ROM):
            print('STALE rom sha1'); bad += 1
        bad += spot_checks(files)
        print('check:', 'OK' if not bad else f'{bad} stale')
        return 1 if bad else 0
    if '--only' in argv:
        name = argv[argv.index('--only') + 1]
        hit = [f for f in files if f[0] == name]
        if not hit:
            print(f'--only: no generated file {name}'); return 1
        _, data, meta = hit[0]
        with open(os.path.join(out_dir, name), 'wb') as f:
            f.write(data)
        man = json.load(open(mpath))
        man['files'][name] = dict(meta, size=len(data), sha1=sha1(data))
        with open(mpath, 'w') as f:
            json.dump(man, f, indent=1)
        print(f'wrote {name} ({len(data)} bytes) + its manifest.json entry -> {out_dir}')
        return 0
    os.makedirs(out_dir, exist_ok=True)
    run_text_sound(out_dir, '--skip-sound' in argv)
    manifest = {'generator': f'tools/gen_web_data.py (session {SESSION})',
                'rom': os.path.relpath(ROM_PATH, ROOT), 'rom_sha1': sha1(ROM),
                'design': 'docs/architecture_web.md section 5', 'files': {}}
    for name, data, meta in files:
        with open(os.path.join(out_dir, name), 'wb') as f:
            f.write(data)
        manifest['files'][name] = dict(meta, size=len(data), sha1=sha1(data))
    for name, src, desc in (('text.json', 'banks 20-23 (tools/text_dump.py --json)',
                             'dictionary, code 05/06/07 tables, all messages'),
                            ('sound.json', 'banks 24/25, 12, 31 (tools/sound_export.py)',
                             'driver tables, DMC sample, decoded music/SFX streams')):
        data = open(os.path.join(out_dir, name), 'rb').read()
        manifest['files'][name] = {'kind': 'json', 'source': src, 'desc': desc,
                                   'size': len(data), 'sha1': sha1(data)}
    with open(mpath, 'w') as f:
        json.dump(manifest, f, indent=1)
    for name, ent in manifest['files'].items():
        print(f'{name:<18} {ent["size"]:>8}  {ent["source"]}')
    print(f'wrote {len(manifest["files"])} files + manifest.json -> {out_dir}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
