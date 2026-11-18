#!/usr/bin/env python3
"""One report per spawn param: where it is placed + what it does (session 70).

Joins, for every (spawn handler, param) that some area places (area_dump.param_index):
  h1 type $30 NPC  -> NpcHidden presence conds (presence_dump), NPC script 14:8000
                      (pointer, section A/B entry counts, default B reply, script area->state
                      list), HitSetFlag status, NpcTalk special-talker list
  h1 other type    -> boss/object record type + presence conds + whether the script is junk
  h2 p < $80       -> chest item (GiveItem 30:D3EB: $70 = TrapChest, $50-$7F remapped via
                      ItemRemapTable 30:D40C[id]) + ItemGetHook set-list
  h2 p >= $80      -> EventTriggerTable 15:A17A[p-$80]: conds, result, set-list
  h0/h3/h4         -> type only (listed when the param also appears in a 26:90xx list)
Then the HitSetFlag lists (26:9045 HitFlagParams -> flag 26:904F, 26:9058 immune list)
with the handlers/areas that place each param, and the hook-2 triggers no area places.

Note: npc_scripts.txt "areas" = the script's own area->state list (section between A and
B), NOT where the param is placed; e.g. boss p $C3 (type $70, area $43) has an empty list.

usage: param_report.py [--out f]
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import area_dump as A  # noqa: E402
import npc_script_dump as N  # noqa: E402
import npc_script_check as C  # noqa: E402
import presence_dump as P  # noqa: E402
import item_dump as I  # noqa: E402
import flag_sites as F  # noqa: E402
from nesrom import load_rom  # noqa: E402

_, PRG, _ = load_rom()


def prg(bank, addr):
    return PRG[bank * 0x2000 + (addr & 0x1FFF)]


HITFLAG_P, HITFLAG_ID, IMMUNE = 0x9045, 0x904F, 0x9058
TALK_SPECIAL = (0x5E, 0x5F, 0x62, 0x6C, 0x6D)   # NpcTalk 30:D0C8: talk although $05A0 != 0


def hitflag_lists():
    hp, i = {}, 0
    while prg(26, HITFLAG_P + i):
        hp[prg(26, HITFLAG_P + i)] = prg(26, HITFLAG_ID + i)
        i += 1
    imm = [prg(26, IMMUNE + j) for j in range(20)]
    return hp, imm


def places(lst):
    return ' '.join(f'{a:02X}.{s:02X}' for a, s, _, _, _ in lst)


def pres(p):
    _, recs = P.plist(p)
    return ' | '.join(f'{ar:02X}: ' + ' & '.join(c.split('(')[0] for c in cs) for ar, cs in recs)


def npc_summary(p):
    if not N.valid(p):
        return ['script: none (no valid pointer)']
    s = N.script(p)
    junk = s['ptr'] == 0x8000 or bool(C.check(p)[1])
    out = [f"script 14:{s['ptr']:04X}-{s['end']:04X} A={len(s['A'])} B={len(s['B'])}"
           + (' [junk/misaligned decode]' if junk else '')
           + (f" state-areas {' '.join(f'{a:02X}:{st:02X}' for a, st in s['areas'])}" if s['areas'] else '')]
    if not junk and s['B']:
        e = s['B'][0]
        out.append(f"first B reply msg {e['group']}.{e['idx']}: \"{N.msg_text(e['msg'], 56)}\"")
    return out


def item_of_chest(p):
    if p == 0x70:
        return 'TrapChest 30:D3DA (no item)'
    it = prg(30, 0xD40C + p) if p >= 0x50 else p
    s = f'item ${it:02X} {I.name(it)}' + (f' (remap of ${p:02X})' if it != p else '')
    if it < I.N_ITEMS:
        sets = I.get_record(it)['sets']
        if sets:
            s += ' get-sets [' + ', '.join(sets) + ']'
    return s


def main(argv):
    idx = A.param_index()
    hp, imm = hitflag_lists()
    hook2 = {e['id']: e for e in F.hook2_entries()}
    L = ['; param_report.py (session 70): spawn params joined with scripts / presence / triggers',
         '; place = area.slot (area_dump.py params); pres = NpcHidden conds per area (absent if any false)', '']

    def tags(p, h):
        t = []
        if p in hp:
            t.append(f'HitFlag->F{hp[p]:03X}')
        if p in imm and h == 1:
            t.append('hit-immune')
        if p in TALK_SPECIAL and h == 1:
            t.append('special talker')
        return (' {' + ', '.join(t) + '}') if t else ''

    L.append('== h1: indirect records (bank 4/5), NPCs (type $30) and bosses/objects')
    for (h, p), lst in sorted(idx.items()):
        if h != 1:
            continue
        r = A.indirect_rec(p)[1]
        typ = '$30 NPC' if r[0] != 0xFF else f'${r[1]:02X}'
        L.append(f'p=${p:02X} type {typ}{tags(p, 1)}  place {places(lst)}')
        pr = pres(p)
        if pr:
            L.append(f'    pres {pr}')
        for ln in npc_summary(p):
            L.append('    ' + ln)
    L.append('')
    L.append('== h2 p<$80: chests (GiveItem)')
    for (h, p), lst in sorted(idx.items()):
        if h == 2 and p < 0x80:
            L.append(f'p=${p:02X} {item_of_chest(p)}{tags(p, 2)}  place {places(lst)}')
    L.append('')
    L.append('== h2 p>=$80: event triggers (EventTriggerTable 15:A17A, ItemHook2 14:80E3)')
    for (h, p), lst in sorted(idx.items()):
        if h == 2 and p >= 0x80:
            e = hook2.get(p)
            L.append(f'p=${p:02X} place {places(lst)}')
            if e:
                conds = ' & '.join(N.fcond(*c).split('(')[0] for c in e['conds'])
                L.append(f"    if {conds} -> act {e['res']['action']} msg {e['res']['group']}.{e['res']['idx']}"
                         f" [{', '.join(e['sets'])}] \"{N.msg_text(e['res']['msg'], 50) if e['res']['msg'] else ''}\"")
            else:
                L.append('    (outside EventTriggerTable)')
    placed2 = {p for (h, p) in idx if h == 2}
    un = [p for p in sorted(hook2) if p not in placed2]
    L.append(f'unplaced triggers ({len(un)}): ' + ' '.join(f'{p:02X}' for p in un))
    L.append('')
    L.append('== HitSetFlag 26:9011: HitFlagParams 26:9045 -> flag 26:904F (set when the object is hit/killed)')
    L.append('; HitHandler3 26:8FC4 returns before both lists unless obj_spawn_b2 = 1 (handler 1) and')
    L.append('; obj_hitflags b5 clear; an immune-list match (checked first) returns without setting the flag')
    for p, f in hp.items():
        where = [f'h{h} {places(lst)}' + ('' if h == 1 else ' (b2!=1: no flag)')
                 for (h, q), lst in sorted(idx.items()) if q == p]
        live = (1, p) in idx and p not in imm
        L.append(f'p=${p:02X} -> F{f:03X} {"LIVE" if live else "DEAD (immune)" if p in imm else "DEAD (no h1)"}'
                 f'  placed: ' + ('; '.join(where) or '-'))
    L.append('immune list 26:9058 (only with obj_spawn_b2 = 1, i.e. handler 1): '
             + ' '.join(f'{p:02X}' + ('' if (1, p) in idx else '(unplaced h1)') for p in imm))
    L.append('')
    L.append('== other handlers (h0 type p+$50, h3 type p+$D0, h4 type p): counts only')
    for h in (0, 3, 4):
        ps = sorted(p for (hh, p) in idx if hh == h)
        L.append(f'h{h}: {len(ps)} params')
    txt = '\n'.join(L)
    if '--out' in argv:
        with open(argv[argv.index('--out') + 1], 'w') as fh:
            fh.write(txt + '\n')
    print(txt)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
