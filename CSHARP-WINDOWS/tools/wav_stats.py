"""RMS / peak of a 16-bit PCM WAV, per channel, from SKIP seconds on (to compare audio paths).

usage: python3 tools/wav_stats.py IN.wav [SKIP_SECONDS]
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
    skip = int(float(argv[1]) * rate) * ch if len(argv) > 1 else 0
    for c in range(ch):
        v = s[skip + c::ch]
        rms = math.sqrt(sum(x * x for x in v) / max(1, len(v)))
        print(f'ch{c}: {len(v) / rate:.2f} s, rms {rms:.1f} ({20 * math.log10(max(rms, 1e-9) / 32767):.1f} dBFS), '
              f'peak {max(abs(x) for x in v)}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
