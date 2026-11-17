#!/usr/bin/env python3
"""Ph6 validate 4/4: build COMPARISON.md (one row per screen/state, image pairs, severity).

usage: python3 tools/comparison.py [ref] [areas] [screens] [sprites] [--out COMPARISON.md]
       (no section names = all four; sections not run are read back from gfx/compare/*.json)

Sections and image pairs (all written to gfx/compare/):
  ref      FCEUX capture ref/frame_NNNN.png (256x224, overscan rows 0-7 / 232-239 cropped) vs
           tools.emu from power-on with the canonical --keys, best frame offset -3..+3 by pixel
           mismatch (palette-independent map, tools/ref/compare_ref.pixel_diff).
           ref_NNNN.png = reference | tools.emu rows 8-231 | mismatch mask.
  areas    every gfx/emu/tour/area_NNN.png (tools/emu/tour.py run) vs the static map
           gfx/areas/area_NNN.png at the camera from docs/emu_tour.txt (camera X read signed).
           Rows >= 3 % get areas/area_NNN.png = tools.emu | static crop | mismatch mask.
  screens  menus / death / save from tools/emu/states/area1_enter.state (button presses and one
           RAM poke), emulator only (no reference or static renderer for runtime-built screens).
  sprites  metasprites: for every active object slot the OAM entries actually rendered are matched
           to a frame of its meta id (tools/meta_dump.py decoder); the decoded tiles are drawn with
           the frame's CHR R2-R5 + sprite palette and compared to the tools.emu framebuffer pixel by
           pixel (palette indices). sprites/<scene>_<slot>.png = tools.emu | static, 4x.
Severity: MATCH (< 3 % / identical), MINOR (difference explained, not a decoder/emulator bug),
FIXED (was a mismatch in an earlier session, matches now), CRITICAL (unexplained).
"""
import os
import sys
# this script lives in tools/, whose dis.py shadows the stdlib dis: load the stdlib first
_HERE = os.path.dirname(os.path.abspath(__file__))
sys.path[:] = [p for p in sys.path if os.path.abspath(p or '.') != _HERE]
import argparse  # noqa: F401,E402
import dataclasses  # noqa: F401,E402
import dis  # noqa: F401,E402
import inspect  # noqa: F401,E402
import json  # noqa: E402
import re  # noqa: E402

TOOLS = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(TOOLS)
sys.path.insert(0, TOOLS)
sys.path.insert(0, os.path.join(TOOLS, 'ref'))
import compare_ref  # noqa: E402  (imports tools.emu with ROOT on sys.path)
from tools.emu.nes import NES  # noqa: E402
from tools.emu import state, debug  # noqa: E402
from tools.emu.pads import parse_buttons  # noqa: E402
from tools.emu.png import write_rgb  # noqa: E402
from render_compare import read_png  # noqa: E402
from palette_dump import NES_RGB  # noqa: E402
from nesrom import load_rom  # noqa: E402
import meta_dump  # noqa: E402

OUT = os.path.join(ROOT, 'gfx', 'compare')
STATE = os.path.join(TOOLS, 'emu', 'states', 'area1_enter.state')
LUT = [bytes(((c >> 16) & 255, (c >> 8) & 255, c & 255)) for c in NES_RGB]
RED = b'\xff\x20\x20'


def rel(p):
    return os.path.relpath(p, ROOT)


def fb_rgb(fb):
    return b''.join(LUT[i & 0x3F] for i in fb)


def triple(path, w, h, imgs):
    """imgs: list of w*h*3 rgb bytes, written side by side with a 4px gap."""
    gap = b'\x40\x40\x40' * 4
    rows = []
    for y in range(h):
        rows.append(gap.join(im[y * w * 3:(y + 1) * w * 3] for im in imgs))
    W = w * len(imgs) + 4 * (len(imgs) - 1)
    write_rgb(path, W, h, b''.join(rows))


