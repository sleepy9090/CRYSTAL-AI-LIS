#!/usr/bin/env python3
"""Ph6 validate 2/4 findings sweep: emulator checks for the Key Findings subsections that
tools/emu/verify.py (session 39) does not cover. Self-emu only (no reference emulator).

usage: python3 tools/emu/sweep.py [--to FRAME] [--out FILE]
  power-on -> canonical --keys script -> f2460 (title, menus, new game, area 0 -> 1)

checks (each compared against the REVERSE.md text):
  boot      31:F2A4  SndInit 24:8020 runs once before MainLoop; at the first MainLoop:
                     $40=2, $51=3, $0054=$F421, $6200-$621F=$FF, "USO" at $0182, $07FE=9,
                     $4017<-$40, last PPUCTRL write $88
  mainloop  30:C900  every dispatch at 30:C916 jumps to MainStateTable[$40]; $09 cleared after
  nmi       31:F3B6  paths: $60!=0 -> nothing; $06!=0 -> $07=1; $09!=0 -> no OAM DMA / no $07D8
                     copy; else $4014<-2 and $07D8-$07DB = $02-$05 (or $83/$A3/$C3/$E3 if $51 = 7/9)
  irq       31:F443  SetIrqHandler 31:F424: $54/$55 = IrqHandlerTable[A], $56 = A; every IRQ
                     dispatch target is a table entry or $F421; handler/scanline histogram
  far-call  31:FF80.. trampolines: $6E/$6F and the mapped banks restored on return
  oamhide   28:80A5  ObjHideUnusedOam: OAM Y=$F0 from $10 to $FF ($10=0 -> nothing); sprite 0
                     untouched by ObjSpriteDraw
  sound     31:F7FE  SoundUpdate: at most 1 SndCommand 24:8048 per frame
            24:86AA  SndVibratoTick exact model (phase, half speed, off-by-one Y, lo-only write)
"""
import argparse
import dataclasses  # noqa: F401  (see verify.py: load stdlib dis before tools/ is on sys.path)
import os
import sys

TOOLS = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.append(TOOLS)
from emu.nes import NES  # noqa: E402
from emu import debug  # noqa: E402
from emu.verify import Report, CallWatch, rom  # noqa: E402

CANON_KEYS = '1250:START 1400:START 1600:START 1700:START 2300:START 2340:DOWN:80'


def w16(bank, addr):
    return rom(bank, addr) | rom(bank, addr + 1) << 8


IRQ_TABLE = [w16(31, 0xF433 + 2 * i) for i in range(8)]
STATE_TABLE = [w16(30, 0xC91F + 2 * i) for i in range(6)]


# ---------------------------------------------------------------- boot
def w_boot(nes, rep):
    rep.boot = {'sndinit': [], 'ppuctrl': [], 'r4017': [], 'first_loop': None}
    b = rep.boot
    nes.on_exec(24, 0x8020, lambda n: b['sndinit'].append((n.ppu.frame_no, n.mapper.prg_bank_at(0x8000))))
    nes.on_write(0x2000, lambda a, v: b['ppuctrl'].append(v) if b['first_loop'] is None else None)
    nes.on_write(0x4017, lambda a, v: b['r4017'].append(v) if b['first_loop'] is None else None)
    for a in (0x51, 0x54, 0x55, 0x6140):
        b[a] = []

        def wr(addr, v, a=a):
            if b['first_loop'] is None:
                b[a].append((v, nes.cpu.pc, nes.mapper.prg_bank_at(nes.cpu.pc) if nes.cpu.pc >= 0x8000 else '*'))
        nes.on_write(a, wr)

    def loop(n):
        if b['first_loop'] is not None:
            return
        r, w = n.bus.ram, n.bus.wram
        b['first_loop'] = n.ppu.frame_no
        chk = [
            ('SndInit 24:8020 ran exactly once, banks 24/25 mapped', len(b['sndinit']) == 1 and b['sndinit'][0][1] == 24,
             b['sndinit']),
            ('$40 main_state = 2', r[0x40] == 2, r[0x40]),
            ('first $51 irq_mode write = 3', b[0x51][:1] and b[0x51][0][0] == 3, b[0x51][:4]),
            ('first $0054/$0055 writes = $F421 (IrqDefault)',
             b[0x54][:1] and b[0x55][:1] and b[0x54][0][0] | b[0x55][0][0] << 8 == 0xF421, (b[0x54][:3], b[0x55][:3])),
            ('first $6140 pal_buf write = $FF', b[0x6140][:1] and b[0x6140][0][0] == 0xFF, b[0x6140][:3]),
            ('$6200-$621F all $FF', all(v == 0xFF for v in w[0x200:0x220]), bytes(w[0x200:0x220]).hex()),
            ('"USO" at $0182-$0184', bytes(r[0x182:0x185]) == b'USO', bytes(r[0x182:0x185])),
            ('$07FE irq_delay = 9', r[0x7FE] == 9, r[0x7FE]),
            ('$4017 <- $40', 0x40 in b['r4017'], b['r4017']),
            ('PPUCTRL written $88 during boot, $88 at MainLoop', 0x88 in b['ppuctrl'] and b['ppuctrl'][-1] == 0x88,
             sorted({hex(v) for v in b['ppuctrl']})),
        ]
        # $6140 pal_buf is filled $FF at reset; the title PaletteBuild may run before MainLoop
        b['pal'] = bytes(w[0x140:0x160]).hex()
        for name, ok, got in chk:
            rep.call('boot', ok, f'{name}: {got}')
            rep.p(f'  {"OK  " if ok else "FAIL"} boot: {name}  ({got})')
    nes.on_exec(30, 0xC900, loop)


