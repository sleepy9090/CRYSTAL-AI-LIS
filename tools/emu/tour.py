#!/usr/bin/env python3
"""Ph6 validate 3/4: area tour of tools.emu over the k3 exit graph (self-emu only).

usage: python3 tools/emu/tour.py graph                 # exit graph from area 0, no emulation
       python3 tools/emu/tour.py run [--state F] [--max N] [--settle N] [--coverage]
                                     [--only AA,BB..] [--out F] [--shots DIR] [--save-states]

Route: start from tools/emu/states/area1_enter.state (area 1, sub_state 8). Greedy walk: if the
current area has a k3 exit to an unvisited area, use it *for real*: slot 0 and slot 1 are moved
onto the exit cell (x = xc*16+8, y = yc*16+8; ExitLookup 31:EB09 reads slot 0) and the game's own
ExitLookup -> sub_state 1 -> AreaLoad path loads the target. Otherwise ("poke" leg) the next
unvisited area of the BFS order is loaded the way the story warps do (30:D5A4): area_id $6C,
area_entry $6D, sub_state $41 = 1, using the entry of the exit that first reached it in the BFS.
A leg ends when sub_state is back to 8 with the new area_id, then --settle frames run.
If a leg gets stuck (no sub_state 8 after 600 frames) the last good state is restored.

Per area: legs/method, frames to load, music $0102, MMC3 R0-R5, palette ids $07E0-$07E7,
$07F0/$07F1/$07F4/$07F5 shadows, active object slots (obj_active $04A0 + meta $0300) vs the
area_dump spawn list, and the playfield (248x184 at camera, y-4) vs gfx/areas/area_NNN.png.
Installs the verify.py call models and the sweep.py checks for the whole run; --coverage adds
an instruction tracer and reports executed (bank, pc) per bank and never-run code labels.
"""
import argparse
import dataclasses  # noqa: F401  (load stdlib dis before tools/ is on sys.path; see verify.py)
import os
import sys
import time
from collections import deque

TOOLS = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.append(TOOLS)
from emu.nes import NES  # noqa: E402
from emu import state, verify, sweep  # noqa: E402
from emu.verify import Report  # noqa: E402
import area_dump  # noqa: E402

from emu.pads import parse_buttons  # noqa: E402

STATES = os.path.join(TOOLS, 'emu', 'states')
BTN_A = parse_buttons('A')
ROOT = os.path.dirname(TOOLS)


# ---------------------------------------------------------------- static exit graph
def exits(area):
    """k3 list: [(xc, yc, area, entry)]."""
    _, ps = area_dump.sub_header(area)
    p, out = ps[3], []
    if not 0x8000 <= p < 0xC000:
        return out
    while area_dump.rd10(p) != 0xFF and len(out) < 64:
        out.append(tuple(area_dump.rd10(p + i) for i in range(4)))
        p += 4
    return out


def valid_area(a):
    """In the table and either has a spawn record or (s75) a valid AreaSubTable header: AreaLoad
    31:E16B (BPL) just skips the spawn record when the AreaPtrTable ptr has b15 clear (area $A7,
    the only way into $58-$5F, has no spawn record)."""
    if a >= area_dump.table_len():
        return False
    if area_dump.ptr(a) & 0x8000:
        return True
    h, ps = area_dump.sub_header(a)
    return bool(h & 0x8000) and 0x8000 <= ps[0] < 0xC000 and 0x8000 <= ps[3] < 0xC000


def graph(start=0):
    """BFS over k3 exits -> order [area], first[area] = (src, xc, yc, entry)."""
    first, order, q = {start: None}, [start], deque([start])
    while q:
        a = q.popleft()
        for xc, yc, t, e in exits(a):
            if t not in first and valid_area(t):
                first[t] = (a, xc, yc, e)
                order.append(t)
                q.append(t)
    return order, first


