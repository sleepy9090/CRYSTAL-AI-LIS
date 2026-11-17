#!/usr/bin/env python3
"""Ph5.5 emu 6/6 verification: trace documented routines in tools.emu and check them
against the REVERSE.md model, then check the Data Structures field tables against RAM.

usage: python3 tools/emu/verify.py [--state FILE] [--to FRAME] [--keys SPEC ...] [--out FILE]
  default: name_entry.state (f2250) + the canonical script tail (2300:START 2340:DOWN:80)
  run to f2460 = new game, start room, area 0 -> 1 transition, 68 frames in area 1.

(a) call checks (exec hook at entry, matched return = PC back at the pushed address with S
    restored), each call compared against a Python model of the REVERSE.md description:
    SwitchBank16 30:C40E   banks at $8000/$A000 = 2n/2n+1, $6E/$6F shadows
    PlaySound    30:C125   music dedupe vs $0102, $80-$9F re-issue, FIFO $0103+, count $0100, Y kept
    ObjSpriteDraw 28:800E  $05C0/$05E0 screen pos, $0380 b7 offscreen, $07FF DEC
    PaletteBuild 26:8C10   $6140-$615F from $07E0-$07E7 + bank 2/3 records, fade $11, mask $07E8
    ObjInit      30:C25D   slot columns $0300-$06E0 = ObjTypeTable record (objtype_dump.decode)
    AreaLoad     31:E14A   $07E6/$07E7/$07F4/$07F5 + spawned slot x/y/$0540/$0560 = area record
    SndStartMusic 24:80E4  $0111, channel ptrs = music header streams, wait/len/octave init
    + per-frame order of ObjCollideAll 26:8F6E vs ObjSpriteDraw (which $05C0/$05E0 collision sees)
(b) end-of-run RAM/WRAM checks: area header k0/k1 fields, object slots, sound channels, save image.
"""
import argparse
import dataclasses  # noqa: F401  (loads stdlib dis/inspect before tools/ lands on sys.path;
import os           # argparse's help colouriser imports them lazily and tools/dis.py would shadow dis)
import sys

TOOLS = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.append(TOOLS)          # append: tools/dis.py must not shadow the stdlib dis
from emu.nes import NES  # noqa: E402
from emu import state, debug  # noqa: E402
import area_dump  # noqa: E402
import objtype_dump  # noqa: E402
import palette_dump  # noqa: E402
from nesrom import load_rom  # noqa: E402

_, PRG, _ = load_rom()
STATES = os.path.join(TOOLS, 'emu', 'states')


def rom(bank, addr):
    return PRG[bank * 0x2000 + (addr & 0x1FFF)]


class Report:
    def __init__(self):
        self.lines = []
        self.stats = {}          # name -> [calls, ok, first fails]

    def call(self, name, ok, msg=''):
        s = self.stats.setdefault(name, [0, 0, []])
        s[0] += 1
        if ok:
            s[1] += 1
        elif len(s[2]) < 6:
            s[2].append(msg)

    def p(self, *a):
        self.lines.append(' '.join(str(x) for x in a))


class CallWatch:
    """enter(nes) -> ctx at the routine's first instruction; ret(nes, ctx) when it returns."""

    def __init__(self, nes, bank, addr, enter, ret):
        self.nes, self.enter_fn, self.ret_fn = nes, enter, ret
        self.pending = []
        self.hooked = set()
        nes.on_exec(bank, addr, self._enter)

    def _enter(self, nes):
        s, r = nes.cpu.s, nes.bus.ram
        ret = ((r[0x100 + ((s + 1) & 0xFF)] | r[0x100 + ((s + 2) & 0xFF)] << 8) + 1) & 0xFFFF
        self.pending.append((ret, (s + 2) & 0xFF, self.enter_fn(nes)))
        if ret not in self.hooked:
            self.hooked.add(ret)
            nes.on_exec(None, ret, self._ret)

    def _ret(self, nes):
        pc, s = nes.cpu.pc, nes.cpu.s
        for i in range(len(self.pending) - 1, -1, -1):
            r, ss, ctx = self.pending[i]
            if r == pc and ss == s:
                del self.pending[i]
                self.ret_fn(nes, ctx)
                return