# ---------------------------------------------------------------- main loop
def w_mainloop(nes, rep):
    rep.states = {}

    def disp(n):
        r = n.bus.ram
        st, tgt = r[0x40], r[0x11] | r[0x12] << 8
        ok = st < 6 and r[0x10] == 0x4C and tgt == STATE_TABLE[st]
        rep.call('mainloop dispatch', ok, f'f{n.ppu.frame_no} $40={st} target {tgt:04X}')
        rep.states.setdefault(st, [n.ppu.frame_no, 0])[1] += 1
    nes.on_exec(30, 0xC916, disp)
    nes.on_exec(30, 0xC91D, lambda n: rep.call('mainloop $09 cleared', n.bus.ram[0x09] == 0, ''))


# ---------------------------------------------------------------- NMI
def w_nmi(nes, rep):
    rep.nmi = {'disabled': 0, 'lag': 0, 'busy_frame': 0, 'full': 0, 'mode79': 0}
    ctx = {}

    def entry(n):                       # F3BA: after PHA/LDA PPUSTATUS
        r = n.bus.ram
        ctx.update(e60=r[0x60], e06=r[0x06], e09=r[0x09], dma=[], f=n.ppu.frame_no)
    nes.on_exec(31, 0xF3BA, entry)
    nes.on_write(0x4014, lambda a, v: ctx.get('dma', []).append(v))

    def early(n):                       # F3C6 PLA/RTI early exit
        r = n.bus.ram
        if ctx['e60']:
            rep.nmi['disabled'] += 1
            rep.call('nmi', True)
        else:
            rep.nmi['lag'] += 1
            rep.call('nmi', ctx['e06'] != 0 and r[0x07] == 1, f'f{ctx["f"]} lag exit $06={ctx["e06"]} $07={r[0x07]}')
    nes.on_exec(31, 0xF3C6, early)

    def mid(n):                         # F40A: after the DMA/copy block (or skipped)
        r = n.bus.ram
        if ctx['e09']:
            rep.nmi['busy_frame'] += 1
            rep.call('nmi', not ctx['dma'], f'f{ctx["f"]} $09!=0 but DMA {ctx["dma"]}')
            return
        rep.nmi['full'] += 1
        if r[0x51] in (7, 9):
            rep.nmi['mode79'] += 1
            exp = bytes([r[0x83], r[0xA3], r[0xC3], r[0xE3]])
        else:
            exp = bytes(r[0x02:0x06])
        got = bytes(r[0x7D8:0x7DC])
        rep.call('nmi', ctx['dma'] == [2] and got == exp,
                 f'f{ctx["f"]} dma {ctx["dma"]} $07D8 {got.hex()} exp {exp.hex()} $51={r[0x51]}')
    nes.on_exec(31, 0xF40A, mid)


