#!/usr/bin/env python3
"""Scripted save-state scenarios with coverage (session 85, Ph6 "scripted scenarios").

Each scenario = start state + optional area poke (poke_probe-style, waits for sub_state 8) +
RAM pokes + a key script, then N frames with an instruction tracer. The report lists, per
scenario, which code labels it ran that the canonical tour (`docs/emu_tour.txt` "never run")
did not, plus the union at the end.

usage (repo root):
  python3 -m tools.emu.scenario [NAME ...] [--list] [--out docs/emu_scenarios.txt]
  python3 -m tools.emu.scenario --custom --state F --area 03.00 --poke 0711=4 --keys 10:START 60:START
                                --frames 200 [--watch 0041]
Key script: OFFSET:BUTTONS[:LEN] (offset from the scenario start, LEN default 2), pad 1;
'P2:' prefix on BUTTONS = pad 2. Pokes: HHHH=VV (RAM or WRAM).
"""
import argparse
import os
import re
import sys

from tools.emu.nes import NES
from tools.emu import state
from tools.emu.cpu import IllegalOpcode
from tools.emu.pads import parse_buttons

STATES = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'states')
TOUR = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'docs', 'emu_tour.txt')
BTN_A = 0x01

def magic(sel, extra=None, keys=None, frames=240, area='03', note=''):
    """Area 03 by default: in area 01 the A press talks to the NPC next to the start."""
    p = {0x0708: 0x60, 0x0709: 0x60, 0x0712: sel}
    p.update(extra or {})
    return dict(note=f'magic {sel} (A, PlayerMagic 27:A022) {note}', pokes=p, area=area,
                keys=keys or ['20:A:30', '120:A', '160:A', '200:B'], frames=frames)


def follow_enemy(slot, dx):
    """Each frame: park enemy `slot` dx px right of the player (slot 1) with 1 HP."""
    def f(nes, o):
        r = nes.bus.ram
        if r[0x04A0 + slot] and r[0x41] == 8:
            x = (r[0x71] | r[0x91] << 8) + dx
            r[0x70 + slot], r[0x90 + slot] = x & 0xFF, x >> 8 & 0xFF
            r[0xB0 + slot], r[0xD0 + slot] = r[0xB1], r[0xD1]
            r[0x03C0 + slot] = min(r[0x03C0 + slot], 1)
    return f


def force(bank, pc, ad, val, once=True):
    """exec hook: when bank:pc runs, write val to ad (first time only unless once=False)."""
    done = [False]

    def f(nes):
        if not (once and done[0]):
            done[0] = True
            write_mem(nes, ad, val)
    return (bank, pc, f)


TALK_PC = 0xD10D     # 30:D10D LDA $22 (NpcTalk, after NpcScript, before the TalkActionTable jump)
EVENT_PC = 0xD552     # 30:D552 EventActionDispatch LDA $22


def talk_act(act, frames=400, extra=None):
    """Area 01 start: A talks to the NPC next to the player; its action $22 is forced to `act`."""
    return dict(note=f'area 01: A on the start NPC, TalkActionTable act {act} forced at 30:D10D',
                exec=[force(30, TALK_PC, 0x22, act)], pokes=extra or {},
                keys=['20:A', '120:A', '160:A', '200:B', '260:A', '320:B'], frames=frames,
                watch=[0x0702, 0x03C1, 0x0708])


def event_act(act, frames=400, area='03', extra=None):
    """use_fail path (item $1D used in area 03 -> ItemUseHook -> EventActionDispatch), $22 forced."""
    p = {0x0715: 0x01, 0x6440: 0x1D, 0x642C: 0}
    p.update(extra or {})
    return dict(note=f'area {area}: use item $1D (ItemUseHook) -> EventActionTable act {act} forced at 30:D552',
                area=area, pokes=p, exec=[force(30, EVENT_PC, 0x22, act)],
                keys=['20:B', '120:A', '160:A', '200:B', '260:A'], frames=frames, watch=[0x6C, 0x41])


