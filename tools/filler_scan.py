#!/usr/bin/env python3
"""Stale-copy filler scan.

For every pair of 8KB PRG banks, find runs of >= MIN bytes that are byte-identical
at the SAME in-bank offset.  Such runs are usually leftover build filler: the linker
left a stale copy of another bank's contents in the unused tail of a bank.

Runs made of very few distinct byte values (e.g. $FF/$00 padding) are reported
separately as 'pad' because matching padding says nothing about stale copies.

For each run the tool lists the labels.csv entries that fall inside it on either side;
the side WITH labels (or referenced by code) is normally the live copy.

Usage: python3 tools/filler_scan.py [--min N] [--distinct D] [--pad] [--out FILE]
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nesrom


def runs_for_pair(a, b, minlen):
    out, i, n = [], 0, len(a)
    while i < n:
        if a[i] != b[i]:
            i += 1
            continue
        j = i
        while j < n and a[j] == b[j]:
            j += 1
        if j - i >= minlen:
            out.append((i, j))
        i = j
    return out


# Session 21 verdicts: dead (stale-copy) ranges, inclusive CPU addresses.
# (bank, start, end, live source, evidence).  disasm_all.py should treat these as data.
DEAD = [
    (9, 0xBF80, 0xBFFF, '07:BF80', 'subst tables k>=12 never selected ($67 max $AC); = bank 7 last screen page'),
    (11, 0xBCFA, 0xBDFF, '09:BCFA', 'after last area sub-table record (area $5F ends 11:BCF9)'),
    # session 32: was BEE0; 11:BE00-BEFF is copied to WRAM $6000 (30:CCA4, 19:BE5E with bank 11 at $8000)
    (11, 0xBF00, 0xBFFF, '09:BF00', 'copy of bank 9 subst tables + bank 7 page'),
    (13, 0xABA3, 0xABFF, '05:ABA3', 'between area records (end ~A9D7) and ObjTypeTable AC00; 0 xrefs'),
    (13, 0xBE91, 0xBFEF, '05:BE91', 'after last ObjType record ($FE @BE7D); BFF0 = area 0 record'),
    (15, 0xA3C0, 0xA3FF, '13:A3C0', '13:A3C0 is live (ptr at 13:B93A); BossPhaseDispatch starts 15:A400'),
    (15, 0xBBAF, 0xBC3F, '13:BBAF', 'copy of ObjType records ($B8 @BBBE, $3F @BBFD)'),
    (15, 0xBF97, 0xBFFF, '13:BF97', 'copy of bank 5 page + 13 area 0 record'),
    (16, 0x9FB0, 0x9FFF, '24:9FB0', 'after FF terminator; = sound-stream bytes'),
    (19, 0xB895, 0xB8FF, '17:B895', 'bank 17 rect ptr table B8D4 -> B8E6..; in 19 they would point into PlayerDeath'),
    (20, 0x8466, 0x84FF, '22:A466', 'bank 22 text (msgs 15.18-15.23)'),
    (20, 0x88A5, 0x88FF, '22:A8A5', 'bank 22 text; frame templates 8899-88A1 stay live'),
    (20, 0x923D, 0x93FF, '22:B23D', 'bank 22 text'),
    (20, 0x9B4E, 0x9BFF, '22:BB4E', 'bank 22 text'),
    (20, 0x9E30, 0x9FFF, '22:BE30', 'bank 22 text + bank 18 tail'),
    (21, 0xBF32, 0xBFFF, '23:BF32', 'SaveAllowed table copy (21 text ends BF30)'),
    (22, 0xBF90, 0xBFFF, '18:9F90', 'bank 18 grid tail + stale code; past last bank 22 message'),
    (23, 0xBBD3, 0xBBFF, '19:BBD3', 'copy of SwordRaisePose-area code (19:BBE4 calls OamClipTop)'),
    (23, 0xBE2E, 0xBE7F, '-', '$FF pad + 8 stale bytes (= 31:FE2E)'),
    (27, 0xBE40, 0xBFFF, '21:BE40', 'bank 21 text + 23:BF30 SaveAllowed tail'),
    (30, 0xC8C0, 0xC8FF, '22:A8C0', 'bank 22 text between RTS C8BF and MainLoop C900'),
    (31, 0xF9BA, 0xF9FF, '23:B9BA', 'bank 23 text after RTS F9B9'),
    (31, 0xFDE0, 0xFDFF, '23:BDE0', 'load descriptors + SaveAllowed copy'),
    (31, 0xFE2E, 0xFE7F, '-', '$FF pad + 8 stale bytes before ReadPads FE80'),
]
# Same-offset matches judged LIVE on both sides (not filler).
LIVE_BOTH = [
    ('08:8835/09:A835 x4', 'tilesets $88 and $A8 share quadrant-page rows'),
    ('09:BCFA-BDFF', 'unreferenced by known readers (attr tables end B2FF); kept as data, unproven'),
    ('18:9FC2-9FFF', 'session 22: NOT dead - main state 2 (30:C931 SwitchBank16(9); JMP $9FCC) '
                     'enters at 18:9FCC and runs on into 19:A000; 9FC2-9FCB has no caller yet'),
]

# Banks the game maps somewhere other than nesrom.bank_window (text banks via SetR7).
WINDOW = {22: 0xA000}

from instruction_set import OPCODES
ABS_OPS = {op for op, (mn, mode) in OPCODES.items()
           if mode in ('abs', 'abx', 'aby', 'ind') and not mn.startswith('*')}


def window(bank):
    return WINDOW.get(bank, nesrom.bank_window(bank))


def labels_in(labels, bank, lo, hi):
    res = []
    for (b, a), (name, _c) in labels.items():
        if b == bank and a >= 0x8000 and lo <= (a & 0x1FFF) < hi:
            res.append('%04X %s' % (a, name))
    return sorted(res)


def text_cover():
    """{bank: set(in-bank offsets)} covered by live messages in gfx/text.json."""
    import json
    p = os.path.join(nesrom.ROOT, 'gfx', 'text.json')
    cov = {}
    if os.path.exists(p):
        for m in json.load(open(p))['messages']:
            a, e = int(m['addr'], 16), int(m['end'], 16)
            cov.setdefault(m['bank'], set()).update(x & 0x1FFF for x in range(a, e))
    return cov


def text_pct(cov, bank, lo, hi):
    s = cov.get(bank, ())
    return 100 * sum(1 for o in range(lo, hi) if o in s) // (hi - lo)


def ref_count(banks, bank, lo, hi):
    """Rough hint: abs-mode operands or raw LE words (outside the run itself) that point
    into [lo,hi) of `bank` at its window, from the bank's 16KB pair + fixed banks."""
    w = window(bank)
    tlo, thi = w + lo, w + hi
    pair = bank & ~1
    seen = 0
    for sb in sorted({pair, pair + 1, 30, 31}):
        if sb >= len(banks):
            continue
        d = banks[sb]
        for p in range(len(d) - 2):
            if sb == bank and lo - 2 <= p < hi:
                continue
            t = d[p + 1] | d[p + 2] << 8
            if tlo <= t < thi and d[p] in ABS_OPS:
                seen += 1
    return seen


