# web/test/obj_frames (s55)

Routine-level fuzz cases for the object code, captured by `tools/emu/routine_capture.py` and replayed by
`node web/test/obj_check.mjs`. Each case has RAM (2KB) + WRAM (sparse vs `wram_base`) at a routine's first
instruction, its registers, how it returned (`rts`, or `popped` when `AiShootCheck` pulled the caller's return
address), and the bytes it changed. The port must leave the same RAM + WRAM (stack scratch excluded) and return the same way.

- `fuzz_static.json`: 100 cases each for `ObjInit`, `PlaySound`, `DirToTarget`, `SpawnShot`, `ObjBgProbe`,
  `AiStraightMover`, `AiDirShot`, `AiBurst`, `AiFanTurret`, `AiTurret` (base = the `ObjUpdateAll` entry after
  loading `gameplay_first.state`; `$6E/$6F/$50` set as `ObjUpdateAll`'s `SwitchBank16($0D)` leaves them).

Regenerate (about 10 s):

    python3 -m tools.emu.routine_capture --load-state tools/emu/states/gameplay_first.state --cases 100 \
        --out web/test/obj_frames/fuzz_static.json

`python3 -m tools.emu.routine_capture --list` shows the routines; `--routine A,B` captures a subset.