# ---------------------------------------------------------------- IRQ
def w_irq(nes, rep):
    rep.irq_hist = {}

    def enter(n):
        return n.cpu.a

    def ret(n, a):
        r = n.bus.ram
        ok = a < 8 and r[0x54] | r[0x55] << 8 == IRQ_TABLE[a] and r[0x56] == a
        rep.call('SetIrqHandler', ok, f'A={a} $54={r[0x55]:02X}{r[0x54]:02X} $56={r[0x56]}')
    CallWatch(nes, 31, 0xF424, enter, ret)

    def disp(n):
        r = n.bus.ram
        v = r[0x54] | r[0x55] << 8
        ok = v in IRQ_TABLE or v == 0xF421
        rep.call('IRQ dispatch target', ok, f'f{n.ppu.frame_no} line {n.ppu.line} -> {v:04X}')
        key = (IRQ_TABLE.index(v) if v in IRQ_TABLE else 'def', n.ppu.line)
        rep.irq_hist[key] = rep.irq_hist.get(key, 0) + 1
    nes.on_exec(31, 0xF44E, disp)


# ---------------------------------------------------------------- far-call trampolines
def w_farcall(nes, rep):
    rep.farcalls = {}
    for a in (0xFF80, 0xFF8C, 0xFF9D, 0xFFA9, 0xFFBE, 0xFFCF):
        def enter(n):
            r = n.bus.ram
            return r[0x6E], r[0x6F], n.mapper.prg_bank_at(0x8000), n.mapper.prg_bank_at(0xA000)

        def ret(n, c, a=a):
            r = n.bus.ram
            got = (r[0x6E], r[0x6F], n.mapper.prg_bank_at(0x8000), n.mapper.prg_bank_at(0xA000))
            rep.farcalls[a] = rep.farcalls.get(a, 0) + 1
            rep.call('far-call restore', got == c, f'{a:04X} before {c} after {got}')
        CallWatch(nes, 31, a, enter, ret)


# ---------------------------------------------------------------- OAM hide + sprite 0
def w_oamhide(nes, rep):
    def enter(n):
        return n.bus.ram[0x10]

    def ret(n, p):
        r = n.bus.ram
        bad = [i for i in range(p, 0x100, 4) if r[0x200 + i] != 0xF0] if p else []
        rep.call('ObjHideUnusedOam', not bad, f'f{n.ppu.frame_no} $10={p:02X} not hidden {bad[:6]}')
    CallWatch(nes, 28, 0x80A5, enter, ret)

    def enter2(n):
        return bytes(n.bus.ram[0x200:0x204])

    def ret2(n, s0):
        rep.call('ObjSpriteDraw keeps sprite 0', bytes(n.bus.ram[0x200:0x204]) == s0,
                 f'{s0.hex()} -> {bytes(n.bus.ram[0x200:0x204]).hex()}')
    CallWatch(nes, 28, 0x800E, enter2, ret2)


# ---------------------------------------------------------------- sound
def w_sound(nes, rep):
    rep.cmds = {}
    rep.sndupd = {}
    for site in (0xF4CF, 0xF611, 0xF6DC, 0xF76B, 0xF77F):
        nes.on_exec(31, site, lambda n, s=site: rep.sndupd.__setitem__(
            (s, n.bus.ram[0x51]), rep.sndupd.get((s, n.bus.ram[0x51]), 0) + 1))
    nes.on_exec(24, 0x8048, lambda n: rep.cmds.__setitem__(n.ppu.frame_no, rep.cmds.get(n.ppu.frame_no, 0) + 1))
    apu = []
    for a in (0x4002, 0x4006, 0x400A, 0x400E):
        nes.on_write(a, lambda a, v: apu.append((a, v)))
    rep.vib = {'calls': 0, 'skip_b7': 0, 'wait_env': 0, 'written': 0, 'hi_block': 0, 'depths': set(), 'octs': set()}

    def enter(n):
        r, x = n.bus.ram, n.cpu.x
        apu.clear()
        return (x, r[0x114 + x], r[0x15C + x], r[0x162 + x], r[0x156 + x], r[0x174 + x], r[0x17A + x], r[0xFD])

    def ret(n, c):
        x, fl, env, vib, octv, plo, phi, fd = c
        r, v = n.bus.ram, rep.vib
        v['calls'] += 1
        exp_vib, exp_fd, exp_w = vib, fd, []
        if fl & 0x80:
            v['skip_b7'] += 1
        elif fl & 0x40 and env & 0x0F != 0x0F:
            v['wait_env'] += 1
        else:
            exp_fd = (fd + 1) & 0xFF
            ph = (vib + 1) & 7
            exp_vib = (vib & 0xF8) | ph
            p = (ph >> 1 if vib & 0x08 else ph) & 3
            e = rom(24, 0x8714 + (((vib >> 2) & 0xFC) | p))
            y = (((e - 2) & 0xFF) | octv) + 4 & 0xFF if e else 0
            off = rom(24, 0x8724 + y) | rom(24, 0x8725 + y) << 8
            s = plo + (off & 0xFF)
            lo, hi = s & 0xFF, (phi + (off >> 8) + (s >> 8)) & 0xFF
            v['depths'].add(vib >> 3)
            v['octs'].add(octv >> 4)
            if hi == phi:
                exp_w = [(0x4002 + rom(24, 0x87AC + x), lo)]
                v['written'] += 1
            else:
                v['hi_block'] += 1
        got = (r[0x162 + x], r[0xFD], list(apu), r[0x174 + x], r[0x17A + x])
        exp = (exp_vib, exp_fd, exp_w, plo, phi)
        rep.call('SndVibratoTick', got == exp, f'f{n.ppu.frame_no} ch{x} in {[hex(q) for q in c]} got {got} exp {exp}')
    CallWatch(nes, 24, 0x86AA, enter, ret)