# name -> dict(state, area, pokes, keys, frames, note, each, exec)
# exec: [(bank, pc, fn(nes))] exec hooks, e.g. force(...)
# keys: 'OFF:BUTTONS[:LEN]' (pad 1), 'OFF:P2:BUTTONS[:LEN]' (pad 2), 'OFF:s:ADDR=VAL' (timed poke)
SCENARIOS = {
    'pause': dict(note='START in play -> PauseScreen 15:BC40 (sub_state 9, NmiMode2), START again',
                  keys=['20:START', '140:START'], frames=200),
    'items': dict(note='SELECT -> SubstEquipMenu 30:D8C7, UP = Item/Magic grid, SELECT out',
                  keys=['20:SELECT', '70:UP', '100:A', '140:SELECT', '200:SELECT'], frames=300),
    'save_refused': dict(note='area 01: SELECT, START = SaveLoadPage, A = MenuSave "Cannot save!", B',
                         keys=['20:SELECT', '70:START', '120:A', '230:B', '280:SELECT', '330:SELECT'],
                         frames=420),
    'save_ok': dict(note='area 02 (AreaSaveFlags $81): SELECT, START, A, A = YES -> MenuSave + SaveSlotCopy',
                    area='02', keys=['20:SELECT', '70:START', '120:A', '170:LEFT', '200:A', '330:B',
                                     '380:SELECT', '430:SELECT'], frames=520, watch=[0x7000, 0x70F0, 0x70F5]),
    'load_ok': dict(note='area 02: save (YES; the menu closes), reopen, RIGHT (LOAD), A, LEFT, A = YES '
                         '-> LoadGame 23:BC8E', area='02',
                    keys=['20:SELECT', '70:START', '120:A', '170:LEFT', '200:A', '330:SELECT', '380:START',
                          '430:RIGHT', '470:A', '520:LEFT', '550:A'], frames=900, watch=[0x7000, 0x6D]),
    'death_new': dict(note='HP 0 -> sub_state 3 PlayerDeath -> title; START every 100 f (new game chain)',
                      keys=['10:s:03C1=00'] + [f'{600 + 100 * i}:START' for i in range(20)], frames=2600,
                      watch=[0x82]),
    'death_cont': dict(note='as death_new, but SELECT ($82=1 continue) before each START',
                       keys=['10:s:03C1=00'] + [f'{600 + 100 * i}:{"SELECT" if i % 2 else "START"}'
                                                 for i in range(20)], frames=2600, watch=[0x82]),
    'boot': dict(note='power-on (WRAM 0) + the canonical keys to f2500 (the tour starts from a state)',
                 state='', keys=[f'{k}:2' if k.count(':') == 1 else k
                                 for k in '1250:START 1400:START 1600:START 1700:START 2300:START'.split()]
                 + ['2340:DOWN:80'], frames=2500),
    'attract': dict(note='power-on, no input for 4000 frames (opening pages + attract loop)',
                    state='', keys=[], frames=4000),
    'warm_reset': dict(note='area 01, then CPU reset at +20 (WRAM kept -> warm boot / save checks)',
                       keys=['20:s:FFFF=00'], frames=1500),
    'use_herb': dict(note='area 03: use_item_sel $0B (item $27) in slot $6440 + B -> ItemUseMenu 30:D497',
                     area='03', pokes={0x0715: 0x0B, 0x6440: 0x27, 0x642C: 0, 0x03C1: 4},
                     keys=['20:B', '120:B'], frames=240),
    'use_fail': dict(note='area 03: use_item_sel $01 (item $1D) + B -> "Nothing happens." (ItemUseFail)',
                     area='03', pokes={0x0715: 0x01, 0x6440: 0x1D, 0x642C: 0}, keys=['20:B'], frames=200),
    'use_warp': dict(note='area 03: Warp Boots ($0715 $08) + B -> SubstTeleportMenu, DOWN, A',
                     area='03', pokes={0x0715: 0x08, 0x6450: 0x24, 0x642E: 0, 0x642C: 0xFF},
                     keys=['20:B', '100:DOWN', '140:A'], frames=500, watch=[0x6C]),
    'opel': dict(note='area 03: Opel Statue selected ($0715 $0A), HP 0 -> PlayerDeath -> OpelRevive 19:B9B0',
                 area='03', pokes={0x0715: 0x0A, 0x6440: 0x26, 0x642C: 0}, keys=['10:s:03C1=00'], frames=300),
    'debug_pause': dict(note='pad 2 in play (substate 4 = SubstDebugPause, 30:CBC7 area cycler)',
                        keys=['20:P2:A', '60:P2:A', '100:P2:B', '140:P2:START', '180:P2:SELECT',
                              '220:P2:A+B', '260:P2:START'], frames=300),
    'kill': dict(note='area 03: enemy slot $0E parked at the sword with 1 HP; level 16, Crystalis off; '
                      'B/A swings', area='03', pokes={0x0421: 16, 0x0711: 1},
                 each=follow_enemy(0x0E, 14), keys=[f'{10 + 20 * i}:{b}:4' for i, b in enumerate(
                     ['RIGHT', 'B', 'A', 'B', 'A', 'B', 'A', 'B', 'A', 'B', 'A'] * 2)], frames=500),
    'mp_heal': magic(1, {0x03C1: 4}, note='heal'),
    'mp_shot': magic(2),
    'mp_out': magic(2, {0x0708: 0}, note='MP 0 -> SpendMp -> SubstOutOfMp 30:CC17'),
    'mp_telepathy': magic(3, keys=['20:A', '80:A', '140:A', '200:B', '260:A', '320:B'], frames=400,
                          note='-> SubstTelepathySelect'),
    'mp_teleport': magic(4, keys=['20:A', '80:DOWN', '120:A', '200:A'], frames=500,
                         note='-> SubstTeleportMenu, pick row 2'),
    'mp_cure': magic(5, {0x0710: 0x03}, note='poisoned -> 19:BB39 SubstCureMagic'),
    'mp_bridge': magic(6, keys=['20:A:60'], note='held 60 frames'),
    'mp_change': magic(7, keys=['20:A', '80:A', '140:A', '200:A'], frames=400,
                       note='-> SubstChangeForm 30:DEC1'),
    'mp_flight': magic(8, keys=['20:A:80', '140:A+RIGHT:40'], note='held = hover'),
    'mp_jump': magic(0, {0x0716: 0x12}, keys=['20:A', '60:A+RIGHT', '100:A'], note='accessory $12 = jump'),
}
# s92: one scenario per distinct TalkActionTable / EventActionTable handler (REVERSE.md "Talk actions")
for _a in (1, 2, 3, 5, 6, 8, 9, 10, 12, 16, 17, 19, 20, 21, 22, 23, 24, 25, 26, 27):
    SCENARIOS[f'talk_{_a:02d}'] = talk_act(_a)
