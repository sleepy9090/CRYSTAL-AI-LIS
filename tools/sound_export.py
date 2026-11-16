#!/usr/bin/env python3
"""Export every re-c music track + SFX to web/data/sound.json (session 26).

Usage:
  sound_export.py [--out web/data/sound.json] [--max-frames N] [--summary]

Output JSON:
  tables  : driver tables copied from bank 24 (period lo/hi, durations, octave
            table incl. the $842D quirk bytes, detune, envelopes tonal/noise,
            volume table, vibrato step + raw offset bytes from $8714, DMC records)
  dmc     : {addr, bytes (hex string)} the single DMC sample 31:FA00 (993 bytes)
  music   : list of {id, hdr, b0, loopId, channels:[{chn, start, ops, events, loop}]}
  sfx     : list of {id, hdr, prio, channels:[...]}
  ops     : static stream disassembly [{a, raw, op, ...}] (A0 targets already fixed up)
  events  : flattened timeline per channel, one entry per frame-ending op:
            {f: start frame, len: frames, k: kind, ...state}
            kind: note | legato | tie | rest | hold | sweep | wait | end
            state fields: x, oct (row 0-7), period, hz, vol (base 0-15), ctl
            ($0168 high nibble), env (envelope # or null), vib (depth or null),
            sweep ($016E), drum (armed DMC record or 0; NOISE only), retrig
  loop    : event index the stream jumps back to forever (null = ends)
Per-frame envelope/vibrato are NOT pre-rendered: the port ticks them from the
tables exactly like SndChannelTick 24:8276 (see REVERSE.md "Sound data format").
"""
import json
import os
import sys
from nesrom import load_rom, BANK_SIZE

_, PRG, _ = load_rom()

MUSIC_TABLE = 0x8C0C
CPU_HZ = 1789773
NOISE_PERIODS = [4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068]
CHN_MUSIC = ['SQ1', 'SQ2', 'TRI', 'NOISE']
CHN_SFX = ['SFX-SQ2', 'SFX-NOISE']


def b24(addr):
    bank = 24 if addr < 0xA000 else 25
    return PRG[bank * BANK_SIZE + (addr & 0x1FFF)]


def b12(addr):
    return PRG[12 * BANK_SIZE + (addr & 0x1FFF)]


def b31(addr):
    return PRG[(30 if addr < 0xE000 else 31) * BANK_SIZE + (addr & 0x1FFF)]


def rng(rd, a, n):
    return [rd(a + i) for i in range(n)]


def rd16(rd, a):
    return rd(a) | (rd(a + 1) << 8)


PERIOD_LO = rng(b24, 0x87B8, 128)
PERIOD_HI = rng(b24, 0x8838, 128)
DUR = rng(b24, 0x8AB8, 32)
OCTAVE_TAB = rng(b24, 0x842D, 16)   # x=0 reads $60 (RTS), x>=9 reads code bytes
DETUNE = rng(b24, 0x83DA, 8)
DMC_RECS = [rng(b24, 0x8BD8 + i * 4, 4) for i in range(13)]


def hz(chn_type, period, x):
    if chn_type == 3:
        return round(CPU_HZ / NOISE_PERIODS[x & 0xF], 2)
    div = 32 if chn_type == 2 else 16
    return round(CPU_HZ / (div * (period + 1)), 2)