# ---------------------------------------------------------------- (a) models
def w_switchbank(nes, rep):
    def enter(nes):
        return nes.cpu.a

    def ret(nes, n):
        m, r = nes.mapper, nes.bus.ram
        got = (m.prg_bank_at(0x8000), m.prg_bank_at(0xA000), r[0x6E], r[0x6F])
        exp = (2 * n & 0x1F, (2 * n | 1) & 0x1F, 2 * n & 0xFF, (2 * n | 1) & 0xFF)
        rep.call('SwitchBank16', got == exp, f'n={n:02X} got {got} exp {exp}')
        rep.sb_ids = getattr(rep, 'sb_ids', set()) | {n}
    CallWatch(nes, 30, 0xC40E, enter, ret)


def w_playsound(nes, rep):
    def enter(nes):
        r = nes.bus.ram
        return nes.cpu.a, nes.cpu.y, bytes(r[0x100:0x109])

    def ret(nes, ctx):
        a, y, old = ctx
        q = bytearray(old)
        if a < 0x20 and a == q[2]:
            pass                                    # dedupe: nothing changes
        else:
            if a < 0x20:
                q[2] = a
            elif a < 0xA0:
                a &= 0x7F                           # $20-$9F: AND #$7F (no-op for SFX $20-$7F)
            cnt = q[0]
            if cnt == 5 and a < 0x20:
                pass                                # full: music dropped
            else:
                yy = 5 if cnt == 5 else cnt
                q[3 + yy] = a
                yy += 1
                q[0] = 5 if yy == 6 else yy
            q[1] = 0
        r = nes.bus.ram
        got = bytes(r[0x100:0x109])
        ok = got == bytes(q) and nes.cpu.y == y
        rep.call('PlaySound', ok, f'id={ctx[0]:02X} before {old.hex()} got {got.hex()} '
                 f'exp {bytes(q).hex()} Y {nes.cpu.y:02X}/{y:02X}')
        rep.snd_ids = getattr(rep, 'snd_ids', []) + [ctx[0]]
    CallWatch(nes, 30, 0xC125, enter, ret)


def w_sprites(nes, rep):
    def enter(nes):
        r = nes.bus.ram
        rep.order.append(('draw', nes.ppu.frame_no))
        return bytes(r[0:0x100]), bytes(r[0x300:0x700]), r[0x7FF]

    def ret(nes, ctx):
        zp, hi, rot = ctx
        r = nes.bus.ram
        cx, cy = zp[2] | zp[3] << 8, zp[4] | zp[5] << 8
        bad = []
        for x in range(32):
            act = hi[0x4A0 - 0x300 + x]
            f0 = hi[0x380 - 0x300 + x]
            sx0, sy0 = hi[0x5C0 - 0x300 + x], hi[0x5E0 - 0x300 + x]
            esx, esy, eb7 = sx0, sy0, f0 & 0x80
            if not act:
                eb7 = 0x80
            else:
                dx = (zp[0x70 + x] | zp[0x90 + x] << 8) - cx
                dy = (zp[0xB0 + x] | zp[0xD0 + x] << 8) - cy
                if (dx >> 8) & 0xFF:
                    eb7 = 0x80
                else:
                    esx = dx & 0xFF
                    if (dy >> 8) & 0xFF:
                        eb7 = 0x80
                    else:
                        lo = dy & 0xFF
                        if zp[0xB0 + x] < zp[4]:
                            lo = (lo - 16) & 0xFF
                        if lo >= 0xE8:
                            pass
                        elif lo < 0x10:
                            eb7 = 0x80
                        else:
                            esy, eb7 = lo, 0
            got = (r[0x5C0 + x], r[0x5E0 + x], r[0x380 + x] & 0x80)
            if got != (esx, esy, eb7):
                bad.append(f'slot {x:02X} got {got} exp {(esx, esy, eb7)}')
        ok = not bad and r[0x7FF] == (rot - 1) & 0xFF
        rep.call('ObjSpriteDraw', ok, f'f{nes.ppu.frame_no} ' + '; '.join(bad[:3]) +
                 ('' if r[0x7FF] == (rot - 1) & 0xFF else f' $07FF {r[0x7FF]:02X}'))
    CallWatch(nes, 28, 0x800E, enter, ret)


