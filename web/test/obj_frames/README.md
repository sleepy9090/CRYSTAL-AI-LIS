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

`python3 -m tools.emu.routine_capture --list` shows the routines; `--routine A,B` captures a subset.