def cmd_graph():
    order, first = graph()
    n = area_dump.table_len()
    print(f'k3 exit graph from area 0: {len(order)} reachable of {n} table entries '
          f'({sum(1 for a in range(n) if valid_area(a))} with an area record)')
    for a in order:
        f = first[a]
        src = 'start' if f is None else f'from {f[0]:02X} cell ({f[1]:X},{f[2]:X}) entry {f[3]:02X}'
        print(f'  {a:02X}  {src}  exits -> ' + ' '.join(f'{t:02X}' for t in sorted({e[2] for e in exits(a)})))
    miss = [a for a in range(n) if valid_area(a) and a not in first]
    print(f'not reachable by k3 exits ({len(miss)}): ' + ' '.join(f'{a:02X}' for a in miss))


# ---------------------------------------------------------------- screen compare
_AREA_PNG = {}


def area_png(a):
    if a not in _AREA_PNG:
        p = os.path.join(ROOT, 'gfx', 'areas', f'area_{a:03d}.png')
        if os.path.exists(p):
            from render_compare import read_png
            _AREA_PNG[a] = read_png(p)
        else:
            _AREA_PNG[a] = None
    return _AREA_PNG[a]


def _cmp(emu, px, w, h, dx, dy, step):
    n = bad = 0
    for y in range(4, 188, step):
        yy = y + dy
        if yy < 0 or yy >= h:
            continue
        row = yy * w + dx
        for x in range(8, 256, step):
            if 0 <= x + dx < w:
                n += 1
                if emu[y * 256 + x] != px[row + x]:
                    bad += 1
    return n, bad


def compare_playfield(nes, a):
    """Emu playfield (x 8..255, y 4..187) vs area PNG pixel (x + dx, y + dy).
    Camera Y is (screen row $05, line $04 0-239), so the expected offset is dx = cam_x,
    dy = s8($05)*240 + $04 (session 42). dx/dy are searched +-3 around it on a 1/4 subsample,
    then the best offset gets a full compare. Returns (compared, mismatches, cam_x, cam_y, dx, dy)."""
    img = area_png(a)
    if img is None:
        return None
    w, h, px = img
    r = nes.bus.ram
    cx = r[0x02] | r[0x03] << 8
    cx = cx - 0x10000 if cx >= 0x8000 else cx
    cy = (r[0x05] - 256 if r[0x05] >= 0x80 else r[0x05]) * 240 + r[0x04]
    rgb = nes.ppu.to_rgb()
    emu = [tuple(rgb[i:i + 3]) for i in range(0, len(rgb), 3)]
    return compare_pixels(emu, a, cx, cy)


def compare_pixels(emu, a, cx, cy):
    img = area_png(a)
    if img is None:
        return None
    w, h, px = img
    best = None
    for dy in range(cy - 3, cy + 4):
        for dx in range(cx - 3, cx + 4):
            n, bad = _cmp(emu, px, w, h, dx, dy, 4)
            if n > 500 and (best is None or bad / n < best[0]):
                best = (bad / n, dx, dy)
    if best is None:
        return 0, 0, cx, cy, 0, 0
    n, bad = _cmp(emu, px, w, h, best[1], best[2], 1)
    return n, bad, cx, cy, best[1], best[2]


