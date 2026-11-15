#!/usr/bin/env python3
"""Dump re-c dialogue text (session 11). Format documented in REVERSE.md "Text / dialogue".

Printer: TextPrint 20:853A. Group g = $07DF, message m = $07DE & $1F.
  bank   = TextGroupBank 20:83FE[g]          (21/22/23, mapped at $A000)
  ptrs   = TextGroupPtrs 20:8422[g]          (array of words in bank 20 $8000-$83FD)
  msg    = ptrs[m]                           (address $A000-$BFFF in `bank`)
Bytes: 00 end | 01 clear box | 02 newline | 03 wait-key + clear | 04 player name ($6400)
       05 n -> str 20:8A00[n] | 06 n -> str 20:8A14[n] | 07 n -> str 20:8A5C[n]
       08 -> str 20:8A5C[$07DC] | 09 n -> n spaces | 20-7E ascii | 80-FF dict word 20:8900[b&7F]
After a dict word or a 05/06/07 string the printer auto-inserts a space when the next byte is 04-09, or >= $40 and != $5F.

usage: text_dump.py [dict|tables|msgs|all] [--json out.json]
"""
import sys, os, json
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom  # noqa

PRG = load_rom()[1]
NGROUPS = 34


def rd(bank, addr):
    return PRG[bank * 0x2000 + (addr & 0x1FFF)]


def w(bank, addr):
    return rd(bank, addr) | rd(bank, addr + 1) << 8


def cstr(bank, addr):
    out = bytearray()
    while True:
        b = rd(bank, addr)
        if b == 0:
            return out.decode('latin-1')
        out.append(b); addr += 1


def ptr_table(addr, n):
    return [w(20, addr + 2 * i) for i in range(n)]


DICT = [cstr(20, p) for p in ptr_table(0x8900, 128)]
T05 = [cstr(20, p) for p in ptr_table(0x8A00, 10)]
T06 = [cstr(20, p) for p in ptr_table(0x8A14, 36)]
T07 = [cstr(20, p) for p in ptr_table(0x8A5C, 74)]


def decode(bank, addr, limit=2048):
    out = []
    end = addr + limit
    while addr < end:
        b = rd(bank, addr); addr += 1
        if b == 0:
            out.append('<END>'); break
        elif b == 1: out.append('<CLR>')
        elif b == 2: out.append('\n')
        elif b == 3: out.append('<KEY>\n')
        elif b == 4: out.append('{NAME}')
        elif b in (5, 6, 7):
            n = rd(bank, addr); addr += 1
            tab = {5: T05, 6: T06, 7: T07}[b]
            out.append('{' + (tab[n] if n < len(tab) else f'?{b:02X}:{n:02X}') + '}')
            nb = rd(bank, addr)  # 05/06/07 return via $8693 -> same auto-space rule as words
            if (4 <= nb <= 9) or (nb >= 0x40 and nb != 0x5F):
                out.append(' ')
        elif b == 8: out.append('{ITEM[$07DC]}')
        elif b == 9:
            n = rd(bank, addr); addr += 1
            out.append(' ' * n)
        elif b < 0x20: out.append(f'<{b:02X}>')
        elif b < 0x80: out.append(chr(b))
        else:
            out.append(DICT[b & 0x7F])
            nb = rd(bank, addr)
            if (4 <= nb <= 9) or (nb >= 0x40 and nb != 0x5F):
                out.append(' ')
    return ''.join(out), addr


def groups():
    banks = [rd(20, 0x83FE + g) for g in range(36)]
    gptr = ptr_table(0x8422, NGROUPS)
    res = []
    for g in range(NGROUPS):
        nxt = gptr[g + 1] if g + 1 < NGROUPS else 0x83FE
        n = min((nxt - gptr[g]) // 2, 32)
        res.append((g, banks[g], gptr[g], [w(20, gptr[g] + 2 * i) for i in range(n)]))
    return res


def main():
    args = sys.argv[1:]
    jpath = None
    if '--json' in args:
        i = args.index('--json'); jpath = args[i + 1]; del args[i:i + 2]
    if '--out' in args:  # redirect listing to a file
        i = args.index('--out'); sys.stdout = open(args[i + 1], 'w'); del args[i:i + 2]
    mode = args[0] if args else 'all'
    if mode in ('dict', 'all'):
        print('== dictionary 20:8900 (code: word)')
        for i, s in enumerate(DICT):
            print(f'  {0x80+i:02X}: {s!r}')
    if mode in ('tables', 'all'):
        for name, base, tab in (('05', 0x8A00, T05), ('06', 0x8A14, T06), ('07/08', 0x8A5C, T07)):
            print(f'== code {name} table 20:{base:04X}')
            for i, s in enumerate(tab):
                print(f'  {i:02X}: {s!r}')
    data = []
    if mode in ('msgs', 'all') or jpath:
        for g, bank, gp, ptrs in groups():
            if mode in ('msgs', 'all'):
                print(f'== group {g} (bank {bank}, ptrs 20:{gp:04X}, {len(ptrs)} msgs)')
            for m, p in enumerate(ptrs):
                txt, endp = decode(bank, p)
                data.append({'group': g, 'msg': m, 'bank': bank, 'addr': f'{p:04X}',
                             'end': f'{endp:04X}', 'text': txt})
                if mode in ('msgs', 'all'):
                    print(f'-- {g}.{m} {bank}:{p:04X}-{endp-1:04X}')
                    print('   ' + txt.replace('\n', '\n   '))
    if jpath:
        json.dump({'dict': DICT, 't05': T05, 't06': T06, 't07': T07, 'messages': data},
                  open(jpath, 'w'), indent=1)
        print(f'wrote {jpath}: {len(data)} messages')


if __name__ == '__main__':
    main()
