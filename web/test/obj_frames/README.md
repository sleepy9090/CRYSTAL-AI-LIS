# web/test/obj_frames (s55)

Routine-level fuzz cases for the object code, captured by `tools/emu/routine_capture.py` and replayed by
`node web/test/obj_check.mjs`. Each case has RAM (2KB) + WRAM (sparse vs `wram_base`) at a routine's first
instruction, its registers, how it returned (`rts`, or `popped` when `AiShootCheck` pulled the caller's return
address), and the bytes it changed. The port must leave the same RAM + WRAM (stack scratch excluded) and return the same way.

- `fuzz_static.json`: 100 cases each for `ObjInit`, `PlaySound`, `DirToTarget`, `SpawnShot`, `ObjBgProbe`,
  `AiStraightMover`, `AiDirShot`, `AiBurst`, `AiFanTurret`, `AiTurret` (base = the `ObjUpdateAll` entry after
  loading `gameplay_first.state`; `$6E/$6F/$50` set as `ObjUpdateAll`'s `SwitchBank16($0D)` leaves them).

- `fuzz_bg.json` (s79): 100 cases each for `PpuQueueFlush` `30:C67D`, `BgRowRender` `31:EB91`, `BgColRender`
  `31:EC87`, `FullScreenDraw` `30:C831` (`world/ppuqueue.js`, `world/bgstream.js`). These also carry `ciram_base` /
  `mirroring` and per case `ciram_after` + `ppu_ctrl`: the check compares the nametables and PPUCTRL too. They run
  with NMI/IRQ suppressed and the PPU's real mask = 0 (PPUDATA as in vblank), banks = the `$6E/$6F` shadows.

- `fuzz_update.json` (s80): 200 cases each for `ObjMoveSlide` `26:98D7` (`obj/move.js`, slot 0 included for
  `ObjScreenEdge`) and the whole object loop `ObjUpdateAll` `30:CCCC` (`obj/objects.js`; knockback, `obj_freeze`
  + `HitSetFlag`, AI dispatch). Every slot gets AI 0, a ported id or an id with a null `ObjAiTable` entry (b7 random),
  but objects spawned *during* the pass (shots, death types) can still run an unported AI: when the port logged
  "AI $xx not ported" in a differing case, `obj_check.mjs` counts it as a gap, not a failure (`--verbose` lists them).
  The gap count drops as AIs are ported; a gap case that still differs after its AI exists becomes a failure.

- `fuzz_walk.json` (s81): 100 cases each for the walker / chaser AIs in `obj/ai_walk.js` (`WALK_AIS` in
  `routine_capture.py`: `AiWalkPause` `$20`, `AiShooterWalk` `$21`, `AiDormant` `$22`, `AiChase` `$24`,
  `AiChaseOnscreen` `$25`, `AiShooterChase` `$26`, `AiShooterChaseOn` `$27`, `AiAimShooter` `$28`, `AiBurrower` `$29`,
  `AiLunger` `$2A`, `AiSpawnChildLunger` `$2E`, `AiShellOnHit` `$41`). `_fix_walk` steers attack starts, the shot
  frame (with free slots), the shell timers, and the base state's real screen grid (so `ObjMoveSlide` hits walls and
  AiChase enters its detour). `python3 tools/obj_case_stats.py web/test/obj_frames/fuzz_walk.json --col 0620=60`
  shows how many cases take each path.

- `fuzz_field.json` (s87): 100 cases each for the field AIs in `obj/ai_field.js` (`FIELD_AIS`: `AiTrapChest` `$2B`,
  `AiDasher` `$44`, `AiHomingFlyer` `$45`, `AiHomingShooter` `$48`, `AiOrbiter` `$58`, `AiPatrolGunner` `$5C`,
  `AiDrifter` `$5D`, `AiWaitClear` `$5E`, `AiCoin` `$7B`) and the shot / effect AIs in `obj/ai_shot.js`
  (`AiCountdown` `$14`, `AiCurveShot` `$16`, `AiHoverShot` `$17`, `AiBounceShot` `$1B`, `AiDirFrameShot` `$1E`,
  `AiShotAnimDec` `$1F`); steered by `_fix_field`.

- `fuzz_shot.json` (s166): 300 cases each of AI `$15` `AiTrailShot` `27:B27A`, `$1C` `AiRingSpawner` `27:B2A4`,
  `$18` `AiShotTurnMeta` `27:B2D5`, `$19` `AiShotTurn` `27:B2E2` (`obj/ai_shot.js`, in `FIELD_AIS`, steered by
  `_fix_field`): `python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 300
  --routine AiTrailShot,AiRingSpawner,AiShotTurnMeta,AiShotTurn --out web/test/obj_frames/fuzz_shot.json`.

