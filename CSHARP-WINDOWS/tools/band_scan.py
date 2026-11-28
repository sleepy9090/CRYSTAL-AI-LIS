"""Find horizontal banding (playtest 14: "lines generating to the right of the player as he moves") in window dumps.

usage: python3 tools/band_scan.py X0 Y0 W H PNG [PNG ..] [--top N]

For each image, the crop (X0, Y0, W, H) is read as grey (ffmpeg); per row the mean brightness is compared with the mean
of the rows 3-6 above and below (a band is a row, or a few, that differ from both sides); prints per image the largest
deviation and its row, sorted, the top N (default 10).
"""
import subprocess
import sys


def grey(path, x0, y0, w, h):
    raw = subprocess.run(['ffmpeg', '-loglevel', 'error', '-i', path, '-vf', f'crop={w}:{h}:{x0}:{y0}',
                          '-f', 'rawvideo', '-pix_fmt', 'gray', '-'], capture_output=True, check=True).stdout
    return [raw[y * w:(y + 1) * w] for y in range(h)]


def score(rows):
    means = [sum(r) / len(r) for r in rows]
    best, at = 0.0, -1
    for y in range(6, len(means) - 6):
        side = (sum(means[y - 6:y - 2]) + sum(means[y + 3:y + 7])) / 8
        d = abs(means[y] - side)
        if d > best:
            best, at = d, y
    return best, at


def main(argv):
    top = 10
    if '--top' in argv:
        i = argv.index('--top'); top = int(argv[i + 1]); argv = argv[:i] + argv[i + 2:]
    x0, y0, w, h = map(int, argv[:4])
    res = []
    for p in argv[4:]:
        s, at = score(grey(p, x0, y0, w, h))
        res.append((s, at + y0, p))
    res.sort(reverse=True)
    for s, at, p in res[:top]:
        print(f'{s:6.2f} row {at:4d}  {p}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
