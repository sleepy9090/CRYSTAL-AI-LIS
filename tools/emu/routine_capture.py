"""Fuzz-capture RAM + WRAM around one ROM routine (session 55, Ph7 web 6/8). Generalises sprite_capture.py fuzz.

usage (repo root):
  python3 -m tools.emu.routine_capture --load-state S [--frames N] --routine NAME[,NAME...] --cases N
         [--seed S] --out F.json
  python3 -m tools.emu.routine_capture --list

The run goes to the next ObjUpdateAll 30:CCCC entry (a gameplay frame) and takes RAM + WRAM there as the base.
Per case: random object slots / temps / sound queue (ROUTINES[name].poke), then nes.call(entry, bank) with
the routine's registers. Cases where an NMI/IRQ fired during the call are redrawn with the next seed.

The routine is entered with two return addresses on the stack (call2: like ObjUpdateAll's JSR $0010 -> JMP), so a
handler that pops its caller (AiShootCheck PLA PLA after a tail JMP) is recorded as "ret": "popped".

JSON: {"source", "wram_base": hex(8KB), "cases": [{"name", "routine", "regs": {"a","x","y"}, "sp", "ret",
"ram": hex(2KB), "wram": [[addr, val], ...] (vs wram_base), "after": [[addr, val], ...]}]} -- after: addr < $800 =
RAM, $6000+ = WRAM; only changed bytes; the stack bytes $0100+sp-32 .. $0100+sp+4 are left out.
web/test/obj_check.mjs replays them through the port.
"""
import argparse
import json
import os
import random
import sys

from .nes import NES, Stop
from . import state
from .sprite_capture import Interrupts
from ..nesrom import load_rom

_PRG = load_rom()[1]
RAND64_ZERO = [i for i in range(64) if _PRG[26 * 0x2000 + 0x17E4 + i] == 0]   # Rand64Table 26:97E4 zero entries

OBJ_UPDATE_ALL = 0xCCCC
STACK_SCRATCH = 32       # s100: was 15; ObjUpdateAll -> boss -> BossSmokeRing -> SpawnShot -> far ObjInit is deeper
OBJ_COLS = range(0x0300, 0x0700, 0x20)        # the 32 slot columns $0300-$06E0
POS_COLS = (0x70, 0x90, 0xB0, 0xD0)


def _poke_common(ram, wram, r):
    w = lambda ad, v: ram.__setitem__(ad, v & 0xFF)
    for x in range(32):
        if r.random() < 0.6:
            for c in OBJ_COLS:
                w(c + x, r.randrange(256))
        w(0x04A0 + x, r.choice([0, 0, r.randrange(1, 0x80), r.randrange(256)]))
    w(0x08, r.randrange(256)); w(0x0E, r.randrange(256))
    for a in range(0x10, 0x24):
        w(a, r.randrange(256))
    for a in range(0x30, 0x38):
        w(a, r.randrange(256))
    w(0x0100, r.choice([0, 1, 4, 5, 5, r.randrange(8)]))     # sound queue count / lock / music
    w(0x0102, r.choice([0, r.randrange(0x20), r.randrange(256)]))
    for a in range(0x0103, 0x0109):
        w(a, r.randrange(256))


def _positions(ram, r, near=True):
    """object positions clustered around slot 1 (the player) so distances < 5 cells happen"""
    w = lambda ad, v: ram.__setitem__(ad, v & 0xFF)
    px, py = r.randrange(0x40, 0x7C0), r.randrange(0x10, 0xE0)
    pyh = r.randrange(0, 4)
    for x in range(32):
        if near and r.random() < 0.8:
            ox, oy = px + r.randrange(-120, 120), py + r.randrange(-100, 100)
            oyh = pyh
            if oy < 0:
                oy += 240; oyh -= 1
            elif oy >= 240:
                oy -= 240; oyh += 1
            w(0x70 + x, ox); w(0x90 + x, ox >> 8); w(0xB0 + x, oy); w(0xD0 + x, oyh)
        else:
            for c in POS_COLS:
                w(c + x, r.randrange(256))


def _poke_obj(ram, wram, r):
    _poke_common(ram, wram, r)
    _positions(ram, r)


def _poke_bg(ram, wram, r):
    _poke_obj(ram, wram, r)
    for i in range(0x40):                          # screen grid $6300 (bank<<5 | page) + visited bits $62F0
        wram[0x300 + i] = r.randrange(256)
    for i in range(0x10):
        wram[0x2F0 + i] = r.randrange(256)
    ram[0x34] = r.randrange(256); ram[0x35] = r.randrange(8); ram[0x36] = r.randrange(0xF0); ram[0x37] = r.randrange(8)


TILESETS = (0x80, 0x84, 0x88, 0x8C, 0x90, 0x94, 0x98, 0x9C, 0xA0, 0xA4, 0xA8, 0xAC)   # k1 $67 values in use


def _poke_queue(ram, wram, r):
    """s79: PPU queue ring $6200 ($0A/$0B, 8 records), BG buffers $6000/$6100, tileset $66/$67, flags"""
    _poke_bg(ram, wram, r)
    ram[0x00] = r.choice([0x88, 0x8C, 0x08, r.randrange(256)])
    ram[0x01] = r.choice([0, 0x18, 0x1E, r.randrange(256)])
    ram[0x0A], ram[0x0B] = r.randrange(8) * 4, r.randrange(8) * 4
    for i in range(8):
        wram[0x200 + 4 * i] = r.choice([0x20, 0x21, 0x22, 0x23, 0x60, r.randrange(256)]) | r.choice([0, 0, 0x80])
        wram[0x201 + 4 * i] = r.randrange(256)
        wram[0x202 + 4 * i] = r.choice([0x20, 0x1E, 0xBF, 0, r.randrange(256), r.randrange(256)])
        wram[0x203 + 4 * i] = r.choice([0xFF, 0, 0x20, r.randrange(256)])
    for i in range(0x40):
        wram[i] = r.randrange(256)
    for i in range(0x40):
        wram[0x100 + i] = r.randrange(256)
    wram[0x2FF] = r.choice([0, 0x80, r.randrange(256)])
    ram[0x66] = r.choice([0, 0, 0, r.randrange(256)])
    ram[0x67] = r.choice(TILESETS) | r.randrange(4)
    ram[0x0744] = r.choice([0, 8, r.randrange(256)])
    ram[0x2F] = r.choice([0, 0, r.randrange(256)])
    ram[0x60] = 0


def _slot(r):
    return r.randrange(0x0D, 0x1F)


def _poke_spawn(ram, wram, r):
    """s112: AreaSpawnEntry 31:E1B6 on a random 4-byte entry in WRAM $7Fxx (($10) -> it, Y = regs y), random
    area id + story flags $6480-$64FF; chest (h2) params kept inside ChestPresencePtrs ($00-$71 or b7), item
    (h4) params steered onto $4x and the areas $C2-$C4 / $F5-$F7 that test a story flag."""
    _poke_obj(ram, wram, r)
    for i in range(0x80):
        wram[0x480 + i] = r.randrange(256)
    ram[0x6C] = r.choice([0xC2, 0xC3, 0xC4, 0xF5, 0xF6, 0xF7, r.randrange(256), r.randrange(256)])
    h = r.choice([0, 1, 1, 2, 2, 3, 4, 4])
    b2 = h | (r.randrange(32) << 3)
    if h == 2:
        p = r.choice([r.randrange(0x72), r.randrange(0x72), 0x80 | r.randrange(0x80), 0x25, 0x3C, 0x3B])
    elif h == 1:                                   # NpcPresenceHi 14:86E0 has $50 entries (lists from $8780)
        p = r.randrange(0xD0)
    elif h == 4:
        p = r.choice([0x40 | r.randrange(16), 0x40 | r.randrange(16), r.randrange(256)])
    else:
        p = r.randrange(256)
    lo = r.randrange(0xF0)
    ram[0x10], ram[0x11] = lo, 0x7F
    _SPAWN['y'] = y = r.randrange(8)               # the Y register (regs lambda below)
    e = [r.randrange(256), r.randrange(256), b2, p]
    for i, v in enumerate(e):
        wram[0x1F00 + lo + y + i] = v


