#!/usr/bin/env python3
"""Cross-reference the 2048 story flags ($6480-$657F) by their data-driven sites (session 14).

Sources (all bank 14/15 flag-ref format, see REVERSE.md "NPC script format"):
  npc   NPC scripts 14:8000 (section A/B conds + set-lists), params with a valid pointer;
        entries whose message group >= 34 are skipped (bogus scripts)
  hook2 ItemHook2 14:80E3, table 14:A17A[(id&7F)] : conds... (until bit7) result(2) set-list
  tele  Telepathy hint lists 14:99F4 (shown while the cond is FALSE)
Session 18 additions:
  get   ItemGetHook 14:826F set-lists (item_dump.get_record); record $70 is unreachable
        (GiveItem sends id $70 to TrapChest 30:D3DA) and is tagged [unreachable]
  use   ItemUseHook 14:831E records: flag preconditions + set-lists (item_dump.USE_KIND)
  pres  NpcHidden 14:80A0 presence conds (presence_dump.plist), only for spawn params
        that some area places with handler 1 (indirect NPC records)
  code  direct $6480+ code sites already decoded in gfx/flag_code_sites.txt
        (lines carrying a 'test/set/clr Fxxx' column; regenerate with flag_code_sites.py)
Hook-2 triggers that no area places (area_dump.param_index, handler 2) are dropped from the
main list; flags referenced ONLY by such triggers are listed under "unplaced-only".
usage: flag_sites.py [--hook2] [--out f]
"""
import sys, os, re
sys.path.insert(0, os.path.dirname(__file__))
import npc_script_dump as N  # noqa
import item_dump as I  # noqa
import presence_dump as P  # noqa
import area_dump as A  # noqa

CODE_SITES = os.path.join(os.path.dirname(__file__), '..', 'gfx', 'flag_code_sites.txt')

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
    pidx = A.param_index()
    placed2 = {p for (h, p) in pidx if h == 2}
    placed1 = {p for (h, p) in pidx if h == 1}
    unplaced = {}  # flag -> [where] for hook-2 triggers that are never placed
    h2 = hook2_entries()
    for e in h2:
        where = 'hook2 item %02X msg %d.%d act %d' % (e['id'], e['res']['group'], e['res']['idx'], e['res']['action'])
        dst = add if e['id'] in placed2 else (lambda f, k, t: unplaced.setdefault(f, []).append((k, t)))
        for b0, b1 in e['conds']:
            if fnum(b0, b1):
                dst(fnum(b0, b1), 'test', where + (' (!)' if b0 & 0x20 else ''))
        for st in e['sets']:
            op, fl = st.split()
            dst(int(fl[1:], 16), op, where)
    # item pickup set-lists
    for i in range(I.N_ITEMS):
        r = I.get_record(i)
        where = 'get item %02X %s' % (i, I.name(i)) + (' [unreachable]' if i == 0x70 else '')
        for st in r['sets']:
            op, fl = st.split()
            add(int(fl[1:], 16), op, where)
    # item use: flag preconditions + set-lists
    for i in range(I.N_USE):
        h = N.w(I.USE_HND + i * 2)
        if h == 0x84DB:
            continue
        a = N.w(I.USE_TAB + i * 2)
        for pre, y in I.USE_KIND.get(h, ('?', []))[1]:
            ya = a + y
            where = 'use item %02X %s' % (i, I.name(i))
            if pre in ('touch', 'slot'):
                where += ' on param %02X' % N.rd(ya); ya += 2
            elif pre == 'area':
                where += ' in area %02X' % N.rd(ya); ya += 1
            elif pre == 'flag':
                b0, b1 = N.rd(ya), N.rd(ya + 1); ya += 2
                add(fnum(b0, b1), 'test', where + (' (!)' if b0 & 0x20 else ''))
            elif pre == 'skip':
                ya += 1
            sets, _ = N.setlist(ya + 2)
            for st in sets:
                op, fl = st.split()
                add(int(fl[1:], 16), op, where)
    # NpcHidden presence conds (placed handler-1 params only)
    for p in sorted(placed1):
        _, recs = P.plist(p)
        for area, conds in recs:
            for c in conds:
                f = int(c.lstrip('!')[1:4], 16)
                if f:
                    add(f, 'pres', 'param %02X in area %02X needs %s' % (p, area, c.split('(')[0]))
    # direct code sites (decoded by flag_code_sites.py)
    if os.path.exists(CODE_SITES):
        for ln in open(CODE_SITES):
            m = re.match(r'(\d\d:[0-9A-F]{4})\s+(.*?)\s*\|\s*(test|set|clr)\s+((?:F[0-9A-F]{3}\s*)+)\|', ln)
            if m:
                for fl in m.group(4).split():
                    add(int(fl[1:], 16), m.group(3), 'code %s %s' % (m.group(1), m.group(2)))
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
    only = sorted(set(unplaced) - set(sites))
    L.append('')
    L.append('unplaced-only: %d flags referenced only by hook-2 triggers that no area places '
             '(placed: %s)' % (len(only), ' '.join('%02X' % p for p in sorted(placed2) if p >= 0x80)))
    for f in only:
        L.append('F%03X ($%04X&%02X)' % (f, 0x6480 + (f >> 3), 1 << (f & 7)))
        for kind, txt in sorted(set(unplaced[f])):
            L.append('    %-4s %s' % (kind, txt))
    txt = '\n'.join(L)
    if '--out' in sys.argv:
        with open(sys.argv[sys.argv.index('--out') + 1], 'w') as fh:
            fh.write(txt + '\n')
    print(txt)


if __name__ == '__main__':
    main()