def decode_op(rd, a, is_sfx):
    """Static decode: returns dict with a, raw, op (+args) and byte length."""
    b = rd(a)
    hi, x = b >> 4, b & 0xF
    n = 1
    d = {'a': a}
    if hi in (0, 1):
        d.update(op='end', next=b & 0x1F)
    elif hi == 2:
        d.update(op='note', x=x)
    elif hi == 3:
        d.update(op='octave', x=x)
    elif hi == 4:
        d.update(op='rest' if x == 0 else 'hold', x=x)
    elif 0x50 <= b <= 0x6F:
        d.update(op='len', frames=DUR[b - 0x50])
    elif hi == 7:
        d.update(op='tie' if x == 7 else 'legato', x=x)
    elif hi == 8:
        d.update(op='vol', x=x)
    elif hi == 9:
        d.update(op='ctl', x=x)
    elif hi == 0xA:
        if x == 0:
            t = rd16(rd, a + 1) + (0x2000 if is_sfx else 0)
            d.update(op='call', target=t)
            n = 3
        elif x in (1, 2):
            d.update(op='loop%d' % x, count=rd(a + 1))
            n = 2
        elif x == 4:
            d.update(op='loop1end')
        else:
            d.update(op='loop2end')
    elif hi == 0xB:
        d.update(op='sweepnote', x=x, sweep=rd(a + 1))
        n = 2
    elif hi == 0xC:
        d.update(op='sweepreg', x=x, sweep=rd(a + 1))
        n = 2
    elif hi == 0xD:
        d.update(op='detune', x=x)
    elif hi == 0xE:
        d.update(op='env', x=x)
    else:
        d.update(op='vib', x=x)
    d['raw'] = ' '.join('%02X' % rd(a + i) for i in range(n))
    return d, n


def static_ops(rd, start, is_sfx):
    """Linear disassembly of every byte reachable from start (follows call/loops)."""
    seen, out, work = set(), [], [start]
    while work:
        a = work.pop()
        while a not in seen:
            d, n = decode_op(rd, a, is_sfx)
            seen.add(a)
            out.append(d)
            if d['op'] == 'call':
                work.append(d['target'])
            if d['op'] == 'end':
                break
            a += n
    out.sort(key=lambda d: d['a'])
    return out


