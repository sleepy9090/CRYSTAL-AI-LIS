#!/usr/bin/env python3
"""RAM / WRAM usage map from the full-ROM disassembly (disasm/bank_NN.asm).

Scans every decoded instruction line (code only, `.db` rows skipped), decodes the
bytes again with instruction_set.decode, and tallies each data reference to
internal RAM $0000-$07FF and WRAM $6000-$7FFF:
  reads / writes (RMW ops count as both) per bank, indexed (,X / ,Y / (zp),Y / (zp,X)),
  pointer use ((zp),Y / (zp,X) count on the zp pointer byte), labels.csv name.
Then detects parallel arrays: indexed bases with the same index register at a
stride of $20 (object table) or $10 / $40 forming chains of >= 3 bases.

Usage:
  python3 tools/ram_map.py                 -> docs/ram_map.txt
  python3 tools/ram_map.py --unlabeled N   -> also list unlabeled addrs with >= N refs
  python3 tools/ram_map.py --csv out.csv   -> machine-readable per-address table
"""
import os
import re
import sys
from collections import defaultdict

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nesrom  # noqa: E402
from instruction_set import decode  # noqa: E402

ASM_DIR = os.path.join(nesrom.ROOT, 'disasm')
OUT = os.path.join(nesrom.ROOT, 'docs', 'ram_map.txt')
LINE_RE = re.compile(r'^\s+([A-Z]{3})\b.*;\s*([0-9A-F]{4})\s\s((?:[0-9a-f]{2} ?)+)\s*$')

WRITE_OPS = {'STA', 'STX', 'STY'}
RMW_OPS = {'INC', 'DEC', 'ASL', 'LSR', 'ROL', 'ROR'}
NO_DATA = {'JMP', 'JSR'}
INDEX_REG = {'abx': 'X', 'zpx': 'X', 'aby': 'Y', 'zpy': 'Y', 'izy': '(),Y', 'izx': '(,X)'}


def is_ram(a):
    return a < 0x0800 or 0x6000 <= a < 0x8000


class Ref:
    __slots__ = ('r', 'w', 'banks', 'idx', 'ptr', 'sites')

    def __init__(self):
        self.r = self.w = self.ptr = 0
        self.banks = defaultdict(lambda: [0, 0])
        self.idx = defaultdict(int)
        self.sites = []


def scan():
    refs = defaultdict(Ref)
    for bank in range(32):
        path = os.path.join(ASM_DIR, f'bank_{bank:02d}.asm')
        if not os.path.exists(path):
            continue
        with open(path) as f:
            for line in f:
                m = LINE_RE.match(line)
                if not m:
                    continue
                pc = int(m.group(2), 16)
                buf = bytes(int(x, 16) for x in m.group(3).split())
                sz, mn, md, val, tgt = decode(buf, 0, pc)
                if mn in NO_DATA or md in ('imm', 'imp', 'acc', 'rel', 'ind') or tgt is None:
                    continue
                if not is_ram(tgt):
                    continue
                rf = refs[tgt]
                if md in ('izy', 'izx'):
                    # zp pointer: the 2-byte pointer is read; target is runtime
                    rf.ptr += 1
                    for a in (tgt, (tgt + 1) & 0xFF):
                        refs[a].r += 1
                        refs[a].banks[bank][0] += 1
                    refs[tgt].idx[INDEX_REG[md]] += 1
                    rf.sites.append((bank, pc, mn, md))
                    continue
                isw = mn in WRITE_OPS or mn in RMW_OPS
                isr = mn not in WRITE_OPS
                if isr:
                    rf.r += 1
                    rf.banks[bank][0] += 1
                if isw:
                    rf.w += 1
                    rf.banks[bank][1] += 1
                if md in INDEX_REG:
                    rf.idx[INDEX_REG[md]] += 1
                rf.sites.append((bank, pc, mn, md))
    return refs


def ram_label(labels, a):
    v = labels.get(('*', a))
    return v[0] if v else ''