for _a in (1, 2, 7, 8, 9, 10, 11, 12, 13, 14, 15, 19, 20, 21, 22, 23, 24, 25, 27, 28, 29, 30, 31):
    SCENARIOS[f'event_{_a:02d}'] = event_act(_a)
# act 1: SwordRaisePose + msg 28.17 run to ~f263, so close the message later and wait for the $F2 load
SCENARIOS['event_01']['keys'] += [f'{300 + 40 * i}:A' for i in range(8)]
SCENARIOS['event_01']['frames'] = 700


def spawn_param_at_give(val):
    """exec hook at GiveItem 30:D3EB: obj_spawn_param[$0623] := val (the slot is only known at run time)."""
    def f(nes):
        nes.bus.ram[0x0560 + nes.bus.ram[0x0623]] = val
    return (30, 0xD3EB, f)


# s96: event act 15 (30:D70D) = trigger slot param $41 (Refresh, inv $6458+8) + GiveItem; magic $6458-5F
# full -> GiveItemFull
SCENARIOS['give_full'] = event_act(15, extra={0x6458 + i: 0x41 + i for i in range(8)})
SCENARIOS['give_full']['note'] = 'event act 15 -> GiveItem $41 with magic $6458-5F full -> GiveItemFull 30:D47C'
SCENARIOS['give_full']['watch'] += [0x23, 0x6458]
SCENARIOS['trap_chest'] = event_act(15, frames=500)
SCENARIOS['trap_chest']['exec'].append(spawn_param_at_give(0x70))
SCENARIOS['trap_chest']['note'] = 'event act 15 -> GiveItem with param forced to $70 -> TrapChest 30:D3DA + AI $2B'
SCENARIOS['mp_telepathy_hint'] = magic(3, {0x0421: 16}, keys=['20:A', '80:A', '140:A', '200:B', '260:A', '320:B'],
                                       frames=400, note='level 16 > TelepathyLevelGate -> TelepathyHint 14:81D1')
