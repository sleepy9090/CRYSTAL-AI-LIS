"""Video menu checks of ReC.exe under Wine + Xvfb (Video.cs / VideoMenu.cs).

usage: python3 tools/win_video_check.py [--exe PATH] [--timeout S] [--keep] [--bg-pack DIR] [NAME..]
Each case runs the real window for 787 frames (frame_no 1000, the intro picture of app.ico) (--turbo) with its own settings file (out/video/NAME.ini), applies menu
commands with `--menu` (the same Command() path as clicking the items), and reads the drawn client area back with
`--dump-window`. Checked per case: exit 0, the client size, the picture rectangle (from the same layout rules as
Video.cs, recomputed here), black bars outside it, the picture itself against the headless frame (upscaled by the same
Scale2x / 3x / 4x rules when the case uses a filter, then nearest-neighbour resample; exact for integer square pixels, <= 2 % differing pixels for stretched shapes, skipped for the smooth filter),
and the settings file written by the menu. Xvfb screen: 1920x1080x24.
`enhanced` (T10, Video > Graphics): Enhanced at 4x (client 1024x960, > 3x the colours, block centres of flat areas
keep their colour, settings file, --export-tiles), then the exported tiles painted magenta as a pack must be drawn,
and Graphics > Original must not draw it.
`enhanced32` (T14): Graphics > Enhanced (32-bit): client 1024x960, > 2000 colours, settings file.
`hdart` (T25): HD with the built-in starter packs vs without (atlas holds every packs/ tile, playfield repainted, HUD identical);
`hdfx` (T19, T20): HD effects all on vs all off (no-flicker stays on) on a cave gameplay frame (playfield changes, HUD
identical, ini records it);
`hd` / `hdfull` / `hdoff` (T18, T20 engine): Graphics > HD (Direct3D 11 under Wine): device + shaders made, 4x window and 1080p
fullscreen pictures (> 2000 colours, flat areas keep their colour, black bars), back to Original draws with GDI again.
`heropack` (T11): packs/hero on a gameplay frame: loads cleanly, repaints only the hero.
`enemypack` (T12): packs/enemies on areas $29 and $05: loads cleanly, repaints enemies, not the hero.
`npcpack` (T16): packs/npcs in a town ($18) and a shop: loads cleanly, repaints NPCs, not the hero.
`bosspack` (T17): packs/bosses on the area-$28 boss speech and the fan turret (palette-0 bosses): repaints, hero untouched.
`bgpack` (T15): packs/backgrounds on a town and a field frame, 16-bit and 32-bit: loads cleanly, repaints scenery,
the hero's own pixels unchanged.
Exit 1 on any failure.
"""
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import core_vs_web as cv  # noqa: E402
from make_icon import png_decode  # noqa: E402
from win_vs_web import wine_env, winpath  # noqa: E402

OUT = os.path.join(cv.ROOT, 'out', 'video')
SCREEN = (1920, 1080)
FRAMES = 787
BG_PACK = os.path.join(cv.ROOT, 'packs', 'backgrounds')
RATIO = {'square': None, 'tv': 256 * 8 / 7, '4:3': 4 / 3, '16:10': 16 / 10, '16:9': 16 / 9}

# name, extra args, menu commands, expected client size, expected settings (aspect, integer, crop, smooth[, upscale]),
# ini lines
CASES = [
    ('up2x', [], ['upscale:scale2x', 'size:2x'], (512, 480), ('square', True, False, False, 2),
     ['upscale=scale2x', 'window=2x']),
    ('up3xcrop', [], ['upscale:scale3x', 'crop', 'size:3x'], (768, 672), ('square', True, True, False, 3),
     ['upscale=scale3x', 'crop_overscan=1']),
    ('up4xwide', [], ['upscale:scale4x', 'aspect:16:9', 'size:3x'], (1280, 720), ('16:9', True, False, False, 4),
     ['upscale=scale4x', 'aspect=16:9']),
    ('scale2', ['--scale', '2'], [], (512, 480), ('square', True, False, False), []),
    ('wide169', [], ['aspect:16:9', 'size:3x'], (1280, 720), ('16:9', True, False, False),
     ['aspect=16:9', 'window=3x']),
    ('tvcrop', [], ['size:1024x768', 'aspect:tv', 'crop'], (1024, 768), ('tv', True, True, False),
     ['aspect=tv', 'crop_overscan=1', 'window=1024x768']),
    ('fit43', [], ['aspect:4:3', 'integer', 'size:1280x720'], (1280, 720), ('4:3', False, False, False),
     ['aspect=4:3', 'scaling=fit']),
    ('fill', [], ['aspect:fill', 'smooth', 'size:800x600'], (800, 600), ('fill', True, False, True),
     ['aspect=fill', 'smooth=1']),
    ('nomenu', [], ['menubar', 'size:2x'], (512, 480), ('square', True, False, False), ['menu_bar=0']),
    ('fullscreen', ['--fullscreen'], [], SCREEN, ('square', True, False, False), ['fullscreen=1']),
    ('fs1280', [], ['fsmode:1280x720', 'fullscreen'], None, ('square', True, False, False),
     ['fullscreen=1', 'fullscreen_mode=1280x720']),
    # settings come back from the file written by `wide169` (no --menu)
    ('reload', [], [], (1280, 720), ('16:9', True, False, False), ['aspect=16:9']),
]