def covering_label(labels, a, maxback=0x40):
    """Nearest labelled RAM base at or below a (for 'inside array X' notes)."""
    for d in range(maxback + 1):
        n = ram_label(labels, a - d)
        if n:
            return n, d
    return '', 0


def parallel_arrays(refs):
    """Chains of indexed bases (same index reg) at a fixed stride."""
    out = []
    for reg in ('X', 'Y'):
        bases = sorted(a for a, r in refs.items() if r.idx.get(reg, 0) >= 1)
        bset = set(bases)
        used = set()            # a base belongs to the first (smallest-$20-first) chain only
        for stride in (0x20, 0x40, 0x10):
            for b in bases:
                if b in used or (b - stride) in bset:
                    continue
                chain = [b]
                while chain[-1] + stride in bset:
                    chain.append(chain[-1] + stride)
                if len(chain) >= 3:
                    out.append((reg, stride, chain))
                    used.update(chain)
    return out


# Names for unlabelled addresses whose meaning is already documented in REVERSE.md
# (session 29 label pass). Object columns are 32-slot arrays indexed by slot X/Y.
CURATED = {
    0x03A0: ('obj_u03A0', '32 slots; low nibble kept when a boss part dies (15:A4E5); NpcVanish clears; meaning TBD (Ph5 structs 2/5)'),
    0x03E0: ('obj_attack', '32 slots; attack power: 26:910C sums player $03E1 + attacker $03E0,X'),
    0x0400: ('obj_defense', '32 slots; defence: target $0400,Y subtracted from the attack sum at 26:911E'),
    0x0420: ('obj_hitflags', '32 slots; hit flags: b5 tested in the hit path 26:8FDF'),
    0x0440: ('obj_pause_timer', '32 slots; AiWalkPause stand-still timer (decrement, no move while !=0)'),
    0x0460: ('obj_bg_mask', '32 slots; BG collision mask: 0 = flies through walls; & ObjBgProbe attr bits & 6 -> blocked'),
    0x0480: ('obj_timer', '32 slots; lifetime / walk time (DEC per frame); low 3 bits = ObjStepVector frame phase'),
    0x04C0: ('obj_death_type', '32 slots; type ObjInit runs when the object dies (0 = EnemyDeathFx puff)'),
    0x0500: ('obj_elem_drop', '32 slots; lo nibble = element bits (attacker&target&$0F !=0 -> no damage, 26:9111); hi nibble = gold-drop class (26:946D)'),
    0x0520: ('obj_xp_value', '32 slots; XP on kill (AddXp 26:924B; b7 -> (v&$7F)*16)'),
    0x05A0: ('obj_u05A0', '32 slots; timer: $FF on hit (26:8FF1), bosses set $10 each frame, at 0 on b2=1 objects HitSetFlag clears; meaning TBD'),
    0x0600: ('obj_phase', '32 slots; AI phase/state (BossPhaseDispatch index), also speed bit pattern / pause count in walker AIs'),
    0x0620: ('obj_timer2', '32 slots; secondary AI timer (e.g. chest monster wander)'),
    0x0640: ('obj_u0640', '32 slots; meaning TBD (Ph5 structs 2/5)'),
    0x0660: ('obj_u0660', '32 slots; NpcVanish sets 1; meaning TBD'),
    0x0680: ('obj_param1', '32 slots; type-$30 NPC param 1 (spawn rec byte 0; item given by talk act 3)'),
    0x06A0: ('obj_param2', '32 slots; type-$30 NPC param 2 (spawn rec byte 1; talk act 17)'),
    0x06E0: ('obj_behav', '32 slots; type-$30 behaviour flags (b2 = AiNpcWander alt mode 27:AE16); menus reuse $06D0-$06EF as palette sets'),
    0x0200: ('oam_y', 'OAM shadow page $0200-$02FF: sprite Y (DMA each NMI)'),
    0x0201: ('oam_tile', 'OAM shadow: tile'),
    0x0202: ('oam_attr', 'OAM shadow: attributes'),
    0x0203: ('oam_x', 'OAM shadow: X'),
    0x6000: ('wram_buf', 'shared WRAM scratch $6000-$60FF: metatile row/col buffer, text box rows (8x32), SaveGame zp $10-$2F copy, FadeSaveZpToWram'),
    0x6100: ('attr_shadow', 'BG attribute shadow (MetatileAttrBits merge, 31:EE80/EEA4)'),
    0x6201: ('ppu_queue_1', 'ppu_queue record byte 1'),
    0x6202: ('ppu_queue_2', 'ppu_queue record byte 2'),
    0x6203: ('ppu_queue_3', 'ppu_queue record byte 3'),
    0x6D00: ('save_mirror', 'backup mirror of $7D00 (SaveGame copies $7D00-$7FFF -> $6D00-$6FFF)'),
    0x6E00: ('save_mirror_flags', 'backup mirror of $7E00'),
    0x6F00: ('save_mirror_npc', 'backup mirror of $7F00'),
    0x7D00: ('save_cur', 'current save image $7D00-$7FFF (player name; $7D80 -> $0702; $7DB0 -> $07DC)'),
    0x7E00: ('save_flags', 'save image: story flags -> $6480'),
    0x7F00: ('save_npc_state', 'save image: per-NPC state -> $6580'),
    0x0703: ('gold_hi', 'gold hi byte'),
    0x0705: ('xp_hi', 'XP hi byte'),
    0x0707: ('xp_next_hi', 'XP-for-next-level hi byte'),
    0x07E1: ('pal_id_bg1', 'BG palette 1 id (pal_ids+1)'),
    0x07E2: ('pal_id_bg2', 'BG palette 2 id (pal_ids+2)'),
    0x07E3: ('pal_id_bg3', 'BG palette 3 id (pal_ids+3)'),
    0x6488: ('story_flags_F040', 'story_flags byte F040-F047 (area hook 31:E7C4: F045)'),
    0x648A: ('story_flags_F050', 'story_flags byte F050-F057 (area hook 31:E7C4: F053)'),
    0x657F: ('story_flags_F7F8', 'last story_flags byte F7F8-F7FF'),
    0x643F: ('inv_643F', 'inventory byte ($6430-$645F)'),
}