# ---------------------------------------------------------------- legs
class Tour:
    def __init__(self, nes, settle, shots):
        self.nes, self.settle, self.shots = nes, settle, shots
        self.rows = []
        self.substates = {}
        self.msg_frames = 0
        nes.on_frame(self._frame)

    def _frame(self, nes):
        r = nes.bus.ram
        k = (r[0x40], r[0x41])
        self.substates[k] = self.substates.get(k, 0) + 1

    def to_gameplay(self, limit=400):
        """Tap A (1 frame in 8) until sub_state is 8 again (message box $10 etc.); True if reached."""
        nes = self.nes
        r = nes.bus.ram
        t0 = nes.ppu.frame_no
        while r[0x41] != 8:
            f = nes.ppu.frame_no - t0
            if f >= limit:
                return False
            nes.keys = {nes.ppu.frame_no: BTN_A} if f % 8 == 4 else {}
            nes.run_frame()
            self.msg_frames += 1
        nes.keys = {}
        return True

    def wait_loaded(self, target, limit=600):
        nes = self.nes
        r = nes.bus.ram
        t0 = nes.ppu.frame_no
        left = seen = False
        self.bounce = None
        while nes.ppu.frame_no - t0 < limit:
            # dialogue boxes / cut-scenes: tap A after 150 frames
            f = nes.ppu.frame_no - t0
            nes.keys = {nes.ppu.frame_no: BTN_A} if f > 150 and f % 8 == 4 else {}
            nes.run_frame()
            if r[0x41] != 8:
                left = True
            if left and r[0x41] == 8 and r[0x6C] == target:
                return nes.ppu.frame_no - t0
            seen |= left and r[0x6C] == target
            if seen and r[0x6C] != target and r[0x41] == 8:
                # s75: the area loaded but an entry event (e.g. $E0 act 7 message) ended on an exit
                # cell, so ExitLookup took the player straight on; count it as loaded
                self.bounce = r[0x6C]
                return nes.ppu.frame_no - t0
        return None

    def leg_exit(self, xc, yc):
        """Put slot 0/1 on the exit cell; True if ExitLookup took it within 4 frames."""
        nes = self.nes
        r = nes.bus.ram
        a0 = r[0x6C]
        x, y = xc * 16 + 8, yc * 16 + 8
        for _ in range(4):
            for s in (0, 1):
                r[0x70 + s], r[0x90 + s] = x & 0xFF, x >> 8
                r[0xB0 + s], r[0xD0 + s] = y & 0xFF, y >> 8
            nes.keys = {}
            nes.run_frame()
            if r[0x41] != 8 or r[0x6C] != a0:
                return True
        return False

    def leg_poke(self, area, entry):
        r = self.nes.bus.ram
        r[0x6C], r[0x6D], r[0x41] = area, entry, 1

    def record(self, area, method, frames):
        nes = self.nes
        r, m = nes.bus.ram, nes.mapper
        for _ in range(self.settle):
            nes.keys = {}
            nes.run_frame()
        d = area_dump.decode(area)
        exp_slots = sorted(x for x, e in d['ents'] if isinstance(e, tuple)) if d and 'ents' in d else []
        act = {s: (r[0x4A0 + s], r[0x300 + s]) for s in range(32) if r[0x4A0 + s]}
        cmp = compare_playfield(nes, area)
        row = {
            'area': area, 'entry': r[0x6D], 'method': method, 'frames': frames, 'f': nes.ppu.frame_no,
            'music': r[0x102], 'chr': list(m.regs[0:6]), 'pal': bytes(r[0x7E0:0x7E8]).hex(),
            'shadow': bytes([r[0x7F0], r[0x7F1], r[0x7F4], r[0x7F5]]).hex(),
            'exp_slots': exp_slots, 'act': act,
            'spawned': sum(1 for s in exp_slots if s in act), 'cmp': cmp,
            'state': (r[0x40], r[0x41]), 'now': r[0x6C],   # s75: real area_id at the end of the leg
        }
        if self.shots:
            nes.dump_screen(os.path.join(self.shots, f'area_{area:03d}.png'))
        self.rows.append(row)
        return row