_SPAWN = {}


ROUTINES = {
    # name: (bank or None, addr, poke, regs(r) -> dict)
    'ObjInit':        (None, 0xC25D, _poke_obj, None),
    'PlaySound':      (None, 0xC125, _poke_obj, lambda r: {'a': r.randrange(256), 'y': r.randrange(256)}),
    'DirToTarget':    (26, 0x8409, _poke_obj, lambda r: {'x': r.randrange(32), 'y': r.randrange(32)}),
    'SpawnShot':      (26, 0x972D, _poke_obj, lambda r: {'a': r.randrange(0x60), 'x': _slot(r), 'y': r.randrange(16)}),
    'ObjBgProbe':     (26, 0x9A30, _poke_bg, lambda r: {'a': r.choice([0, 0x40, 0x80, 0xC0, r.randrange(256)]),
                                                         'x': r.randrange(32)}),
    'AiStraightMover': (26, 0xB0B5, _poke_bg, lambda r: {'x': _slot(r)}),
    'AiDirShot':      (26, 0xB0A8, _poke_bg, lambda r: {'x': _slot(r)}),
    'AiBurst':        (26, 0xACC0, _poke_obj, lambda r: {'x': _slot(r)}),
    'AiFanTurret':    (26, 0xB595, _poke_obj, lambda r: {'x': _slot(r)}),
    'AiTurret':       (26, 0xB5D9, _poke_obj, lambda r: {'x': _slot(r)}),
    # s79 (Ph7 web 4/8 (a)): PPU queue + BG streaming; PPU_ROUTINES also record CIRAM and run without NMI/IRQ
    'PpuQueueFlush':  (None, 0xC67D, _poke_queue, None),
    'BgRowRender':    (None, 0xEB91, _poke_queue, None),
    'BgColRender':    (None, 0xEC87, _poke_queue, None),
    'FullScreenDraw': (None, 0xC831, _poke_queue, lambda r: {'a': r.choice([0, 1, r.randrange(256)])}),
    # s80 (Ph7 web 5/8 (a)): the object loop + the slide/step mover it uses for knockback
    'ObjMoveSlide':   (26, 0x98D7, _poke_bg, lambda r: {'x': r.choice([0, 0, 1, r.randrange(32)])}),
    'ObjUpdateAll':   (None, 0xCCCC, _poke_bg, None),
    # s112 (gameplay task (e1)): spawn entries (web/src/game/world/spawn.js) + AI $50 (obj/ai_static.js)
    'AreaSpawnEntry': (None, 0xE1B6, _poke_spawn, lambda r: {'x': _slot(r), 'y': _SPAWN['y']}),
    'AiS0DTransform': (26, 0xA660, _poke_bg, lambda r: {'x': _slot(r)}),
}
# s81 (Ph7 web 6/8 (a)): the walker / chaser AIs (web/src/game/obj/ai_walk.js), bank 27 entries
WALK_AIS = {
    'AiWalkPause': (0x20, 0xA915), 'AiShooterWalk': (0x21, 0xA969), 'AiDormant': (0x22, 0xAB42),
    'AiChase': (0x24, 0xAA57), 'AiChaseOnscreen': (0x25, 0xA9D2), 'AiShooterChase': (0x26, 0xA9E4),
    'AiShooterChaseOn': (0x27, 0xA9DB), 'AiAimShooter': (0x28, 0xAB61), 'AiBurrower': (0x29, 0xAB9E),
    'AiLunger': (0x2A, 0xABF8), 'AiSpawnChildLunger': (0x2E, 0xA9B4), 'AiShellOnHit': (0x41, 0xB339),
}
for _n, (_id, _ad) in WALK_AIS.items():
    ROUTINES[_n] = (26, _ad, _poke_bg, lambda r: {'x': _slot(r)})
# s87 (enemy port leftovers (b)): the remaining field AIs (web/src/game/obj/ai_field.js), bank 27 entries
FIELD_AIS = {
    'AiTrapChest': (0x2B, 0xAC60), 'AiDasher': (0x44, 0xB436), 'AiHomingFlyer': (0x45, 0xB4BA),
    'AiHomingShooter': (0x48, 0xB55A), 'AiOrbiter': (0x58, 0xB6AA), 'AiPatrolGunner': (0x5C, 0xB626),
    'AiDrifter': (0x5D, 0xB690), 'AiWaitClear': (0x5E, 0xB5F3), 'AiCoin': (0x7B, 0xB778),
    # boss shot / effect AIs (web/src/game/obj/ai_shot.js)
    'AiCountdown': (0x14, 0xB2C5), 'AiCurveShot': (0x16, 0xB0F6), 'AiHoverShot': (0x17, 0xB11B),
    'AiBounceShot': (0x1B, 0xB12D), 'AiDirFrameShot': (0x1E, 0xB094), 'AiShotAnimDec': (0x1F, 0xB0A2),
}
for _n, (_id, _ad) in FIELD_AIS.items():
    ROUTINES[_n] = (26, _ad, _poke_bg, lambda r: {'x': _slot(r)})
# s93 (boss / effect AI port (a)): web/src/game/obj/ai_effect.js; Y = id*2 as ObjUpdateAll's TAY 30:CD07 leaves it
EFFECT_AIS = {'AiPlayerFollowC': (0x04, 0xB2FC), 'AiStraightMoverTl': (0x57, 0xB0BE), 'AiMpDrain': (0x76, 0xA4DC)}
for _n, (_id, _ad) in EFFECT_AIS.items():
    ROUTINES[_n] = (26, _ad, _poke_bg, lambda r, _i=_id: {'x': _slot(r), 'y': (_i << 1) & 0xFF})
# s97 (boss / final-scene AI port (a)): AI $70 (web/src/game/obj/ai_final.js); Y = $E0 as ObjUpdateAll leaves it
ROUTINES['Ai70PhaseDispatch'] = (26, 0xBA2E, _poke_bg, lambda r: {'x': _slot(r), 'y': 0xE0})
# s111 (gameplay frames (e2)): the player's two objects (web/src/game/obj/player.js); X = slot, Y = id*2
PLAYER_AIS = {'PlayerUpdate': (0x01, 0x9B45, 1), 'PlayerMoveLocked': (0x02, 0x9D81, 0), 'PlayerMove': (0x03, 0x9D9A, 0)}
for _n, (_id, _ad, _x) in PLAYER_AIS.items():
    ROUTINES[_n] = (26, _ad, _poke_bg, lambda r, _i=_id, _s=_x: {'x': _s, 'y': (_i << 1) & 0xFF})