def layout(cw, ch, aspect, integer, crop):
    """= VideoSettings.Layout"""
    vh = 224 if crop else 240
    if aspect == 'fill':
        return 0, 0, cw, ch
    r = RATIO[aspect]
    r = 256 / vh if r is None else (r / vh if aspect == 'tv' else r)
    if integer:
        s = max(1, ch // vh)
        while s > 1 and round(s * vh * r) > cw:
            s -= 1
        h = s * vh
        w = round(h * r)
    else:
        h, w = ch, round(ch * r)
        if w > cw:
            w, h = cw, round(cw / r)
    return (cw - w) // 2, (ch - h) // 2, w, h


def run(exe, args, timeout):
    cmd = ['xvfb-run', '-a', '-s', f'-screen 0 {SCREEN[0]}x{SCREEN[1]}x24', 'wine', exe] + args
    try:
        p = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, env=wine_env(),
                           timeout=timeout)
    except subprocess.TimeoutExpired:
        return 124, f'timeout after {timeout} s'
    return p.returncode, p.stdout


def scale2x(img):
    """= Upscale.Scale2x (edges clamp)"""
    h, w = len(img), len(img[0])
    out = [[None] * (2 * w) for _ in range(2 * h)]
    for y in range(h):
        up, row, dn = img[max(y - 1, 0)], img[y], img[min(y + 1, h - 1)]
        for x in range(w):
            b, d, e, f, hh = up[x], row[max(x - 1, 0)], row[x], row[min(x + 1, w - 1)], dn[x]
            e0 = e1 = e2 = e3 = e
            if b != hh and d != f:
                e0 = d if d == b else e
                e1 = f if b == f else e
                e2 = d if d == hh else e
                e3 = f if hh == f else e
            out[2 * y][2 * x], out[2 * y][2 * x + 1], out[2 * y + 1][2 * x], out[2 * y + 1][2 * x + 1] = e0, e1, e2, e3
    return out