PAL_SLOTS = palette_dump.read_slots(PRG)


def model_palette(ids, mask, fade, buf):
    buf = list(buf)
    for X in range(0, 32, 4):
        p = ids[X >> 2]
        s = (p + 0xB0) & 0xFF if X >= 0x10 else p
        f = 0 if (mask >> (X >> 2)) & 1 else fade
        for i in range(4):
            c = PAL_SLOTS[s][i]
            if c == 0xFF:
                c = buf[0]
            v = (c - f) & 0xFF
            buf[X + i] = 0x0F if v & 0x80 else v
    return bytes(buf)


def w_palette(nes, rep):
    def enter(nes):
        r, w = nes.bus.ram, nes.bus.wram
        return bytes(r[0x7E0:0x7E8]), r[0x7E8], nes.cpu.a, bytes(w[0x140:0x160])

    def ret(nes, ctx):
        ids, mask, fade, buf = ctx
        exp = model_palette(ids, mask, fade, buf)
        got = bytes(nes.bus.wram[0x140:0x160])
        rep.call('PaletteBuild', got == exp, f'ids {ids.hex()} fade {fade:02X} mask {mask:02X} '
                 f'got {got.hex()} exp {exp.hex()}')
        rep.pal_last = (ids.hex(' '), fade, mask, got.hex(' '))
    CallWatch(nes, 26, 0x8C10, enter, ret)


def w_objinit(nes, rep):
    def enter(nes):
        r = nes.bus.ram
        return r[0x10], r[0x11], bytes(r[0x300 + r[0x10]::0x20][:32])

    def ret(nes, ctx):
        x, t, before = ctx
        r = nes.bus.ram
        got = bytes(r[0x300 + x + 0x20 * c] for c in range(32))
        if t == 1:
            exp = before
        else:
            _, _, cols, _ = objtype_dump.decode(t)
            exp = bytes(cols.get(0x300 + 0x20 * c, 0) for c in range(32))
        diff = [f'${0x300 + 0x20 * c:04X}={got[c]:02X}/{exp[c]:02X}' for c in range(32) if got[c] != exp[c]]
        rep.call('ObjInit', not diff, f'slot {x:02X} type {t:02X}: ' + ' '.join(diff))
        rep.inits = getattr(rep, 'inits', []) + [(nes.ppu.frame_no, x, t)]
    CallWatch(nes, 30, 0xC25D, enter, ret)


def w_areaload(nes, rep):
    def enter(nes):
        return nes.bus.ram[0x6C]

    def ret(nes, area):
        r = nes.bus.ram
        d = area_dump.decode(area)
        rep.areas = getattr(rep, 'areas', []) + [(nes.ppu.frame_no, area)]
        if not d or 'ents' not in d:
            rep.call('AreaLoad', True)
            return
        bad = []
        flag_item = any(e and e != 'skip' and e[2] == 4 and e[3] & 0xF0 == 0x40 for _, e in d['ents'])
        exp_e = ((area & 7) + 9,) * 2 if flag_item else (d['e6'], d['e7'])
        if (r[0x7E6], r[0x7E7]) != exp_e:
            bad.append(f'e6/e7 {r[0x7E6]:02X}{r[0x7E7]:02X} exp {exp_e}')
        if (r[0x7F4], r[0x7F5]) != (d['r4'], d['r5']):
            bad.append(f'R4/R5 {r[0x7F4]:02X}{r[0x7F5]:02X} exp {d["r4"]:02X}{d["r5"]:02X}')
        for x, e in d['ents']:
            if not e or e == 'skip':
                continue
            y, xx, h, p, b2 = e
            got = (r[0x70 + x] | r[0x90 + x] << 8, r[0xB0 + x] | r[0xD0 + x] << 8)
            rec = area_dump.indirect_rec(p)[1] if h == 1 else None
            # y|=$0E (31:E323) only on the type-$30 path; rec[0]=$FF -> E363 -> E340 skips it (s42)
            ey = y | (0x0E if rec and rec[0] != 0xFF and rec[2] & 0x08 else 0)
            if got != (xx, ey) or r[0x540 + x] != (b2 & 3) or r[0x560 + x] != p:
                bad.append(f'slot {x:02X} h{h} p{p:02X} pos {got} exp {(xx, ey)} '
                           f'$0540={r[0x540 + x]:02X}/{b2 & 3:02X} $0560={r[0x560 + x]:02X}/{p:02X}')
        rep.call('AreaLoad', not bad, f'area {area:02X}: ' + '; '.join(bad))
    CallWatch(nes, 31, 0xE14A, enter, ret)


