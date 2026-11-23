"""Video menu checks of ReC.exe under Wine + Xvfb (Video.cs / VideoMenu.cs).

usage: python3 tools/win_video_check.py [--exe PATH] [--timeout S] [--keep] [NAME..]
Each case runs the real window for 787 frames (frame_no 1000, the intro picture of app.ico) (--turbo) with its own settings file (out/video/NAME.ini), applies menu
commands with `--menu` (the same Command() path as clicking the items), and reads the drawn client area back with
`--dump-window`. Checked per case: exit 0, the client size, the picture rectangle (from the same layout rules as
Video.cs, recomputed here), black bars outside it, the picture itself against the headless frame (nearest-neighbour
resample; exact for integer square pixels, <= 2 % differing pixels for stretched shapes, skipped for the smooth filter),
and the settings file written by the menu. Xvfb screen: 1920x1080x24.
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
RATIO = {'square': None, 'tv': 256 * 8 / 7, '4:3': 4 / 3, '16:10': 16 / 10, '16:9': 16 / 9}

# name, extra args, menu commands, expected client size, expected settings (aspect, integer, crop, smooth), ini lines
CASES = [
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


def check_picture(rows, rect, frame, crop, exact):
    """bars black; picture = nearest-neighbour resample of the frame (exact, or <= 2 % differing pixels)"""
    x0, y0, w, h = rect
    ch, cw = len(rows), len(rows[0])
    for y in range(ch):
        row = rows[y]
        inside_y = y0 <= y < y0 + h
        for x in (range(cw) if not inside_y else list(range(0, max(0, x0))) + list(range(x0 + w, cw))):
            if row[x] != (0, 0, 0):
                return f'bar pixel ({x},{y}) = {row[x]}, rect {rect}'
    vh, first = (224, 8) if crop else (240, 0)
    bad = total = 0
    for y in range(max(0, y0), min(ch, y0 + h)):
        sy = first + (y - y0) * vh // h
        for x in range(max(0, x0), min(cw, x0 + w)):
            total += 1
            if rows[y][x] != frame[sy][(x - x0) * 256 // w]:
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
    for name, extra, menu, size, (aspect, integer, crop, smooth), ini_lines in CASES:
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
                    msg = check_picture(rows, rect, frame, crop, exact=aspect == 'square' and integer)
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
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
