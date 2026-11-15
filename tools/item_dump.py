#!/usr/bin/env python3
"""Dump the bank 14 item hook tables (session 16).

Hook 0 = ItemGetHook 14:826F (called by GiveItem 30:D401), table 14:9B00[item] -> record:
    start  inventory index into $6430 (X)
    count  bit7 clear: number of slots; first $FF slot from $6430+start gets the item
           bit7 set:   fixed equip slot $6430+start (overwritten)
    r0 r1  NpcResult (action/message), only if the item was stored
    set-list (NpcFlagApply, ends at bit 6)
    next   item id to give next (chain) or bit7 = end
  stored value = 14:9D66[item].
Hook 1 = ItemUseHook 14:831E (30:D51C), record ptr 14:9BE2[item], handler 14:8399[item]
  (JMP ($26), Y=0 on entry). Records are listed raw (format depends on the handler).
usage: item_dump.py [--out f]
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
import npc_script_dump as N  # noqa
import text_dump  # noqa

GET_TAB, GET_VAL, USE_TAB, USE_HND = 0x9B00, 0x9D66, 0x9BE2, 0x8399
N_ITEMS = (USE_TAB - GET_TAB) // 2
N_USE = 0x50  # 14:8399 + $50*2 = 14:8439 = first handler
# handler -> (description, [(precondition kind, record offset)]); each alternative is
# followed by NpcResult(2) + set-list (applied by ItemUseHook after the handler returns)
T = [('touch', 0)]
USE_KIND = {
    0x8491: ('use while touching a spawn', T),
    0x8439: ('use while touching a spawn, not in area $62', T),
    0x84D0: ('use while touching a spawn, then $62F2=$FF', T),
    0x8565: ('use while touching a spawn', T),
    0x84A9: ('use while touching spawn A or B', [('touch', 0), ('touch', 6)]),
    0x859E: ('use while touching one of 4 spawns', [('touch', 0), ('touch', 6), ('touch', 12), ('touch', 18)]),
    0x84E0: ('HP +32 (clamped to max), or the touch result', [('touch', 0), ('-', 6)]),
    0x845F: ('use in one area', [('area', 0)]),
    0x8451: ('object present in slots 0D-1F and $07D7 in 1..7F', [('slot', 0)]),
    0x8442: ('as 8451, plus $060D<>0 and area $A6', [('slot', 0)]),
    0x84B3: ('area $1A, slot 13 active+$04AD<>0, player x>=$0A -> slot 13 type $E2', [('skip', 0)]),
    0x856B: ('area $60/$64/$68 and flag', [('flag', 0)]),
    0x8585: ('turns a Broken Statue ($38) in the inventory into Statue of Gold ($3A), SFX $68', [('-', 0)]),
    0x8507: ('MP +32 (clamped)', [('-', 0)]),
    0x851D: ('MP = max', [('-', 0)]),
    0x8524: ('cure status 3 (obj1_flash&0F)', [('-', 0)]),
    0x852F: ('cure status 1', [('-', 0)]),
    0x853A: ('cure status 2, $05A0=0', [('-', 0)]),
    0x854A: ('cure status 4', [('-', 0)]),
    0x8450: ('always (RTS)', [('-', 0)]),
    0x8564: ('always (RTS)', [('-', 0)]),
}


def name(i):
    return text_dump.T07[i] if i < len(text_dump.T07) else '?'


def get_record(i):
    a = N.w(GET_TAB + i * 2)
    start, cnt = N.rd(a), N.rd(a + 1)
    res = N.result(a + 2)
    sets, b = N.setlist(a + 4)
    nxt = N.rd(b)
    return {'item': i, 'addr': a, 'start': start, 'count': cnt, 'val': N.rd(GET_VAL + i),
            'res': res, 'sets': sets, 'next': nxt}


def main():
    L = ['ItemGetHook 14:826F records 14:9B00 (%d items)' % N_ITEMS]
    for i in range(N_ITEMS):
        r = get_record(i)
        slot = ('equip $%04X' % (0x6430 + r['start'])) if r['count'] & 0x80 else \
            ('inv $%04X+%d' % (0x6430 + r['start'], r['count']))
        msg = ''
        if r['res']['msg']:
            msg = ' msg %d.%d "%s"' % (r['res']['group'], r['res']['idx'], N.msg_text(r['res']['msg'], 40))
        L.append('  %02X %-16s @%04X %-14s val %02X act %2d%s [%s]%s' % (
            i, name(i), r['addr'], slot, r['val'], r['res']['action'], msg, ', '.join(r['sets']),
            '' if r['next'] & 0x80 else ' -> next %02X %s' % (r['next'], name(r['next']))))
    L.append('')
    L.append('ItemUseHook 14:831E: handler 14:8399[item], record 14:9BE2[item] (items 00-4F;'
             ' the tables end at $50, higher ids read code)')
    for i in range(N_USE):
        h = N.w(USE_HND + i * 2)
        if h == 0x84DB:
            continue  # default: $23=$FF (cannot use)
        a = N.w(USE_TAB + i * 2)
        what, alts = USE_KIND.get(h, ('?', []))
        L.append('  %02X %-16s handler %04X rec %04X: %s' % (i, name(i), h, a, what))
        for pre, y in alts:
            ya = a + y
            cond = ''
            if pre == 'touch':
                cond = 'touching spawn param %02X b2 %02X' % (N.rd(ya), N.rd(ya + 1)); ya += 2
            elif pre == 'slot':
                cond = 'object in slots 0D-1F with param %02X b2 %02X' % (N.rd(ya), N.rd(ya + 1)); ya += 2
            elif pre == 'area':
                cond = 'area %02X' % N.rd(ya); ya += 1
            elif pre == 'flag':
                cond = 'if %s' % N.fcond(N.rd(ya), N.rd(ya + 1)).split('(')[0]; ya += 2
            elif pre == 'skip':
                ya += 1
            res = N.result(ya)
            sets, _ = N.setlist(ya + 2)
            msg = ' msg %d.%d "%s"' % (res['group'], res['idx'], N.msg_text(res['msg'], 40)) if res['msg'] else ''
            L.append('       %-44s act %2d%s [%s]' % (cond or '-', res['action'], msg, ', '.join(sets)))
    txt = '\n'.join(L)
    if '--out' in sys.argv:
        with open(sys.argv[sys.argv.index('--out') + 1], 'w') as fh:
            fh.write(txt + '\n')
    print(txt)


if __name__ == '__main__':
    main()