# phases whose capture can finish without an NMI (3 = FadeIn's WaitFrame; 7 / 11 are null; 1's table end is
# steered off ShowMessage)
FINAL_PHASES = (0, 1, 1, 2, 4, 5, 5, 6, 6, 8, 8, 9, 10, 12, 12, 12)
# s100 (boss AI port, rest (b)): bank 15 bosses (web/src/game/obj/ai_boss.js). The bank is (R6, R7) = ($1A, $0F):
# ObjUpdateAll's SwitchBank16($0D) then SetR7($0F) for ids $6x. name: (id, entry, phases to pick from)
BOSS_AIS = {'BossAiE0': (0x60, 0xA477, range(8)), 'BossHomingShot': (0x61, 0xA5EE, (0,)),
            'BossAiE3': (0x63, 0xA95D, range(5)), 'BossRockShot': (0x64, 0xAB3E, (0, 0, 1, 2, 2)),
            # s104: $E6 phases 2/3 are null (15:0000), $E8 phase 2 = 15:B051
            'BossAiE6': (0x66, 0xAC7B, (0, 1, 1)), 'AiBoss88': (0x67, 0xADD4, range(6)),
            'BossAiE8': (0x68, 0xAF48, (0, 1, 1, 2, 2))}
for _n, (_id, _ad, _p) in BOSS_AIS.items():
    ROUTINES[_n] = ((26, 15), _ad, _poke_bg, lambda r: {'x': _slot(r)})
# AI ids the port has (web/src/game/obj/ai_static.js, ai_walk.js, ai_field.js) + ids whose ObjAiTable 27:A314 entry is 0
PORTED_AIS = (0x10, 0x11, 0x2C, 0x4C, 0x4D, 0x4E) + tuple(i for i, _ in WALK_AIS.values()) + \
    tuple(i for i, _ in FIELD_AIS.values()) + tuple(i for i, _ in EFFECT_AIS.values()) + \
    tuple(i for i, _, _ in BOSS_AIS.values())
NULL_AIS = tuple(i for i in range(0x80) if i != 0x1A and not (_PRG[27 * 0x2000 + 0x314 + 2 * i] |
                                                               _PRG[27 * 0x2000 + 0x315 + 2 * i]))
HIT_FLAG_PARAMS = tuple(_PRG[26 * 0x2000 + 0x1045 + i] for i in range(10) if _PRG[26 * 0x2000 + 0x1045 + i])
IMMUNE_PARAMS = tuple(_PRG[26 * 0x2000 + 0x1058 + i] for i in range(20))   # HitImmuneParams 26:9058
COLL_IDS = (0x52, 0x10, 0x87, 0x0B, 0x06, 0x07, 0x7B, 0x38, 0x3C, 0x78, 0x6E, 0x30, 0xB0)


def _poke_coll(ram, wram, r):
    """s113 (gameplay frames (e3)): ObjCollideAll 26:8F6E + its hit handlers (web/src/game/obj/collide.js).
    Screen positions $05C0/$05E0 clustered so hitboxes overlap, collision groups / no-hit bits mostly open,
    the handler-relevant ids / params / equipment, HUD queue empty with rendering off (HudRedraw / HudHpBar /
    StatRecalc flush at once instead of waiting for an NMI)."""
    _poke_bg(ram, wram, r)
    cx, cy = r.randrange(0x20, 0xE0), r.randrange(0x20, 0xD0)
    for s in range(32):
        ram[0x05C0 + s] = (cx + r.randrange(-24, 24)) & 0xFF if r.random() < 0.85 else r.randrange(256)
        ram[0x05E0 + s] = (cy + r.randrange(-24, 24)) & 0xFF if r.random() < 0.85 else r.randrange(256)
        if r.random() < 0.8:
            ram[0x0380 + s] &= 0x7F
        if r.random() < 0.8:
            ram[0x0340 + s] &= 0x7F
        ram[0x03A0 + s] = r.choice([0xF0, 0x10, 0x20, 0x40, 0x80, r.randrange(256)]) & 0xF0 | r.randrange(16)
        ram[0x0420 + s] = r.choice([0, 1, 3, 0x40, 0x80, 0x20, r.randrange(256)]) | r.choice([0, r.randrange(0x20)])
        if s >= 0x0D:
            ram[0x04A0 + s] = r.choice(COLL_IDS + (r.randrange(256), 0)) | r.choice([0, 0, 0x80])
        ram[0x0540 + s] = r.choice([0, 0, 1, 1, 0xFF, 0xFF, r.randrange(256)])
        ram[0x0560 + s] = r.choice(HIT_FLAG_PARAMS + IMMUNE_PARAMS[:4] + (0, 1, 2, 3, 4, r.randrange(256)))
        ram[0x04C0 + s] = r.choice([0, 0, 0x0B, r.randrange(256)])
        ram[0x0500 + s] = r.choice([0, 0x10, 0x01, 0x22, r.randrange(256)])
        ram[0x05A0 + s] = r.choice([0, 0, 0, r.randrange(256)])
        if r.random() < 0.1:
            ram[0x0300 + s] = 0xA7
    ram[0x0421] = r.choice([0, 1, 5, 0x0F, 0x10, 0x11, r.randrange(256)])
    ram[0x0704], ram[0x0705] = r.randrange(256), r.choice([0, 0, 1, r.randrange(256)])
    ram[0x0706], ram[0x0707] = r.randrange(256), r.choice([0, 1, r.randrange(256)])
    ram[0x0711] = r.choice([0, 1, 4, 4, r.randrange(8)])
    ram[0x0713] = r.choice([0, 7, 8, r.randrange(10)])
    ram[0x0714] = r.choice([0, 4, 6, 8, r.randrange(10)])
    ram[0x0716] = r.choice([0, 0x0E, 0x10, 0x14, r.randrange(256)])
    ram[0x6C] = r.choice([0x5F, 0x1A, 0x1A, r.randrange(256), r.randrange(256)])
    ram[0x07D7] = r.choice([0, 0x80, r.randrange(256)])
    ram[0x07E8] = r.choice([0, 0x80])
    ram[0x49] = r.choice([0, 2, 4, 0x80, r.randrange(256)])
    ram[0x41] = r.choice([8, 8, r.randrange(0x28)])
    ram[0x0620] = r.choice([0, 0, r.randrange(256)])
    ram[0x0603] = r.choice([0, 0x10, r.randrange(256)])
    ram[0x0623] = r.choice([r.randrange(0x0D, 0x1F), r.randrange(256)])
    ram[0x01] &= ~0x18                             # rendering off: HUD records flush at once
    ram[0x0B] = ram[0x0A]                          # PPU queue ring empty (WaitPpuQueue / PpuQueueWaitSrc)


def _coll_target(r):
    return r.randrange(0x0D, 0x1F)


ROUTINES['ObjCollideAll'] = (26, 0x8F6E, _poke_coll, None)
ROUTINES['ObjOverlapScan'] = (26, 0x95F8, _poke_coll, lambda r: {'x': r.choice([1, 2, 3, 0x0C, r.randrange(32)])})
# HitHandlerTable 26:8FBB entries with the CollPairTable attacker slots (X) and a target Y in $0D-$1E
HIT_HANDLERS = {'HitByPlayerShot': (0x90AA, (2, 4, 5, 6, 7, 8, 9, 10, 11)), 'HitPlayer': (0x9357, (1,)),
                'TalkProbeHit': (0x94B0, (3,)), 'HitHandler3': (0x8FC4, (0x0C,))}
for _n, (_ad, _xs) in HIT_HANDLERS.items():
    ROUTINES[_n] = (26, _ad, _poke_coll, lambda r, _xs=_xs: {'x': r.choice(_xs), 'y': _coll_target(r)})
PPU_ROUTINES = ('PpuQueueFlush', 'BgRowRender', 'BgColRender', 'FullScreenDraw',
                'Ai70PhaseDispatch')          # s97: phases 0/2/9 flush PPU blocks (NMI would discard the case)


