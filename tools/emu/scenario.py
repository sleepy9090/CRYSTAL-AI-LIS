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


def follow_enemy(slot, dx, hp=1):
    """Each frame: park enemy `slot` dx px right of the player (slot 1) with at most `hp` HP (None = keep)."""
    def f(nes, o):
        r = nes.bus.ram
        if r[0x04A0 + slot] and r[0x41] == 8:
            x = (r[0x71] | r[0x91] << 8) + dx
            r[0x70 + slot], r[0x90 + slot] = x & 0xFF, x >> 8 & 0xFF
            r[0xB0 + slot], r[0xD0 + slot] = r[0xB1], r[0xD1]
            if hp is not None:
                r[0x03C0 + slot] = min(r[0x03C0 + slot], hp)
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
    # s135: a pick needs the town's warp flag F2F5+index ($64DE b5-7, $64DF b0-7; TestFlagF2F0 30:DD58),
    # which a new game has none of, so the two scenarios above stay in the menu. These set all 11.
    'warp_pick': dict(note='area 03: Warp Boots + all town flags F2F5-F2FF -> DOWN, A = index 3 (area $D5)',
                      # item $24 is a consumable (ItemGetTable 14:9EDD = range $6440+, 8 slots), so it sits
                      # in $6440, not $6450 as in use_warp; ItemUseHook then removes it after the warp.
                      area='03', pokes={0x0715: 0x08, 0x6440: 0x24, 0x642C: 0,
                                        0x64DE: 0xE0, 0x64DF: 0xFF},
                      keys=['20:B', '100:DOWN', '140:A'], frames=500, watch=[0x6C, 0x6D, 0x6440, 0x0715],
                      pcs=[(23, 0xBC00), (14, 0x8354), (14, 0x826F)]),
    'mp_teleport_pick': magic(4, {0x64DE: 0xE0, 0x64DF: 0xFF}, keys=['20:A', '80:RIGHT', '120:A'],
                              frames=500, note='+ all town flags -> RIGHT, A = index 1 (area $18)'),
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
# s177: the fan-turret quirk (AiFanTurretIdle 27:B5D3 JMPs to AiShootCheck, whose attack-start PLA PLA pops
# ObjUpdateAll's return): a dasher (AI $44, moves every frame) in slot $1C must stand still on that frame
SCENARIOS['fan_skip'] = dict(note='area 03: fan turret $5D in slot $0F + dasher $5B in slot $1C at +10; the attack '
                                  'start skips the AI of slots $10-$1E for one frame',
                             area='03', pokes={0x03C1: 0x60, 0x0421: 16},
                             spawn=[(10, 0x0F, 0x5D, 48, -32), (10, 0x1C, 0x5B, -48, 0)],
                             keys=['200:LEFT:20'], frames=500, watch=[0x064F, 0x061C, 0x007C])
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
# s155: the same kill run on: SubstFinalSceneReset 19:BDF2 -> FinalSceneToEnding 19:BFC4 -> main_state 4
# MainStateEndingInit 30:C944 -> 5 MainStateEnding 30:C94C (bank-17 ending, steps $0600 0..29; no keys needed)
SCENARIOS['ending'] = dict(SCENARIOS['boss_fade'], frames=9000, watch=[0x0600, 0x8D], peek=[0x0600, 0x0602],
                           keys=[f'{f}:A' for f in range(460, 1520, 40)],   # the final scene's 5 messages
                           note='area 5F: boss_fade kill run on through the final scene (A every 40 f for its '
                                'messages) into the bank-17 ending (main_state 4/5)')