def w_music(nes, rep):
    def enter(nes):
        return nes.bus.ram[0x110]

    def ret(nes, cmd):
        r = nes.bus.ram
        if r[0x111] != cmd & 0x7F:            # header lookup failed / paused -> nothing started
            rep.call('SndStartMusic', True)
            return
        hp = r[0xF5] | r[0xF6] << 8
        hb = 24 if hp < 0xA000 else 25
        exp = [rom(hb if a < 0xA000 else 25, a) | rom(hb if a + 1 < 0xA000 else 25, a + 1) << 8
               for a in (hp + 1, hp + 3, hp + 5, hp + 7)]
        got = [r[0x11A + c] | r[0x120 + c] << 8 for c in range(4)]
        init = all(r[0x14A + c] == 1 and r[0x150 + c] == 1 and r[0x156 + c] == 0 for c in range(4))
        tp = rom(24, 0x8C0C + 2 * (cmd & 0x1F)) | rom(24, 0x8C0D + 2 * (cmd & 0x1F)) << 8
        rep.call('SndStartMusic', got == exp and init and tp == hp,
                 f'id {cmd:02X} hdr {hp:04X}/{tp:04X} ptrs {[hex(v) for v in got]} exp {[hex(v) for v in exp]} init {init}')
        rep.music = getattr(rep, 'music', []) + [(nes.ppu.frame_no, cmd & 0x7F, hp, got)]
    CallWatch(nes, 24, 0x80E4, enter, ret)


def w_savegame(nes, rep):
    """SaveGame 23:BC09: check the $7D00 image against its source RAM at the return
    (REVERSE.md "Current save image" table; 257-byte flag copy quirk)."""
    def enter(nes):
        return nes.ppu.frame_no, bytes(nes.bus.wram[0x1D00:0x2000]), nes.bus.ram[0x6C]

    def ret(nes, ctx):
        f, before, area = ctx
        r, w = nes.bus.ram, nes.bus.wram
        img = bytes(w[0x1D00:0x2000])
        if not rom(23, 0xBF00 + area) & 0x80:      # SaveAllowed C=0: image kept, only mirrored (s42)
            for name, ok in (('not allowed: $7D00 image unchanged', img == before),
                             ('not allowed: $6D00 mirror == $7D00', bytes(w[0xD00:0x1000]) == img)):
                rep.call('struct: save image at SaveGame return', ok, f'f{f} area {area:02X}: {name}')
            rep.saves_blocked = getattr(rep, 'saves_blocked', []) + [(f, area)]
            return
        exp_pl = bytes([r[0x6C], r[0x70], r[0x90], r[0xB0], r[0xD0], r[0x3C0], r[0x3C1], r[0x421], r[0x620]])
        src80 = bytearray(r[0x702:0x71F])
        src80[0x710 - 0x702] &= 0xF0                       # obj1_flash: hi nibble only
        checks = [
            ('+00-7F == $6400-$647F', img[0:0x80] == bytes(w[0x400:0x480])),
            ('+80-9C == $0702-$071E ($0710 hi nibble)', img[0x80:0x9D] == bytes(src80)),
            ('+9D-AF zero', img[0x9D:0xB0] == bytes(0x13)),
            ('+B0-D3 == $07DC-$07FF', img[0xB0:0xD4] == bytes(r[0x7DC:0x800])),
            ('+F0-F8 == area, slot0 x/y, $03C0 $03C1 $0421 $0620', img[0xF0:0xF9] == exp_pl),
            ('+FD-FF "AMI"', img[0xFD:0x100] == b'AMI'),
            ('+100-200 == $6480-$6580 (257 B)', img[0x100:0x201] == bytes(w[0x480:0x581])),
            ('+201-2FF zero (npc_state[1..] not saved)', img[0x201:0x300] == bytes(0xFF)),
            ('$6D00 mirror == $7D00', bytes(w[0xD00:0x1000]) == img),
        ]
        for name, ok in checks:
            rep.call('struct: save image at SaveGame return', ok, f'f{f}: {name}')
        rep.saves = getattr(rep, 'saves', []) + [(f, img[0xF0:0xF9].hex(' '),
                                                    sum(ok for _, ok in checks), len(checks))]
    CallWatch(nes, 23, 0xBC09, enter, ret)