def _steer_attack(ram, r, x, dist):
    """onscreen, next Rand64 = 0, (X*16 + $08) & $1F = 0 (AiShootCheck's slot frame), slot near the player"""
    ram[0x0380 + x] &= 0x7F
    ram[0x08] = (-(x << 4)) & 0x1F | (r.randrange(8) << 5)
    ram[0x0E] = (r.choice(RAND64_ZERO) - 1) & 0x3F
    for c in POS_COLS:
        ram[c + x] = ram[c + 1]
    ram[0x70 + x] = (ram[0x71] + r.randrange(-dist, dist)) & 0xFF


FIRE_AT = {'AiShooterWalk': 0x10, 'AiShooterChase': 0x10, 'AiShooterChaseOn': 0x10, 'AiAimShooter': 0x10,
           'AiBurrower': 0x10, 'AiLunger': 9, 'AiSpawnChildLunger': 9, 'AiShellOnHit': 0x10}
_BASE = {}                                         # run(): base wram (the real screen grid for the wall steer)


def _fix_walk(name, ram, r, x, wram=None):
    """s81: keep the walker / chaser AI cases on their rare paths (attacks, pauses, detours, timers at edges)"""
    ram[0x0440 + x] = r.choice([0x40 + r.randrange(0x20), r.randrange(0x60), 0])
    ram[0x0340 + x] = r.choice([r.randrange(16), r.randrange(16), r.randrange(256)])
    ram[0x0460 + x] = r.choice([0, 6, 0x27, r.randrange(256)])
    if r.random() < 0.6:
        ram[0x0380 + x] &= 0x7F
    if r.random() < 0.3:
        ram[0x0380 + x] &= 0xF7                    # obj_flags b3 (fixed pose) clear
    ram[0x0640 + x] = r.choice([0, 0, 0, 1, 2, 8, 9, 0x0F, 0x10, 0x11, 0x1F, r.randrange(256)])
    ram[0x0660 + x] = r.randrange(256)
    ram[0x0480 + x] = r.choice([0, 1, 0x10, 0x11, r.randrange(256)])
    ram[0x04E0 + x] = r.choice([1, 2, 8, 0x0A, r.randrange(256)])
    ram[0x0620 + x] = r.choice([0, 0, 1, 0x60, r.randrange(256)])
    ram[0x0600 + x] = r.choice([0, 0, 1, r.randrange(256)])
    ram[0x06A0 + x] = r.choice([0, 0, r.randrange(256)])
    if name == 'AiShellOnHit':
        ram[0x06A0 + x] = r.choice([0, 0, 1, 0x1E, 0x1F, 0x20, 0x21, 0x3F, r.randrange(256)])
        ram[0x0340 + x] |= r.choice([0, 0x80])
    if name == 'AiSpawnChildLunger' and r.random() < 0.6:
        ram[0x06A0 + x] = 0
    if name == 'AiBurrower':
        ram[0x0620] = r.choice([0, 0, 1, r.randrange(256)])
    if name == 'AiAimShooter':
        ram[0x0660 + x] = r.choice([0, 0, 1, r.randrange(256)])
    if r.random() < 0.35:                          # start an attack: AiShootCheck / AiLungeCheck succeed
        ram[0x0640 + x] = 0
        _steer_attack(ram, r, x, 24 if name in ('AiLunger', 'AiSpawnChildLunger') else 60)
    elif r.random() < 0.3:                         # AiChase detour: blocked on its next move
        ram[0x0620 + x] = r.choice([0, 1, 0x30])
        ram[0x04E0 + x] = r.choice([2, 8, 0x0A])
    if name in FIRE_AT and r.random() < 0.3:       # the shot frame: timer one step before it, aux parity right
        ram[0x0640 + x] = FIRE_AT[name]
        ram[0x0660 + x] = r.randrange(128) * 2
        ram[0x0440 + x] = 0x40 + r.randrange(0x20)
        ram[0x0380 + x] &= 0xF7 if r.random() < 0.5 else 0xFF
        if name == 'AiShellOnHit':
            ram[0x06A0 + x] = 0
            ram[0x0340 + x] &= 0x7F
        if name == 'AiAimShooter':
            ram[0x0660 + x] = 1
        for s in r.sample(range(0x0D, 0x1F), 4):
            if s != x:
                ram[0x04A0 + s] = 0
    if wram is not None and 'wram' in _BASE and r.random() < 0.4:
        # real map: the base state's screen grid + visited bits, the object on the player's screen, speed 1-4
        wram[0x2F0:0x340] = _BASE['wram'][0x2F0:0x340]
        ram[0x90 + x], ram[0xD0 + x] = ram[0x90], ram[0xD0]
        ram[0x70 + x], ram[0xB0 + x] = r.randrange(256), r.randrange(0xF0)
        ram[0x0340 + x] = r.randrange(1, 5)
        ram[0x0360 + x] = r.randrange(8)
        ram[0x0460 + x] = r.choice([6, 0x27])
        if r.random() < 0.6:
            ram[0x0620 + x] = 0
            ram[0x0480 + x] = r.choice([5, 0x22, 0x37])  # keep obj_meta_dir (no re-aim this step)


def _rand_zero_next(ram, r):
    ram[0x0E] = (r.choice(RAND64_ZERO) - 1) & 0x3F


