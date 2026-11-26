"""Print the samples of a 16-bit PCM WAV around a time (to look at a click / glitch).

usage: python3 tools/wav_dump.py IN.wav SECONDS [FRAMES_BEFORE=12] [FRAMES_AFTER=24]
One line per frame: index offset, then each channel's value.
"""
import struct
import sys


def main(argv):
    if len(argv) < 2:
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
    t = float(argv[1])
    before = int(argv[2]) if len(argv) > 2 else 12
    after = int(argv[3]) if len(argv) > 3 else 24
    f0 = int(t * rate)
    for f in range(max(0, f0 - before), f0 + after):
        o = f * ch * 2
        if o + ch * 2 > len(data):
            break
        vals = struct.unpack(f'<{ch}h', data[o:o + ch * 2])
        print(f'{f - f0:+5d}  ' + '  '.join(f'{v:7d}' for v in vals))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
