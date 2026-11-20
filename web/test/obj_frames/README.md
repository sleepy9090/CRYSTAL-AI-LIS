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
  s104: + `BossAiE6` `15:AC7B`, `AiBoss88` `15:ADD4`, `BossAiE8` `15:AF48` (2100 cases). Debugging a failing case:
  `python3 tools/case_bytes.py FILE NAME ADDR col:COL ..` (before → after bytes) and
  `node web/test/case_trace.mjs FILE NAME ADDR ..` (replays an `ai_boss.js` case, JS stack per port write).

- `fuzz_player.json` (s111): 300 cases each of the player AIs `PlayerUpdate` `26:9B45` (X=1, Y=2), `PlayerMoveLocked`
  `26:9D81` (X=0, Y=4), `PlayerMove` `26:9D9A` (X=0, Y=6) (`obj/player.js`; `PLAYER_AIS` + `_fix_player`: status,
  jump timer, swing phase, charge, magic id / MP, conveyor, pads; PPU-queue records with src `$60`/`$7B` removed so
  the HUD writers never wait). The emulator side of a case:
  `python3 -m tools.emu.case_trace --load-state tools/emu/states/gameplay_first.state --cases FILE --case NAME`
  (JSR/JMP/RTS flow with registers).

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
        --routine BossAiE0,BossHomingShot,BossAiE3,BossRockShot,BossAiE6,AiBoss88,BossAiE8 \
        --out web/test/obj_frames/fuzz_boss.json
    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 300 \
        --routine PlayerUpdate,PlayerMoveLocked,PlayerMove --out web/test/obj_frames/fuzz_player.json

`python3 -m tools.emu.routine_capture --list` shows the routines; `--routine A,B` captures a subset.