def _fix_field(name, ram, r, x, wram=None):
    """s87: keep the field AI cases on their rare paths (pick / dash, steer levels, shot frames, wake-up, hops)"""
    if r.random() < 0.6:
        ram[0x0380 + x] &= 0x7F                    # onscreen most of the time
    ram[0x0440 + x] = r.choice([0, 0x40 + r.randrange(0x20), r.randrange(0x60)])
    if r.random() < 0.3:
        for s in r.sample(range(0x0D, 0x1F), 5):   # free slots for the shot
            if s != x:
                ram[0x04A0 + s] = 0
    if name == 'AiDasher':
        ram[0x0340 + x] = r.choice([1, 6, 6, r.randrange(16), r.randrange(256)])
        ram[0x0600 + x] = r.choice([0, 0, 0, 1, r.randrange(256)])
        ram[0x0480 + x] = r.choice([0, 1, 2, r.randrange(256)])
    if name in ('AiHomingFlyer', 'AiHomingShooter'):
        if r.random() < 0.3:
            ram[0x0560 + x] = 1
            ram[0x04AD] = r.choice([0, 0x45, 0xC5])
        ram[0x0480 + x] = r.choice([0, 1, 0x10, 0x11, r.randrange(256)])
        ram[0x0600 + x] = r.choice([0, 1, 2, 3, 0xFF, r.randrange(256)])
        ram[0x0360 + x] = r.choice([r.randrange(16), r.randrange(256)])
        ram[0x0640 + x] = r.choice([0, 0, 0x19, 0x18, r.randrange(256)])
        if r.random() < 0.4:                       # shot frame: $08 & 7 = 0 and Rand64 = 0 next
            ram[0x08] &= 0xF8
            _rand_zero_next(ram, r)
            if ram[0x0440 + x] == 0:
                ram[0x0440 + x] = 0x40 + r.randrange(0x20)
        elif name == 'AiHomingShooter' and r.random() < 0.3:
            ram[0x0640 + x] = 0
            _steer_attack(ram, r, x, 60)
    if name == 'AiWaitClear':
        if r.random() < 0.7:
            ram[0x08] = (-(x << 4)) & 0x3F | (r.randrange(4) << 6)
        for s in range(0x0D, 0x1F):
            if r.random() < 0.15:
                ram[0x0560 + s] = 0x50
                ram[0xD0 + s] = ram[0xD0] if r.random() < 0.6 else r.randrange(256)
            elif r.random() < 0.5:
                ram[0x0560 + s] = r.randrange(256)
    if name == 'AiPatrolGunner':
        ram[0x0620 + x] = r.choice([0, 0, 0, 1, 8, 9, 0x20, r.randrange(256)])
        ram[0x70 + x] = r.choice([0x35, 0x36, 0xE7, 0xE8, r.randrange(256)])
        ram[0x0480 + x] = r.choice([1, 0x21, r.randrange(256)])
        ram[0x0360 + x] = r.choice([r.randrange(8), r.randrange(256)])
    if name == 'AiDrifter':
        ram[0x0480 + x] = r.choice([1, 0x41, r.randrange(256)])
        ram[0x0360 + x] = r.choice([r.randrange(8), r.randrange(256)])
    if name == 'AiOrbiter':
        ram[0x04E0 + x] = r.choice([1, 0x41, r.randrange(256)])
        if r.random() < 0.5:
            _rand_zero_next(ram, r)
    if name == 'AiTrapChest':
        ram[0x0600 + x] = r.choice([0, 0, 1, r.randrange(256)])
        ram[0x0620 + x] = r.choice([1, 0x41, 0x81, r.randrange(256)])
        ram[0x0360 + x] = r.choice([r.randrange(8), r.randrange(256)])
        if r.random() < 0.4:                       # the player in the chest's cell (DirToTarget distance 0)
            for c in POS_COLS:
                ram[c + x] = ram[c + 1]
            ram[0x70 + x] = (ram[0x71] + r.randrange(-8, 8)) & 0xFF
    if name == 'AiCoin':
        ram[0x0480 + x] = r.choice([1, 2, 0x30, 0x31, 0xC0, 0xC1, 0xC2, r.randrange(256)])
    if name in ('AiCountdown', 'AiCurveShot', 'AiHoverShot', 'AiBounceShot', 'AiDirFrameShot', 'AiShotAnimDec'):
        ram[0x04E0 + x] = r.choice([0, 1, 2, 3, 0x80, 0x81, r.randrange(256)])
        ram[0x0480 + x] = r.choice([1, 2, 0x10, 0x11, r.randrange(256)])
        ram[0x0600 + x] = r.choice([0, 1, 4, r.randrange(256)])
        ram[0x0460 + x] = r.choice([0, 0, 6, 0x27, r.randrange(256)])
        ram[0x0360 + x] = r.choice([r.randrange(16), r.randrange(256)])
    if wram is not None and 'wram' in _BASE and name in ('AiDasher', 'AiTrapChest', 'AiBounceShot') \
            and r.random() < 0.4:
        wram[0x2F0:0x340] = _BASE['wram'][0x2F0:0x340]   # the real map, the object on the player's screen
        ram[0x90 + x], ram[0xD0 + x] = ram[0x90], ram[0xD0]
        ram[0x70 + x], ram[0xB0 + x] = r.randrange(256), r.randrange(0xF0)
        ram[0x0460 + x] = r.choice([6, 0x27])


def _fix_effect(name, ram, r, x, wram):
    """s93: AI $76 sound / MP frame + button + borrow edges; AI $04 slot $0C timer edges; AI $57 = mover tail"""
    if name == 'AiMpDrain':
        if r.random() < 0.6:
            ram[0x08] &= 0xF0                      # the every-16th-frame drain
        ram[0x0708] = r.choice([0, 1, 2, r.randrange(256)])
        ram[0x4B] = r.choice([0, 0, r.randrange(256)])
        ram[0x04E0 + x] = r.choice([0, 1, 2, 3, 4, r.randrange(256)])
        ram[0x01] = r.choice([ram[0x01], ram[0x01] & ~0x18])   # rendering off: HudRedraw flushes at once
    if name == 'AiPlayerFollowC':
        ram[0x04EC] = r.choice([0, 1, 2, 0x80, 0x81, r.randrange(256)])
        ram[0x062C] = r.choice([0, 1, 2, r.randrange(256)])
    if name == 'AiStraightMoverTl':
        if r.random() < 0.7:
            ram[0x0380 + x] &= 0x7F
        ram[0x0480 + x] = r.choice([1, 2, r.randrange(256)])
        ram[0x0460 + x] = r.choice([0, 0, 6, 0x27, r.randrange(256)])


def _fix_player(name, ram, r, x, wram):
    """s111: player AI cases on their edges: status / jump timer / swing phase / charge / magic / MP / conveyor"""
    c = r.choice
    ram[0x0710] = c([0, 0, 0, 1, 2, 3, 4, 0x40, 0x40, 0x80, 0x90, 0xA0, 0xB0, r.randrange(256)])
    ram[0x0620] = c([0, 0, 0, 1, 2, 0x0F, 0x11, 0x12, 0x13, 0x1E, 0x1F, 0x20, 0x21, 0x22, r.randrange(0x31),
                     r.randrange(256)])
    ram[0x0621] = c([0, r.randrange(256)])
    ram[0x43] = c([0, 0x40, 0x80, 0xC0, r.randrange(256)])
    ram[0x4B] = c([0, 0, 0x40, 0x80, 0xC0, r.randrange(256)])
    ram[0x49] = c([0, 1, 2, 3, 4, 4, 5, 6, 7, 0x80, 0xFF, 0xFF, r.randrange(256)])
    ram[0x0712] = c([r.randrange(12), r.randrange(12), 0, 8, 9, r.randrange(256)])
    ram[0x0716] = c([0, 0, 0x0F, 0x12, 0x13, r.randrange(256)])
    ram[0x0708] = c([0, 1, 2, 3, 4, 8, 0x10, 0x14, 0x18, 0x28, 0x29, 0x40, r.randrange(256)])
    ram[0x0711] = c([0, 1, 2, 3, 4, 5, r.randrange(256) & 7])
    ram[0x0719] = r.randrange(3)
    ram[0x06C0] = c([0, 3, 7, 8, 0x0F, 0x10, 0x17, 0x18, 0x19, 0x1F, r.randrange(256)])
    ram[0x0600] = c([0, 0, 1, 3, 4, 0x0B, 0x0C, 0x0F, 0x10, 0x10, 0x11, 0x12, 0x12, 0x13, r.randrange(256)])
    ram[0x0680] = c([0, 4, 0x84, 0x80, 0x83, r.randrange(256)])
    ram[0x06A0] = c([0, 0, 1, 8, r.randrange(256)])
    ram[0x0660] = c([0, 1, 5, 6, 7, 8, 9, r.randrange(256)])
    ram[0x0641] = c([0, 0, 0x20, 0x02, 0x06, 0x80, r.randrange(256)])
    ram[0x0561] = c([0, 0, 0x02, 0x06, 0x80, r.randrange(256)])
    ram[0x6C] = c([0x58, 0x5F, 0x5A, 0x1A, 0, r.randrange(256)])
    ram[0x41] = c([8, 8, 8, r.randrange(256)])
    ram[0x0715] = c([0, r.randrange(256)])
    ram[0x0714] = c([0, 0, 4, r.randrange(256)])
    ram[0x04A0] = c([3, 3, 2, 0x0B])
    ram[0x04A1] = 1
    ram[0x04AC] = c([0, 0, r.randrange(256)])
    ram[0x06E0] = c([0, 2, 4, 6, 0xFF, r.randrange(256)])
    ram[0x08] = c([0, 4, 8, 0x10, 0x80, r.randrange(256)])
    ram[0x0360] = c([r.randrange(8), r.randrange(256)])
    ram[0x0340] = c([6, 0x06, 0x16, r.randrange(256)])
    ram[0x0341] = c([6, r.randrange(256)])
    ram[0x0380] = c([0, 0x40, r.randrange(256)])
    ram[0x0460] = c([0x32, 0x34, 0x72, r.randrange(256)])
    hm = ram[0x03C0] = c([0x30, 0x40, r.randrange(1, 256)])
    ram[0x03C1] = c([hm, (hm - 1) & 0xFF, (hm - 2) & 0xFF, (hm - 4) & 0xFF, r.randrange(256)])
    if r.random() < 0.7:                           # the ground body near the camera so ObjScreenEdge passes
        ram[0x70] = (ram[0x02] + r.randrange(0x10, 0xF0)) & 0xFF
        ram[0xB0] = (ram[0x04] + r.randrange(0x20, 0xC8)) % 0xF0
    if r.random() < 0.5:
        for s in r.sample(range(2, 0x1F), 12):     # free slots for the slash / charged / magic shots
            ram[0x04A0 + s] = 0
    for i in range(8):                             # PpuQueueWaitSrc $60 (HudChargeMeter / HudHpBar) / $7B (HudRedraw)
        if wram[0x203 + 4 * i] in (0x60, 0x7B):
            wram[0x203 + 4 * i] = 0xFF