def simulate(rd, start, chn_type, is_sfx, max_frames):
    """Flatten one channel stream into frame-ending events (SndChannelTick semantics)."""
    ptr = start
    l1p = l1c = l2p = l2c = 0
    # SndInitChannel 24:8132: $0150=1, octave 0, $0168/$0174/$017A=0, sweep $08
    st = {'oct': 0, 'len': 1, 'vol': 0, 'ctl': 0, 'env': None, 'vib': None,
          'sweep': 0x08, 'drum': 0, 'period': 0, 'x': 0}
    events, seen, f = [], {}, 0
    while f < max_frames:
        key = (ptr, l1p, l1c, l2p, l2c, st['oct'], st['len'], st['vol'], st['ctl'],
               st['env'], st['vib'], st['sweep'], st['drum'])
        if key in seen:
            return events, seen[key]
        seen[key] = len(events)
        ev = None
        base = ptr  # $F5/$F6; $F1 = offset from here (reset on every jump)
        for _ in range(4096):  # ops until one ends the frame
            d, n = decode_op(rd, ptr, is_sfx)
            op = d['op']
            ptr += n
            x = d.get('x', 0)
            if op == 'end':
                ev = {'k': 'end', 'next': d['next']}
                break
            if op == 'octave':
                st['oct'] = OCTAVE_TAB[x] >> 4
            elif op == 'len':
                st['len'] = d['frames']
            elif op == 'vol':
                st['vol'] = x
            elif op == 'ctl':
                if chn_type == 2:
                    st['ctl'] = x
                else:
                    st['ctl'] = ((x & 6) << 1) | ((x & 1)) | 2  # $0168 hi nibble
                    if not x & 1:
                        st['env'] = None
            elif op == 'env':
                st['env'] = x
            elif op == 'vib':
                if x < 8:
                    st['vib'] = x
                elif x == 8:
                    st['vib'] = None
                else:
                    if st['vib'] is not None:
                        st['vib'] = x  # quirk: $0162 = x*8 while b5 stays on
                    ev = {'k': 'wait'}
                    break
            elif op == 'call':
                l2p, l2c = ptr, (ptr - base) & 0xFF  # count = Y = $F1+2 (24:85D1)
                ptr = base = d['target']
            elif op in ('loop1', 'loop2'):
                c = d['count']
                if op == 'loop1':
                    l1p, l1c = ptr, c
                else:
                    l2p, l2c = ptr, c
            elif op == 'loop1end':
                l1c = (l1c - 1) & 0xFF
                if l1c:
                    ptr = base = l1p
            elif op == 'loop2end':
                l2c = (l2c - 1) & 0xFF
                if l2c:
                    ptr = base = l2p
            elif op == 'sweepreg':
                st['sweep'] = 0 if x & 1 else (d['sweep'] | 0x80)
                ev = {'k': 'sweep'}
                break
            elif op in ('note', 'sweepnote', 'detune'):
                if op == 'sweepnote':
                    st['sweep'] = d['sweep'] | 0x80
                if op == 'detune' and chn_type == 3:
                    if is_sfx:
                        continue
                    st['drum'] = x
                    continue
                if chn_type == 3:
                    st['period'] = x
                    ev = {'k': 'note', 'retrig': True}
                    if st['drum'] and not is_sfx:
                        ev['dmc'] = st['drum']
                else:
                    y = (st['oct'] << 4) | x
                    det = DETUNE[st['oct'] & 7] if op == 'detune' else 0
                    lo = PERIOD_LO[y] + det
                    st['period'] = ((PERIOD_HI[y] + (lo >> 8)) << 8) | (lo & 0xFF)
                    ev = {'k': 'note', 'retrig': True}
                st['x'] = x
                break
            elif op == 'legato':
                old_hi = st['period'] >> 8
                if chn_type == 3:
                    st['period'] = x
                else:
                    y = (st['oct'] << 4) | x
                    st['period'] = (PERIOD_HI[y] << 8) | PERIOD_LO[y]
                st['x'] = x
                # NOISE: hi stays 0 -> never retriggers, but an armed drum still fires (24:8651)
                ev = {'k': 'legato', 'retrig': (st['period'] >> 8) != old_hi}
                if chn_type == 3 and st['drum'] and not is_sfx:
                    ev['dmc'] = st['drum']
                break
            elif op == 'tie':
                ev = {'k': 'tie'}
                break
            elif op == 'rest':
                ev = {'k': 'rest'}
                break
            elif op == 'hold':
                ev = {'k': 'hold', 'holdVol': x}
                break
        if ev is None:
            ev = {'k': 'end', 'next': 0, 'error': 'no frame-ending op in 4096 ops'}
        length = st['len'] or 256
        ev.update(f=f, len=length if ev['k'] != 'end' else 0, oct=st['oct'], x=st['x'],
                  period=st['period'], vol=st['vol'], ctl=st['ctl'], env=st['env'],
                  vib=st['vib'], sweep=st['sweep'])
        if chn_type == 3:
            ev['drum'] = st['drum']
        if ev['k'] in ('note', 'legato'):
            ev['hz'] = hz(chn_type, st['period'], st['x'])
        events.append(ev)
        if ev['k'] == 'end':
            return events, None
        f += length
    events.append({'k': 'truncated', 'f': f})
    return events, None


CHN_TYPE = {'SQ1': 0, 'SQ2': 1, 'TRI': 2, 'NOISE': 3, 'SFX-SQ2': 1, 'SFX-NOISE': 3}


def export_channels(rd, ptrs, names, is_sfx, max_frames):
    out = []
    for name, p in zip(names, ptrs):
        evs, loop = simulate(rd, p, CHN_TYPE[name], is_sfx, max_frames)
        out.append({'chn': name, 'start': p, 'ops': static_ops(rd, p, is_sfx),
                    'events': evs, 'loop': loop,
                    'frames': evs[-1]['f'] if evs else 0})
    return out