- `fuzz_misc.json` (s167): 300 cases each of AI `$0E` `AiShopEmpty`, `$0F` `AiSetSubstate`, `$0C` `AiFollowPlayerWait`,
  `$0D` `AiMessageAfterWait`, `$1D` `AiShotEmitter`, `$2F` `AiFallingObj`, `$36` `AiDropSpawner` (`obj/ai_shot.js`, in
  `FIELD_AIS`, steered by `_fix_field`): `python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state
  --cases 300 --routine AiShopEmpty,AiSetSubstate,AiFollowPlayerWait,AiMessageAfterWait,AiShotEmitter,AiFallingObj,AiDropSpawner
  --out web/test/obj_frames/fuzz_misc.json`.

- `fuzz_duel.json` (s168): 300 cases each of AI `$0A` `AiDuelStart`, `$33` `AiPhaseUnhide`, `$51` `AiFlagGatedSpawn`,
  `$52` `AiDuelOpponent`, `$53` `AiHideAfterEvent`, `$54` `AiChasePlayer`, `$55` `AiCarryLeft` (`obj/ai_shot.js`, in
  `FIELD_AIS`, steered by `_fix_field`) and `$77` `AiMpDrain99` (`EFFECT_AIS`, `_fix_effect`):
  `python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 300 --routine
  AiDuelStart,AiPhaseUnhide,AiFlagGatedSpawn,AiDuelOpponent,AiHideAfterEvent,AiChasePlayer,AiCarryLeft,AiMpDrain99
  --out web/test/obj_frames/fuzz_duel.json`.
- `fuzz_effect2.json` (s169): 300 cases each of AI `$06` `AiCrumbleRedraw06` (in `PPU_ROUTINES`: CIRAM compared, PPU queue
  steered empty for `WaitPpuQueue`), `$7D` `AiDeathPuff` (`obj/ai_effect.js`) and `$79` `AiBank15Fall79` (`obj/ai_boss.js`;
  `SetR7($0F)` falls into `15:A911`), all `EFFECT_AIS` steered by `_fix_effect`:
  `python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 300 --routine
  AiCrumbleRedraw06,AiDeathPuff,AiBank15Fall79 --out web/test/obj_frames/fuzz_effect2.json`.
- `fuzz_field2.json` (s170): 300 cases each of AI `$34` `AiPeriodicShot`, `$39` `AiArenaLock` and `$40` `AiWander16`
  (`obj/ai_field.js`, in `FIELD_AIS`, steered by `_fix_field`):
  `python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 300 --routine
  AiPeriodicShot,AiArenaLock,AiWander16 --out web/test/obj_frames/fuzz_field2.json`.
- `fuzz_field3.json` (s171): 300 cases each of AI `$07` `AiElementWall`, `$12` `AiWiggleShot`, `$13` `AiSpiralShot`
  (X = the shot slots 3-7 mostly: both index ROM tables by slot) and `$32` `AiPathWalk` (X = `$0D` mostly; 40 % steered
  onto a path terminator `$FC-$FF`), `FIELD_AIS`, steered by `_fix_field`:
  `python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 300 --routine
  AiElementWall,AiWiggleShot,AiSpiralShot,AiPathWalk --out web/test/obj_frames/fuzz_field3.json`.

- `fuzz_platform.json` (s117): 300 cases each for the pit platforms in `obj/ai_field.js` (`PLATFORM_AIS`:
  `AiPitShuttle` `$38`, `AiCrumblePlatform` `$3C`, Y = id·2), captured from `gfx/compare/s116_a6a.state` (area `$6A`,
  which has pit cells) so that `_fix_platform` can put half the cases on the real map, where the probe finds b0:
  `python3 -m tools.emu.routine_capture --load-state gfx/compare/s116_a6a.state --cases 300
  --routine AiPitShuttle,AiCrumblePlatform --out web/test/obj_frames/fuzz_platform.json`.

- `fuzz_reveal.json` (s133): 300 cases for AI `$3A` `AiNpcBossReveal` `27:A555` (`obj/ai_field.js`, Y = `$74`),
  steered in `_fix_case` (NPC slot X−1 on/off, phase 3 → `ObjInit(X, $7D)`, timer2 0/1, hit b7, the row-1 y window
  edges `$27/$28/$AF/$B0`): `python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state
  --cases 300 --routine AiNpcBossReveal --out web/test/obj_frames/fuzz_reveal.json`.