def _fix_final(ram, r, x, wram):
    """s97: AI $70 cases, one phase each, kept off the NMI waits (FadeIn, ShowMessage) and on each phase's edges"""
    p = r.choice(FINAL_PHASES)
    ram[0x0600 + x] = p
    ram[0x0B] = ram[0x0A]                          # WaitPpuQueue: ring empty
    ram[0x01] &= ~0x18                             # rendering off: PpuQueueBlock flushes each record at once
    ram[0x0340 + x] = r.choice([0, 0x80, r.randrange(256)])     # hit flash (26:9B07) or PaletteBuild
    ram[0x08] = r.choice([0, 1, 4, 5, 8, 9, r.randrange(256)])
    if r.random() < 0.3:
        for s in r.sample(range(0x0D, 0x1F), 4):   # free slots for the shots
            if s != x:
                ram[0x04A0 + s] = 0
    if p == 0:
        ram[0x04AD] = r.choice([0, 0, 0, r.randrange(256)])
        if r.random() < 0.5:                       # BA9A column starts land on $7C cells
            wram[0:0xDC] = bytes([0x7C]) * 0xDC
    if p == 1:
        ram[0x06E0 + x] = r.choice([0x80, 0x81, 0xFF, r.randrange(256)])
        ram[0x04E0 + x] = r.choice([0x0F, 0x1F, 0x2F, 0x3F, 0x4F, 0x5F, 0x5F, r.randrange(256)])
        ram[0x06C0 + x] = r.choice([0, 1, 0x7F, r.randrange(256)]) & 0x7F   # b7 set = ShowMessage (waits)
    if p in (4, 5, 6):
        ram[0x06E0 + x] = r.choice([1, 1, 2, 0x20, r.randrange(256)])
        ram[0x04E0 + x] = r.choice([0, 1, 0x1E, 0x1F, r.randrange(256)])
        ram[0x0620 + x] = r.choice([0, 0x0F, 0xFF, r.randrange(256)])
        ram[0x064E] = r.choice([0, 1, 3, r.randrange(256)])
    if p == 8:
        ram[0x034D] = r.choice([0, 0x80, r.randrange(256)])
        ram[0x0640 + x] = r.randrange(256)
        ram[0x70] = r.randrange(256)
    if p == 10:
        ram[0x0620 + x] = r.choice([0xFF, 0xFF, r.randrange(256)])
    if p == 12:
        ld = ram[0x06A0 + x] = r.randrange(0x20)  # 27:BDA9 LDY obj_param2,X: the leader slot
        ram[0x04A0 + ld] = r.choice([0x2E, 0x2E, 0x2E, r.randrange(256)])
        ram[0x05A0 + ld] = r.choice([0, 0, 1, r.randrange(256)])
        ram[0x04E0 + x] = r.choice([4, 0x84, 0x08, r.randrange(256)])
        ram[0x0380 + x] = r.choice([0, 0x80, r.randrange(256)])
        ram[0x0640 + ld] = r.choice([0, r.randrange(256)])


def _fix_boss(name, ram, r, x, wram):
    """s100: bank 15 boss cases, one phase each, steered onto their edges (camera aligned, Rand64 = 0, timers at
    0/1/7/8/$20, the slam frame, offscreen, knockback, the arena edges $30/$48/$D0)"""
    _id, _ad, phases = BOSS_AIS[name]
    p = ram[0x0600 + x] = r.choice(tuple(phases))
    ram[0x0340 + x] = r.choice([0, 0, 0x80, r.randrange(16), r.randrange(256)])
    ram[0x0380 + x] = r.choice([0, 1, 9, 0x80, r.randrange(256)])
    ram[0x07D5] = r.choice([0, 1, r.randrange(256)])
    ram[0x08] = r.choice([0, 1, 7, 8, 0x9F, 0xA0, r.randrange(256)])
    ram[0x0480 + x] = r.choice([0, 1, 2, 8, 9, 0x20, 0x21, 0x22, r.randrange(256)])
    ram[0x04E0 + x] = r.choice([0, 1, 0x0F, 0x10, 0x7F, 0x80, 0x88, 0x8F, r.randrange(256)])
    ram[0x0660 + x] = r.choice([0, 0x0A, 0x0B, 0x0C, r.randrange(256)])
    ram[0x03C0 + x] = r.choice([0, 0x28, 0x3F, 0x50, r.randrange(256)])
    ram[0x0640 + x] = r.choice([r.randrange(16), r.randrange(256)])
    ram[0x0360 + x] = r.choice([r.randrange(8), r.randrange(16), r.randrange(256)])
    ram[0x0680 + x] = r.choice([0, 1, 3, 0xFD, 0xFE, 0xFF, r.randrange(256)])
    ram[0x06A0 + x] = r.choice([0, 2, 3, 0xFC, 0xFD, r.randrange(256)])
    ram[0x6C] = r.choice([0x0A, 0x28, 0xA9, 0xAC, 0xB9, 0xB6, r.randrange(256)])
    if _id in (0x66, 0x67, 0x68):                  # s104: obj_timer2 / act_timer / aux edges, $08 & $3F = 0
        ram[0x0620 + x] = r.choice([0, 0, 1, 2, 8, 9, 0x10, 0x3F, 0x40, 0x50, 0x0F, r.randrange(0x14),
                                    r.randrange(256)])
        ram[0x0640 + x] = r.choice([1, 2, 0x10, 0x11, 0x20, 0x21, 0x50, r.randrange(256)])
        ram[0x0660 + x] = r.choice([0, 1, 2, 3, r.randrange(256)])
        ram[0x04E0 + x] = r.choice([0, 1, 2, 3, 4, 8, 9, 0x0E, 0x0F, 0x28, r.randrange(256)])
        if r.random() < 0.3:
            ram[0x08] = r.choice([0, 0x40, 0x80, 0xC0])
        if r.random() < 0.5:                       # $E7 arena edges
            ram[0x90 + x], ram[0xD0 + x] = ram[0x90], ram[0xD0]
            ram[0xB0 + x] = r.choice([0x14, 0x15, 0x16, 0xCF, 0xD0, r.randrange(0x18, 0xCE)])
            ram[0x70 + x] = r.choice([0x1F, 0x20, 0x21, 0xDF, 0xE0, r.randrange(256)])
        if _id == 0x66 and r.random() < 0.6:       # $E6: intro end (timer2 $3F) / fly turns (timer2 0, anim 8n+1)
            ram[0x0380 + x] &= 0x7F
            ram[0x07D5] = 1
            ram[0x0620 + x] = 0x3F if p == 0 else r.choice([0, 0, 1, 8])
            ram[0x04E0 + x] = r.choice([1, 9, 0x11, r.randrange(256)])
        if _id == 0x67 and p == 3 and r.random() < 0.5:   # $E7: the bounce's act_timer end
            ram[0x0620 + x] |= 1
            ram[0x0640 + x] = 1
    if r.random() < 0.4:
        _rand_zero_next(ram, r)
    if r.random() < 0.5:                           # the boss on the player's screen, near the arena edges
        ram[0x90 + x], ram[0xD0 + x] = ram[0x90], ram[0xD0]
        ram[0x70 + x] = r.choice([0x2F, 0x30, 0xCF, 0xD0, r.randrange(256)])
        ram[0xB0 + x] = r.choice([0x47, 0x48, r.randrange(0xF0)])
    if r.random() < 0.5:
        for s in r.sample(range(0x0D, 0x1F), 9):   # free slots for the shots
            if s != x:
                ram[0x04A0 + s] = 0
    if r.random() < 0.3:
        for s in range(0x0D, 0x1F):                # ClearEnemySlots' keep list
            if r.random() < 0.3:
                ram[0x04A0 + s] = r.choice([0x78, 0x7F, 0x30, 0x39])


