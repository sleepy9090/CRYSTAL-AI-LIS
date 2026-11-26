"""DC (mean) and AC (RMS around the mean) per window and channel of a 16-bit PCM WAV: a DC offset that jumps is a pop.

usage: python3 tools/wav_dc.py IN.wav [WINDOW_MS=100] [FROM_S=0] [TO_S=end]
One line per window: time, then per channel mean / ac-rms.
"""
import math
import struct
import sys


def main(argv):
    if not argv:
        print(__doc__); return 2
    b = open(argv[0], 'rb').read()
    pos, ch, rate, data = 12, 2, 44100, b''
    while pos + 8 <= len(b):
        cid, size = b[pos:pos + 4], struct.unpack('<I', b[pos + 4:pos + 8])[0]
        if cid == b'fmt ':
            ch, rate = struct.unpack('<HI', b[pos + 10:pos + 16])
        elif cid == b'data':
            data = b[pos + 8:pos + 8 + size]
            break
        pos += 8 + size + (size & 1)
    win = int(argv[1]) if len(argv) > 1 else 100
    t0 = float(argv[2]) if len(argv) > 2 else 0
    frames = len(data) // (2 * ch)
    t1 = float(argv[3]) if len(argv) > 3 else frames / rate
    w = rate * win // 1000
    s = struct.unpack(f'<{frames * ch}h', data[:frames * ch * 2])
    for k in range(int(t0 * rate) // w, min(frames // w, int(t1 * rate) // w)):
        out = []
        for c in range(ch):
            v = s[k * w * ch + c:(k + 1) * w * ch:ch]
            m = sum(v) / len(v)
            ac = math.sqrt(sum((x - m) ** 2 for x in v) / len(v))
            out.append(f'{m:8.0f} / {ac:6.0f}')
        print(f'{k * win / 1000:7.2f}s  ' + '   '.join(out))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