def fmt_row(w):
    c = w['cmp']
    cs = 'no png' if c is None else (f'{100.0 * c[1] / max(c[0], 1):5.1f}% diff of {c[0]} '
                                     f'cam {c[2]},{c[3]} best {c[4] - c[2]:+d},{c[5] - c[3]:+d}')
    act = ' '.join(f'{s:02X}:{ai:02X}/{mt:02X}' for s, (ai, mt) in sorted(w['act'].items()))
    return (f'  {w["area"]:02X}.{w["entry"]:02X} {w["method"]:5s} load {w["frames"] if w["frames"] is not None else "STUCK":>5} '
            f'f{w["f"]:<6d} mus {w["music"]:02X} chr {" ".join(f"{v:02X}" for v in w["chr"])} '
            f'pal {w["pal"]} sh {w["shadow"]} spawn {w["spawned"]}/{len(w["exp_slots"])}  bg {cs}'
            + (f'  NOW IN {w["now"]:02X}' if w.get('now', w['area']) != w['area'] else '') + '\n'
            f'        slots(ai/meta) {act}')


# ---------------------------------------------------------------- coverage
def code_labels():
    import disasm_all
    from nesrom import load_labels
    out = []
    for (b, a), (name, _) in load_labels().items():
        if b == '*' or disasm_all.is_data_label(name) or a < 0x8000:
            continue
        out.append((int(b), a, name))
    return out