def non_gameplay(rf):
    """True if every site is in the title/menu/state-2/3 code (banks 17/18, bank 19 below
    PlayerDeath $B900), which reuses the object-column RAM after MainState2Init clears it."""
    return all(b in (17, 18) or (b == 19 and pc < 0xB900) for b, pc, mn, md in rf.sites)
OBJ_COLS = (0x0300, 0x06C0)     # $0300-$06BF: 30 columns of 32 slots ($06C0+ is mixed use)


def auto_label(labels, refs, a):
    """(name, comment) for an unlabelled address, or None."""
    rf = refs[a]
    banks = ','.join(str(b) for b in sorted(rf.banks))
    use = f'auto (ram_map.py s29): r{rf.r} w{rf.w} banks {banks}'
    if a in CURATED:
        return CURATED[a]
    in_cols = (OBJ_COLS[0] <= a < OBJ_COLS[1]) or (0x70 <= a < 0xF0)
    if in_cols and a & 0x1F and non_gameplay(rf):
        return (f'ram_{a:04X}' if a >= 0x100 else f'zp_{a:02X}'), \
            f'title/menu/state-2-3 reuse of the object-column area; {use}'
    if in_cols and a & 0x1F:
        base = a & ~0x1F
        bname = ram_label(labels, base) or (CURATED.get(base, (f'obj_u{base:04X}',))[0])
        return f'{bname}_s{a & 0x1F:02X}', f'slot {a & 0x1F:#04x} of {bname}; {use}'
    if a < 0x32:
        return f'tmp_{a:02X}', f'zp scratch; {use}'
    if 0xF1 <= a <= 0xFF:
        return f'snd_zp{a:02X}', f'sound driver zp; {use}'
    if a < 0x100:
        return f'zp_{a:02X}', use
    if 0x113 < a < 0x180:
        return f'snd_ch{a:04X}', f'sound driver per-channel array (,X); {use}'
    if a < 0x800:
        return f'ram_{a:04X}', use
    return f'wram_{a:04X}', use