def scale3x(img):
    """= Upscale.Scale3x (edges clamp)"""
    h, w = len(img), len(img[0])
    out = [[None] * (3 * w) for _ in range(3 * h)]
    for y in range(h):
        up, row, dn = img[max(y - 1, 0)], img[y], img[min(y + 1, h - 1)]
        for x in range(w):
            xl, xr = max(x - 1, 0), min(x + 1, w - 1)
            a, b, c, d, e, f, g, hh, i = up[xl], up[x], up[xr], row[xl], row[x], row[xr], dn[xl], dn[x], dn[xr]
            px = [e] * 9
            if b != hh and d != f:
                px[0] = d if d == b else e
                px[1] = b if (d == b and e != c) or (b == f and e != a) else e
                px[2] = f if b == f else e
                px[3] = d if (d == b and e != g) or (d == hh and e != a) else e
                px[5] = f if (b == f and e != i) or (hh == f and e != c) else e
                px[6] = d if d == hh else e
                px[7] = hh if (d == hh and e != i) or (hh == f and e != g) else e
                px[8] = f if hh == f else e
            for j in range(9):
                out[3 * y + j // 3][3 * x + j % 3] = px[j]
    return out


def upscale(frame, k):
    return scale2x(frame) if k == 2 else scale3x(frame) if k == 3 else scale2x(scale2x(frame)) if k == 4 else frame


def check_picture(rows, rect, frame, crop, exact):
    """bars black; picture = nearest-neighbour resample of the (upscaled) frame (exact, or <= 2 % differing pixels)"""
    x0, y0, w, h = rect
    ch, cw = len(rows), len(rows[0])
    for y in range(ch):
        row = rows[y]
        inside_y = y0 <= y < y0 + h
        for x in (range(cw) if not inside_y else list(range(0, max(0, x0))) + list(range(x0 + w, cw))):
            if row[x] != (0, 0, 0):
                return f'bar pixel ({x},{y}) = {row[x]}, rect {rect}'
    k = len(frame[0]) // 256
    vh, first = (224 * k, 8 * k) if crop else (240 * k, 0)
    sw = 256 * k
    bad = total = 0
    for y in range(max(0, y0), min(ch, y0 + h)):
        sy = first + (y - y0) * vh // h
        for x in range(max(0, x0), min(cw, x0 + w)):
            total += 1
            if rows[y][x] != frame[sy][(x - x0) * sw // w]:
                bad += 1
    if exact and bad:
        return f'{bad} / {total} picture pixels differ'
    if bad > total * 0.02:
        return f'{bad} / {total} picture pixels differ (> 2 %)'
    return None


def main(argv):
    exe, timeout, keep, names = os.path.join(cv.ROOT, 'dist', 'ReC.exe'), 300, False, []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--exe':
            exe = argv[i + 1]; i += 1
        elif a == '--timeout':
            timeout = int(argv[i + 1]); i += 1
        elif a == '--keep':
            keep = True
        elif a == '--bg-pack':                  # bgpack: another backgrounds pack folder (default packs/backgrounds)
            global BG_PACK
            BG_PACK = argv[i + 1]; i += 1
        elif a in ('-h', '--help'):
            print(__doc__); return 0
        else:
            names.append(a)
        i += 1
    os.makedirs(OUT, exist_ok=True)
    for f in os.listdir(OUT):
        if f.endswith(('.ini', '.png')) and not keep:
            os.remove(os.path.join(OUT, f))
    hpng = os.path.join(OUT, 'headless.png')
    rc, out = run(exe, ['--headless', '--frames', str(FRAMES), '--quiet', '--dump-screen', winpath(hpng)], timeout)
    if rc or not os.path.exists(hpng):
        print(f'FAIL headless rc={rc}\n{out[-2000:]}'); return 1
    _, _, frame = png_decode(open(hpng, 'rb').read())

    fails = 0
    for name, extra, menu, size, (aspect, integer, crop, smooth, *up), ini_lines in CASES:
        if names and name not in names:
            continue
        ini = os.path.join(OUT, ('wide169' if name == 'reload' else name) + '.ini')
        wpng, spng = os.path.join(OUT, f'{name}_window.png'), os.path.join(OUT, f'{name}_screen.png')
        args = ['--frames', str(FRAMES), '--turbo', '--no-audio', '--video-config', winpath(ini),
                '--save', winpath(os.path.join(OUT, 'video.sav')), '--dump-window', winpath(wpng),
                '--dump-screen', winpath(spng)] + extra + (['--menu', ','.join(menu)] if menu else [])
        rc, out = run(exe, args, timeout)
        msg = None
        if rc or not os.path.exists(wpng):
            msg = f'rc={rc}, window png {"written" if os.path.exists(wpng) else "missing"}'
        elif open(spng, 'rb').read() != open(hpng, 'rb').read():
            msg = 'game frame differs from the headless run'
        else:
            cw, ch, rows = png_decode(open(wpng, 'rb').read())
            note = ''
            if size is None:            # exclusive mode: the display mode changed, or the fallback = desktop size
                if (cw, ch) not in ((1280, 720), SCREEN):
                    msg = f'client {cw}x{ch}, want 1280x720 or {SCREEN[0]}x{SCREEN[1]}'
                note = ' (mode changed)' if (cw, ch) == (1280, 720) else ' (mode not available under Xvfb: desktop fallback)'
            elif (cw, ch) != size:
                msg = f'client {cw}x{ch}, want {size[0]}x{size[1]}'
            if msg is None:
                rect = layout(cw, ch, aspect, integer, crop)
                if not smooth:
                    src = upscale(frame, up[0] if up else 1)
                    msg = check_picture(rows, rect, src, crop, exact=aspect == 'square' and integer)
                if msg is None:
                    text = open(ini).read() if os.path.exists(ini) else ''
                    missing = [line for line in ini_lines if line not in text.splitlines()]
                    if missing:
                        msg = f'settings file lacks {missing}'
                if msg is None:
                    msg_ok = f'client {cw}x{ch} picture {rect}{note}'
        if msg:
            fails += 1
            print(f'FAIL {name:10s} {msg}')
            print('     ' + '\n     '.join(out.strip().splitlines()[-8:]))
        else:
            print(f'ok   {name:10s} {msg_ok}', flush=True)
    if not names or 'enhanced' in names:
        fails += enhanced_checks(exe, timeout, hpng, frame)
    if not names or any(n in names for n in ('hd', 'hdfull', 'hdoff', 'hdfx', 'hdart')):
        fails += hd_checks(exe, timeout, hpng, frame, names)
    if not names or 'heropack' in names:
        fails += hero_check(exe, timeout)
    if not names or 'enemypack' in names:
        fails += enemy_check(exe, timeout)
    if not names or 'npcpack' in names:
        fails += enemy_check(exe, timeout, 'npcpack', 'npcs', NPC_CASES)
    if not names or 'bosspack' in names:
        fails += enemy_check(exe, timeout, 'bosspack', 'bosses', BOSS_CASES)
    if not names or 'bgpack' in names:
        fails += bg_check(exe, timeout)
    return 1 if fails else 0


def enhanced_checks(exe, timeout, hpng, frame):
    """T10 Video > Graphics > Enhanced: picture, --export-tiles, a pack tile, reload"""
    import shutil
    fails = 0
    ini, exp, pack = (os.path.join(OUT, n) for n in ('enhanced.ini', 'export', 'pack'))
    for d in (exp, pack):
        shutil.rmtree(d, ignore_errors=True)
    common = ['--frames', str(FRAMES), '--turbo', '--no-audio', '--video-config', winpath(ini),
              '--save', winpath(os.path.join(OUT, 'video.sav')), '--pack', winpath(pack)]

    def one(name, extra, check):
        nonlocal fails
        wpng, spng = os.path.join(OUT, f'{name}_window.png'), os.path.join(OUT, f'{name}_screen.png')
        rc, out = run(exe, common + ['--dump-window', winpath(wpng), '--dump-screen', winpath(spng)] + extra, timeout)
        msg = None
        if rc or not os.path.exists(wpng):
            msg = f'rc={rc}'
        elif open(spng, 'rb').read() != open(hpng, 'rb').read():
            msg = 'game frame differs from the headless run (Ppu.Tags must not change Fb)'
        else:
            cw, ch, rows = png_decode(open(wpng, 'rb').read())
            msg = check(cw, ch, rows)
        if msg and not msg.startswith('ok:'):
            fails += 1
            print(f'FAIL {name:10s} {msg}')
            print('     ' + '\n     '.join(out.strip().splitlines()[-8:]))
        else:
            print(f'ok   {name:10s} {msg[3:] if msg else ""}', flush=True)

    orig_colours = {c for row in frame for c in row}

    def enhanced(cw, ch, rows):
        if (cw, ch) != (1024, 960):
            return f'client {cw}x{ch}, want 1024x960'
        cols = {c for row in rows for c in row}
        if len(cols) < 3 * len(orig_colours):
            return f'{len(cols)} colours, want > 3 x {len(orig_colours)}'
        # blocks of flat areas keep the frame's colour: >= 60 % of the block centres equal the NES pixel
        same = sum(rows[y * 4 + 2][x * 4 + 2] == frame[y][x] for y in range(240) for x in range(256))
        if same < 0.6 * 256 * 240:
            return f'only {same} block centres keep their colour'
        diff = sum(rows[y * 4 + 2][x * 4 + 2] != frame[y][x] for y in range(240) for x in range(256))
        if not os.path.exists(ini) or 'graphics=enhanced' not in open(ini).read().splitlines():
            return 'settings file lacks graphics=enhanced'
        names = [f for f in os.listdir(exp) if f.endswith('.png')] if os.path.isdir(exp) else []
        if not names or len(os.listdir(os.path.join(exp, 'original'))) != len(names):
            return f'--export-tiles wrote {len(names)} tiles (+ originals)'
        return f'ok: {len(orig_colours)} -> {len(cols)} colours, {diff} reshaded block centres, {len(names)} tiles exported'

    one('enhanced', ['--menu', 'graphics:enhanced,size:4x', '--export-tiles', winpath(exp)], enhanced)

    def enhanced32(cw, ch, rows):
        if (cw, ch) != (1024, 960):
            return f'client {cw}x{ch}, want 1024x960'
        cols = {c for row in rows for c in row}
        if len(cols) < 2000:
            return f'{len(cols)} colours, want > 2000 (soft lighting + anti-aliasing)'
        if 'graphics=enhanced32' not in open(ini).read().splitlines():
            return 'settings file lacks graphics=enhanced32'
        return f'ok: {len(orig_colours)} -> {len(cols)} colours (32-bit look)'

    one('enhanced32', ['--menu', 'graphics:32bit,size:4x'], enhanced32)

    # a hand-drawn pack tile: the first exported tile, painted magenta, must appear; Original hides it again
    names = sorted(f for f in os.listdir(exp) if f.endswith('.png')) if os.path.isdir(exp) else []
    if names:
        os.makedirs(pack, exist_ok=True)
        from make_icon import png_encode
        magenta_png = png_encode(32, [[(255, 0, 255)] * 32 for _ in range(32)])
        for n in names:                    # the export covers the whole run: some of these tiles are on screen
            open(os.path.join(pack, n), 'wb').write(magenta_png)

        def magenta(cw, ch, rows):
            n = sum(c == (255, 0, 255) for row in rows for c in row)
            return f'ok: {n} pack-tile pixels ({len(names)} pack tiles)' if n >= 16 else f'pack tiles not drawn ({n} px)'

        def no_magenta(cw, ch, rows):
            n = sum(c == (255, 0, 255) for row in rows for c in row)
            return 'ok: Original shows no pack tiles' if n == 0 else f'{n} pack pixels in Original'

        one('packtile', [], magenta)              # settings reloaded from enhanced.ini: graphics=enhanced
        one('original', ['--menu', 'graphics:original'], no_magenta)
    return fails



def hd_checks(exe, timeout, hpng, frame, names):
    """T18 Video > Graphics > HD: the Direct3D 11 renderer draws (not the 32-bit fallback), the picture is lit
    (> 2000 colours) with flat areas at their exact colour, fullscreen keeps the layout's black bars, and leaving HD
    gives the GDI picture back"""
    fails = 0
    ini = os.path.join(OUT, 'hd.ini')
    orig = {c for row in frame for c in row}
    cases = [('hd', ['--menu', 'graphics:hd,size:4x']), ('hdfull', ['--menu', 'graphics:hd', '--fullscreen']),
             ('hdoff', ['--menu', 'graphics:hd,size:4x,graphics:original'])]
    for name, extra in cases:
        if names and name not in names:
            continue
        wpng, spng = os.path.join(OUT, f'{name}_window.png'), os.path.join(OUT, f'{name}_screen.png')
        if os.path.exists(ini):
            os.remove(ini)                               # each case starts from default settings
        rc, out = run(exe, ['--frames', str(FRAMES), '--turbo', '--no-audio', '--video-config', winpath(ini),
                            '--save', winpath(os.path.join(OUT, 'video.sav')), '--dump-window', winpath(wpng),
                            '--dump-screen', winpath(spng)] + extra, timeout)
        msg = None
        if rc or not os.path.exists(wpng):
            msg = f'rc={rc}'
        elif 'HD engine: Direct3D 11' not in out:
            msg = 'no Direct3D 11 device (' + next((l for l in out.splitlines() if 'HD ' in l), 'no HD line') + ')'
        elif open(spng, 'rb').read() != open(hpng, 'rb').read():
            msg = 'game frame differs from the headless run'
        else:
            cw, ch, rows = png_decode(open(wpng, 'rb').read())
            if name == 'hdoff':
                same = sum(rows[y * 4 + 2][x * 4 + 2] == frame[y][x] for y in range(240) for x in range(256))
                msg = f'ok: back to Original: {same} / 61440 block centres = the NES frame' if (cw, ch) == (1024, 960) \
                    and same == 256 * 240 else f'Original after HD: client {cw}x{ch}, {same} / 61440 centres match'
            else:
                x0, y0, pw, ph = (0, 0, 1024, 960) if name == 'hd' else (448, 60, 1024, 960)
                want = (1024, 960) if name == 'hd' else SCREEN
                cols = {rows[y][x] for y in range(y0, y0 + ph) for x in range(x0, x0 + pw)}
                bars = sum(rows[y][x] != (0, 0, 0) for y in range(ch) for x in range(cw)
                           if not (x0 <= x < x0 + pw and y0 <= y < y0 + ph)) if (cw, ch) == want else 0
                same = sum(rows[y0 + y * 4 + 2][x0 + x * 4 + 2] == frame[y][x] for y in range(240) for x in range(256)) \
                    if (cw, ch) == want else 0
                if (cw, ch) != want:
                    msg = f'client {cw}x{ch}, want {want[0]}x{want[1]}'
                elif len(cols) < 2000:
                    msg = f'{len(cols)} colours, want > 2000 (lit relief, shadows, bloom)'
                elif same < 0.5 * 256 * 240:
                    msg = f'only {same} block centres keep their colour (text and the cut-scene black must stay exact)'
                elif bars:
                    msg = f'{bars} non-black pixels in the bars'
                elif name == 'hd' and 'graphics=hd' not in open(ini).read().splitlines():
                    msg = 'settings file lacks graphics=hd'
                else:
                    msg = f'ok: {len(orig)} -> {len(cols)} colours, {same} / 61440 block centres exact (text, black)'
        if not msg.startswith('ok:'):
            fails += 1
            print(f'FAIL {name:10s} {msg}')
            print('     ' + '\n     '.join(out.strip().splitlines()[-8:]))
        else:
            print(f'ok   {name:10s} {msg[4:]}', flush=True)
    if not names or 'hdfx' in names:
        fails += hdfx_check(exe, timeout)
    if not names or 'hdart' in names:
        fails += hdart_check(exe, timeout)
    return fails


def hdfx_check(exe, timeout):
    """T19 HD effects on a group-29 gameplay frame (ice cave with dark pits): all four effects (parallax, shadows,
    atmosphere, material detail) on vs all switched off from the menu: the playfield changes, the HUD is identical
    (it is never lit), and the settings file records hd_effects=none"""
    state = os.path.join(cv.ROOT, '..', 'web', 'test', 'states', 'group29.json')
    keys = ['2520:LEFT:30', '2555:UP:30', '2590:RIGHT:30', '2625:DOWN:30', '2660:A:4']
    pics, ini = {}, os.path.join(OUT, 'hdfx.ini')
    for name, menu in (('on', 'graphics:hd,size:4x'),
                       ('off', 'graphics:hd,size:4x,hd:parallax,hd:shadows,hd:atmosphere,hd:detail,hd:reflections,hd:lights')):
        if os.path.exists(ini):
            os.remove(ini)
        png = os.path.join(OUT, f'hdfx_{name}.png')
        rc, out = run(exe, ['--load-state', winpath(state), '--frames', '175', '--turbo', '--no-audio',
                            '--video-config', winpath(ini), '--save', winpath(os.path.join(OUT, 'video.sav')),
                            '--keys'] + keys + ['--menu', menu, '--dump-window', winpath(png)], timeout)
        if rc or not os.path.exists(png) or 'HD engine: Direct3D 11' not in out:
            print(f'FAIL hdfx       run {name}: rc={rc}')
            print('     ' + '\n     '.join(out.strip().splitlines()[-8:]))
            return 1
        pics[name] = png_decode(open(png, 'rb').read())[2]
        if name == 'off':
            lines = open(ini).read().splitlines()
    on, off = pics['on'], pics['off']
    hud = sum(on[y][x] != off[y][x] for y in range(780, 890) for x in range(1024))
    play = sum(on[y][x] != off[y][x] for y in range(0, 720) for x in range(32, 1024))
    total = 720 * 992
    if 'hd_effects=noflicker,art' not in lines:
        msg = 'settings file lacks hd_effects=noflicker,art'
    elif hud:
        msg = f'{hud} HUD pixels change with the effects (HUD must stay unlit)'
    elif play < 0.3 * total:
        msg = f'only {play} / {total} playfield pixels change with the effects'
    else:
        msg = f'ok: effects change {play} / {total} playfield pixels, HUD identical, hd_effects=noflicker,art saved'
    print(f'{"ok  " if msg.startswith("ok:") else "FAIL"} {"hdfx":10s} {msg[4:] if msg.startswith("ok:") else msg}', flush=True)
    return 0 if msg.startswith('ok:') else 1


def hdart_check(exe, timeout):
    """T25 built-in art: the cave gameplay frame in HD with the starter packs built into the exe vs Graphics > HD:
    Built-in art switched off: the exe reports every packs/ tile in its atlas, the playfield changes (hero, enemies,
    scenery), the HUD is identical (its font is never in a pack)"""
    state = os.path.join(cv.ROOT, '..', 'web', 'test', 'states', 'group29.json')
    pics, ini = {}, os.path.join(OUT, 'hdart.ini')
    for name, menu in (('on', 'graphics:hd,size:4x'), ('off', 'graphics:hd,size:4x,hd:art')):
        if os.path.exists(ini):
            os.remove(ini)
        png = os.path.join(OUT, f'hdart_{name}.png')
        rc, out = run(exe, ['--load-state', winpath(state), '--frames', '60', '--turbo', '--no-audio',
                            '--video-config', winpath(ini), '--save', winpath(os.path.join(OUT, 'video.sav')),
                            '--pack', winpath(os.path.join(OUT, 'no_pack')), '--menu', menu, '--dump-window', winpath(png)], timeout)
        if rc or not os.path.exists(png) or 'HD engine: Direct3D 11' not in out:
            print(f'FAIL hdart      run {name}: rc={rc}')
            print('     ' + '\n     '.join(out.strip().splitlines()[-8:]))
            return 1
        if name == 'on':
            tiles = next((l for l in out.splitlines() if 'HD engine' in l), '')
        pics[name] = png_decode(open(png, 'rb').read())[2]
    on, off = pics['on'], pics['off']
    hud = sum(on[y][x] != off[y][x] for y in range(780, 890) for x in range(1024))
    play = sum(on[y][x] != off[y][x] for y in range(0, 720) for x in range(32, 1024))
    total = sum(len([f for f in os.listdir(os.path.join(cv.ROOT, 'packs', d)) if f.endswith('.png')])
                for d in os.listdir(os.path.join(cv.ROOT, 'packs')) if os.path.isdir(os.path.join(cv.ROOT, 'packs', d)))
    if f'{total} pack tiles in the atlas' not in tiles:
        msg = f'atlas line: {tiles}'
    elif hud:
        msg = f'{hud} HUD pixels change with the built-in art'
    elif play < 20000:
        msg = f'only {play} playfield pixels change with the built-in art'
    else:
        msg = f'ok: {total} built-in tiles, {play} playfield pixels repainted, HUD identical'
    print(f'{"ok  " if msg.startswith("ok:") else "FAIL"} {"hdart":10s} {msg[4:] if msg.startswith("ok:") else msg}', flush=True)
    return 0 if msg.startswith('ok:') else 1


def hero_check(exe, timeout):
    """T11 packs/hero: a group-29 gameplay frame, Enhanced with and without the pack. The pack loads without skipped
    files, the hero's 4x box gets the pack's colours (hair ramp), and nothing outside the box changes."""
    state = os.path.join(cv.ROOT, '..', 'web', 'test', 'states', 'group29.json')
    pack = os.path.join(cv.ROOT, 'packs', 'hero')
    keys = ['2520:LEFT:30', '2555:UP:30', '2590:RIGHT:30', '2625:DOWN:30', '2660:A:4']
    pics, nes = {}, os.path.join(OUT, 'hero_nes.png')
    for name, pk in (('with', pack), ('without', os.path.join(OUT, 'no_pack'))):
        png = os.path.join(OUT, f'hero_{name}.png')
        rc, out = run(exe, ['--load-state', winpath(state), '--frames', '175', '--turbo', '--no-audio',
                            '--video-config', winpath(os.path.join(OUT, 'hero.ini')), '--keys'] + keys +
                      ['--menu', 'graphics:enhanced,size:4x', '--pack', winpath(pk), '--dump-window', winpath(png),
                       '--dump-screen', winpath(nes), '--save', winpath(os.path.join(OUT, 'video.sav'))], timeout)
        if rc or not os.path.exists(png) or 'graphics pack:' in out:
            print(f'FAIL heropack  run {name}: rc={rc}' + ('' if 'graphics pack:' not in out else ', pack files skipped'))
            print('     ' + '\n     '.join(out.strip().splitlines()[-8:]))
            return 1
        pics[name] = png_decode(open(png, 'rb').read())[2]
    # the hero: bounding box of its magenta ($14 = D800CC) in the NES frame, 2 px margin, at 4x
    _, _, fr = png_decode(open(nes, 'rb').read())
    pts = [(x, y) for y, row in enumerate(fr) for x, p in enumerate(row) if p == (0xD8, 0x00, 0xCC)]
    if not pts:
        print('FAIL heropack  no hero colour in the frame'); return 1
    x0, y0 = (min(p[0] for p in pts) - 6) * 4, (min(p[1] for p in pts) - 8) * 4
    x1, y1 = (max(p[0] for p in pts) + 6) * 4, (max(p[1] for p in pts) + 8) * 4
    a, b = pics['with'], pics['without']
    inside = sum(a[y][x] != b[y][x] for y in range(y0, y1) for x in range(x0, x1))
    outside = sum(a[y][x] != b[y][x] for y in range(len(a)) for x in range(len(a[0]))
                  if not (x0 <= x < x1 and y0 <= y < y1))
    hair = sum(a[y][x] == (0xC8, 0x2C, 0xC4) for y in range(y0, y1) for x in range(x0, x1))
    if inside < 500 or outside or hair < 50:
        print(f'FAIL heropack  {inside} hero pixels changed, {outside} outside the hero box, {hair} hair-ramp pixels')
        return 1
    print(f'ok   heropack   {inside} hero pixels repainted, 0 elsewhere, {hair} hair-ramp pixels')
    return 0


ENEMY_CASES = (('group29', 175, ['2520:LEFT:30', '2555:UP:30', '2590:RIGHT:30', '2625:DOWN:30', '2660:A:4']),
               ('group05', 120, ['2530:RIGHT:20', '2560:A:6']))
NPC_CASES = (('group18', 120, ['2520:LEFT:20']), ('armorshop', 60, []))
# the walk of tools/sprite_capture.py (the boss pieces in palette 0 show while the hero moves)
BOSS_CASES = (('bossflee', 170, [f'{2504 + o}:{b}:{h}' for o, b, h in ((14, 'LEFT', 24), (40, 'A', 4), (60, 'UP', 24), (86, 'A', 4),
                                                                      (106, 'RIGHT', 30), (138, 'A', 4), (160, 'DOWN', 24))]),
              ('fanturret', 60, [f'{2509 + o}:{b}:{h}' for o, b, h in ((14, 'LEFT', 24), (40, 'A', 4))]))


def hero_pixels(state, frames, keys, nes_png):
    """the hero's own pixels (sprite tiles from the player banks $40-$42) in the last frame of a headless ReC.Run run
    with --dump-tags; checked against the exe's --dump-screen frame (same colours there) -> list of (x, y) or None"""
    import json
    import shutil
    from sprite_pack import load_dump, VALID, HERO_BANKS
    d = os.path.join(OUT, 'hero_tags')
    shutil.rmtree(d, ignore_errors=True)
    os.makedirs(d)
    run_bin = os.path.join(cv.ROOT, 'src', 'ReC.Run', 'bin', 'Release', 'net10.0', 'ReC.Run')
    r = subprocess.run([run_bin, '--load-state', state, '--frames', str(frames), '--quiet', '--dump-tags', d, '1'] +
                       (['--keys'] + keys if keys else []), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=300)
    files = sorted(os.listdir(d))
    if r.returncode or not files:
        return None
    tags, fb, _ = load_dump(os.path.join(d, files[-1]))
    rgb = json.load(open(os.path.join(cv.ROOT, '..', 'web', 'data', 'palettes.json')))['nesRgb']
    _, _, fr = png_decode(open(nes_png, 'rb').read())
    if any(fr[p // 256][p % 256] != (rgb[fb[p] & 0x3F] >> 16, rgb[fb[p] & 0x3F] >> 8 & 255, rgb[fb[p] & 0x3F] & 255)
           for p in range(0, 256 * 240, 97)):
        return None                                          # not the same frame
    return [(p % 256, p // 256) for p, t in enumerate(tags)
            if t & VALID and (t >> 19 & 7) >= 4 and ((t & 0x1FFF) >> 6) in HERO_BANKS]


def enemy_check(exe, timeout, label='enemypack', pack_name='enemies', cases=ENEMY_CASES):
    """T12 packs/enemies: areas $29 and $05 with enemies on screen, Enhanced with and without the pack (T16: also
    packs/npcs on a town and a shop). The pack loads without skipped files, sprite pixels change, and the hero's
    pixels do not (enemypack: the box around its magenta; npcpack / bosspack: its exact pixels from a headless --dump-tags run,
    as NPCs and palette-0 bosses wear the same magenta)."""
    fails = 0
    for st, frames, keys in cases:
        state = os.path.join(cv.ROOT, '..', 'web', 'test', 'states', st + '.json')
        pics, nes = {}, os.path.join(OUT, f'enemy_{st}_nes.png')
        for name, pk in (('with', os.path.join(cv.ROOT, 'packs', pack_name)), ('without', os.path.join(OUT, 'no_pack'))):
            png = os.path.join(OUT, f'{label}_{st}_{name}.png')
            rc, out = run(exe, ['--load-state', winpath(state), '--frames', str(frames), '--turbo', '--no-audio',
                                '--video-config', winpath(os.path.join(OUT, 'enemy.ini'))] + (['--keys'] + keys if keys else []) +
                          ['--menu', 'graphics:enhanced,size:4x', '--pack', winpath(pk), '--dump-window', winpath(png),
                           '--dump-screen', winpath(nes), '--save', winpath(os.path.join(OUT, 'video.sav'))], timeout)
            if rc or not os.path.exists(png) or 'graphics pack:' in out:
                print(f'FAIL {label:9s} {st} {name}: rc={rc}' + (', pack files skipped' if 'graphics pack:' in out else ''))
                fails += 1
                break
            pics[name] = png_decode(open(png, 'rb').read())[2]
        if len(pics) < 2:
            continue
        a, b = pics['with'], pics['without']
        changed = sum(a[y][x] != b[y][x] for y in range(len(a)) for x in range(len(a[0])))
        if label != 'enemypack':
            hp = hero_pixels(state, frames, keys, nes)
            if not hp:
                print(f'FAIL {label:9s} {st}: no hero pixels from the headless run (or not the same frame)')
                fails += 1
                continue
            in_hero = sum(a[y * 4 + 2][x * 4 + 2] != b[y * 4 + 2][x * 4 + 2] for x, y in hp)
        else:
            _, _, fr = png_decode(open(nes, 'rb').read())
            hero = [(x, y) for y, row in enumerate(fr) for x, p in enumerate(row) if p == (0xD8, 0x00, 0xCC)]
            box = ((min(p[0] for p in hero) - 4) * 4, (min(p[1] for p in hero) - 6) * 4,
                   (max(p[0] for p in hero) + 4) * 4, (max(p[1] for p in hero) + 6) * 4) if hero else (0, 0, 0, 0)
            in_hero = sum(a[y][x] != b[y][x] for y in range(box[1], box[3]) for x in range(box[0], box[2]))
        if changed < 300 or in_hero:
            print(f'FAIL {label:9s} {st}: {changed} pixels changed, {in_hero} on the hero')
            fails += 1
        else:
            print(f'ok   {label:10s} {st}: {changed} sprite pixels repainted, hero untouched')
    return fails


def bg_check(exe, timeout):
    """T15 packs/backgrounds: a town (town02_enter) and a field (group29) frame, Enhanced 16-bit and 32-bit, with and
    without the pack. The pack loads without skipped files, >= 15 % of the picture changes, and the hero's own
    magenta pixels (sprite) are the same in both pictures."""
    fails = 0
    for st, frames, keys in (('town02_enter', 120, ['2520:UP:20']), ('group29', 120, ['2520:LEFT:30'])):
        state = os.path.join(cv.ROOT, '..', 'web', 'test', 'states', st + '.json')
        for mode in ('enhanced', '32bit'):
            pics, nes = {}, os.path.join(OUT, f'bg_{st}_nes.png')
            for name, pk in (('with', BG_PACK), ('without', os.path.join(OUT, 'no_pack'))):
                png = os.path.join(OUT, f'bg_{st}_{mode}_{name}.png')
                rc, out = run(exe, ['--load-state', winpath(state), '--frames', str(frames), '--turbo', '--no-audio',
                                    '--video-config', winpath(os.path.join(OUT, 'bg.ini')), '--keys'] + keys +
                              ['--menu', f'graphics:{mode},size:4x', '--pack', winpath(pk), '--dump-window', winpath(png),
                               '--dump-screen', winpath(nes), '--save', winpath(os.path.join(OUT, 'video.sav'))], timeout)
                if rc or not os.path.exists(png) or 'graphics pack:' in out:
                    print(f'FAIL bgpack    {st} {mode} {name}: rc={rc}' + (', pack files skipped' if 'graphics pack:' in out else ''))
                    fails += 1
                    break
                pics[name] = png_decode(open(png, 'rb').read())[2]
            if len(pics) < 2:
                continue
            a, b = pics['with'], pics['without']
            _, _, fr = png_decode(open(nes, 'rb').read())
            hero = [(x, y) for y, row in enumerate(fr) for x, p in enumerate(row) if p == (0xD8, 0x00, 0xCC)]
            changed = sum(a[y][x] != b[y][x] for y in range(len(a)) for x in range(len(a[0])))
            # centre of each hero pixel's 4x block (edges may be anti-aliased / shadowed against new scenery)
            hero_diff = sum(a[y * 4 + 2][x * 4 + 2] != b[y * 4 + 2][x * 4 + 2] for x, y in hero)
            total = len(a) * len(a[0])
            if changed < total * 0.15 or hero_diff or not hero:
                print(f'FAIL bgpack    {st} {mode}: {changed} of {total} pixels changed, {hero_diff} of {len(hero)} hero pixels')
                fails += 1
            else:
                print(f'ok   bgpack     {st} {mode}: {100 * changed / total:.0f} % repainted, {len(hero)} hero pixels untouched')
    return fails


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