def cmd_run(a):
    nes = NES()
    state.load(nes, a.state)
    rep = Report()
    verify.w_order(nes, rep)
    for fn in (verify.w_switchbank, verify.w_playsound, verify.w_sprites, verify.w_palette,
               verify.w_objinit, verify.w_areaload, verify.w_music, verify.w_savegame,
               sweep.w_mainloop, sweep.w_nmi, sweep.w_irq, sweep.w_farcall, sweep.w_oamhide,
               sweep.w_sound):
        fn(nes, rep)
    upd_frames = {}                  # frame -> [SoundUpdate call sites] (2-commands-per-frame check)
    for site in (0xF4CF, 0xF611, 0xF6DC, 0xF76B, 0xF77F):
        nes.on_exec(31, site, lambda n, s=site: upd_frames.setdefault(n.ppu.frame_no, []).append(
            f'{s:04X}@{n.ppu.line}'))
    cov = set()
    if a.coverage:
        bank_at = nes.mapper.prg_bank_at
        add = cov.add

        def tr(cpu):
            pc = cpu.pc
            add((bank_at(pc) if pc >= 0x8000 else -1, pc))
        nes.set_tracer(tr)
    if a.shots:
        os.makedirs(a.shots, exist_ok=True)
    tour = Tour(nes, a.settle, a.shots)
    order, first = graph()
    into = {}
    for s in order:
        for xc, yc, t, e in exits(s):
            into.setdefault(t, []).append((s, xc, yc, e))
    only = [int(x, 16) for x in a.only.split(',')] if a.only else None
    todo = [t for t in order if t not in (0, 1)] if only is None else only
    visited = {0, 1}
    good = state.snapshot(nes)
    t_start = time.time()
    tour.record(nes.bus.ram[0x6C], 'start', 0)
    while todo and len(tour.rows) <= a.max:
        cur = nes.bus.ram[0x6C]
        nxt = None
        if True:                          # s75: --only legs also take real exits when they can
            for xc, yc, t, e in exits(cur):
                if t in todo and valid_area(t):
                    nxt = (t, 'exit', xc, yc, e)
                    break
        if nxt is None:
            t = todo[0]
            ins = into.get(t, [])
            plain = [x for x in ins if not x[3] & 0x20]
            if plain or not ins:
                e = plain[0][3] if plain else 0
                nxt = (t, 'poke', None, None, e)
            else:                         # only "keep position" ($2x) entries: go via the source
                src, xc, yc, e = ins[0]
                se = next((x[3] for x in into.get(src, []) if not x[3] & 0x20), 0)
                tour.to_gameplay()
                tour.leg_poke(src, se)
                if tour.wait_loaded(src) is None:
                    state.restore(nes, good)
                nxt = (t, 'via', xc, yc, e)
        t, method, xc, yc, e = nxt
        todo.remove(t)
        if not tour.to_gameplay():
            print(f'  sub_state {nes.bus.ram[0x41]} did not return to 8; restoring', flush=True)
            state.restore(nes, good)
        if method == 'via' and not tour.leg_exit(xc, yc):
            method = 'poke'
        if method == 'exit' and not tour.leg_exit(xc, yc):
            method = 'poke'
        if method == 'poke':
            tour.leg_poke(t, e & 0x1F if e & 0x20 == 0 else e)
        frames = tour.wait_loaded(t)
        if frames is None:
            r = nes.bus.ram
            tour.rows.append({'area': t, 'entry': e, 'method': method, 'frames': None, 'f': nes.ppu.frame_no,
                              'music': r[0x102], 'chr': list(nes.mapper.regs[0:6]), 'pal': '', 'shadow': '',
                              'exp_slots': [], 'act': {}, 'spawned': 0, 'cmp': None,
                              'state': (r[0x40], r[0x41]), 'stuck_area': r[0x6C]})
            print(f'  {t:02X} STUCK at $40/$41={r[0x40]}/{r[0x41]} area {r[0x6C]:02X}; restoring', flush=True)
            state.restore(nes, good)
            continue
        visited.add(t)
        if tour.bounce is not None:
            method += f'>{tour.bounce:02X}'    # screen/slots below are of the area it bounced to
        w = tour.record(t, method, frames)
        print(fmt_row(w).split('\n')[0], flush=True)
        if tour.to_gameplay():
            good = state.snapshot(nes)
        if a.save_states:
            state.save(nes, os.path.join(STATES, f'tour_{t:02X}.state'))
    el = time.time() - t_start

    out = [f'tour.py: {os.path.basename(a.state)} -> f{nes.ppu.frame_no}, {len(tour.rows)} legs, '
           f'settle {a.settle}, {el:.0f} s', '']
    out.append('per area: area.entry method load-frames end-frame music MMC3 R0-R5 pal ids $07E0-7 '
               'shadows $07F0 $07F1 $07F4 $07F5, spawn-list slots active/expected, BG diff vs gfx/areas')
    out += [fmt_row(w) for w in tour.rows]
    stuck = [w for w in tour.rows if w['frames'] is None]
    out.append(f'stuck legs: {len(stuck)} ' + ' '.join(f'{w["area"]:02X}(at {w["stuck_area"]:02X} {w["state"]})' for w in stuck))
    out.append(f'frames spent tapping A to close messages (sub_state != 8 between legs): {tour.msg_frames}')
    out.append('')
    out.append('main/sub states seen (frames): ' +
               ', '.join(f'{s}/{u}: {c}' for (s, u), c in sorted(tour.substates.items())))
    out.append('')
    for name, (n, ok, fails) in rep.stats.items():
        out.append(f'  {name:30s} {ok:6d} / {n:<6d} {"PASS" if ok == n else "FAIL"}')
        out += [f'      ! {m}' for m in fails]
    hist = {}
    for (h, line), c in rep.irq_hist.items():
        hist.setdefault(h, {})[line] = c
    out.append('IRQ handler -> {scanline: count}: ' +
               '; '.join(f'{h}: {dict(sorted(d.items()))}' for h, d in sorted(hist.items(), key=str)))
    out.append(f'NMI paths: {rep.nmi}')
    out.append('SoundUpdate call sites (site, irq_mode): ' +
               ', '.join(f'{s:04X} mode {m}: {c}' for (s, m), c in sorted(rep.sndupd.items())))
    mx = max(rep.cmds.values()) if rep.cmds else 0
    out.append(f'SndCommand max per frame {mx} {"PASS" if mx <= 1 else "FAIL"}')
    multi = sorted(f for f, c in rep.cmds.items() if c > 1)
    out.append(f'frames with >1 SndCommand: {len(multi)}; SoundUpdate calls in them (site@line): ' +
               ', '.join(f'f{f}: {upd_frames.get(f)}' for f in multi[:12]))
    two = sum(1 for v in upd_frames.values() if len(v) > 1)
    out.append(f'frames with >1 SoundUpdate call: {two} of {len(upd_frames)}; first: ' +
               ', '.join([f'f{f}: {v}' for f, v in sorted(upd_frames.items()) if len(v) > 1][:4]))
    v = dict(rep.vib)
    v['depths'], v['octs'] = sorted(v['depths']), sorted(v['octs'])
    out.append(f'SndVibratoTick paths: {v}')
    out.append(f'SaveGame written (f, +F0-F8): {[(f, pl) for f, pl, _, _ in getattr(rep, "saves", [])]}')
    out.append(f'SaveGame blocked by AreaSaveFlags b7=0 (f, area): '
               f'{[(f, hex(x)) for f, x in getattr(rep, "saves_blocked", [])]}')
    out.append(f'SwitchBank16 n values:{" ".join(f"{x:02X}" for x in sorted(getattr(rep, "sb_ids", [])))}')
    out.append(f'PlaySound ids: {" ".join(sorted({f"{x:02X}" for x in getattr(rep, "snd_ids", [])}))}')
    if a.coverage:
        per = {}
        for b, pc in cov:
            per[b] = per.get(b, 0) + 1
        out.append('')
        out.append('coverage: distinct executed instruction addresses per bank (-1 = RAM): ' +
                   ', '.join(f'{b}: {c}' for b, c in sorted(per.items())))
        labs = code_labels()
        never = [(b, ad, n) for b, ad, n in labs if (b, ad) not in cov]
        out.append(f'code labels executed: {len(labs) - len(never)} / {len(labs)}; never run:')
        for b in sorted({x[0] for x in never}):
            out.append(f'  bank {b:2d}: ' + ' '.join(f'{ad:04X}:{n}' for bb, ad, n in sorted(never) if bb == b))
    text = '\n'.join(out) + '\n'
    if a.out:
        with open(a.out, 'w') as f:
            f.write(text)
    print(text, end='')
    return 1 if sum(n - ok for n, ok, _ in rep.stats.values()) else 0