SCENARIOS['use_bow'] = dict(note='area 03: Bow of Moon ($3E, $0715 = id-$1C) as key item 0, $07D7=1 (cam lock) + B '
                                 '-> UseBowCheck', area='03', pokes={0x0715: 0x22, 0x6450: 0x3E, 0x642E: 0, 0x07D7: 1},
                            keys=['20:B', '120:A', '160:B'], frames=240, watch=[0x23, 0x6450])
# s96: shops. Area $C4 = ShopAreaTable 16:9F54 entry 0: the player arrives on the keeper, whose AI $0F
# AiSetSubstate 27:A5C5 sets sub_state = $0620 = $20 (SubstArmorShop) on the first frame. The tool shop
# ($21) / SubstShopEmpty ($24) variants overwrite sub_state at its RTS 27:A5CA. Gold 9999 so a buy succeeds
_SHOP_KEYS = ['200:RIGHT', '240:RIGHT', '280:DOWN', '320:A', '400:A', '460:B', '520:B', '580:B']
for _n, _ss in (('shop_armor', None), ('shop_tool', 0x21), ('shop_empty', 0x24)):
    SCENARIOS[_n] = dict(note=f'area C4: keeper AI $0F -> sub_state {_ss or 0x20:#04x}, cursor RIGHT/DOWN, A buy, B out',
                         area='C4', pokes={0x0702: 0x0F, 0x0703: 0x27}, keys=_SHOP_KEYS, frames=900,
                         exec=[force(27, 0xA5CA, 0x41, _ss)] if _ss else [],
                         watch=[0x6427, 0x6474, 0x646D, 0x0702])

# s99: enemy types no area places directly (enemy_catalog.py: no areas) -> ObjInit at +10 into slot $0F
# (area 03 skips it), 48 px right of the player. $DC = AI $74 ObjAiExplode -> $DD -> $CF (AI $6F BossDefeated)
for _n, _t, _dx, _dy in (('obj_dasher', 0x5B, 48, 0), ('obj_fan', 0x5D, 48, -32), ('obj_fan77', 0x77, 48, -32),
                         ('obj_orbiter', 0xC8, 48, 0), ('obj_burst', 0x4D, 48, 0), ('obj_explode', 0xDC, 48, 0)):
    SCENARIOS[_n] = dict(note=f'area 03: ObjInit type ${_t:02X} in slot $0F at player+({_dx},{_dy}) at +10',
                         area='03', pokes={0x03C1: 0x60, 0x0421: 16}, spawn=[(10, 0x0F, _t, _dx, _dy)],
                         keys=['200:LEFT:20'], frames=500, watch=[0x04AF, 0x060F])
# BossDefeated scans BossAreaList 15:B95D for $6C with no end mark: outside a boss area the index runs on
# ($42 from area 03) and the record's code hook JSRs into RAM (crash at $1890). Area $0A = list entry 0.
SCENARIOS['obj_explode'].update(area='0A', note='area 0A (BossAreaList entry 0): ObjInit type $DC '
                                '(ObjAiExplode -> $DD -> $CF BossDefeated) in slot $0F at +10',
                                watch=[0x04AF, 0x060F, 0x0102, 0x07D7])

# s103: BossAreaList entry 2 = area $28, whose record hook is BossFleeSpeech 15:BA98 -> spawn $CF directly
SCENARIOS['boss_flee'] = dict(note='area 28 (BossAreaList entry 2): ObjInit type $CF (AI $6F BossDefeated) in slot '
                                   '$0F at +10 -> record hook BossFleeSpeech 15:BA98', area='28',
                              pokes={0x03C1: 0x60}, spawn=[(10, 0x0F, 0xCF, 48, 0)],
                              keys=[f'{200 + 40 * i}:A' for i in range(8)], frames=700,
                              watch=[0x04AF, 0x060F, 0x0102, 0x061F])