def _fix_case(name, ram, r, regs, wram=None):
    """routine-specific pokes that keep a case on a meaningful path"""
    x = regs.get('x')
    if name == 'ObjInit':
        ram[0x10] = _slot(r) if r.random() < 0.9 else r.randrange(32)
        ram[0x11] = r.choice([1, 0xFF, r.randrange(256), r.randrange(256), r.randrange(256)])
    if name == 'AiS0DTransform':                   # s112: timer2 steps 0 / 1 / >= 2, act_timer 1 = the step 1 end
        ram[0x0620 + x] = r.choice([0, 1, 1, 2, r.randrange(256)])
        ram[0x0640 + x] = r.choice([1, 1, r.randrange(256)])
        ram[0x04CD] = r.choice([8, r.randrange(256)])
        if r.random() < 0.6:
            ram[0x0380 + x] &= 0x7F
    if name in ('AiFanTurret',) and r.random() < 0.6:
        ram[0x0640 + x] = r.choice([0, 1, 0x08, 0x09, 0x10, 0x11, 0x18, 0x19, 0x1F])
    if name in ('AiFanTurret', 'AiTurret') and r.random() < 0.4:
        # steer AiShootCheck into its attack: onscreen, the 1-in-32 frame slot, Rand64 = 0 next, player close
        if name == 'AiFanTurret':
            ram[0x0640 + x] = 0
        ram[0x0380 + x] &= 0x7F
        ram[0x08] = (-(x << 4)) & 0x1F | (r.randrange(8) << 5)
        ram[0x0E] = (r.choice(RAND64_ZERO) - 1) & 0x3F
        for c in POS_COLS:
            ram[c + x] = ram[c + 1]
        ram[0x70 + x] = (ram[0x71] + r.randrange(-40, 40)) & 0xFF
    if name in ('AiStraightMover', 'AiDirShot'):
        if r.random() < 0.7:
            ram[0x0380 + x] &= 0x7F                # onscreen most of the time
        if r.random() < 0.5:
            ram[0x0460 + x] = 0
        ram[0x0480 + x] = r.choice([1, 2, r.randrange(256)])
        ram[0x0340 + x] = r.choice([r.randrange(16), r.randrange(256)])
    if name == 'AiTurret' and r.random() < 0.5:
        ram[0x04E0 + x] = r.choice([0x01, 0x11, 0x21])
    if name in ('BgRowRender', 'BgColRender', 'FullScreenDraw'):
        # no ring slot may hold the routine's src byte: 30:C72B scans all 8 slots (stale ones too) and would spin
        src = 0x20 if name == 'BgColRender' else 0
        for i in range(8):
            if wram[0x203 + 4 * i] == src:
                wram[0x203 + 4 * i] = 0xFF
    if name == 'FullScreenDraw':
        ram[0x0B] = ram[0x0A]                      # WaitPpuQueue: ring empty
        ram[0x01] &= ~0x18                         # rendering off: every row is flushed at once (else it waits for NMI)
    if name == 'ObjMoveSlide':
        ram[0x0360 + x] = r.choice([r.randrange(8), r.randrange(256)])
        ram[0x0340 + x] = r.choice([8, r.randrange(16), r.randrange(256)])
        ram[0x0460 + x] = r.choice([0, 6, 0x27, 0xFF, r.randrange(256)])
        ram[0x0710] = r.choice([0, 0x40, r.randrange(256)])
        if x == 0 and r.random() < 0.6:            # near the camera so ObjScreenEdge passes sometimes
            ram[0x70] = (ram[0x02] + r.randrange(0x08, 0x100)) & 0xFF
            ram[0xB0] = (ram[0x04] + r.randrange(0x10, 0xD8)) % 0xF0
    if name == 'ObjUpdateAll':
        for s in range(32):
            ram[0x04A0 + s] = r.choice([0, 0, r.choice(PORTED_AIS), r.choice(NULL_AIS)]) | r.choice([0, 0, 0x80])
            if r.random() < 0.3:
                ram[0x0340 + s] = r.choice([0x80, 0xE0, 0xF0, r.randrange(256)])
            elif r.random() < 0.8:
                ram[0x0340 + s] &= 0x0F
            ram[0x05A0 + s] = r.choice([0, 0, 0, 1, 2, r.randrange(256)])
            ram[0x0540 + s] = r.choice([0, 1, 1, r.randrange(256)])
            ram[0x0560 + s] = r.choice(HIT_FLAG_PARAMS + (r.randrange(256),))
            if ram[0x04A0 + s] & 0x7F in (0x4C, 0x4D, 0x4E, 0x2C, 0x21, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2E, 0x41):
                ram[0x0440 + s] = r.choice([0x40 + r.randrange(0x20), r.randrange(0x60)])
            if ram[0x04A0 + s] & 0x7F in (0x10, 0x11) and r.random() < 0.5:
                ram[0x0380 + s] &= 0x7F
            for _bi, _ba, _bp in BOSS_AIS.values():    # s100: a boss phase past its run jumps through garbage
                if ram[0x04A0 + s] & 0x7F == _bi:
                    ram[0x0600 + s] = r.choice(tuple(_bp))
        ram[0x0710] = r.choice([0, 0x40, r.randrange(256)])
    if name in WALK_AIS:
        _fix_walk(name, ram, r, x, wram)
    if name in FIELD_AIS:
        _fix_field(name, ram, r, x, wram)
    if name in EFFECT_AIS:
        _fix_effect(name, ram, r, x, wram)
    if name == 'Ai70PhaseDispatch':
        _fix_final(ram, r, x, wram)
    if name in BOSS_AIS:
        _fix_boss(name, ram, r, x, wram)
    if name in PLAYER_AIS:
        _fix_player(name, ram, r, x, wram)
    if name in ('AiMpDrain', 'ObjUpdateAll', 'Ai70PhaseDispatch'):
        for i in range(8):                         # HudRedraw's PpuQueueWaitSrc($7B) would spin until an NMI
            if wram[0x203 + 4 * i] == 0x7B:
                wram[0x203 + 4 * i] = 0xFF
    if name == 'ObjCollideAll' or name in HIT_HANDLERS:
        for i in range(8):                         # s113: + HudHpBar's PpuQueueWaitSrc($60)
            if wram[0x203 + 4 * i] in (0x7B, 0x60):
                wram[0x203 + 4 * i] = 0xFF
    if name in ('AiFanTurret', 'AiTurret', 'AiBurst', 'SpawnShot'):
        ram[0x0440 + x] = r.choice([0x40 + r.randrange(0x20), r.randrange(0x60)])
        if 'a' in regs:
            regs['a'] = ram[0x0440 + x]