def cmd_recheck(a):
    """Redo the BG compare from --shots PNGs + the 'cam X,Y' printed in --out (signed-X fix, s42)."""
    import re
    from render_compare import read_png
    pat = re.compile(r'^  ([0-9A-F]{2})\.[0-9A-F]{2} .* cam (\d+),(-?\d+) best')
    seen = set()
    for line in open(a.out):
        m = pat.match(line)
        if not m or m.group(1) in seen or (a.only and int(m.group(1), 16) not in
                                            [int(x, 16) for x in a.only.split(',')]):
            continue
        seen.add(m.group(1))
        ar, cx, cy = int(m.group(1), 16), int(m.group(2)), int(m.group(3))
        cx = cx - 0x10000 if cx >= 0x8000 else cx
        p = os.path.join(a.shots, f'area_{ar:03d}.png')
        if not os.path.exists(p):
            continue
        w, h, px = read_png(p)
        c = compare_pixels(px, ar, cx, cy)
        if c:
            print(f'  {ar:02X} cam {cx},{cy}: {100.0 * c[1] / max(c[0], 1):5.1f}% diff of {c[0]} '
                  f'best {c[4] - cx:+d},{c[5] - cy:+d}', flush=True)
    return 0


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('cmd', choices=('graph', 'run', 'recheck'))
    ap.add_argument('--state', default=os.path.join(STATES, 'area1_enter.state'))
    ap.add_argument('--max', type=int, default=999)
    ap.add_argument('--settle', type=int, default=30)
    ap.add_argument('--only')
    ap.add_argument('--coverage', action='store_true')
    ap.add_argument('--out')
    ap.add_argument('--shots')
    ap.add_argument('--save-states', action='store_true')
    a = ap.parse_args(argv)
    if a.cmd == 'graph':
        cmd_graph()
        return 0
    if a.cmd == 'recheck':
        return cmd_recheck(a)
    return cmd_run(a)


if __name__ == '__main__':
    sys.exit(main())