# s103: the dormant area-$1A boss ($5E, AI $7F) wakes as AI $E2 when the Insect Flute (item $27) is used
# Area 1A entrance 0 does not place it in this state (slot $0D empty -> UseFail), so spawn $5E into $0D first
SCENARIOS['boss_e2'] = dict(note='area 1A: ObjInit type $5E (dormant, AI $7F) in slot $0D at +10, Insect Flute '
                                 '($0715 $0B, item $27) + B at +20 -> AI $E2 BossAiE2 15:A675',
                            area='1A', pokes={0x0715: 0x0B, 0x6440: 0x27, 0x642C: 0, 0x03C1: 0x60, 0x0421: 16},
                            spawn=[(10, 0x0D, 0x5E, 48, -32)], keys=['20:B', '120:B'], frames=900,
                            watch=[0x04AD, 0x060D, 0x0103])
# s103: AI $0E AiShopEmpty 27:A5C0 (type $4E) is placed by no area. In a shop area the keeper's sub_state
# stops object updates, so spawn it in area 03 (normal play)
SCENARIOS['shop_empty_ai'] = dict(note='area 03: ObjInit type $4E (AI $0E AiShopEmpty) in slot $0F at +10',
                                  area='03', spawn=[(10, 0x0F, 0x4E, 32, 0)],
                                  keys=['100:B', '160:B', '220:B', '280:A'], frames=400, watch=[0x04AF])
# s126: MagicFailSound 27:A1BB = PlaySound($4E): telepathy/change in areas $58-$5F, or status 4 (MagicStatus4Check)
SCENARIOS['mp_fail_area'] = magic(3, keys=['20:A'], frames=120, area='58', note='area $58 -> MagicFailSound')
SCENARIOS['mp_fail_status'] = magic(7, {0x0710: 0x04}, keys=['20:A'], frames=120,
                                    note='status 4 ($0710) -> MagicStatus4Check -> MagicFailSound')
# s127: BossRecs entry 12 (area $5F) has music byte $FF -> snd_cur_music; the reward AI 15:B770 replays it
# (PlaySound($FF)) once slots $0D-$1E are clear -> SndCommand $FF -> snd_fade_timer $BC -> SndFadeStep 24:8609
SCENARIOS['boss_fade'] = dict(note='area 5F (BossAreaList entry 13): ObjInit type $CF (AI $6F BossDefeated) in slot '
                                   '$0F at +10 -> music $FF -> fade-out (SndFadeStep)', area='5F',
                              pokes={0x03C1: 0x60}, spawn=[(10, 0x0F, 0xCF, 48, 0)], frames=2500,
                              watch=[0x04AF, 0x060F, 0x0102, 0x0103, 0x00FE, 0x0110, 0x04B4])


def set_from(dst, src):
    """exec hook value: write RAM[src] to dst (e.g. make a save checksum match the running sum $16)."""
    return lambda nes: write_mem(nes, dst, nes.bus.ram[src])


# s103: BootSaveCheck 31:F0A4 repair paths. Slot 0 primary != backup (timed poke of $7150) + warm reset;
# the checksum is forced equal to the computed sum $16 right after SaveSumSlot, so SlotCopy runs.
for _n, _pc, _ck, _note in (('slot_repair_a', 0xF0EC, 0x70F0, 'primary sum matches -> SlotCopy(0) primary->backup'),
                            ('slot_repair_b', 0xF108, 0x70F1, 'primary fails, backup sum matches -> SlotCopy(1)')):
    SCENARIOS[_n] = dict(note=f'area 01: $7150 changed, warm reset: {_note}',
                         keys=['10:s:7150=A5', '11:s:7450=5A', '20:s:FFFF=00'], frames=600,
                         exec=[(31, _pc, set_from(_ck, 0x16))] + ([(31, 0xF0EC, lambda nes: write_mem(
                             nes, 0x70F0, nes.bus.ram[0x16] ^ 0xFF))] if _ck == 0x70F1 else []),
                         watch=[0x7001, 0x70F0, 0x70F1, 0x7150, 0x7450])


def parse_poke(s):
    a, _, v = s.partition('=')
    return int(a, 16), int(v, 16)


