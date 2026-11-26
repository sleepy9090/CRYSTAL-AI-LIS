"""Loudness envelope of a 16-bit PCM WAV: RMS (dBFS, both channels) per window, as one text bar per line.

usage: python3 tools/wav_env.py IN.wav [WINDOW_SECONDS=0.25] [MAX_LINES=80]
"""
import math
import struct
import sys


def main(argv):
    if not argv:
        print(__doc__); return 2
    b = open(argv[0], 'rb').read()
    ch, rate = struct.unpack('<HI', b[22:28])
    n = (len(b) - 44) // 2
    s = struct.unpack(f'<{n}h', b[44:44 + 2 * n])
    win = float(argv[1]) if len(argv) > 1 else 0.25
    lines = int(argv[2]) if len(argv) > 2 else 80
    step = int(win * rate) * ch
    for k, i in enumerate(range(0, n - step + 1, step)):
        if k >= lines:
            break
        v = s[i:i + step]
        rms = math.sqrt(sum(x * x for x in v) / len(v))
        db = 20 * math.log10(max(rms, 1) / 32768)
        print(f'{k * win:6.2f}s {db:6.1f} dB ' + '#' * max(0, int(db + 60)))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