OUTER, INNER = 0xFFFE, 0xFFFF


def call2(nes, addr, bank, regs, max_instr=200_000):
    """Call like ObjUpdateAll's JSR $0010 -> JMP handler, with a caller frame under the return address:
    push OUTER-1 (the caller's caller) then INNER-1. Returns 'rts' when the routine returns normally,
    'popped' when it pulled its return address (AiShootCheck's PLA PLA) and returned to the caller's caller."""
    cpu, m = nes.cpu, nes.mapper
    if bank is not None and 0x8000 <= addr < 0xC000:
        m.regs[6], m.regs[7] = bank if isinstance(bank, tuple) else (bank & ~1, bank | 1)
        m._update_prg()
    for r, v in regs.items():
        setattr(cpu, r, v & 0xFF)
    s0 = cpu.s
    for v in (OUTER - 1, INNER - 1):
        cpu._push(v >> 8)
        cpu._push(v & 0xFF)
    cpu.pc = addr
    for _ in range(max_instr):
        if cpu.pc == INNER and cpu.s == (s0 - 2) & 0xFF:
            cpu.s = s0
            return 'rts'
        if cpu.pc == OUTER and cpu.s == s0:
            return 'popped'
        nes.step()
    raise Stop(f'call ${addr:04X}: no return after {max_instr} instructions')


def _diff(before, ram, wram, sp):
    lo, hi = 0x100 + max(0, sp - STACK_SCRATCH), 0x100 + min(0xFF, sp + 4)
    out = [[i, ram[i]] for i in range(0x800) if ram[i] != before[i] and not lo <= i <= hi]
    out += [[0x6000 + i, wram[i]] for i in range(0x2000) if wram[i] != before[0x800 + i]]
    return out


def run(nes, a):
    nes.run_frames(a.frames)
    base = {}

    def grab(n):
        base['ram'], base['wram'] = bytes(n.bus.ram), bytes(n.bus.wram)
        base['ciram'] = bytes(n.ppu.ciram)
        raise Stop('base')
    nes.on_exec(None, OBJ_UPDATE_ALL, grab)
    try:
        nes.run_frames(600)
    except Stop:
        pass
    if 'ram' not in base:
        sys.exit('ObjUpdateAll 30:CCCC not reached within 600 frames of the start state')
    nes.clear_hooks()
    _BASE['wram'] = base['wram']
    ints = Interrupts(nes)
    cases = []
    names = a.routine.split(',')
    for name in names:
        bank, addr, poke, regf = ROUTINES[name]
        seed, got = a.seed, 0
        while got < a.cases:
            r = random.Random(f'{name}:{seed}')
            seed += 1
            ram, wram = bytearray(base['ram']), bytearray(base['wram'])
            poke(ram, wram, r)
            regs = regf(r) if regf else {}
            _fix_case(name, ram, r, regs, wram)
            if isinstance(bank, tuple):           # s100: + SetR7($0F) for the bank 15 AIs ($50 ends as 7)
                ram[0x6E], ram[0x6F], ram[0x50] = bank[0], bank[1], 7
            elif bank is not None:                # shadows as ObjUpdateAll's SwitchBank16($0D) leaves them
                ram[0x6E], ram[0x6F], ram[0x50] = bank & ~1, bank | 1, 6
            nes.bus.ram[:] = ram
            nes.bus.wram[:] = wram
            ppu_case = name in PPU_ROUTINES
            if ppu_case:                          # s79: fresh CIRAM, PPU latch clear, banks = the RAM shadows,
                nes.ppu.ciram[:] = base['ciram']  # no NMI (ppu.cpu unhooked) and no mapper IRQ during the call
                nes.ppu.w = 0
                nes.ppu.ctrl = ram[0x00]          # (the port starts from the same: ctrl = ppuctrl_shadow)
                nes.ppu.mask = 0                  # PPUDATA as in vblank / rendering off (no mid-frame $2007 glitch)
                nes.mapper.regs[6], nes.mapper.regs[7] = ram[0x6E], ram[0x6F]
                nes.mapper._update_prg()
                nes.mapper.irq_enabled = nes.mapper.irq_pending = False
                nes.cpu.irq_flags = 0
                ppu_cpu, nes.ppu.cpu = nes.ppu.cpu, None
            i0 = ints.n
            sp = (nes.cpu.s - 4) & 0xFF          # S at the first instruction (2 return addresses pushed)
            try:
                how = call2(nes, addr, bank, regs, 400_000 if ppu_case else 200_000)
            except Stop:
                continue
            finally:
                if ppu_case:
                    nes.ppu.cpu = ppu_cpu
            if ints.n != i0:
                continue
            case = {'name': f'{name}#{seed - 1}', 'routine': name, 'regs': regs, 'sp': sp, 'ret': how,
                    'ram': bytes(ram).hex(),
                    'wram': [[0x6000 + i, v] for i, v in enumerate(wram) if v != base['wram'][i]],
                    'after': _diff(bytes(ram) + bytes(wram), nes.bus.ram, nes.bus.wram, sp)}
            if ppu_case:
                case['ciram_after'] = [[i, v] for i, v in enumerate(nes.ppu.ciram) if v != base['ciram'][i]]
                case['ppu_ctrl'] = nes.ppu.ctrl
            cases.append(case)
            got += 1
    return cases, base


def main(argv=None):
    ap = argparse.ArgumentParser(prog='python3 -m tools.emu.routine_capture')
    ap.add_argument('--load-state')
    ap.add_argument('--frames', type=int, default=0)
    ap.add_argument('--routine', default=','.join(ROUTINES))
    ap.add_argument('--cases', type=int, default=200)
    ap.add_argument('--seed', type=int, default=1)
    ap.add_argument('--out')
    ap.add_argument('--list', action='store_true')
    a = ap.parse_args(argv)
    if a.list:
        for n, (b, ad, _, _) in ROUTINES.items():
            print(f'{n:16} {"fixed" if b is None else b:>5}:{ad:04X}')
        return
    if not a.load_state or not a.out:
        ap.error('--load-state and --out are required')
    nes = NES()
    state.load(nes, a.load_state)
    cases, base = run(nes, a)
    d = os.path.dirname(a.out)
    if d:
        os.makedirs(d, exist_ok=True)
    src = f'fuzz {os.path.basename(a.load_state)} frames={a.frames} seed={a.seed}'
    out = {'source': src, 'wram_base': base['wram'].hex(), 'cases': cases}
    if any('ciram_after' in c for c in cases):
        out['ciram_base'] = base['ciram'].hex()
        out['mirroring'] = nes.mapper.mirroring
    with open(a.out, 'w') as f:
        json.dump(out, f, separators=(',', ':'))
    print(f'{len(cases)} case(s), {sum(len(c["after"]) for c in cases)} changed bytes -> {a.out}')


if __name__ == '__main__':
    main()