def parse_keys(items):
    """['20:START', '60:P2:A:8'] -> list of (offset, port, mask, len); 'OFF:s:ADDR=VAL' -> port 's'."""
    out = []
    for it in items:
        p = re.fullmatch(r'(\d+):s:([0-9A-Fa-f]+)=([0-9A-Fa-f]+)', it)
        if p:
            out.append((int(p[1]), 's', int(p[2], 16), int(p[3], 16)))
            continue
        m = re.fullmatch(r'(\d+):(P2:)?([A-Za-z+]+)(?::(\d+))?', it)
        if not m:
            raise SystemExit(f'bad key item {it!r}')
        out.append((int(m[1]), 1 if m[2] else 0, parse_buttons(m[3]), int(m[4] or 2)))
    return out


def never_run():
    """Labels listed under 'never run' in docs/emu_tour.txt -> {(bank, addr): name}."""
    out = {}
    on = False
    for line in open(TOUR):
        if 'never run' in line:
            on = True
            continue
        m = re.match(r'\s+bank\s+(\d+):(.*)', line) if on else None
        if on and not m:
            break
        if m:
            for tok in m[2].split():
                ad, _, name = tok.partition(':')
                out[(int(m[1]), int(ad, 16))] = name
    return out


def spawn_obj(nes, slot, typ, dx, dy):
    """s99: ObjInit 30:C25D (slot $10, type $11) between frames, then place the object dx/dy px from
    the player (slot 1). CPU regs, mapper regs, $10-$13 and the bank shadows $50/$6F are restored."""
    cpu, m, r = nes.cpu, nes.mapper, nes.bus.ram
    regs = (cpu.a, cpu.x, cpu.y, cpu.p, cpu.pc)
    mregs, msel = list(m.regs), m.bank_select
    zp = {a: r[a] for a in (0x10, 0x11, 0x12, 0x13, 0x50, 0x6F)}
    r[0x10], r[0x11] = slot, typ
    nes.call(0xC25D)
    for a, v in zp.items():
        r[a] = v
    m.regs[:] = mregs
    m.bank_select = msel
    m._update_prg()
    cpu.a, cpu.x, cpu.y, cpu.p, cpu.pc = regs
    x = (r[0x71] | r[0x91] << 8) + dx
    y = (r[0xB1] | r[0xD1] << 8) + dy
    r[0x70 + slot], r[0x90 + slot] = x & 0xFF, x >> 8 & 0xFF
    r[0xB0 + slot], r[0xD0 + slot] = y & 0xFF, y >> 8 & 0xFF


def write_mem(nes, ad, v):
    """RAM / WRAM poke; the pseudo address $FFFF = CPU reset (RAM + WRAM kept)."""
    if ad == 0xFFFF:
        nes.cpu.reset()
    elif ad < 0x0800:
        nes.bus.ram[ad] = v
    else:
        nes.bus.write(ad, v)