- `fuzz_wander.json` (s140): 300 cases each for the town NPC AIs `AiNpcWander` `$30` `27:ACD9` and `AiNpcStill`
  `$31` `27:AE16` (`obj/ai_field.js`, Y = id·2), captured from `tools/emu/states/town02_enter.state` (a town map),
  steered in `_fix_case` (pause timer 0, `obj_behav` b2/b0, step-frame bits, 16-step edges, the `$08` & `$D6` = 0
  pause frame, other objects put in the look-ahead cells): `python3 -m tools.emu.routine_capture --load-state
  tools/emu/states/town02_enter.state --cases 300 --routine AiNpcWander,AiNpcStill --out web/test/obj_frames/fuzz_wander.json`.
- `real_npc.json` (s140): 200 real `AiNpcWander` calls (town `$02` entry, the 4 NPCs in slots `$0D-$10`):
  `python3 -m tools.emu.routine_capture --load-state tools/emu/states/town02_enter.state --frames 40
  --routine AiNpcWander --out web/test/obj_frames/real_npc.json`.

- `fuzz_effect.json` (s93): 100 cases each for `obj/ai_effect.js` (`EFFECT_AIS`: `AiPlayerFollowC` `$04`,
  `AiStraightMoverTl` `$57` = `AiStraightMoverTail`, `AiMpDrain` `$76`, regs Y = id·2 as `ObjUpdateAll` leaves it);
  steered by `_fix_effect`. `AiMpDrain` runs through `runNow` (its `HudRedraw` is a generator; the capture removes
  queued `$7B` records so it never spins). `fuzz_update.json` was regenerated in s93 with these ids in the pool.
  `python3 tools/obj_case_stats.py web/test/obj_frames/fuzz_effect.json --abs 0708 --col 04A0=0` = path coverage.

- `fuzz_final.json` (s97): 200 cases of AI `$70` `Ai70PhaseDispatch` `27:BA2E` (`obj/ai_final.js`), regs Y = `$E0`,
  one phase per case from `FINAL_PHASES` (`_fix_final`; phase 3's `FadeIn` and phase 1's `ShowMessage` path wait for
  frames and are left out). Captured as a PPU routine (NMI unhooked, CIRAM compared) so the phase-0 window wipe and
  the block `$04`/`$44` flushes finish in one call. Coverage: `python3 tools/obj_case_stats.py
  web/test/obj_frames/fuzz_final.json --by 0600 --abs 62F0=FF`.

- `fuzz_boss.json` (s100): 300 cases each of the bank 15 bosses `BossAiE0` `15:A477`, `BossHomingShot` `15:A5EE`,
  `BossAiE3` `15:A95D`, `BossRockShot` `15:AB3E` (`obj/ai_boss.js`), called with R6 = `$1A`, R7 = `$0F` (`$50` = 7)
  as `ObjUpdateAll` leaves them; one in-range phase per case (`_fix_boss`). The boss ids are also in the
  `ObjUpdateAll` pool of `fuzz_update.json` (boss slots get an in-range phase). The stack window left out of the
  compare is now `sp-32 .. sp+4` (was `sp-15`): the boss → `SpawnShot` → far `ObjInit` chain is deeper.
  `python3 tools/case_slots.py FILE NAME` prints one case's sp and active slots.
  s104: + `BossAiE6` `15:AC7B`, `AiBoss88` `15:ADD4`, `BossAiE8` `15:AF48` (2100 cases). s156: + `BossAiE9`
  `15:B0B6`, `BossAiEA` `15:B130` (2700 cases). s157: + `AiTalkEventE5` `15:ABDA` (3000 cases); cases whose ROM
  run hits an illegal opcode are now skipped like `Stop` cases. s158: + `AiReward6C` `15:B728` (AI `$6C-$6E`,
  3300 cases; path counts with `python3 tools/case_paths.py FILE AiReward6C 0041 07E7 col:04A0 ..`). s159: + `BossDefeated` `15:B807` (AI `$6F`, 3600 cases; its own
  `ROUTINES` entry, in `PPU_ROUTINES`; only areas without NMI waits; not in the `ObjUpdateAll` pool). s163: + `BossAiE2`
  `15:A675`. s165: + `BossAiEB` `15:B2DA` (final boss, phases 1-6, in `PPU_ROUTINES`; 4200 cases). Debugging a failing case:
  `python3 tools/case_bytes.py FILE NAME ADDR col:COL ..` (before → after bytes) and
  `node web/test/case_trace.mjs FILE NAME ADDR ..` (replays an `ai_boss.js` case, JS stack per port write).

- `fuzz_player.json` (s111): 300 cases each of the player AIs `PlayerUpdate` `26:9B45` (X=1, Y=2), `PlayerMoveLocked`
  `26:9D81` (X=0, Y=4), `PlayerMove` `26:9D9A` (X=0, Y=6) (`obj/player.js`; `PLAYER_AIS` + `_fix_player`: status,
  jump timer, swing phase, charge, magic id / MP, conveyor, pads; PPU-queue records with src `$60`/`$7B` removed so
  the HUD writers never wait). The emulator side of a case:
  `python3 -m tools.emu.case_trace --load-state tools/emu/states/gameplay_first.state --cases FILE --case NAME`
  (JSR/JMP/RTS flow with registers).