def w_order(nes, rep):
    rep.order = []
    nes.on_exec(26, 0x8F6E, lambda n: rep.order.append(('coll', n.ppu.frame_no)))


# ---------------------------------------------------------------- (b) structs
def check_area_header(nes, rep):
    r, w = nes.bus.ram, nes.bus.wram
    area = r[0x6C]
    h, ps = area_dump.sub_header(area)
    rd = area_dump.rd10
    snd, W, H, fe, ff = (rd(ps[0] + i) for i in range(5))
    k1 = [rd(ps[1] + i) for i in range(7)]
    rows_ok = all(w[0x300 + 8 * y + x] == rd(ps[0] + 5 + y * (W + 1) + x)
                  for y in range(H + 1) for x in range(W + 1))
    checks = [
        ('k0 W H f62FE f62FF -> $62FC-$62FF', bytes(w[0x2FC:0x300]), bytes([W, H, fe, ff])),
        ('k0 grid -> $6300 (stride 8)', rows_ok, True),
        ('k0 snd -> snd_cur_music $0102', r[0x102], snd),
        ('k1 p0-p2 -> $07E0-$07E2, $07E3=$7F', bytes(r[0x7E0:0x7E4]), bytes(k1[:3] + [0x7F])),
        ('k1 v67 -> $67&$FC (b0-1 = quadrant page, set by the metatile draw 31:EDD7)',
         r[0x67] & 0xFC, k1[3] & 0xFC),
        ('k1 v69 -> $69', r[0x69], k1[4]),
        ('k1 R0 R1 -> $07F0/$07F1', bytes(r[0x7F0:0x7F2]), bytes(k1[5:7])),
    ]
    rep.p(f'Area header (area ${area:02X}, header 10:{h:04X}):')
    for name, got, exp in checks:
        ok = got == exp
        rep.call('struct: area header', ok, name)
        fmt = (lambda v: v.hex(' ') if isinstance(v, bytes) else (f'{v:02X}' if isinstance(v, int) and not isinstance(v, bool) else v))
        rep.p(f'  {"OK  " if ok else "FAIL"} {name}: ram {fmt(got)} rom {fmt(exp)}')