def run(sc, verbose=False):
    nes = NES()
    st = sc.get('state', os.path.join(STATES, 'area1_enter.state'))
    if st:                                          # '' = power-on
        state.load(nes, st)
    r = nes.bus.ram
    while st and r[0x41] != 8:                      # reach gameplay first (tour.py style A taps)
        nes.keys = {nes.ppu.frame_no: BTN_A} if nes.ppu.frame_no % 8 == 4 else {}
        nes.run_frame()
    if sc.get('area'):
        ar, _, en = sc['area'].partition('.')
        ar, en = int(ar, 16), int(en or '0', 16)
        r[0x6C], r[0x6D], r[0x41] = ar, en, 1
        left = False
        for i in range(600):
            nes.keys = {nes.ppu.frame_no: BTN_A} if i > 150 and i % 8 == 4 else {}
            nes.run_frame()
            left |= r[0x41] != 8
            if left and r[0x41] == 8 and r[0x6C] == ar:
                break
    for ad, v in sc.get('pokes', {}).items():
        write_mem(nes, ad, v)
    keys = parse_keys(sc.get('keys', []))
    f0 = nes.ppu.frame_no
    p2 = [0]

    each = sc.get('each')

    def pad1(frame):
        o = frame - f0
        m1 = m2 = 0
        if each:
            each(nes, o)
        for off, slot, typ, dx, dy in sc.get('spawn', []):
            if o == off:
                spawn_obj(nes, slot, typ, dx, dy)
        for off, port, mask, ln in keys:
            if port == 's':
                if o == off:
                    write_mem(nes, mask, ln)
            elif off <= o < off + ln:
                if port:
                    m2 |= mask
                else:
                    m1 |= mask
        p2[0] = m2
        nes.pads.set(1, m2)
        return m1
    nes.keys = pad1
    for bank, pc, fn in sc.get('exec', []):
        nes.on_exec(bank, pc, fn)
    cov = set()
    bank_at = nes.mapper.prg_bank_at
    add = cov.add

    def tr(cpu):
        pc = cpu.pc
        add((bank_at(pc) if pc >= 0x8000 else -1, pc))
    nes.set_tracer(tr)
    log = []
    for ad in [0x40, 0x41] + sc.get('watch', []):
        def w(addr, val, ad=ad):
            pc = nes.cpu.pc
            log.append(f'    f{nes.ppu.frame_no - f0}: ${ad:04X} <- {val:02X}  pc '
                       f'{bank_at(pc) if pc >= 0x8000 else -1}:{pc:04X}')
        nes.on_write(ad, w)
    crash = ''
    for _ in range(sc.get('frames', 300)):
        try:
            nes.run_frame()
        except IllegalOpcode as e:              # a forced act can send the CPU into data
            crash = f'  CRASH f{nes.ppu.frame_no - f0}: {e}'
            break
    nes.set_tracer(None)
    # collapse repeated identical writes
    seen, slog = set(), []
    for line in log:
        k = line.split(': ', 1)[1]
        if k not in seen:
            seen.add(k)
            slog.append(line)
    end = (f'end: $40/$41 {r[0x40]}/{r[0x41]:02X} area {r[0x6C]:02X} hp {r[0x3C1]} lv {r[0x421]} '
           f'gold {r[0x702] | r[0x703] << 8} xp {r[0x704] | r[0x705] << 8} mp {r[0x708]}{crash}')
    return cov, slog, end


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('names', nargs='*')
    ap.add_argument('--list', action='store_true')
    ap.add_argument('--custom', action='store_true')
    ap.add_argument('--state')
    ap.add_argument('--area')
    ap.add_argument('--poke', nargs='*', default=[])
    ap.add_argument('--keys', nargs='*', default=[])
    ap.add_argument('--frames', type=int, default=300)
    ap.add_argument('--watch', nargs='*', default=[])
    ap.add_argument('--out')
    a = ap.parse_args(argv)
    if a.list:
        for n, sc in SCENARIOS.items():
            print(f'{n:12s} {sc["note"]}')
        return 0
    if a.custom:
        sc = dict(note='custom', area=a.area, pokes=dict(parse_poke(p) for p in a.poke), keys=a.keys,
                  frames=a.frames, watch=[int(x, 16) for x in a.watch])
        if a.state:
            sc['state'] = a.state
        todo = {'custom': sc}
    else:
        todo = {n: SCENARIOS[n] for n in (a.names or SCENARIOS)}
    nr = never_run()
    union = set()
    out = [f'tools.emu scenarios vs the {len(nr)} labels the tour never ran ({os.path.relpath(TOUR)})', '']
    for n, sc in todo.items():
        cov, log, end = run(sc)
        hit = sorted(k for k in nr if k in cov)
        union |= set(hit)
        out.append(f'[{n}] {sc["note"]}')
        out.append(f'  {end}')
        out.append(f'  state writes ($40/$41{"".join(f" ${x:04X}" for x in sc.get("watch", []))}), first of each:')
        out += log[:40]
        out.append(f'  newly run ({len(hit)}): ' + ' '.join(f'{b}:{ad:04X}:{nr[(b, ad)]}' for b, ad in hit))
        out.append('')
    left = sorted(k for k in nr if k not in union)
    out.append(f'union: {len(union)} / {len(nr)} newly run; still never run ({len(left)}):')
    for b in sorted({k[0] for k in left}):
        out.append(f'  bank {b:2d}: ' + ' '.join(f'{ad:04X}:{nr[(bb, ad)]}' for bb, ad in left if bb == b))
    text = '\n'.join(out) + '\n'
    if a.out:
        with open(a.out, 'w') as f:
            f.write(text)
    print(text, end='')
    return 0


if __name__ == '__main__':
    sys.exit(main())