- `fuzz_spawn.json` (s112): 400 cases each of `AreaSpawnEntry` `31:E1B6` (`world/spawn.js`; `_poke_spawn`: a random
  4-byte entry in WRAM `$7Fxx` with `($10)` pointing at it and Y = 0-7, random area id + story flags `$6480-$64FF`,
  chest params inside `ChestPresencePtrs`, item params `$4x`, NPC params `< $D0` = the real `NpcPresenceHi` range)
  and AI `$50` `AiS0DTransform` `27:A660` (`obj/ai_static.js`; timer2 steps 0/1/≥2).

- `fuzz_collide.json` (s113): 150 cases each of `ObjCollideAll` `26:8F6E`, `ObjOverlapScan` `26:95F8` and the four
  `HitHandlerTable` entries `HitByPlayerShot` `26:90AA` (X = a player shot slot 2/4-11), `HitPlayer` `26:9357`
  (X=1), `TalkProbeHit` `26:94B0` (X=3), `HitHandler3` `26:8FC4` (X=`$0C`), Y = a target `$0D-$1E`
  (`obj/collide.js`; `_poke_coll`: screen positions clustered so hitboxes overlap, the handler-relevant AI ids /
  spawn params / equipment / level / XP, rendering off + empty PPU queue so the HUD writers flush at once).
  `node web/test/obj_check.mjs --dir D` replays the capture files in another directory (extra-seed runs).
- `fuzz_npc.json` (s115): 600 cases of `NpcScript` `14:8000` (`states/talk.js` `npcScript`; `_poke_npc`: params whose
  script pointer is in `$8000-$BEFF`, story flags random / all 0 / all 1, random NPC states `$6580+`, area id often
  from the script's own list). Run without NMI/IRQ (it is in `PPU_ROUTINES`). Seed 115.

Regenerate (about 10 s each):

    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 100 \
        --routine ObjInit,PlaySound,DirToTarget,SpawnShot,ObjBgProbe,AiStraightMover,AiDirShot,AiBurst,AiFanTurret,AiTurret \
        --out web/test/obj_frames/fuzz_static.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 100 \
        --routine PpuQueueFlush,BgRowRender,BgColRender,FullScreenDraw --out web/test/obj_frames/fuzz_bg.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 200 \
        --routine ObjMoveSlide,ObjUpdateAll --out web/test/obj_frames/fuzz_update.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 100 \
        --routine AiWalkPause,AiShooterWalk,AiDormant,AiChase,AiChaseOnscreen,AiShooterChase,AiShooterChaseOn,AiAimShooter,AiBurrower,AiLunger,AiSpawnChildLunger,AiShellOnHit \
        --out web/test/obj_frames/fuzz_walk.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 100 \
        --routine AiTrapChest,AiDasher,AiHomingFlyer,AiHomingShooter,AiOrbiter,AiPatrolGunner,AiDrifter,AiWaitClear,AiCoin,AiCountdown,AiCurveShot,AiHoverShot,AiBounceShot,AiDirFrameShot,AiShotAnimDec \
        --out web/test/obj_frames/fuzz_field.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 100 \
        --routine AiPlayerFollowC,AiStraightMoverTl,AiMpDrain --out web/test/obj_frames/fuzz_effect.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 200 \
        --routine Ai70PhaseDispatch --out web/test/obj_frames/fuzz_final.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 300 \
        --routine BossAiE0,BossHomingShot,BossAiE3,BossRockShot,BossAiE6,AiBoss88,BossAiE8,BossAiE9,BossAiEA,AiTalkEventE5,AiReward6C,BossDefeated,BossAiE2,BossAiEB \
        --out web/test/obj_frames/fuzz_boss.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 300 \
        --routine PlayerUpdate,PlayerMoveLocked,PlayerMove --out web/test/obj_frames/fuzz_player.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 400 \
        --routine AreaSpawnEntry,AiS0DTransform --out web/test/obj_frames/fuzz_spawn.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 150 \
        --routine ObjCollideAll,ObjOverlapScan,HitByPlayerShot,HitPlayer,TalkProbeHit,HitHandler3 \
        --out web/test/obj_frames/fuzz_collide.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 600 --seed 115 \
        --routine NpcScript --out web/test/obj_frames/fuzz_npc.json

`python3 -m tools.emu.routine_capture --list` shows the routines; `--routine A,B` captures a subset.
