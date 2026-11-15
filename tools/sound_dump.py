#!/usr/bin/env python3
"""Disassemble re-c sound streams (bank 24 driver) into op text.

Usage:
  sound_dump.py music <id>     music id $00-$1F (header table 24:8C0C)
  sound_dump.py sfx <id>       sfx id $20-$77 (header table bank 12 offset 0)
  sound_dump.py all            every non-null music + sfx header summary (no streams)
  options: --max N  (bytes per stream, default 512)

Memory map during playback: R6=24 ($8000-$9FFF), R7=25 ($A000-$BFFF) for music;
SFX: R7=12 at $A000, stream ptrs stored as $8xxx and +$2000 at load (also for A0 targets).
Ids may be given as hex ($13, 0x13) or decimal.
"""
import sys
from nesrom import load_rom, BANK_SIZE

_, PRG, _ = load_rom()

MUSIC_TABLE = 0x8C0C
NOTE = {0xC: 'C', 0xD: 'D', 0xE: 'E', 0xF: 'F', 0xA: 'A', 0xB: 'B',
        0x0: 'G', 0x1: 'C#', 0x2: 'D#', 0x4: 'F#', 0x5: 'G#', 0x6: 'A#'}
DUR = [0x05, 0x40, 0x38, 0x01, 0x14, 0x02, 0x28, 0x03, 0x50, 0x04, 0x1E, 0x1C, 0x07, 0x2A, 0x13, 0x15,
       0x06, 0x08, 0x0C, 0x09, 0x18, 0x0A, 0x30, 0x0B, 0x60, 0x12, 0x24, 0x0D, 0x20, 0x0E, 0x10, 0x0F]
CHN_MUSIC = ['SQ1', 'SQ2', 'TRI', 'NOISE']
CHN_SFX = ['SFX-SQ2', 'SFX-NOISE']


def rd_music(addr):
    bank = 24 if addr < 0xA000 else 25
    return PRG[bank * BANK_SIZE + (addr & 0x1FFF)]


def rd_sfx(addr):
    return PRG[12 * BANK_SIZE + (addr & 0x1FFF)]


def rd16(rd, a):
    return rd(a) | (rd(a + 1) << 8)


def note_name(x, noise):
    if noise:
        return 'noise-period %d' % x
    return NOTE.get(x, '?%X' % x)


def op_text(rd, a, noise, is_sfx):
    """Return (text, length, is_end)."""
    b = rd(a)
    hi, x = b >> 4, b & 0xF
    if hi in (0, 1):
        if is_sfx:
            return 'END (release channel)', 1, True
        n = b & 0x1F
        return ('END next-music=$%02X' % n) + (' (stop)' if n == 0 else ' (loop/chain)'), 1, True
    if hi == 2:
        return 'NOTE %s' % note_name(x, noise), 1, False
    if hi == 3:
        return 'OCTAVE %d' % x, 1, False
    if hi == 4:
        return 'REST' if x == 0 else 'HOLD vol=%d' % x, 1, False
    if 0x50 <= b <= 0x6F:
        return 'LEN %d frames' % DUR[b - 0x50], 1, False
    if hi == 7:
        return 'TIE' if x == 7 else 'LEGATO %s' % note_name(x, noise), 1, False
    if hi == 8:
        return 'VOL %d' % x, 1, False
    if hi == 9:
        return 'DUTY/CTRL $%X (duty=%d constvol=%d)' % (x, (x >> 1) & 3, x & 1), 1, False
    if hi == 0xA:
        if x == 0:
            t = rd16(rd, a + 1)
            if is_sfx:
                t += 0x2000
            return 'CALL $%04X' % t, 3, False
        if x in (1, 2):
            return 'LOOP%d count=%d' % (x, rd(a + 1)), 2, False
        if x == 4:
            return 'LOOP1 END', 1, False
        return 'LOOP2/RETURN END', 1, False
    if hi == 0xB:
        return 'SWEEP $%02X + NOTE %s' % (rd(a + 1), note_name(x, noise)), 2, False
    if hi == 0xC:
        return 'SWEEPREG $%02X (%s)' % (rd(a + 1), 'off' if x & 1 else 'on'), 2, False
    if hi == 0xD:
        return ('DRUM #%d' % x) if noise else 'NOTE %s detuned' % note_name(x, noise), 1, False
    if hi == 0xE:
        return 'ENVELOPE #%d' % x, 1, False
    if x < 8:
        return 'VIBRATO depth=%d' % x, 1, False
    return 'VIBRATO off' if x == 8 else 'WAIT 1 frame', 1, False


def dump_stream(rd, start, noise, is_sfx, maxb):
    a = start
    while a < start + maxb:
        text, n, end = op_text(rd, a, noise, is_sfx)
        raw = ' '.join('%02X' % rd(a + i) for i in range(n))
        print('    $%04X  %-9s %s' % (a, raw, text))
        a += n
        if end:
            return
    print('    ... (truncated at %d bytes)' % maxb)


def music_header(mid):
    p = rd16(rd_music, MUSIC_TABLE + mid * 2)
    if p == 0:
        return None
    return p, rd_music(p), [rd16(rd_music, p + 1 + i * 2) for i in range(4)]


def sfx_header(sid):
    p = rd16(rd_sfx, 0xA000 + (sid - 0x20) * 2)
    if p == 0:
        return None
    return p, rd_sfx(p), [rd16(rd_sfx, p + 1 + i * 2) + 0x2000 for i in range(2)]


def parse_id(s):
    s = s.strip()
    if s.startswith('$'):
        return int(s[1:], 16)
    return int(s, 0)


def main():
    args = sys.argv[1:]
    maxb = 512
    if '--max' in args:
        i = args.index('--max')
        maxb = int(args[i + 1], 0)
        del args[i:i + 2]
    if not args:
        print(__doc__)
        return
    kind = args[0]
    if kind == 'all':
        for m in range(0x20):
            h = music_header(m)
            print('music $%02X: %s' % (m, 'null' if h is None else
                  'hdr $%04X b0=%02X streams %s' % (h[0], h[1], ' '.join('$%04X' % s for s in h[2]))))
        for s in range(0x20, 0x78):
            h = sfx_header(s)
            print('sfx   $%02X: %s' % (s, 'null' if h is None else
                  'hdr $%04X prio=%02X streams %s' % (h[0], h[1], ' '.join('$%04X' % x for x in h[2]))))
        return
    sid = parse_id(args[1])
    if kind == 'music':
        h = music_header(sid & 0x1F)
        rd, names, is_sfx = rd_music, CHN_MUSIC, False
    else:
        h = sfx_header(sid)
        rd, names, is_sfx = rd_sfx, CHN_SFX, True
    if h is None:
        print('%s $%02X: null entry' % (kind, sid))
        return
    print('%s $%02X: header $%04X byte0=$%02X' % (kind, sid, h[0], h[1]))
    for name, ptr in zip(names, h[2]):
        print('  [%s] $%04X' % (name, ptr))
        dump_stream(rd, ptr, 'NOISE' in name, is_sfx, maxb)


if __name__ == '__main__':
    main()