# ---------------------------------------------------------------- main
def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--to', type=int, default=2460)
    ap.add_argument('--keys', nargs='+', default=[CANON_KEYS])
    ap.add_argument('--out')
    a = ap.parse_args(argv)
    rep = Report()
    nes = NES()
    keys = ' '.join(a.keys).split()
    nes.keys = debug.parse_keys(keys)
    for fn in (w_boot, w_mainloop, w_nmi, w_irq, w_farcall, w_oamhide, w_sound):
        fn(nes, rep)
    while nes.ppu.frame_no < a.to:
        nes.run_frame()

    out = [f'sweep.py: power-on -> f{nes.ppu.frame_no}, keys {" ".join(keys)}', '']
    out += rep.lines
    out.append(f'  boot: first MainLoop at f{rep.boot["first_loop"]}, $6140 then {rep.boot.get("pal")}')
    for ad in (0x51, 0x54, 0x55):
        runs = []
        for w in rep.boot[ad]:
            if runs and runs[-1][0] == w:
                runs[-1][1] += 1
            else:
                runs.append([w, 1])
        out.append(f'  boot: writes to ${ad:04X} before MainLoop (value pc bank xN): '
                   + ' '.join(f'({v:02X} {pc:04X} {bk} x{c})' for (v, pc, bk), c in runs))
    out.append('')
    for name, (n, ok, fails) in rep.stats.items():
        out.append(f'  {name:30s} {ok:6d} / {n:<6d} {"PASS" if ok == n else "FAIL"}')
        out += [f'      ! {m}' for m in fails]
    out.append('')
    out.append('main states dispatched (state: first frame, dispatches): ' +
               ', '.join(f'{k}: f{v[0]} x{v[1]}' for k, v in sorted(rep.states.items())))
    out.append(f'NMI paths: {rep.nmi}')
    hist = {}
    for (h, line), c in rep.irq_hist.items():
        hist.setdefault(h, {})[line] = c
    out.append('IRQ handler -> {scanline: count}: ' +
               '; '.join(f'{h}: {dict(sorted(d.items()))}' for h, d in sorted(hist.items(), key=str)))
    out.append(f'far-call trampolines returned: { {hex(k): v for k, v in rep.farcalls.items()} }')
    mx = max(rep.cmds.values()) if rep.cmds else 0
    rep.call('SndCommand <= 1 per frame', mx <= 1, f'max {mx}')
    out.append('SoundUpdate call sites (site, irq_mode): ' +
               ', '.join(f'{s:04X} mode {m}: {c}' for (s, m), c in sorted(rep.sndupd.items())))
    out.append(f'SndCommand frames: {len(rep.cmds)}, max per frame {mx} {"PASS" if mx <= 1 else "FAIL"}')
    v = dict(rep.vib)
    v['depths'] = sorted(v['depths'])
    v['octs'] = sorted(v['octs'])
    out.append(f'SndVibratoTick paths: {v}')
    text = '\n'.join(out) + '\n'
    if a.out:
        with open(a.out, 'w') as f:
            f.write(text)
    print(text, end='')
    return 1 if sum(n - ok for n, ok, _ in rep.stats.values()) else 0


if __name__ == '__main__':
    sys.exit(main())