def check_objects(nes, rep):
    r = nes.bus.ram
    rep.p('Object slots (active, end of run):')
    rep.p('  slot  ai  x     y     meta dir flags coll  hp  scr_x scr_y AI handler')
    for x in range(32):
        ai = r[0x4A0 + x]
        if not ai:
            continue
        a = 0xA314 + 2 * (ai & 0x7F)
        hnd = rom(27, a) | rom(27, a + 1) << 8
        ok = hnd != 0 or x == 0x1F
        rep.call('struct: obj_active -> ObjAiTable non-null', ok, f'slot {x:02X} ai {ai:02X}')
        rep.p(f'  {x:02X}    {ai:02X}  {r[0x90 + x]:02X}{r[0x70 + x]:02X}  {r[0xD0 + x]:02X}{r[0xB0 + x]:02X}  '
              f'{r[0x300 + x]:02X}   {r[0x360 + x]:02X}  {r[0x380 + x]:02X}    {r[0x3A0 + x]:02X}    '
              f'{r[0x3C0 + x]:02X}  {r[0x5C0 + x]:02X}    {r[0x5E0 + x]:02X}    27:{hnd:04X}')
    ok = r[0x4A1] != 0
    rep.call('struct: slot 1 = player active', ok, f'obj_active_s01={r[0x4A1]:02X}')
    ok = 0 < r[0x3C1] <= r[0x3C0]
    rep.call('struct: HP slot1 <= max slot0', ok, f'{r[0x3C1]:02X}/{r[0x3C0]:02X}')
    rep.p(f'  player HP $03C1={r[0x3C1]:02X} max $03C0={r[0x3C0]:02X}; slot 0 active={r[0x4A0]:02X} '
          f'pos {r[0x90]:02X}{r[0x70]:02X},{r[0xD0]:02X}{r[0xB0]:02X}; slot 1 pos {r[0x91]:02X}{r[0x71]:02X},{r[0xD1]:02X}{r[0xB1]:02X}')


def check_sound(nes, rep):
    r = nes.bus.ram
    music = getattr(rep, 'music', [])
    rep.p('Sound channels (end of run):')
    rep.p('  ch flags ptr   wait len oct env vib reg0 period')
    starts = music[-1][3] if music else None
    for c in range(6):
        fl = r[0x114 + c]
        ptr = r[0x11A + c] | r[0x120 + c] << 8
        per = r[0x174 + c] | r[0x17A + c] << 8
        rep.p(f'  {c}  {fl:02X}    {ptr:04X}  {r[0x14A + c]:02X}   {r[0x150 + c]:02X}  {r[0x156 + c]:02X}  '
              f'{r[0x15C + c]:02X}  {r[0x162 + c]:02X}  {r[0x168 + c]:02X}   {per:04X}')
        if fl & 1:
            ok = 0x8000 <= ptr < 0xC000 and r[0x156 + c] & 0x0F == 0
            rep.call('struct: active channel ptr in $8000-$BFFF, octave*16', ok, f'ch {c} ptr {ptr:04X} oct {r[0x156 + c]:02X}')
            if starts and c < 4:
                ok = ptr >= starts[c]
                rep.call('struct: music ptr >= stream start', ok, f'ch {c} {ptr:04X} < {starts[c]:04X}')


def check_save(nes, rep):
    w = nes.bus.wram
    cur, mir = bytes(w[0x1D00:0x2000]), bytes(w[0x0D00:0x1000])
    rep.call('struct: save $7D00 == mirror $6D00', cur == mir, 'differs')
    rep.call('struct: save tag AMI at $7DFD', cur[0xFD:0x100] == b'AMI', cur[0xFD:0x100].hex())
    tpl = bytes(rom(23, 0xBE00 + i) for i in range(256))
    rep.p(f'Save image $7DF0-$7DFF: {cur[0xF0:0x100].hex(" ")}  (template {tpl[0xF0:0x100].hex(" ")})')
    for f, pl, ok, n in getattr(rep, 'saves', []):
        rep.p(f'  SaveGame at f{f}: +F0-F8 = {pl}; {ok}/{n} field checks')


def check_title_copy(rep):
    """Boot copy quirk: CopySramBlock $7D00->$6480, $7E00->$6580 (one page off)."""
    nes = NES()
    state.load(nes, os.path.join(STATES, 'title.state'))
    w = nes.bus.wram
    a = bytes(w[0x480:0x580]) == bytes(w[0x1D00:0x1E00])
    b = bytes(w[0x580:0x680]) == bytes(w[0x1E00:0x1F00])
    rep.call('struct: title $6480==$7D00 and $6580==$7E00 (boot copy quirk)', a and b, f'{a} {b}')
    rep.p(f'Title state: $6480 page == $7D00 page: {a}; $6580 page == $7E00 page: {b}; '
          f'$6480.. = {bytes(w[0x480:0x488]).hex(" ")}')


