"""Audio problems in a 16-bit PCM WAV (e.g. a recording of a playtest), with timestamps.

usage: python3 tools/wav_events.py IN.wav [--window MS=20] [--list N=40]
Reports per window (both channels mixed):
  clip     samples at or beyond +-32700 (the output hit full scale: distortion / crackle)
  click    a second-difference spike > 6x the window's median and > 3000 (a step in the waveform: a pop)
  gap      a window under -60 dBFS between two windows above -35 dBFS (a dropout: the sound stopped)
Prints a summary, then the first N events (time in seconds, kind, detail).
"""
import math
import struct
import sys


def main(argv):
    if not argv:
        print(__doc__); return 2
    win_ms, n_list = 20, 40
    if '--window' in argv:
        win_ms = int(argv[argv.index('--window') + 1])
    if '--list' in argv:
        n_list = int(argv[argv.index('--list') + 1])
    b = open(argv[0], 'rb').read()
    # find the fmt and data chunks
    pos, ch, rate, data = 12, 2, 44100, b''
    while pos + 8 <= len(b):
        cid, size = b[pos:pos + 4], struct.unpack('<I', b[pos + 4:pos + 8])[0]
        if cid == b'fmt ':
            ch, rate = struct.unpack('<HI', b[pos + 10:pos + 16])
        elif cid == b'data':
            data = b[pos + 8:pos + 8 + size]
            break
        pos += 8 + size + (size & 1)
    n = len(data) // 2
    s = struct.unpack(f'<{n}h', data[:n * 2])
    frames = n // ch
    mono = [sum(s[f * ch:(f + 1) * ch]) / ch for f in range(frames)]
    w = rate * win_ms // 1000
    events, rms_db, clip_total, click_total, gap_total = [], [], 0, 0, 0
    for k in range(frames // w):
        seg = mono[k * w:(k + 1) * w]
        raw = s[k * w * ch:(k + 1) * w * ch]
        rms = math.sqrt(sum(x * x for x in seg) / len(seg))
        rms_db.append(20 * math.log10(max(rms, 1) / 32768))
        clip = sum(1 for x in raw if abs(x) >= 32700)
        if clip:
            clip_total += clip
            events.append((k * win_ms / 1000, 'clip', f'{clip} samples at full scale'))
        d2 = [abs(seg[i] - 2 * seg[i - 1] + seg[i - 2]) for i in range(2, len(seg))]
        if d2:
            med = sorted(d2)[len(d2) // 2]
            m = max(d2)
            if m > 3000 and m > 6 * max(med, 50):
                click_total += 1
                events.append((k * win_ms / 1000 + d2.index(m) / rate, 'click', f'step {m:.0f} (median {med:.0f})'))
    for k in range(1, len(rms_db) - 1):
        if rms_db[k] < -60 and rms_db[k - 1] > -35 and max(rms_db[k + 1:k + 6] or [-99]) > -35:
            gap_total += 1
            events.append((k * win_ms / 1000, 'gap', f'{rms_db[k - 1]:.0f} dB -> {rms_db[k]:.0f} dB'))
    events.sort()
    dur = frames / rate
    print(f'{argv[0]}: {dur:.1f} s, {ch} ch, {rate} Hz; level {sum(rms_db) / max(1, len(rms_db)):.1f} dBFS mean')
    print(f'clipped samples {clip_total} in {sum(1 for e in events if e[1] == "clip")} windows; clicks {click_total}; dropouts {gap_total}')
    for t, kind, detail in events[:n_list]:
        print(f'  {t:7.2f}s  {kind:5s}  {detail}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