def place_player(x, y, skip=10, cam=False):
    """exec hook at PlayerMove 26:9D9A (slot 0 ground body): on its `skip`-th run, put slots 0 and 1 at
    x/y (16-bit: hi byte = screen). A pad-time `each` poke is too late (the move already copied the position).
    cam=True (s133): also centre the camera ($02-$05 + the CameraUpdate copy $3C-$3F) there, else a jump to
    another screen row leaves the camera behind and every object near the player flagged off-screen."""
    n = [0]

    def f(nes):
        n[0] += 1
        if n[0] == skip:
            r = nes.bus.ram
            for s in (0, 1):
                r[0x70 + s], r[0x90 + s], r[0xB0 + s], r[0xD0 + s] = x & 0xFF, x >> 8, y & 0xFF, y >> 8
            if cam:
                cx = max(x - 0x80, 0)
                cy = max((y >> 8) * 240 + (y & 0xFF) - 0x70, 0)
                for i, v in enumerate((cx & 0xFF, cx >> 8 & 0xFF, cy % 240, cy // 240)):
                    r[0x02 + i] = r[0x3C + i] = v
    return (26, 0x9D9A, f)


# s128: PlayerConveyor 26:9FD2 runs on BG attr b5 (gfx/areas/coll_index.txt: areas $20/$21/$28/$29/$40-$42/$60).
# Area $20 screen (2,1) cells x 6-9, rows $B0-$E0 are 'c'; drop the player there and let it ride
SCENARIOS['conveyor'] = dict(note='area 20: slots 0/1 placed on the conveyor strip (x $0280, y $01C8) at the '
                                  '10th PlayerMove 26:9D9A, DOWN held -> PlayerConveyor 26:9FD2',
                             area='20', pokes={0x03C1: 0x60}, exec=[place_player(0x0280, 0x01C8)],
                             keys=['14:DOWN:30'], frames=200,
                             watch=[0x0641, 0x0661, 0x0681, 0x0071, 0x0091, 0x00B1, 0x00D1])
# s128: the only AI $02 setter is AI $70 phase 1's end 27:BB6E (after messages $1B.05-07): PlayerMoveLocked.
# Phase 1 = type $B3 (objtype_dump: 0600=01), spawned only by BossRecs area $A6's hook 15:B8FD (slot $15) and the
# reward AI 15:B728 (spawn param b7). Area $A6 = BossAreaList entry 11; area $5F's AI $70 starts at phase 2.
SCENARIOS['final_locked'] = dict(note='area A6 (BossAreaList entry 11): ObjInit type $CF (AI $6F BossDefeated) at +10 '
                                      '-> hook 15:B8FD -> type $B3 (AI $70 phase 1), A taps through messages '
                                      '$1B.05-07 -> player obj_active 2 = PlayerMoveLocked 26:9D81',
                                 area='A6', pokes={0x03C1: 0x60, 0x03C0: 0x60}, spawn=[(10, 0x0F, 0xCF, 48, 0)],
                                 each=lambda nes, o: nes.bus.ram.__setitem__(0x03C1, 0x60),
                                 keys=[f'{40 + 30 * i}:A' for i in range(40)], frames=1500,
                                 watch=[0x04A0, 0x060F, 0x07DE, 0x0676, 0x006C],
                                 pcs=[(15, 0xB8FD), (27, 0xBAF5), (27, 0xBB6E), (26, 0x9D81), (26, 0x9D91)],
                                 peek=[0x04A0, 0x04A1, 0x0340, 0x0341, 0x05A0, 0x07D7, 0x0710])


def place_on_slot(slot, skip=10, dy=0, hold=1, park=None, debug=False):
    """exec hook at PlayerMove 26:9D9A: on runs skip..skip+hold-1, put slots 0/1 on object `slot`'s position (+dy).
    park=(x, y, run): from run `run` until `skip`, hold the player at x/y instead (a non-pit cell next to the
    platform), so the camera catches up first: ObjOverlapScan compares the sprite pass's screen coords
    $05C0/$05E0 and skips off-screen objects (obj_flags b7), and a far teleport leaves both off-screen."""
    n = [0]

    def f(nes):
        n[0] += 1
        r = nes.bus.ram
        if park and park[2] <= n[0] < skip:
            if n[0] == park[2]:  # camera ($02-$05 and the CameraUpdate copy $3C-$3F) centred on the park spot
                cx = park[0] - 0x80
                cy = (park[1] >> 8) * 240 + (park[1] & 0xFF) - 0x70
                cam = (cx & 0xFF, cx >> 8 & 0xFF, cy % 240, cy // 240)
                for i, v in enumerate(cam):
                    r[0x02 + i] = r[0x3C + i] = v
            for s in (0, 1):
                r[0x70 + s], r[0x90 + s], r[0xB0 + s], r[0xD0 + s] = (park[0] & 0xFF, park[0] >> 8,
                                                                      park[1] & 0xFF, park[1] >> 8)
        if skip <= n[0] < skip + hold:
            if debug:
                print(f'    place #{n[0]}: slot {slot:02X} act {r[0x04A0 + slot]:02X} coll {r[0x03A0 + slot]:02X} '
                      f'fl {r[0x0380 + slot]:02X}/{r[0x0340 + slot]:02X} xy {r[0x90 + slot]:02X}{r[0x70 + slot]:02X},'
                      f'{r[0xD0 + slot]:02X}{r[0xB0 + slot]:02X} scr {r[0x05C0 + slot]:02X},{r[0x05E0 + slot]:02X} | '
                      f'p1 coll {r[0x03A1]:02X} fl {r[0x0381]:02X}/{r[0x0341]:02X} scr {r[0x05C1]:02X},{r[0x05E1]:02X} '
                      f'$0641 {r[0x0641]:02X}')
            y = (r[0xB0 + slot] | r[0xD0 + slot] << 8) + dy
            for s in (0, 1):
                r[0x70 + s], r[0x90 + s] = r[0x70 + slot], r[0x90 + slot]
                r[0xB0 + s], r[0xD0 + s] = y & 0xFF, y >> 8 & 0xFF
    return (26, 0x9D9A, f)


# s129: AreaPitFall 31:E544 / AreaEdgeLink 31:E573 (k5) / RidePlatform 26:943F in the suite. Area $6A (k5) pit
# column ($578,$168) = the s116 web_state.py spot; slot $18 there is the AI $38 pit shuttle (s117)
SCENARIOS['pitfall'] = dict(note='area 6A: slots 0/1 placed on the pit column ($0578,$0168) at the 10th PlayerMove '
                                 '-> AreaPitFall 31:E544 -> AreaEdgeLink k5 match -> area $69 fall-in',
                            area='6A', pokes={0x03C1: 0x60}, exec=[place_player(0x0578, 0x0168)], frames=300,
                            watch=[0x006C, 0x006D, 0x0041, 0x0620, 0x00D0],
                            pcs=[(31, 0xE544), (31, 0xE573), (31, 0xE5E5), (31, 0xE600), (31, 0xE603)])
SCENARIOS['ride_platform'] = dict(note='area 6A: player parked at ($0580,$0100) + camera set (10th PlayerMove), '
                                       'then put on slot $18 (AI $38 AiPitShuttle over the pit strip) at the 400th '
                                       '-> ObjCollideAll -> RidePlatform 26:943F, no pit fall',
                                  area='6A', pokes={0x03C1: 0x60}, exec=[place_on_slot(0x18, skip=400, hold=3, park=(0x0580, 0x0100, 10))],
                                  frames=600,
                                  watch=[0x0541, 0x04B8, 0x0071, 0x00B1, 0x006D],
                                  pcs=[(26, 0x943F), (31, 0xF073)],
                                  peek=[0x04B8, 0x0088, 0x00A8, 0x00C8, 0x00E8, 0x0071, 0x0091, 0x00B1, 0x00D1, 0x006C])

# s130: ThunderTransform 26:9229 (from HitByPlayerShot 26:90BB): thunder sword ($0711 = 4) hit on a spawn-h0 type
# with param 0/3 -> ObjInit(obj_behav). Area $06 slots $0D/$0E = h0 p=$03 = type $53 (obj_behav $69)
SCENARIOS['thunder_transform'] = dict(note='area 06: thunder sword ($0711=4), level 16; slot $0D (type $53) parked at '
                                           'the sword, B/A swings -> ThunderTransform -> type $69',
                                      area='06', pokes={0x0421: 16, 0x0711: 4, 0x03C1: 0x60},
                                      each=lambda nes, o, _f=follow_enemy(0x0D, 14): (
                                          _f(nes, o), nes.bus.ram.__setitem__(0x03C1, 0x60),
                                          nes.bus.ram.__setitem__(0x071A, 0x1E), nes.bus.ram.__setitem__(0x03E1, 0x20)),
                                      keys=[f'{10 + 20 * i}:{b}:4' for i, b in enumerate(
                                          ['RIGHT', 'B', 'A', 'B', 'A', 'B', 'A', 'B', 'A', 'B', 'A'] * 2)]
                                      + ['200:s:0711=01'],
                                      frames=500, watch=[0x0711, 0x04AD, 0x03CD, 0x06ED, 0x056D, 0x054D, 0x0704],
                                      pcs=[(26, 0x90BB), (26, 0x923E), (26, 0x9125), (26, 0x913B), (26, 0x9152)],
                                      peek=[0x050D, 0x040D, 0x042D, 0x03E1, 0x0502, 0x03E2, 0x04AD, 0x03CD])

# s131: sword charge (26:9DEF: B held, pad_prev b6) -> orbit_level $06C0 += 1 every 4 frames; at 8 the burst
# timer $06C1 <- $0E (26:9E1F) -> OrbitSprites burst ring = BurstRadius 28:81A5. Release -> 26:9C91 charged shot
# (level & $F8 = $18 also costs MP). sword_power $0719 caps the level (SwordChargeCap 08 10 18)
for _n, _pw, _hold in (('sword_charge', 0, 80), ('sword_charge2', 2, 120)):
    SCENARIOS[_n] = dict(note=f'area 03: sword 1, sword_power {_pw}, B held {_hold} frames at +20 -> orbit + burst '
                              '(BurstRadius 28:81A5), release -> charged shot',
                         area='03', pokes={0x0711: 1, 0x0719: _pw, 0x0708: 0x40}, keys=[f'20:B:{_hold}'],
                         frames=240,
                         watch=[0x06C0, 0x06C1, 0x0708],
                         pcs=[(26, 0x9E13), (26, 0x9E1F), (28, 0x81A5), (26, 0x9C91), (26, 0x9CBA), (26, 0x9CCE)],
                         peek=[0x06C0, 0x06C1, 0x0708, 0x0719])


# s132: magic 6 held = SpawnShot($11) each frame -> slot $0C type $21 (AI $04, obj_phase 1) = shot eater.
# HitHandler3 26:8FC4 with phase 1 -> HitShotEater 26:906C: an enemy shot target (obj_spawn_b2 $FF, e.g.
# type $3F AI $10) is removed + SFX $3A. Spawn enemy shots onto the player while A is held
SCENARIOS['shot_eater'] = dict(note='area 03: magic 6 held at +20 (slot $0C shot eater), enemy shot type $3F '
                                    '(obj_spawn_b2 $FF) spawned on the player into slots $0F/$10/$11 -> HitShotEater 26:906C',
                               area='03', pokes={0x0708: 0x60, 0x0709: 0x60, 0x0712: 6, 0x03C1: 0x60},
                               spawn=[(40, 0x0F, 0x3F, 0, 0), (70, 0x10, 0x3F, 4, 0), (100, 0x11, 0x3F, -4, 0),
                                      (200, 0x0F, 0x3F, 0, 0)],
                               each=lambda nes, o: nes.bus.ram.__setitem__(0x03C1, 0x60),
                               keys=['20:A:120'], frames=260,
                               watch=[0x04AC, 0x060C, 0x04AF, 0x04B0, 0x04B1, 0x0708],
                               pcs=[(26, 0x8FC4), (26, 0x906C), (26, 0x9071), (26, 0x92BF)],
                               peek=[0x04AC, 0x04AF, 0x04B0, 0x04B1, 0x03C1])


# s133: area $6E reveal. Slot $0E = type $2A (AI $3A AiNpcBossReveal 27:A555, invisible, hittable) on the slot-$0D
# NPC at (128, $016C). Each sword hit (obj_flags2 b7, player y hi 1 / lo $28-$AF) -> msg $10.$10+n, sub_state $10;
# after the 3rd: slots $0D/$0F off, ObjInit(self, $7D) = BossAiE6 15:AC7B (death $CF -> flee)
SCENARIOS['npc_reveal'] = dict(note='area 6E: player + camera put below the slot-$0D NPC (x $0080, y $0178), B swings '
                                    '-> 3 hits on the hidden slot $0E (AI $3A) -> msgs $10.$10-$12 -> type $7D boss',
                               area='6E', pokes={0x0711: 1, 0x03C1: 0x60, 0x0421: 16},
                               exec=[place_player(0x0080, 0x0178, cam=True)],
                               each=lambda nes, o: nes.bus.ram.__setitem__(0x03C1, 0x60),
                               keys=[f'{24 + 24 * i}:B:4' for i in range(36)],
                               frames=900,
                               watch=[0x060E, 0x04AD, 0x04AE, 0x04AF, 0x06C3, 0x06E3, 0x00D1, 0x034E],
                               pcs=[(26, 0x9CBA), (26, 0x90AA), (27, 0xA58B), (27, 0xA5A0), (27, 0xA56D), (15, 0xAC7B)],
                               peek=[0x04AD, 0x04AE, 0x04AF, 0x060E, 0x03CE, 0x0071, 0x0091, 0x00B1, 0x00D1,
                                     0x7D, 0x9D, 0xBD, 0xDD, 0x7E, 0x9E, 0xBE, 0xDE, 0x034E, 0x03AE,
                                     0x038E, 0x05CE, 0x05EE, 0x0381, 0x05C1, 0x05E1, 0x02, 0x03, 0x04, 0x05, 0x0301])
# s133: run the same fight on to the flee: sword hits on the type-$7D boss until it dies ($CF BossFleeSpeech)
def _park_boss_7d(nes, o):
    """npc_reveal_flee: hold player HP; once slot $0E is AI $E6 (the flyer), park it 12 px in front of the
    player (facing = player meta $0301 & 3: 0 up, 1 right, 2 down, 3 left)."""
    r = nes.bus.ram
    r[0x03C1] = 0x60
    r[0x03E1] = 0x20
    if r[0x04AE] == 0xE6 and r[0x41] == 8:
        dx, dy = ((0, -12), (12, 0), (0, 12), (-12, 0))[r[0x0301] & 3]
        x = (r[0x71] | r[0x91] << 8) + dx
        y = (r[0xB1] | r[0xD1] << 8) + dy
        r[0x7E], r[0x9E], r[0xBE], r[0xDE] = x & 0xFF, x >> 8 & 0xFF, y & 0xFF, y >> 8 & 0xFF


SCENARIOS['npc_reveal_flee'] = dict(SCENARIOS['npc_reveal'], frames=2400, each=_park_boss_7d,
                                    exec=[place_player(0x0080, 0x0178, cam=True)],   # own hook (its run counter)
                                    pokes={0x0711: 2, 0x03C1: 0x60, 0x0421: 16},
                                    note='npc_reveal with sword 2 + $03E1 held at $20, boss $7D (BossAiE6) parked in '
                                         'front of the player -> death type $CF (BossDefeated + BossFleeSpeech)',
                                    keys=['16:UP:1'] + [f'{24 + 24 * i}:B:4' for i in range(98)],
                                    watch=[0x060E, 0x04AE, 0x03CE, 0x06C3, 0x06E3, 0x0102],
                                    peek=[0x04AE, 0x060E, 0x03CE, 0x050E, 0x040E, 0x042E, 0x0502, 0x03E2, 0x03E1,
                                          0x0102, 0x006C],
                                    pcs=[(27, 0xA5A0), (27, 0xA56D), (15, 0xAC7B), (26, 0x90AA), (26, 0x9125),
                                         (26, 0x913B), (26, 0x9152), (15, 0xB807), (15, 0xBA98)])


# s134: talk act 1 (30:D19C) for real. NPC param $2D = the two guards in slots $0F/$10 of areas $28/$73
# ("You cannot pass here! Attack!"); act 1 = ObjInit type $64 (AI $2A AiLunger) into the slot pair
# ((Y-1)&$FE)+1 = $0F/$10, i.e. both guards turn into fighters. Area $73 = script state 5: F02A set -> act 8
# "On your way", else (F000 clear) act 1. Clear F000 ($6480 b0) + F02A ($6485 b2) at NpcScript entry.
def _clr_guard_flags(nes):
    b = nes.bus
    b.write(0x6480, b.read(0x6480) & 0xFE)
    b.write(0x6485, b.read(0x6485) & 0xFB)


SCENARIOS['guard_attack'] = dict(note='area 73: player + camera put below guard slot $0F (x $0068, y $0090), UP into '
                                      'it -> NpcTalk -> NPC $2D act 1 (30:D19C) -> slots $0F/$10 = type $64 (AI $2A)',
                                 area='73', pokes={0x03C1: 0x60},
                                 exec=[place_player(0x0068, 0x0090, cam=True), (14, 0x8000, _clr_guard_flags)],
                                 each=lambda nes, o: nes.bus.ram.__setitem__(0x03C1, 0x60),
                                 keys=['14:UP:20'] + [f'{60 + 30 * i}:A' for i in range(8)], frames=600,
                                 watch=[0x04AF, 0x04B0, 0x0623, 0x07DE, 0x07DF],
                                 pcs=[(30, 0xD0C8), (30, 0xD19C), (27, 0xABF8), (27, 0xAC40)],
                                 peek=[0x04AF, 0x04B0, 0x060F, 0x0610, 0x03CF, 0x03D0, 0x0623, 0x03C1])


# s136: hit rules of HitByPlayerShot 26:90AA, each forced on the `kill` setup (area 03, enemy slot $0E parked
# 14 px right of the player). KILL_KEYS = RIGHT, then B/A swings every 20 frames.
KILL_KEYS = [f'{10 + 20 * i}:{b}:4' for i, b in enumerate(['RIGHT', 'B', 'A', 'B', 'A', 'B', 'A', 'B', 'A', 'B', 'A'] * 2)]
HIT_PCS = [(26, 0x90FA), (26, 0x9125), (26, 0x913B), (26, 0x9152), (26, 0x9094), (26, 0x9093), (26, 0x90A7),
           (26, 0x946D)]


def _hit_each(need=None, defense=None, drop=None, hp=1):
    """kill-style each hook: park slot $0E; optionally force its obj_hitflags low bits (need level),
    obj_defense, and obj_elem_drop hi nibble (gold class) while it is still the enemy (AI != coin)."""
    park = follow_enemy(0x0E, 14, hp=hp)
    dead = [False]

    def f(nes, o):
        r = nes.bus.ram
        r[0x03C1] = 0x60
        dead[0] |= r[0x04AE] == 0x7D                 # EnemyDeathFx puff: stop forcing (the coin keeps its own)
        if not dead[0] and r[0x04AE] and r[0x41] == 8 and not r[0x04AE] & 0x80:
            park(nes, o)
            if need is not None:
                r[0x042E] = r[0x042E] & 0xE0 | need
            if defense is not None:
                r[0x040E] = defense
            if drop is not None:
                r[0x050E] = r[0x050E] & 0x0F | drop << 4
    return f


SCENARIOS['hit_level_gate'] = dict(note='area 03: level 1, enemy $0E with need level 5 (obj_hitflags & $1F) -> miss '
                                        '26:9125 (clink $3A); level <- 5 at +250 -> damage 26:913B / EnemyKilled',
                                   area='03', pokes={0x0421: 1, 0x0711: 1}, each=_hit_each(need=5, hp=None),
                                   keys=KILL_KEYS + ['250:s:0421=05'], frames=500,
                                   watch=[0x0421, 0x04AE, 0x03CE, 0x04A2, 0x0702],
                                   pcs=HIT_PCS, peek=[0x042E, 0x03CE, 0x04AE])
SCENARIOS['hit_defense'] = dict(note='area 03: level 16, enemy $0E defence $FF -> borrow at 26:9123 -> miss 26:9125; '
                                     'defence <- 0 at +250 (each hook stops forcing) -> damage',
                                area='03', pokes={0x0421: 16, 0x0711: 1},
                                each=lambda nes, o, _a=_hit_each(defense=0xFF, hp=None), _b=_hit_each(defense=0, hp=None):
                                (_a if o < 250 else _b)(nes, o),
                                keys=KILL_KEYS, frames=500, watch=[0x04AE, 0x03CE, 0x0702],
                                pcs=HIT_PCS, peek=[0x040E, 0x03CE, 0x04AE])
for _c in (0, 3, 6, 9, 15):
    SCENARIOS[f'gold_class_{_c:X}'] = dict(note=f'area 03: kill (1 HP) with obj_elem_drop hi = {_c} -> PickupGold '
                                                f'adds GoldDropTable[{_c}]',
                                           area='03', pokes={0x0421: 16, 0x0711: 1, 0x0702: 0, 0x0703: 0},
                                           each=_hit_each(drop=_c), keys=KILL_KEYS, frames=400,
                                           watch=[0x0702, 0x0703, 0x04AE], pcs=HIT_PCS,
                                           peek=[0x0702, 0x0703, 0x050E])
# s175: the kill at level 1 with XP next = 1 -> EnemyKilled level-up branch 26:9188 (HudHpBar, HudRedraw 0/3/4/5,
# sub_state $10, msg 32.13, StatRecalc); the B/A swings then page the message
SCENARIOS['kill_levelup'] = dict(note='area 03: kill (1 HP) at level 1 with XP next = 1 -> level up 26:9188, msg 32.13',
                                 area='03', pokes={0x0421: 1, 0x0711: 1, 0x0704: 0, 0x0705: 0, 0x0706: 1, 0x0707: 0},
                                 each=_hit_each(), keys=KILL_KEYS, frames=400,
                                 watch=[0x0421, 0x04AE, 0x0706, 0x0707, 0x03C1, 0x0709], pcs=HIT_PCS,
                                 peek=[0x0421, 0x0704, 0x0706, 0x03C0, 0x03C1, 0x0708, 0x0709])
# element walls: types $D0-$D3 (obj_active $87) spawned 14 px right of the player (facing right) in area 03.
# Sword 1 = wind (SwordElemBit 1): tap at +30 (shot hitflags 1 -> nothing), charge B 50 f from +60 -> break $D0 only
# HitHandler3 phase 0 = the magic-2 freeze shot (slot $0C, type $20): enemy $0E parked 40 px right of the player
# (facing right) -> obj_freeze $05AE <- $FF (26:8FF1). Variant b5: target obj_hitflags b5 forced -> no freeze.
def _freeze_each(b5):
    park = follow_enemy(0x0E, 40, hp=None)

    def f(nes, o):
        r = nes.bus.ram
        r[0x03C1] = 0x60
        if r[0x04AE] and r[0x41] == 8:
            park(nes, o)
            if b5:
                r[0x042E] |= 0x20
    return f


for _n, _b5 in (('freeze_shot', False), ('freeze_shot_b5', True)):
    SCENARIOS[_n] = dict(note=f'area 03: magic 2 (freeze shot, slot $0C) at enemy $0E parked 40 px right'
                              f'{" with obj_hitflags b5 forced" if _b5 else ""} -> HitHandler3 26:8FC4',
                         area='03', pokes={0x0708: 0x60, 0x0709: 0x60, 0x0712: 2, 0x0421: 16},
                         each=_freeze_each(_b5), keys=['4:RIGHT:2', '20:A', '200:A'], frames=400,
                         watch=[0x05AE, 0x04AC], pcs=[(26, 0x8FC4), (26, 0x8FDF), (26, 0x8FE6), (26, 0x8FF1)],
                         peek=[0x05AE, 0x042E, 0x054E, 0x056E])


def hit_logger(tag, limit=12):
    """exec hook fn: print frame, X (attacker), Y (target) and the hit fields of both on each call."""
    n = [0]

    def f(nes):
        if n[0] < limit:
            n[0] += 1
            r, x, y = nes.bus.ram, nes.cpu.x, nes.cpu.y
            print(f'    {tag} abs f{nes.ppu.frame_no}: X={x:02X} ai {r[0x04A0 + x]:02X} hitfl {r[0x0420 + x]:02X} '
                  f'elem {r[0x0500 + x]:02X} atk {r[0x03E0 + x]:02X} | Y={y:02X} ai {r[0x04A0 + y]:02X} '
                  f'hitfl {r[0x0420 + y]:02X} elem {r[0x0500 + y]:02X} hp {r[0x03C0 + y]:02X} def {r[0x0400 + y]:02X}')
    return f


def obj_logger(tag, other=0x0E, limit=12, every=4):
    """exec hook fn (an AI entry, X = self): print X's pos/coll/hitflags/elem vs slot `other` every `every` calls."""
    n = [0]

    def f(nes):
        n[0] += 1
        if n[0] % every == 1 and n[0] // every < limit:
            r, x, o = nes.bus.ram, nes.cpu.x, other
            print(f'    {tag} abs f{nes.ppu.frame_no}: X={x:02X} type? ai {r[0x04A0 + x]:02X} coll {r[0x03A0 + x]:02X} '
                  f'hitfl {r[0x0420 + x]:02X} elem {r[0x0500 + x]:02X} x {r[0x90 + x]:02X}{r[0x70 + x]:02X} '
                  f'y {r[0xD0 + x]:02X}{r[0xB0 + x]:02X} fl {r[0x0380 + x]:02X}/{r[0x0340 + x]:02X} | '
                  f'{o:02X}: coll {r[0x03A0 + o]:02X} x {r[0x90 + o]:02X}{r[0x70 + o]:02X} '
                  f'y {r[0xD0 + o]:02X}{r[0xB0 + o]:02X} fl {r[0x0380 + o]:02X}/{r[0x0340 + o]:02X}')
    return f


for _t in (0xD0, 0xD1, 0xD2, 0xD3):
    for _sw in (1, 4):
        SCENARIOS[f'wall_{_t:02X}_s{_sw}'] = dict(
            note=f'area 03: type ${_t:02X} element wall spawned at +10 (dx 40), sword {_sw}: tap +30, charged B +60..110',
            area='03', pokes={0x0421: 16, 0x0711: _sw, 0x0719: 2, 0x0708: 0x60, 0x03C1: 0x60},
            spawn=[(10, 0x0E, _t, 40, 0)], each=lambda nes, o: nes.bus.ram.__setitem__(0x03C1, 0x60),
            keys=['4:RIGHT:2', '30:B:4', '60:B:40', '120:B:110'], frames=320,
            watch=[0x04AE, 0x02EE], pcs=HIT_PCS, peek=[0x04AE, 0x050E, 0x030E, 0x06EE],
            exec=[(26, 0x9094, hit_logger('wall')), (27, 0xB0A8, obj_logger('shot', every=1))])


# s154: the final boss (type $9E, AI $EB AiBowOfTruth9E 15:B2DA) from a live fight. Area $A6, EvActSpawn9E-style
# ObjInit $9E into slot $0D at +10, player (level 16) held below the boss every PlayerMove, B swings. Logs each
# BossEBSetElem 15:B618 call (A) and each HitByPlayerShot on a boss slot $0D-$13 with the boss phase and outcome.
def boss_hit_log(limit=24):
    n, last = [0], [None]

    def tag(t):
        def f(nes):
            r, x, y = nes.bus.ram, nes.cpu.x, nes.cpu.y
            if t == 'elem':                       # only when the value changes (phase 5 calls it every frame)
                if nes.cpu.a != last[0]:
                    last[0] = nes.cpu.a
                    print(f'    f{nes.ppu.frame_no}: BossEBSetElem A={nes.cpu.a:02X} phase {r[0x060D]}')
            elif 0x0D <= y <= 0x13:
                if n[0] < limit:
                    n[0] += 1
                    if True:
                        print(f'    f{nes.ppu.frame_no}: {t} shot X={x:02X} elem {r[0x0500 + x]:02X} '
                              f'atk {r[0x03E0 + x]:02X} $03E1 {r[0x03E1]:02X} -> slot {y:02X} '
                              f'elem {r[0x0500 + y]:02X} hp {r[0x03C0 + y]:02X} def {r[0x0400 + y]:02X} '
                              f'| boss phase {r[0x060D]} hp {r[0x03CD]:02X}')
        return f
    return [(15, 0xB618, tag('elem')), (26, 0x9125, tag('MISS')),
            (26, 0x913B, tag('DAMAGE'))]


# $03E1 = StatRecalc 30:C008's attack: level (x2 with accessory $0E) + SwordAttackTable[sword] (00 01 02 04 08 10);
# the pokes skip StatRecalc, so hold it: 'max' = level 16 x2 + table, the highest value the game can produce
def _boss_each(atk):
    def f(nes, o):
        r = nes.bus.ram
        r[0x03C1], r[0x071A], r[0x03E1] = 0x60, 0x1E, atk
    return f


for _n, _sw, _atk in (('final_boss', 1, 16 + 1), ('final_boss_s4', 4, 32 + 8), ('final_boss_cx', 5, 16 + 16),
                      ('final_boss_cx_max', 5, 32 + 16)):
    SCENARIOS[_n] = dict(note=f'area A6: ObjInit type $9E (final boss) in slot $0D at +10, sword {_sw}, level 16, '
                              f'$03E1 held {_atk:#04x}, player held under the boss, UP+B swings -> hits per phase',
                         area='A6', pokes={0x0421: 16, 0x0711: _sw, 0x03C1: 0x60, 0x0708: 0x60},
                         spawn=[(10, 0x0D, 0x9E, 0, -48)],
                         each=_boss_each(_atk),
                         exec=[place_on_slot(0x0D, skip=30, dy=0x28, hold=100000)] + boss_hit_log(),
                         keys=[f'{30 + 12 * i}:UP+B:4' for i in range(160)], frames=2000,
                         watch=[0x060D, 0x04AD, 0x03CD, 0x0051],
                         pcs=[(15, 0xB301), (15, 0xB3BC), (15, 0xB4DD), (15, 0xB524), (15, 0xB552), (15, 0xB587),
                              (15, 0xB67A), (15, 0xB6C1)],
                         peek=[0x04AD, 0x060D, 0x03CD, 0x050D])
# s165: the same spawn with no per-frame pokes / keys, for a port state (web/test/finalboss_check.mjs)
SCENARIOS['final_boss_idle'] = dict(note='area A6: ObjInit type $9E (final boss) in slot $0D at +10, level 16, no '
                                         'keys -> AI $6B phase 0 setup (fades, BG strips) then sway / dive / mouth',
                                    area='A6', pokes={0x0421: 16, 0x0711: 1, 0x03C1: 0x60, 0x0708: 0x60},
                                    spawn=[(10, 0x0D, 0x9E, 0, -48)], frames=1500,
                                    watch=[0x060D, 0x04AD, 0x0051],
                                    pcs=[(15, 0xB301), (15, 0xB3BC), (15, 0xB4DD), (15, 0xB524), (15, 0xB552),
                                         (15, 0xB587), (15, 0xB67A)])
# s179: per-enemy-group runs, areas' own spawn lists (no ObjInit pokes), level 16 + full HP so nothing dies
# group_08 = area 08 slot $0D type $54 (AI $28 27:AB61, shoots type $45); the keys walk the player about
SCENARIOS['group_08'] = dict(note='area 08: natural spawns (type $54 AI $28 + NPC), level 16, walk about',
                             area='08', pokes={0x0421: 16, 0x03C1: 0x60, 0x0708: 0x60},
                             keys=['30:UP:85', '200:LEFT:16', '260:RIGHT:32', '330:DOWN:20'],
                             frames=500, watch=[0x060D, 0x03C1, 0x04AD, 0x04AE, 0x04AF])
# s180: group_09 = area 09 slots $0D/$0F/$11 type $56 (p=$06) + $14 type $53 (p=$03), h3 p=$00 at $16
SCENARIOS['group_09'] = dict(note='area 09: natural spawns (types $56 x3, $53, h3 $00), level 16, walk about',
                             area='09', pokes={0x0421: 16, 0x03C1: 0x60, 0x0708: 0x60},
                             keys=['34:UP:50', '89:RIGHT:330', '434:B:4', '454:B:4', '474:B:4', '494:RIGHT:20',
                                   '524:B:4', '544:B:4', '564:RIGHT:30', '604:B:4', '624:B:4', '644:B:4',
                                   '664:B:4'],
                             frames=750, watch=[0x03C1, 0x03D1, 0x03D4, 0x04B1, 0x04B4])


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
    pc_log, pc_seen = [], set()                     # s128: 'pcs' = [(bank, pc)] logged from the start
    def pc_hit(nes, b, p):
        k = (b, p, nes.bus.ram[0x04A0], nes.bus.ram[0x41])
        if k not in pc_seen:                        # first hit per (pc, $04A0, $41)
            pc_seen.add(k)
            pc_log.append(f'    abs f{nes.ppu.frame_no}: {b}:{p:04X} $04A0={k[2]:02X} $41={k[3]:02X}')
    for bank, pc in sc.get('pcs', []):
        nes.on_exec(bank, pc, lambda nes, b=bank, p=pc: pc_hit(nes, b, p))
    while st and r[0x41] != 8:                   # reach gameplay first (tour.py style A taps)
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
    if pc_log:
        log[:0] = [f'    scenario start: abs f{f0}'] + pc_log[:30]
    for line in log:
        k = line.split(': ', 1)[1]
        if k not in seen:
            seen.add(k)
            slog.append(line)
    end = (f'end: $40/$41 {r[0x40]}/{r[0x41]:02X} area {r[0x6C]:02X} hp {r[0x3C1]} lv {r[0x421]} '
           f'gold {r[0x702] | r[0x703] << 8} xp {r[0x704] | r[0x705] << 8} mp {r[0x708]}{crash}'
           + ''.join(f' ${a:04X}={r[a]:02X}' for a in sc.get('peek', [])))
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