# ---------------------------------------------------------------- main
def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--state', default=os.path.join(STATES, 'name_entry.state'))
    ap.add_argument('--to', type=int, default=2460)
    ap.add_argument('--keys', nargs='+', default=['2300:START', '2340:DOWN:80'])
    ap.add_argument('--out')
    a = ap.parse_args(argv)
    rep = Report()
    nes = NES()
    state.load(nes, a.state)
    nes.keys = debug.parse_keys(a.keys)
    f0 = nes.ppu.frame_no
    w_order(nes, rep)
    for fn in (w_switchbank, w_playsound, w_sprites, w_palette, w_objinit, w_areaload, w_music, w_savegame):
        fn(nes, rep)
    while nes.ppu.frame_no < a.to:
        nes.run_frame()

    out = [f'verify.py: {os.path.basename(a.state)} f{f0} -> f{nes.ppu.frame_no}, keys {" ".join(a.keys)}', '']
    out.append('(a) routine checks: calls / matching the REVERSE.md model')
    for name in ('SwitchBank16', 'PlaySound', 'ObjSpriteDraw', 'PaletteBuild', 'ObjInit', 'AreaLoad', 'SndStartMusic'):
        n, ok, fails = rep.stats.get(name, [0, 0, []])
        out.append(f'  {name:14s} {ok:5d} / {n:<5d} {"PASS" if n and ok == n else ("NOT CALLED" if not n else "FAIL")}')
        out += [f'      ! {m}' for m in fails]
    out.append(f'  SwitchBank16 n values seen: {" ".join(f"{v:02X}" for v in sorted(getattr(rep, "sb_ids", [])))}')
    out.append(f'  PlaySound ids: {" ".join(f"{v:02X}" for v in getattr(rep, "snd_ids", []))}')
    out.append(f'  ObjInit (frame, slot, type): {getattr(rep, "inits", [])[:40]}')
    out.append(f'  AreaLoad (frame, area): {getattr(rep, "areas", [])}')
    out.append(f'  SndStartMusic (frame, id, header, ptrs): '
               f'{[(f, i, hex(h), [hex(p) for p in ps]) for f, i, h, ps in getattr(rep, "music", [])]}')
    if hasattr(rep, 'pal_last'):
        out.append(f'  last PaletteBuild: ids {rep.pal_last[0]} fade {rep.pal_last[1]:02X} mask {rep.pal_last[2]:02X}')
        out.append(f'                     pal_buf {rep.pal_last[3]}')
    # frame order collision vs sprite pass
    seq = {}
    for kind, f in rep.order:
        seq.setdefault(f, []).append(kind)
    pats = {}
    for f, s in seq.items():
        pats.setdefault(' '.join(s), []).append(f)
    out.append('  per-frame order (ObjCollideAll=coll, ObjSpriteDraw=draw): ' +
               '; '.join(f'[{k}] x{len(v)} (f{v[0]}-{v[-1]})' for k, v in pats.items()))
    out.append('')
    out.append('(b) struct checks at the end of the run')
    check_area_header(nes, rep)
    check_objects(nes, rep)
    check_sound(nes, rep)
    check_save(nes, rep)
    check_title_copy(rep)
    out += rep.lines
    out.append('')
    for name, (n, ok, fails) in rep.stats.items():
        if name.startswith('struct'):
            out.append(f'  {name}: {ok}/{n} {"PASS" if ok == n else "FAIL"}' + ''.join(f'\n      ! {m}' for m in fails))
    text = '\n'.join(out) + '\n'
    if a.out:
        with open(a.out, 'w') as f:
            f.write(text)
    print(text, end='')
    bad = sum(n - ok for n, ok, _ in rep.stats.values())
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())
