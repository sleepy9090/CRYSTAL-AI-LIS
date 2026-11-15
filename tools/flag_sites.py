#!/usr/bin/env python3
"""Cross-reference the 2048 story flags ($6480-$657F) by their data-driven sites (session 14).

Sources (all bank 14/15 flag-ref format, see REVERSE.md "NPC script format"):
  npc   NPC scripts 14:8000 (section A/B conds + set-lists), params with a valid pointer;
        entries whose message group >= 34 are skipped (bogus scripts)
  hook2 ItemHook2 14:80E3, table 14:A17A[(id&7F)] : conds... (until bit7) result(2) set-list
  tele  Telepathy hint lists 14:99F4 (shown while the cond is FALSE)
Code sites that touch $6480+ directly are not included (use xref.py).
usage: flag_sites.py [--hook2] [--out f]
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
import npc_script_dump as N  # noqa

SAGES = ['Tornel', 'Zebu', 'Asina', 'Kensu']
HOOK2 = 0xA17A


def fnum(b0, b1):
    return ((b0 & 7) << 8) | b1


def hook2_entries():
    n = (N.w(HOOK2) - HOOK2) // 2
    out = []
    for i in range(n):
        a = N.w(HOOK2 + i * 2)
        conds = []
        for _ in range(16):
            b0, b1 = N.rd(a), N.rd(a + 1); a += 2
            conds.append((b0, b1))
            if b0 & 0x80:
                break
        res = N.result(a); a += 2
        sets, _ = N.setlist(a)
        out.append({'id': 0x80 + i, 'conds': conds, 'res': res, 'sets': sets})
    return out


def main():
    sites = {}

    def add(f, kind, txt):
        sites.setdefault(f, []).append((kind, txt))

    for p in range(256):
        if not N.valid(p):
            continue
        s = N.script(p)
        for sec in ('A', 'B'):
            for e in s[sec]:
                if e['group'] >= 34:
                    continue
                c = e['cond']
                f = int(c.lstrip('!')[1:4], 16)
                where = 'npc %02X %s+%02X msg %d.%d' % (p, sec, e['off'], e['group'], e['idx'])
                if f:
                    add(f, 'test', '%s (%s)' % (where, c.split('(')[0]))
                for st in e.get('sets', []):
                    op, fl = st.split()
                    add(int(fl[1:], 16), op, where)
    h2 = hook2_entries()
    for e in h2:
        where = 'hook2 item %02X msg %d.%d act %d' % (e['id'], e['res']['group'], e['res']['idx'], e['res']['action'])
        for b0, b1 in e['conds']:
            if fnum(b0, b1):
                add(fnum(b0, b1), 'test', where + (' (!)' if b0 & 0x20 else ''))
        for st in e['sets']:
            op, fl = st.split()
            add(int(fl[1:], 16), op, where)
    for c in range(7):
        for s in range(4):
            a = N.w(0x99F4 + (c * 4 + s) * 2)
            for _ in range(32):
                b0, b1 = N.rd(a), N.rd(a + 1)
                mid = N.rd(a + 2) << 8 | N.rd(a + 3)
                add(fnum(b0, b1), 'hint', 'tele class %d %s until set: %d.%d "%s"' % (
                    c, SAGES[s], mid >> 5, mid & 31, N.msg_text(mid, 50)))
                a += 6 if b0 & 0x40 else 4
                if b0 & 0x80:
                    break
    L = []
    if '--hook2' in sys.argv:
        L.append('ItemHook2 table 14:%04X (%d entries)' % (HOOK2, len(h2)))
        for e in h2:
            conds = ' & '.join(N.fcond(*c).split('(')[0] for c in e['conds'])
            L.append('  item %02X if %s -> act %d msg %d.%d [%s] "%s"' % (
                e['id'], conds, e['res']['action'], e['res']['group'], e['res']['idx'],
                ', '.join(e['sets']),
                N.msg_text(e['res']['msg'], 50) if e['res']['msg'] else '(no message)'))
        L.append('')
    L.append('Story flag sites (%d flags referenced)' % len(sites))
    for f in sorted(sites):
        L.append('F%03X ($%04X&%02X)' % (f, 0x6480 + (f >> 3), 1 << (f & 7)))
        for kind, txt in sorted(set(sites[f])):
            L.append('    %-4s %s' % (kind, txt))
    txt = '\n'.join(L)
    if '--out' in sys.argv:
        with open(sys.argv[sys.argv.index('--out') + 1], 'w') as fh:
            fh.write(txt + '\n')
    print(txt)


if __name__ == '__main__':
    main()