def region(a):
    if a < 0x100:
        return 'zp'
    if a < 0x200:
        return 'stack-page'
    if a < 0x300:
        return 'oam-buf'
    if a < 0x800:
        return 'ram'
    return 'wram'


def main(argv):
    unl_min = None
    csv_out = None
    if '--unlabeled' in argv:
        unl_min = int(argv[argv.index('--unlabeled') + 1])
    if '--csv' in argv:
        csv_out = argv[argv.index('--csv') + 1]
    labels = nesrom.load_labels()
    refs = scan()
    addrs = sorted(refs)
    lines = ['; RAM/WRAM reference map (tools/ram_map.py, from disasm/bank_NN.asm)',
             '; addr  region  R  W  ptr  idx            banks(r/w)                     name',
             f'; {len(addrs)} addresses referenced']
    for a in addrs:
        rf = refs[a]
        idx = ' '.join(f'{k}{v}' for k, v in sorted(rf.idx.items())) or '-'
        bk = ' '.join(f'{b}:{r}/{w}' for b, (r, w) in sorted(rf.banks.items()))
        name = ram_label(labels, a)
        if not name:
            n, d = covering_label(labels, a, 0x1F)
            name = f'({n}+{d})' if n else ''
        lines.append(f'{a:04X}  {region(a):10s} {rf.r:3d} {rf.w:3d} {rf.ptr:3d}  {idx:14s} {bk:30s} {name}')
    lines.append('')
    lines.append('; parallel arrays (indexed bases, same index register, fixed stride, >=3 bases)')
    for reg, stride, chain in parallel_arrays(refs):
        names = [ram_label(labels, a) or f'${a:04X}' for a in chain]
        lines.append(f'{reg} stride ${stride:02X}: ' + ' '.join(f'{a:04X}' for a in chain)
                     + '  [' + ', '.join(names) + ']')
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w') as f:
        f.write('\n'.join(lines) + '\n')
    nlab = sum(1 for a in addrs if ram_label(labels, a))
    print(f'{len(addrs)} addresses, {nlab} labelled -> {OUT}')
    if csv_out:
        with open(csv_out, 'w') as f:
            f.write('addr,region,reads,writes,ptr,idx,banks,name\n')
            for a in addrs:
                rf = refs[a]
                f.write(f'{a:04X},{region(a)},{rf.r},{rf.w},{rf.ptr},'
                        f'{"/".join(f"{k}{v}" for k, v in sorted(rf.idx.items()))},'
                        f'{" ".join(str(b) for b in sorted(rf.banks))},{ram_label(labels, a)}\n')
    if '--emit-labels' in argv or '--append-labels' in argv:
        flag = '--emit-labels' if '--emit-labels' in argv else '--append-labels'
        n = int(argv[argv.index(flag) + 1])
        rows = []
        for a in addrs:
            rf = refs[a]
            if rf.r + rf.w >= n and not ram_label(labels, a):
                name, com = auto_label(labels, refs, a)
                rows.append(f'*,{a:04X},{name},{com.replace(",", ";")}')
        if flag == '--append-labels':
            with open(nesrom.LABELS_PATH, 'a') as f:
                f.write('\n'.join(rows) + '\n')
            print(f'appended {len(rows)} labels to {nesrom.LABELS_PATH}')
        else:
            print('\n'.join(rows))
        return
    if unl_min is not None:
        for a in addrs:
            rf = refs[a]
            tot = rf.r + rf.w
            if tot >= unl_min and not ram_label(labels, a):
                sites = ' '.join(f'{b}:{pc:04X}{mn[0]}' for b, pc, mn, md in rf.sites[:6])
                print(f'{a:04X} {region(a):10s} r{rf.r} w{rf.w} idx={dict(rf.idx)} '
                      f'banks={sorted(rf.banks)}  {sites}')


if __name__ == '__main__':
    main(sys.argv[1:])