def mask_img(base_rgb, bad, n):
    """darkened base with mismatch pixels in red."""
    out = bytearray(b // 3 for b in base_rgb)
    for i in bad:
        out[i * 3:i * 3 + 3] = RED
    return bytes(out)


def flat(px):
    return b''.join(bytes(p) for p in px)


# ---------------------------------------------------------------- ref
def sec_ref():
    refs = sorted(int(m.group(1)) for p in os.listdir(os.path.join(ROOT, 'ref'))
                  for m in [re.match(r'frame_(\d+)\.png$', p)] if m)
    want = {n + d for n in refs for d in range(-3, 4)}
    nes = NES()
    nes.keys = debug.parse_keys([compare_ref.CANON_KEYS])
    fbs = {}

    def hook(n):
        f = n.ppu.frame_no - 1
        if f in want:
            fbs[f] = (bytes(n.ppu.fb), n.bus.ram[0x40], n.bus.ram[0x41], n.bus.ram[0x6C])
    nes.on_frame(hook)
    nes.run_frames(max(want) + 1)
    rows = []
    for n in refs:
        img = read_png(os.path.join(ROOT, 'ref', f'frame_{n:04d}.png'))
        best = None
        for d in range(-3, 4):
            if n + d not in fbs:
                continue
            rgb = fb_rgb(fbs[n + d][0])
            px = [tuple(rgb[i:i + 3]) for i in range(0, len(rgb), 3)]
            bad, msg = compare_ref.pixel_diff(img, px)
            if bad is not None and (best is None or len(bad) < len(best[0])):
                best = (bad, msg, d, rgb)
        bad, msg, d, rgb = best
        fb, ms, ss, area = fbs[n + d]
        crop = rgb[8 * 256 * 3:232 * 256 * 3]
        path = os.path.join(OUT, f'ref_{n:04d}.png')
        triple(path, 256, 224, [flat(img[2]), crop, mask_img(flat(img[2]), bad, 256 * 224)])
        rows.append({'frame': n, 'offset': d, 'bad': len(bad), 'pct': 100.0 * len(bad) / (256 * 224),
                     'msg': msg, 'main': ms, 'sub': ss, 'area': area, 'img': rel(path)})
        print(f'ref {n}: offset {d:+d} {msg}', file=sys.stderr)
    return rows


# ---------------------------------------------------------------- areas
TOUR_RX = re.compile(r'^\s+([0-9A-F]{2})\.([0-9A-F]{2}) (\w+)\s+load\s+(\d+) f(\d+)\s+mus (\w\w) '
                     r'.*bg\s+([\d.]+)% diff of (\d+) cam (-?\d+),(-?\d+)')


def sec_areas():
    import emu.tour as tour  # noqa: E402  (tools/ on sys.path)
    rows = []
    for ln in open(os.path.join(ROOT, 'docs', 'emu_tour.txt')):
        m = TOUR_RX.match(ln)
        if not m:
            continue
        a = int(m.group(1), 16)
        cx, cy = int(m.group(9)), int(m.group(10))
        cx = cx - 0x10000 if cx >= 0x8000 else cx
        shot = os.path.join(ROOT, 'gfx', 'emu', 'tour', f'area_{a:03d}.png')
        row = {'area': a, 'entry': int(m.group(2), 16), 'method': m.group(3), 'music': m.group(6),
               'old_pct': float(m.group(7)), 'old_n': int(m.group(8)), 'cam': (cx, cy), 'shot': rel(shot),
               'static': f'gfx/areas/area_{a:03d}.png'}
        if not os.path.exists(shot) or tour.area_png(a) is None:
            row['pct'] = None
            rows.append(row)
            continue
        w, h, px = read_png(shot)
        n, bad, _, _, dx, dy = tour.compare_pixels(px, a, cx, cy)
        row.update(n=n, bad=bad, dx=dx, dy=dy, pct=100.0 * bad / n if n else None)
        if n and bad * 100.0 / n >= 3:
            sw, sh, spx = tour.area_png(a)
            crop, badi, rowsbad = bytearray(256 * 240 * 3), [], {}
            for y in range(240):
                for x in range(256):
                    X, Y = x + dx, y + dy
                    if 0 <= X < sw and 0 <= Y < sh:
                        c = spx[Y * sw + X]
                        crop[(y * 256 + x) * 3:(y * 256 + x) * 3 + 3] = bytes(c)
                        if 4 <= y < 188 and x >= 8 and c != px[y * 256 + x]:
                            badi.append(y * 256 + x)
                            rowsbad[y // 8] = rowsbad.get(y // 8, 0) + 1
            row['tile_rows'] = rowsbad
            p = os.path.join(OUT, 'areas', f'area_{a:03d}.png')
            os.makedirs(os.path.dirname(p), exist_ok=True)
            triple(p, 256, 240, [flat(px), bytes(crop), mask_img(flat(px), badi, 0)])
            row['pair'] = rel(p)
        rows.append(row)
        print(f'area {a:02X}: {row["pct"]}', file=sys.stderr)
    return rows


# ---------------------------------------------------------------- screens
SCENES = [
    ('status', 'START: status screen', ['w:10', 'p:START', 'w:40']),
    ('equip', 'SELECT: equipment grid (Sword/Armor/Shield/Power)', ['w:10', 'p:SELECT', 'w:40']),
    ('items', 'SELECT, UP: item/magic grid', ['w:10', 'p:SELECT', 'w:40', 'p:UP', 'w:20']),
    ('save', 'SELECT, START: save/load page (16:9AEE)', ['w:10', 'p:SELECT', 'w:40', 'p:START', 'w:40']),
    ('save_ask', 'save page, A', ['w:10', 'p:SELECT', 'w:40', 'p:START', 'w:40', 'p:A', 'w:40']),
    ('load_ask', 'save page, RIGHT, A: "Load this game?"',
     ['w:10', 'p:SELECT', 'w:40', 'p:START', 'w:40', 'p:RIGHT', 'w:20', 'p:A', 'w:40']),
    ('death', 'HP $03C1 := 0: PlayerDeath (sub_state 3) +200 frames', ['w:10', 's:3C1=0', 'w:200']),
    ('gameover', 'HP $03C1 := 0, +500 frames: back to the intro (main_state 3)',
     ['w:10', 's:3C1=0', 'w:500']),
]


def play(nes, steps):
    r = nes.bus.ram
    for st in steps:
        if st.startswith('p:'):
            m = parse_buttons(st[2:])
            for _ in range(2):
                nes.keys = {nes.ppu.frame_no: m}
                nes.run_frame()
            nes.keys = {}
        elif st.startswith('w:'):
            nes.keys = {}
            nes.run_frames(int(st[2:]))
        elif st.startswith('s:'):
            a, v = st[2:].split('=')
            r[int(a, 16)] = int(v, 16)


def sec_screens():
    rows = []
    for name, desc, steps in SCENES:
        nes = NES()
        state.load(nes, STATE)
        play(nes, steps)
        r = nes.bus.ram
        p = os.path.join(OUT, 'screens', f'{name}.png')
        os.makedirs(os.path.dirname(p), exist_ok=True)
        nes.dump_screen(p)
        rows.append({'name': name, 'desc': desc, 'steps': ' '.join(steps), 'main': r[0x40],
                     'sub': r[0x41], 'irq': r[0x51], 'img': rel(p)})
        print(f'screen {name}: main {r[0x40]} sub {r[0x41]:02X}', file=sys.stderr)
    return rows


# ---------------------------------------------------------------- sprites
SPRITE_SCENES = [('area01', None), ('area03', (0x03, 0x00)), ('area29', (0x29, 0x00)),
                 ('area2B', (0x2B, 0x00)), ('area28', (0x28, 0x05))]


def load_area(nes, area, entry):
    r = nes.bus.ram
    r[0x6C], r[0x6D], r[0x41] = area, entry, 1
    left = False
    for _ in range(600):
        nes.keys = {}
        nes.run_frame()
        left |= r[0x41] != 8
        if left and r[0x41] == 8 and r[0x6C] == area:
            return True
    return False


def match_meta(prg, meta, oam):
    """-> (frame, anchor_x, anchor_y, [oam idx]) for the first frame of meta whose sprites are all
    in OAM (same tile + flip bits) at one anchor, else None."""
    spr = [(oam[4 * i + 3], oam[4 * i], oam[4 * i + 2], oam[4 * i + 1]) for i in range(64)]
    for fr in meta_dump.frames(prg, meta):
        if not fr:
            continue
        dx0, dy0, at0, t0 = fr[0]
        for i, (ox, oy, at, t) in enumerate(spr):
            if t != t0 or (at ^ at0) & 0xC0 or oy >= 0xEF:
                continue
            ax, ay = ox - dx0, oy - dy0
            used = []
            for dx, dy, a2, t2 in fr:
                for j, (x2, y2, b2, u2) in enumerate(spr):
                    if j not in used and u2 == t2 and not (b2 ^ a2) & 0xC0 and x2 == (ax + dx) & 0xFF \
                            and y2 == (ay + dy) & 0xFF:
                        used.append(j)
                        break
                else:
                    break
            else:
                return fr, ax, ay, used
    return None


def sec_sprites():
    _, prg, chr_rom = load_rom()
    rows = []
    for name, leg in SPRITE_SCENES:
        nes = NES()
        state.load(nes, STATE)
        if leg and not load_area(nes, *leg):
            rows.append({'scene': name, 'error': 'area load timed out'})
            continue
        nes.run_frames(40)
        oam, regs = bytes(nes.ppu.oam), list(nes.mapper.regs)
        pal = [nes.ppu.vram_read(0x3F10 + i) & 0x3F for i in range(16)]
        nes.run_frame()
        fb = bytes(nes.ppu.fb)
        r = nes.bus.ram
        owner = {}   # screen pixel -> lowest OAM index with an opaque pixel there (NES sprite priority)
        for j in range(63, 0, -1):
            oy, t, at, ox = oam[4 * j:4 * j + 4]
            if oy >= 0xEF:
                continue
            tp = meta_dump.tile_pixels(chr_rom, regs[2:6], t)
            for yy in range(8):
                for xx in range(8):
                    if tp[7 - yy if at & 0x80 else yy][7 - xx if at & 0x40 else xx]:
                        owner[(ox + xx, oy + 1 + yy)] = j
        for s in range(32):
            if not r[0x4A0 + s] or not r[0x300 + s]:
                continue
            meta = r[0x300 + s]
            m = match_meta(prg, meta, oam)
            row = {'scene': name, 'area': r[0x6C], 'slot': s, 'ai': r[0x4A0 + s], 'meta': meta,
                   'chr': regs[2:6]}
            if m is None:
                row['error'] = 'no frame of this meta id in OAM (hidden / flicker / off screen)'
                rows.append(row)
                continue
            fr, ax, ay, used = m
            n = ok = 0
            xs = [(oam[4 * j + 3]) for j in used]
            ys = [(oam[4 * j]) + 1 for j in used]
            x0, y0 = min(xs) - 4, min(ys) - 4
            W, H = max(xs) + 12 - x0, max(ys) + 12 - y0
            emu_c, st_c = bytearray(W * H * 3), bytearray(W * H * 3)
            for j in sorted(used, reverse=True):   # lower OAM index wins: draw back to front
                oy, t, at, ox = oam[4 * j:4 * j + 4]
                tp = meta_dump.tile_pixels(chr_rom, regs[2:6], t)
                for yy in range(8):
                    for xx in range(8):
                        c = tp[7 - yy if at & 0x80 else yy][7 - xx if at & 0x40 else xx]
                        X, Y = ox + xx, oy + 1 + yy
                        if c == 0 or X > 255 or Y > 239:
                            continue
                        exp = pal[4 * (at & 3) + c]
                        st_c[((Y - y0) * W + X - x0) * 3:((Y - y0) * W + X - x0) * 3 + 3] = LUT[exp]
                        if at & 0x20 or X < 8 or not 4 <= Y < 184 or owner.get((X, Y)) != j:
                            continue
                        n += 1
                        ok += (fb[Y * 256 + X] & 0x3F) == exp
            for yy in range(H):
                for xx in range(W):
                    X, Y = x0 + xx, y0 + yy
                    if 0 <= X < 256 and 0 <= Y < 240:
                        emu_c[(yy * W + xx) * 3:(yy * W + xx) * 3 + 3] = LUT[fb[Y * 256 + X] & 0x3F]
            p = os.path.join(OUT, 'sprites', f'{name}_s{s:02X}_m{meta:02X}.png')
            os.makedirs(os.path.dirname(p), exist_ok=True)
            big = []
            for im in (bytes(emu_c), bytes(st_c)):
                big.append(b''.join(b''.join(im[(y * W + x) * 3:(y * W + x) * 3 + 3] * 4 for x in range(W)) * 4
                                    for y in range(H)))
            triple(p, W * 4, H * 4, big)
            row.update(nspr=len(used), n=n, ok=ok, pct=100.0 * ok / n if n else None, img=rel(p),
                       pos=(ax, ay))
            rows.append(row)
            print(f'sprite {name} slot {s:02X} meta {meta:02X}: {ok}/{n}', file=sys.stderr)
    return rows


# ---------------------------------------------------------------- report
S38 = [('title', 'Title screen (main state 3, f1450)', 'gfx/emu/cmp_title.png', 'gfx/screens/'),
       ('name entry', 'Name entry (title_step 6)', 'gfx/emu/cmp_name_entry.png', 'tools/render_screen.py'),
       ('area 0 start', 'First gameplay frame, area $00', 'gfx/emu/cmp_area0.png', 'gfx/areas/area_000.png'),
       ('area 1 enter', 'Area $01 after the first exit', 'gfx/emu/cmp_area1.png', 'gfx/areas/area_001.png')]
TEXT_ROOMS_NOTE = ('NPC text window open over tile rows 1-10 on entry (runtime text); BG elsewhere exact')


def area_sev(r):
    if r.get('pct') is None:
        return 'CRITICAL', 'not compared (no shot or no static map)'
    if r['pct'] < 3:
        if r['old_n'] == 0:
            return 'FIXED', 's42 report had no compare (camera X printed unsigned); signed camera X now matches'
        return 'MATCH', 'sprites only'
    tr = {int(k): v for k, v in r.get('tile_rows', {}).items()}   # json keys are strings
    tot = sum(tr.values()) or 1
    if sum(v for k, v in tr.items() if 1 <= k <= 11) / tot > 0.9:
        return 'MINOR', TEXT_ROOMS_NOTE
    if r['area'] == 0x01 and r['pct'] < 4:
        return 'MINOR', 'sprites only (player + 8-slot orbit ring + NPC; pair checked visually s43)'
    if r['area'] in (0x60, 0x64, 0x68) and r['pct'] < 15:
        return 'MINOR', 'scattered: animated water/rows (BG tile animation at runtime)'
    if r['area'] in (0x15, 0x1A, 0x65):
        return 'CRITICAL', ('leg ended in another scene (story event?); not a renderer diff but unverified — '
                            'see "Ph6 tour leftovers" (a)')
    return 'CRITICAL', 'unexplained'


def ref_sev(r):
    m = re.search(r'colour map (\d+) emu -> (\d+) ref', r['msg'])
    if m and m.group(2) == '1' and int(m.group(1)) > 1:
        return 'CRITICAL', ('reference frame is blank (1 colour) but tools.emu shows the faded room: the '
                            'palette-independent map hides this, so "0 px" is not a match. Frame timing around '
                            'the area load (triage task (c)/(g))')
    if r['pct'] < 0.01:
        return 'FIXED', ('pixel compare never ran in s40 (224-row PNG); identical now'
                         + (' (blank-ish screen: offset ties, first one kept)' if m and int(m.group(2)) <= 4 else ''))
    rows = re.search(r'per tile row (.*)$', r['msg'])
    rows = rows.group(1) if rows else ''
    if rows.startswith('23:') and ' ' not in rows:
        return 'MINOR', ('only screen line 191 (x 56-127, `tools/mask_peek.py`): the line right after the '
                         'IRQ handler 1 HUD split (line 190) differs = 1-scanline IRQ timing vs FCEUX, triage (g)')
    if r['pct'] < 3:
        return 'MINOR', f'{r["bad"]} px in tile rows {rows} (cursor / blink timing)'
    return 'CRITICAL', 'unexplained'


def report(data, out):
    L = ['# COMPARISON — re-c screens and states', '',
         'Generated by `python3 tools/comparison.py` (session 43). Image pairs are in `gfx/compare/`.',
         'Severity: **MATCH** = identical or < 3 % (sprites only); **MINOR** = difference explained and not a '
         'decoder/emulator bug; **FIXED** = mismatch in an earlier session that matches now; **CRITICAL** = '
         'unexplained. The **Web port** column stays empty until Ph7 (web 8/8 fills it).', '']
    # summary counts
    allsev = []
    # ---- ref
    L += ['## 1. Reference (FCEUX) vs tools.emu', '',
          'Canonical `--keys` run from power-on (`ref/capture.txt`). FCEUX PNGs are 256×224, so tools.emu '
          'rows 8-231 are compared (palette-independent colour map, `compare_ref.pixel_diff`). Pair = '
          'reference | tools.emu | mismatch mask. The two emulators use different RGB tables for the same NES '
          'colour (e.g. `$02` = `#24188e` FCEUX vs `#0000fc` tools.emu), so the pairs look different in hue '
          'although palette RAM is identical in all 12 frames (`docs/ref_compare.txt` PAL lines). '
          'RAM/OAM/WRAM diffs are in `docs/ref_compare.txt` (triage task).', '',
          '| Frame | State (main/sub, area) | Offset | Mismatch | Image pair | Web port | Severity | Notes / REVERSE.md |',
          '|---|---|---|---|---|---|---|---|']
    for r in data.get('ref', []):
        sev, note = ref_sev(r)
        allsev.append(sev)
        L.append(f'| {r["frame"]} | {r["main"]}/{r["sub"]}, `${r["area"]:02X}` | {r["offset"]:+d} | '
                 f'{r["bad"]} px ({r["pct"]:.2f} %) | [{os.path.basename(r["img"])}]({r["img"]}) |  | {sev} | '
                 f'{note}; "Verification Log" |')
    # ---- static renderer
    L += ['', '## 2. Static renderer vs tools.emu (session 38, `tools/render_compare.py`)', '',
          '| Screen | State | Image pair | Web port | Severity | Notes / REVERSE.md |', '|---|---|---|---|---|---|']
    for name, st, img, src in S38:
        allsev.append('MATCH')
        L.append(f'| {name} | {st} | [{os.path.basename(img)}]({img}) vs `{src}` |  | MATCH | exact BG; '
                 '"Screens / metatiles" row of "Verification Log" |')
    # ---- areas
    ar = data.get('areas', [])
    L += ['', f'## 3. Areas: tools.emu tour vs static map ({len(ar)} legs)', '',
          'Shots from `tools/emu/tour.py run --shots gfx/emu/tour` (session 42), playfield x 8-255, y 4-187 vs '
          '`gfx/areas/area_NNN.png` at the camera (x = signed `$02/$03`, y = s8(`$05`)·240+`$04`), best offset '
          '±3. Rows ≥ 3 % have a pair in `gfx/compare/areas/` (tools.emu | static crop | mismatch mask). '
          'REVERSE.md: "Area tour" rows of the "Verification Log", "Screens / metatiles".', '',
          '| Area.entry | Leg | Music | Mismatch | Offset | Image pair | Web port | Severity | Notes |',
          '|---|---|---|---|---|---|---|---|---|']
    for r in ar:
        sev, note = area_sev(r)
        allsev.append(sev)
        mm = '—' if r.get('pct') is None else f'{r["pct"]:.1f} % of {r["n"]}'
        off = '—' if r.get('pct') is None else f'{r["dx"] - r["cam"][0]:+d},{r["dy"] - r["cam"][1]:+d}'
        pair = f'[pair]({r["pair"]})' if r.get('pair') else f'[emu]({r["shot"]}) / [static]({r["static"]})'
        L.append(f'| `${r["area"]:02X}.{r["entry"]:02X}` | {r["method"]} | `{r["music"]}` | {mm} | {off} | '
                 f'{pair} |  | {sev} | {note} |')
    # ---- screens
    L += ['', '## 4. Menus, death, game over, save (tools.emu only)', '',
          'From `tools/emu/states/area1_enter.state`. These screens are built at run time (bank 16 menu '
          'printer, bank 19 death), so there is no static-renderer image, and the FCEUX capture does not reach '
          'them: severity is **CRITICAL (unpaired)** until a reference capture of the same steps exists. '
          'REVERSE.md: "Text / dialogue" (menu strings `16:8C1B`), "Save / load module", `PlayerDeath` `19:B900`.', '',
          '| Screen | How (steps) | main/sub, irq_mode | tools.emu | Reference | Web port | Severity |',
          '|---|---|---|---|---|---|---|']
    for r in data.get('screens', []):
        allsev.append('CRITICAL')
        L.append(f'| {r["name"]} — {r["desc"]} | `{r["steps"]}` | {r["main"]}/`${r["sub"]:02X}`, {r["irq"]} | '
                 f'[{os.path.basename(r["img"])}]({r["img"]}) | — |  | CRITICAL (unpaired) |')
    # ---- sprites
    sp = data.get('sprites', [])
    good = [r for r in sp if r.get('pct') is not None and r['pct'] >= 95 and r['n'] >= 20]
    gmeta = sorted({r['meta'] for r in good})
    L += ['', '## 5. Sprites: `meta_dump.py` decoder vs tools.emu framebuffer (Ph4 checkpoint)', '',
          'For each active object slot (`obj_active` `$04A0` ≠ 0) the rendered OAM is matched to a frame of the '
          'slot\'s meta id `$0300` (tile + flip bits at one anchor). The decoded tiles are drawn with the frame\'s '
          'CHR R2-R5 and sprite palette and compared pixel by pixel to the framebuffer (palette index; '
          'behind-BG sprites, x < 8 and the HUD rows skipped). Pair = tools.emu | static, 4×. '
          'REVERSE.md: "Object sprite pass", "Metasprites".', '',
          f'**{len(good)} object sprites ({len(gmeta)} distinct meta ids: '
          + ' '.join(f'`${m:02X}`' for m in gmeta) + ') match ≥ 95 % on ≥ 20 visible pixels** (Ph4 needs 5+). '
          'Pixels covered by a lower OAM index (NES sprite priority) are not counted: slot 0 (`$A7`, AI `$03`) is '
          'drawn last under the player, and the 8 orbit-ring slots (`$EB/$EC`) overlap each other.', '',
          '| Scene (area) | Slot | AI | Meta | Sprites | Pixels equal | Image pair | Web port | Severity |',
          '|---|---|---|---|---|---|---|---|---|']
    for r in sp:
        if r.get('pct') is None:
            if 'slot' in r:
                L.append(f'| {r["scene"]} (`${r["area"]:02X}`) | `{r["slot"]:02X}` | `${r["ai"]:02X}` | '
                         f'`${r["meta"]:02X}` | — | — | — |  | n/a: {r.get("error", "no opaque pixels")} |')
            continue
        sev = 'MATCH' if r['pct'] >= 95 else ('MINOR' if r['pct'] >= 80 else 'CRITICAL')
        if sev == 'MATCH' and r['n'] < 20:
            sev = 'MINOR (mostly covered)'
        allsev.append(sev)
        L.append(f'| {r["scene"]} (`${r["area"]:02X}`) | `{r["slot"]:02X}` | `${r["ai"]:02X}` | `${r["meta"]:02X}` | '
                 f'{r["nspr"]} | {r["ok"]}/{r["n"]} ({r["pct"]:.1f} %) | [pair]({r["img"]}) |  | {sev} |')
    cnt = {s: allsev.count(s) for s in ('MATCH', 'FIXED', 'MINOR', 'CRITICAL')}
    L[5:5] = ['**Totals:** ' + ', '.join(f'{k} {v}' for k, v in cnt.items()) + f' ({len(allsev)} rows).', '']
    open(out, 'w').write('\n'.join(L) + '\n')
    print(f'wrote {out}: {cnt}', file=sys.stderr)


def main(argv):
    out = os.path.join(ROOT, 'COMPARISON.md')
    if '--out' in argv:
        i = argv.index('--out')
        out = argv[i + 1]
        argv = argv[:i] + argv[i + 2:]
    secs = argv or ['ref', 'areas', 'screens', 'sprites']   # 'report' = rebuild from the json caches only
    os.makedirs(OUT, exist_ok=True)
    data = {}
    for s in ('ref', 'areas', 'screens', 'sprites'):
        cache = os.path.join(OUT, f'{s}.json')
        if s in secs:
            data[s] = globals()['sec_' + s]()
            json.dump(data[s], open(cache, 'w'), indent=0, default=list)
        elif os.path.exists(cache):
            data[s] = json.load(open(cache))
    report(data, out)


if __name__ == '__main__':
    main(sys.argv[1:])