def main():
    # argparse is unusable here: tools/dis.py shadows the stdlib 'dis' module it imports.
    class args:
        min, distinct, pad, out = 32, 4, False, None
    av = sys.argv[1:]
    while av:
        k = av.pop(0)
        if k == '--min':
            args.min = int(av.pop(0))
        elif k == '--distinct':
            args.distinct = int(av.pop(0))
        elif k == '--pad':
            args.pad = True
        elif k == '--out':
            args.out = av.pop(0)
        else:
            sys.exit(__doc__)

    _, prg, _ = nesrom.load_rom()
    labels = nesrom.load_labels()
    nb = nesrom.nbanks(prg)
    banks = [prg[k * 0x2000:(k + 1) * 0x2000] for k in range(nb)]

    cov = text_cover()
    real, pad = [], []
    for x in range(nb):
        for y in range(x + 1, nb):
            for lo, hi in runs_for_pair(banks[x], banks[y], args.min):
                seg = banks[x][lo:hi]
                rec = (x, y, lo, hi, len(set(seg)))
                (real if rec[4] >= args.distinct else pad).append(rec)

    lines = ['# filler_scan: same-offset identical runs >= %d bytes (distinct >= %d)'
             % (args.min, args.distinct),
             '# bankA:start-end  bankB:start-end  len  distinct  refsA/refsB  | labels A | labels B',
             '# refs = abs operands into the run from the bank pair + fixed banks (rough hint)']
    for x, y, lo, hi, d in sorted(real, key=lambda r: -(r[3] - r[2])):
        wa, wb = window(x), window(y)
        la = labels_in(labels, x, lo, hi)
        lb = labels_in(labels, y, lo, hi)
        ta, tb = text_pct(cov, x, lo, hi), text_pct(cov, y, lo, hi)
        lines.append('%02d:%04X-%04X  %02d:%04X-%04X  %5d  %3d  %3d/%-3d %s| A: %s | B: %s' % (
            x, wa + lo, wa + hi - 1, y, wb + lo, wb + hi - 1, hi - lo, d,
            ref_count(banks, x, lo, hi), ref_count(banks, y, lo, hi),
            'text%%%d/%d ' % (ta, tb) if ta or tb else '',
            ', '.join(la[:4]) + (' ...' if len(la) > 4 else '') or '-',
            ', '.join(lb[:4]) + (' ...' if len(lb) > 4 else '') or '-'))
    lines.append('# %d content runs, %d pad runs (%d pad bytes total)' % (
        len(real), len(pad), sum(h - l for _, _, l, h, _ in pad)))
    lines.append('# verdicts (session 21): dead stale-copy ranges')
    for b, s, e, src, why in DEAD:
        lines.append('dead %02d:%04X-%04X %5d  src %s  %s' % (b, s, e, e - s + 1, src, why))
    lines.append('# %d dead ranges, %d bytes' % (len(DEAD), sum(e - s + 1 for _, s, e, _, _ in DEAD)))
    for r, why in LIVE_BOTH:
        lines.append('live %s  %s' % (r, why))
    if args.pad:
        for x, y, lo, hi, d in sorted(pad):
            lines.append('pad %02d/%02d +%04X-%04X %d bytes, byte %s' % (
                x, y, lo, hi - 1, hi - lo, '%02X' % banks[x][lo] if d == 1 else '(%d vals)' % d))
    text = '\n'.join(lines) + '\n'
    if args.out:
        open(args.out, 'w').write(text)
    print(text, end='')


if __name__ == '__main__':
    main()