def main():
    args = sys.argv[1:]
    out = 'web/data/sound.json'
    max_frames = 30000
    summary = '--summary' in args
    if '--out' in args:
        out = args[args.index('--out') + 1]
    if '--max-frames' in args:
        max_frames = int(args[args.index('--max-frames') + 1], 0)

    music = []
    for mid in range(0x20):
        p = rd16(b24, MUSIC_TABLE + mid * 2)
        if p == 0:
            continue
        ptrs = [rd16(b24, p + 1 + i * 2) for i in range(4)]
        music.append({'id': mid, 'hdr': p, 'b0': b24(p),
                      'channels': export_channels(b24, ptrs, CHN_MUSIC, False, max_frames)})
    sfx = []
    for sid in range(0x20, 0x78):
        p = rd16(b12, 0xA000 + (sid - 0x20) * 2)
        if p == 0:
            continue
        ptrs = [rd16(b12, p + 1 + i * 2) + 0x2000 for i in range(2)]
        sfx.append({'id': sid, 'hdr': p, 'prio': b12(p),
                    'channels': export_channels(b12, ptrs, CHN_SFX, True, max_frames)})

    dmc_len = DMC_RECS[1][3] * 16 + 1
    dmc_addr = 0xC000 + DMC_RECS[1][2] * 64
    data = {
        'source': 'tools/sound_export.py (session 26); driver bank 24, sfx bank 12',
        'cpuHz': CPU_HZ, 'frameHz': 60.0988,
        'tables': {
            'periodLo': PERIOD_LO, 'periodHi': PERIOD_HI, 'duration': DUR,
            'octave': OCTAVE_TAB, 'detune': DETUNE,
            'envTonal': rng(b24, 0x88B8, 256), 'envNoise': rng(b24, 0x89B8, 256),
            'volume': rng(b24, 0x8AD8, 256),
            'vibStep': rng(b24, 0x8714, 16),
            'vibRawFrom8714': rng(b24, 0x8714, 0x1A4),  # $8714-$88B7 for quirk reads
            'chnType': rng(b24, 0x87A6, 6), 'chnReg': rng(b24, 0x87AC, 6),
            'chnMask': rng(b24, 0x87B2, 6),
            'dmc': DMC_RECS, 'noisePeriods': NOISE_PERIODS,
        },
        'dmc': {'addr': dmc_addr, 'len': dmc_len,
                'bytes': ''.join('%02X' % b31(dmc_addr + i) for i in range(dmc_len))},
        'music': music, 'sfx': sfx,
    }
    os.makedirs(os.path.dirname(out) or '.', exist_ok=True)
    with open(out, 'w') as fh:
        json.dump(data, fh, separators=(',', ':'))
    print('wrote %s: %d music, %d sfx, %d bytes' % (out, len(music), len(sfx), os.path.getsize(out)))
    report = None
    if '--report' in args:
        report = open(args[args.index('--report') + 1], 'w')
        summary = True
    if summary:
        def emit(s):
            print(s)
            if report:
                report.write(s + '\n')
        for kind, lst in (('music', music), ('sfx', sfx)):
            for t in lst:
                parts = []
                for c in t['channels']:
                    ev = c['events']
                    last = ev[-1] if ev else {}
                    tail = ('loop->ev%d' % c['loop']) if c['loop'] is not None else \
                        ('end next=$%02X' % last.get('next', 0) if last.get('k') == 'end' else last.get('k'))
                    parts.append('%s %dev %df %s' % (c['chn'], len(ev), c['frames'], tail))
                emit('%s $%02X: %s' % (kind, t['id'], ' | '.join(parts)))
    if report:
        report.close()
    if '--show' in args:  # --show music|sfx <id>: print the flattened events
        i = args.index('--show')
        kind, sid = args[i + 1], int(args[i + 2], 0)
        for t in (music if kind == 'music' else sfx):
            if t['id'] != sid:
                continue
            for c in t['channels']:
                print('[%s] start $%04X loop=%s' % (c['chn'], c['start'], c['loop']))
                for e in c['events'][:40]:
                    print('   ', {k: v for k, v in e.items() if v not in (None,)})


if __name__ == '__main__':
    main()
