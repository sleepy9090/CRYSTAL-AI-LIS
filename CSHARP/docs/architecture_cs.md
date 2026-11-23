# re-c (NES) — C# Port Architecture

Separate project under `CSHARP/` (paths below are relative to it). It is a **translation of the finished
web port** `../web/src/` (docs: `../docs/architecture_web.md`), which is verified frame by frame against
`tools.emu` and FCEUX. `../REVERSE.md` stays the source of truth for the ROM; the JS module is the source of
truth for *how* each routine was ported (timing quirks, lag model, split hooks). Read the JS first, translate
it; go back to the ROM only when the JS and REVERSE.md disagree.

Nothing outside `CSHARP/` is edited by this project. It only *reads* `../web/` (source + `web/data/` + test
states), `../REVERSE.md`, `../docs/`, and runs `../web/test/run.mjs` / `../tools/compare_frames.py` as oracles.

---

## 1. Design rules

1. **One C# file per JS module, same relative path, PascalCase.** `web/src/game/states/death.js` →
   `src/ReC.Core/Game/States/Death.cs` (`static class Death`); `waitFrames` → `WaitFrames`.
   Every ported member carries `// js: game/states/death.js waitFrames` on the line(s) directly above it.
   `tools/cs_coverage.py` checks the tags against the REA inventory (§6).
2. **Same RAM, same addresses.** `Machine` holds `byte[] Ram = new byte[0x800]`, `byte[] Wram = new byte[0x2000]`;
   named addresses come from `../web/data/ram_labels.json`, generated into `src/ReC.Core/Gen/R.cs`
   (`R.SubState`, …) by `tools/gen_cs_data.py`. `Rd(a)` / `Wr(a, v)` mirror `game/ram.js`.
3. **Instruction-level ports** including zero-page temps and the bank shadows `$50/$6E/$6F`.
4. **No hand-copied ROM data.** `ReC.Core` loads `../web/data/` at run time (manifest + sha/size check, = `data.js`),
   located via `--data DIR` or by walking up from the executable to `web/data/manifest.json`. JSON via
   `System.Text.Json` (in the BCL). ROM reads go through `Prg.CpuRd(m, addr)` exactly as `prg.js`.
5. **No NuGet packages.** net10.0, BCL only. Tests are a console project (`tests/ReC.Tests`) with its own tiny
   assert helper, run by `dotnet run --project tests/ReC.Tests`. The SDL2 frontend P/Invokes `libSDL2-2.0.so.0`
   (`[DllImport]`, hand-written declarations in `src/ReC.Sdl/Sdl.cs`), which is already installed on this machine.
6. **Integer semantics.** Use `int` for JS numbers and `byte` / explicit `& 0xFF` where the JS masks; JS `>>>`
   → `(uint)` shifts. `checked` is off; never rely on wrap without a mask the JS also has.
7. **Platform-free core.** `ReC.Core` has no I/O besides loading data/state files; `ReC.Run` (headless) and
   `ReC.Sdl` (playable) are thin hosts.

## 2. Layout

```
CSHARP/
├─ ReC.slnx                      solution (dotnet build ReC.slnx)
├─ TASKS.md                      ordered task list (drives re_loop.sh; `- [ ]` = open)
├─ re_loop.sh                    autonomous sessions over TASKS.md (run from CSHARP/)
├─ CLAUDE.md                     instructions for sessions working in this folder
├─ docs/  architecture_cs.md, obligations.csv (REA inventory), waivers.csv
├─ sessions/  logs of the autonomous sessions (re_loop.sh)
├─ tools/ cs_coverage.py, cs_rea_members.py, cs_vs_web.py, pass_count_diff.py, apu_wav_compare.py, gen_cs_data.py,
│         state_peek.py, json_peek.py
├─ src/
│  ├─ ReC.Core/  Data.cs Machine.cs  Nes/{Ppu,Apu}.cs  Game/{Ram,Prg,Boot,MainLoop,LagModel,Pads,Palette,
│  │             PlaySound,Y}.cs  Game/Sound/*  Game/World/*  Game/Obj/*  Game/States/*  Util/{Png,Csv,StateFile}.cs
│  │             Gen/R.cs (generated RAM labels)
│  ├─ ReC.Run/   Program.cs = ../web/test/run.mjs (same CLI, same CSV columns / PNG / raw dumps, + --pass-count)
│  └─ ReC.Sdl/   Program.cs Sdl.cs Input.cs Audio.cs Save.cs = web/src/main.js input.js audio.js save.js
└─ tests/ReC.Tests/  Program.cs SoundTests.cs (unit tests, sound_frames replay, APU cases, --wav)
```

## 3. Multi-frame routines: C# iterators

The JS port writes ROM wait loops as generators; `mainStep` resumes the one pending generator per frame.
C# iterators are the same construct:

| JS | C# |
|---|---|
| `function* f(m) { … yield; … }` | `static IEnumerable<Y> F(Machine m) { … yield return Y.Frame; … }` |
| `yield* g(m)` | `foreach (var y in G(m)) yield return y;` (helper `Co.All(G(m))` is not allowed to hide yields — keep the foreach) |
| `yield SPIN` / `yield NO_NMI` | `yield return Y.Spin` / `yield return Y.NoNmi` |
| `m.pending = g; r = g.next()` | `m.Pending = F(m).GetEnumerator(); more = m.Pending.MoveNext()` |
| `m.pending.next()` | `m.Pending.MoveNext()`, value = `m.Pending.Current` |
| plain function handler | method returning `IEnumerable<Y>` with no `yield` → use `yield break` after the work, or a non-iterator overload that MainStep detects (keep one convention, document it in MainLoop.cs) |
| `ENTRY_POINTS` | same table of `Func<Machine, IEnumerable<Y>>` |

`Y` is `enum Y : byte { Frame, Spin, NoNmi }`. Iterator bodies cannot contain `ref` locals or `try/catch`
around `yield` with `catch` — none of the JS needs them.

## 4. Frame loop and timing

`Machine.RunFrame` is a line-by-line port of `game/machine.js` (`runFrame`): scanline render with split hooks
(`ppu.splits`, `ppu.timed`), NMI work, `MainStep`, the lag model (`lagmodel.js` coefficients copied as
literals — fitted port constants, not ROM data), IRQ bodies. Same order of side effects → same CSV rows.

## 5. Verification

Oracle = the web port: **same start (Reset or `--load-state`) + same `--keys` → byte-identical `--compare` CSV,
`--dump-ram/--dump-wram/--dump-ciram` files and `--dump-screen` PNG pixels.**

- `dotnet run --project src/ReC.Run -c Release -- <run.mjs options>` (or the built `src/ReC.Run/bin/Release/net10.0/ReC.Run`)
  accepts `--frames --frame0 --keys --compare --dump-screen --scale --dump-ram --dump-wram --dump-ciram --load-state --quiet`.
- `python3 tools/cs_vs_web.py [--canon] [--states] [NAME..]` runs both and diffs with `../tools/compare_frames.py`;
  `--canon` = canonical keys from Reset to f2460, `--states` = every `../web/test/states/*.json` with the frames/keys
  its `*_check.mjs` uses (table in cs_vs_web.py). Exit 1 on any difference.
- Unported routines log `not ported: <js module> <name>` like the JS stubs did.
- `dotnet run --project tests/ReC.Tests` = unit tests.

### 5.1 As built so far (C# s1)

- Namespaces: `ReC` (Data), `ReC.Game` (Machine, MainLoop, Ram, Prg, …), `ReC.Game.States|Sound|World|Obj`,
  `ReC.Nes` (Ppu, Apu), `ReC.Util` (Png + Crc32, Csv + Keys, StateFile), `ReC.Gen` (R).
- Handler convention (§3): `Func<Machine, IEnumerable<Y>?>` — an iterator method = JS generator; a plain method does
  its work and returns `null` (or another routine's iterator). `m.Pending` is an `IEnumerator<Y>`.
- Stubs: unported callees live in their future file, without a `js:` tag, and call `Stub.NotPorted(m, module, name)`
  (logs once per name into `m.Unported`, counts `m.StubHits`). C# s3: a state whose `until` has no `Play.EntryPoints`
  entry starts nothing pending, exactly as machine.js (the s1 `ENTRY_POINTS` log is gone; the table is complete).
- Name changes: obj/util.js → `Game/Obj/ObjUtil.cs` (`Util` clashes with `ReC.Util`); soundupdate.js `soundUpdate`
  → `SoundUpdate.Run`, playsound.js `playSound` → `PlaySound.Play` (member ≠ class name); machine.js `lagModel`
  flag → `LagModelOn`; `ram.ram` → `Ram.RamBytes`. JS `MAIN_STATES` / `NMI_MODES` / `ENTRY_POINTS` are tables, not
  inventory callables (no tag).
- `cs_vs_web.py` writes into `out/<case>/` (gitignored); `--only png` etc. limits the kinds compared.
- C# s2 (sound): `Machine.ApuWrite` / `ApuRead` are delegate fields like the JS closures (tests swap them to log
  writes). `Apu` sample sink = `Action<double>`; mixer tables are `float[]` (JS Float32Array), all sums / filters
  `double`, so samples are bit-identical. apu.js classes → `Envelope`, `Pulse`, `Triangle`, `Noise`, `Dmc` in
  `Nes/Apu.cs`; `Apu.TriCh` / `NoiseCh` / `DmcCh` (JS `tri` / `noise` / `dmc`, renamed to avoid the type names).
  Same-named JS methods share one inventory key, so their tags carry the class: `// js: nes/apu.js write (Pulse)`.
  Sound oracles: ReC.Tests `sound_frames` (= sound_check.mjs on `../web/test/sound_frames`) and
  `tools/apu_wav_compare.py` (`ReC.Tests --wav` vs `apu_check.mjs --wav`, byte-identical).
- C# s2 (T3): a JS generator's *return value* (e.g. death.js `hudRedraw`'s Y register) is passed out through a small
  result object argument (`Death.YOut`), set when the iterator ends; JS `undefined` results are `int?` null
  (`PpuQueue.PpuQueueFlush`, `Death.QueueFlushNow`). Title dispatch tables (JS `MODES` / `STEPS` maps) are
  `Dictionary<int, Func<Machine, IEnumerable<Y>?>>`; the two inline JS lambdas stay lambdas (no tag).
- C# s3 (T4 + parts of T5/T6):
  - Generator return values: `Ret<T>` (Game/Y.cs), an argument the iterator sets before it ends (`Camera.CameraTick(m,
    res)` = JS `if (yield* cameraTick(m)) return`, `Pause.PauseScreen(m, res)` = ReadPads' Y, `Player.SpendMp`).
    JS `true`-as-PLA-PLA in PlayDeathMenuCheck = `Ret<int?>` null.
  - AI handlers: `delegate IEnumerable<Y>? AiFn(Machine m, int x, AiRet r)` (Obj/Objects.cs). null = plain JS function,
    else its iterator; JS return `true` (popped ObjUpdateAll) = `r.Pop`, a number (new X) = `r.X`. Each AI module has
    `static Dictionary<int, AiFn> Ai` (= JS `AI_xxx`, untagged table); `Objects.AiTable` merges them in the JS spread
    order. Plain-function AI modules (ai_shot.js) keep `void F(Machine, int)` members wrapped by `P(F)`.
  - Name changes: objinit.js `objInit` -> `ObjInit.Init`; areaenter.js `areaEnter` -> `AreaEnter.AreaEnterRun`;
    giveitem.js `giveItem` -> `GiveItem.Give` (stub); sprites.js module variable `regY` -> `m.SpriteRegY`;
    ObjMoveStep's `'moved' | bool` -> `Move.Step { Clear, Blocked, Moved }`; bg.js renderArea's `visited`
    (false / true / 'ram') -> `Bg.Visit { None, All, Ram }`; `Machine.SpawnEntry` returns the next Y (= JS hook).
  - Exported JS lag predicates (`FSD_LAG`, `FSD_LAG_WF`, saveload `RESTORE_LAG`) are `Func<int, bool>` fields (tagged).
  - `docs/waivers.csv` (first rows): sprites.js `rd` / `wr` per-function aliases (C# calls m.Rd / m.Wr).

- C# s4 (T6 + part of T8):
  - **Read/write counts are behaviour.** The lag model judges each pass from `m.Rd` / `m.Wr` / `Prg.CpuRd` counts per
    `m.PassStage`, so a C# member must make exactly the JS's counted accesses: no extra `m.Rd` for a bounds check or
    a cached value the JS reads twice (s4 fix: `MainLoop.MainStep` read `main_state` twice). Uncounted reads go
    through `m.Mem.Rd`, as the JS's `m.mem.rd`. `tools/pass_count_diff.py` checks this per frame and stage.
  - Plain-function AI modules (ai_walk.js, ai_field.js) keep `void F(Machine, int)` members wrapped by `P(F)` in `Ai`.
  - JS default parameters read from RAM (`yIn = m.rd(0x010F)`) become an overload `F(m) => F(m, m.Rd(0x010F))`
    (the read happens at the call, as in JS). Talk.NpcTalk, GiveItem.ItemUseMenu, Teleport / SwordRaise stubs.
  - giveitem.js mutates talk.js's `TALK_ACTIONS` object; in C# `Talk.TalkActions` lists those entries directly
    (pointing at `GiveItem.TalkGiveParam` / `TalkAct14` ...). The event-act PLA symbol is a `Ret<bool>` argument.
  - The JS tables' anonymous generators / arrows (TALK_ACTIONS, EVENT_ACTIONS, USE_HANDLERS) are private named
    methods or lambdas without tags (they are not inventory callables).

- C# s5 (T6-T10, game layer complete):
  - Boss phase handlers (ai_boss.js): `delegate IEnumerable<Y>? Ph(m, x, c, r)` in a `Dictionary<int, Ph>` keyed by the
    BossPhaseTable handler address (c = the dispatch's ASL carry, r = the AI's `AiRet`). JS return values kept for parity:
    e9Fire returns SpawnShot's bool, so a statue shot that spawns pops ObjUpdateAll (JS `true`); ebSetup's `return 0`
    = `r.X = 0` set at the iterator's end; AI $79 `r.X = $2F`. BossDefeated's hooks are a `Dictionary<int, Func<..>>`.
  - JS string results (equip.js `'exit'`) and boolean generator results (MenuConfirmYesNo's C, MenuSave's "back") are
    `Ret<bool>` arguments. Inner JS closures that the inventory lists (equip `put` / `lookup` / `clamp`, ending `rel` /
    `next`, wake `rd` / `oam`, telepathy `row`, shop `leave`) are C# local functions with their own `// js:` tag; the two
    identical shop.js `pr` closures are one static `Pr` (one inventory key).
  - JS `y = 0` defaults on substates (cure, formrevert, finalscene, swordraise) = a one-argument overload passing 0
    (play.js calls every substate with `(m)`). A member can't share its class name: telepathy.js `telepathy` ->
    `Telepathy.TelepathyMsg`.
  - States/Substates9.cs (the s3 stub file) is comment-only; every T9 module has its own file.
  - `tools/cs_vs_web.py` CASES now hold every web check (boss, event / talk, giveitem / itemuse, T9 substates, wake from
    Reset) plus T8 menu walks with chosen keys (MenuConfirmYesNo starts on NO: a direction press first = YES).

- C# s6 (T11, platform layer): `src/ReC.Sdl` = main.js / input.js / audio.js / save.js. Browser listeners become one
  SDL event loop in `Program.Start` that hands each event to `Input.HandleEvent` (keydown / keyup / blur + controller
  hot-plug); KeyboardEvent.code → SDL scancodes (physical keys). WebAudio scheduling → `SDL_QueueAudio`: the JS
  "start LEAD ahead when the queue ran dry" is LEAD seconds of queued silence; mute scales the queued samples by 0.
  localStorage base64 → raw `re-c.sav` (waivers for main.js `$`, save.js `toB64` / `fromB64`); `status` = window title.
  The host only calls `Machine.Reset` / `LoadState` / `RunFrame` / `Ppu.Fb` / `Apu.SetOutput`, so its frames are the
  ReC.Run frames (checked: same f600 PNG + WRAM from Reset).

### 5.2 As built (C# s7, T12 close-out)

**JS → C# modules** (`python3 tools/cs_rea_members.py --modules` prints this with tag counts; 1,029 tags, 1,031 + 15
waived of 1,046 inventory callables). Class name = file name unless noted.

| web/src module | C# file (namespace) | | web/src module | C# file |
|---|---|---|---|---|
| data.js | `ReC.Core/Data.cs` (`ReC`) | | game/states/areaenter.js | `Game/States/AreaEnter.cs` |
| nes/ppu.js | `Nes/Ppu.cs` (`ReC.Nes`) | | game/states/play.js | `Game/States/Play.cs` |
| nes/apu.js | `Nes/Apu.cs` (`Apu`, `Envelope`, `Pulse`, `Triangle`, `Noise`, `Dmc`) | | game/states/death.js | `Game/States/Death.cs` |
| game/machine.js | `ReC.Core/Machine.cs` (`ReC.Game`) | | game/states/pause.js | `Game/States/Pause.cs` |
| game/ram.js, prg.js | `Game/Ram.cs`, `Game/Prg.cs` | | game/states/title.js | `Game/States/Title.cs` |
| game/boot.js, mainloop.js | `Game/Boot.cs`, `Game/MainLoop.cs` | | game/states/talk.js | `Game/States/Talk.cs` |
| game/lagmodel.js | `Game/LagModel.cs` | | game/states/giveitem.js | `Game/States/GiveItem.cs` |
| game/pads.js, palette.js | `Game/Pads.cs`, `Game/Palette.cs` | | game/states/equip.js | `Game/States/Equip.cs` |
| game/playsound.js | `Game/PlaySound.cs` (`PlaySound.Play`) | | game/states/shop.js | `Game/States/Shop.cs` |
| game/sound/driver.js | `Game/Sound/Driver.cs` | | game/states/saveload.js | `Game/States/SaveLoad.cs` |
| game/sound/soundupdate.js | `Game/Sound/SoundUpdate.cs` (`SoundUpdate.Run`) | | game/states/wake.js | `Game/States/Wake.cs` |
| game/world/area.js, bg.js | `Game/World/Area.cs`, `Bg.cs` | | game/states/teleport.js | `Game/States/Teleport.cs` |
| game/world/bgstream.js | `Game/World/BgStream.cs` | | game/states/telepathy.js | `Game/States/Telepathy.cs` |
| game/world/camera.js | `Game/World/Camera.cs` | | game/states/changeform.js | `Game/States/ChangeForm.cs` |
| game/world/ppuqueue.js | `Game/World/PpuQueue.cs` | | game/states/cure.js | `Game/States/Cure.cs` |
| game/world/spawn.js | `Game/World/Spawn.cs` | | game/states/formrevert.js | `Game/States/FormRevert.cs` |
| game/obj/objects.js, objinit.js | `Game/Obj/Objects.cs`, `ObjInit.cs` (`ObjInit.Init`) | | game/states/swordraise.js | `Game/States/SwordRaise.cs` |
| game/obj/move.js, collide.js | `Game/Obj/Move.cs`, `Collide.cs` | | game/states/mesia.js | `Game/States/Mesia.cs` |
| game/obj/util.js | `Game/Obj/ObjUtil.cs` | | game/states/finalscene.js | `Game/States/FinalScene.cs` |
| game/obj/sprites.js, player.js | `Game/Obj/Sprites.cs`, `Player.cs` | | game/states/ending.js | `Game/States/Ending.cs` |
| game/obj/ai_static/walk/field.js | `Game/Obj/AiStatic.cs`, `AiWalk.cs`, `AiField.cs` | | main.js | `ReC.Sdl/Program.cs` |
| game/obj/ai_shot/effect/thunder.js | `Game/Obj/AiShot.cs`, `AiEffect.cs`, `AiThunder.cs` | | input.js, audio.js | `ReC.Sdl/Input.cs`, `Audio.cs` |
| game/obj/ai_boss.js, ai_final.js | `Game/Obj/AiBoss.cs`, `AiFinal.cs` | | save.js | `ReC.Sdl/Save.cs` |

Extra C# files with no JS module: `Game/Y.cs` (`Y` enum, `Ret<T>`, `Stub`), `Gen/R.cs` (generated labels),
`Util/{Png,Csv,StateFile}.cs` (what node's zlib / fs / JSON gave the JS test runner), `ReC.Sdl/Sdl.cs` (SDL2 P/Invoke).
`Game/States/Substates9.cs` is a comment-only leftover of the s3 stubs (delete it; the build does not need it).

**Deviations from the JS (all behaviour-neutral; the oracle runs are byte-identical):**
- Generators → C# iterators (§3); generator return values → `Ret<T>` / `Death.YOut` arguments set before the iterator
  ends; JS `undefined` → `int?` null; JS `true` from an AI (popped ObjUpdateAll) → `AiRet.Pop`, a new X → `AiRet.X`.
- JS default parameters read from RAM → overloads that read at the call; `y = 0` defaults → one-argument overloads.
- Dispatch tables (MODES / STEPS / AI_xxx / BossPhaseTable / EVENT_ACTIONS / TALK_ACTIONS / ENTRY_POINTS) →
  `Dictionary` / arrays of delegates; their anonymous JS lambdas are untagged lambdas or private methods.
  giveitem.js's mutation of TALK_ACTIONS is folded into `Talk.TalkActions` directly.
- Renames where a member would share its class name or clash with a namespace: `PlaySound.Play`, `SoundUpdate.Run`,
  `ObjInit.Init`, `AreaEnter.AreaEnterRun`, `GiveItem.Give`, `Telepathy.TelepathyMsg`, `ObjUtil`; machine.js's `lagModel`
  flag → `LagModelOn`; sprites.js module variable `regY` → `m.SpriteRegY`; apu.js `tri/noise/dmc` → `TriCh/NoiseCh/DmcCh`.
- JS tri-state values → enums (`Move.Step`, `Bg.Visit`); equip.js `'exit'` and boolean generator results → `Ret<bool>`.
- Inner JS closures listed by the inventory → C# local functions with their own tag (equip `put/lookup/clamp`, ending
  `rel/next`, wake `rd/oam`, telepathy `row`, shop `leave`, main.js `blit/fit/tick`); shop.js's two `pr` → one `Pr`.
- JS `throw` → `InvalidOperationException`. Counted reads/writes are kept exactly (lag model input, §5.1 s4).
- Platform layer: browser events → one SDL event loop; WebAudio → `SDL_QueueAudio` (+ a 0.25 s queue cap the JS lacks);
  localStorage base64 → raw `re-c.sav` (written via .tmp + move); DOM status → window title. Waivers: `docs/waivers.csv`.
- Known web-port quirks are kept for parity (e.g. ai_boss.js e9Fire: a statue shot that spawns pops ObjUpdateAll).

**Final checks (C# s7):** build 0 warnings; ReC.Tests 88/88; `cs_coverage.py` exit 0 (100 %, 0 stale, 0 bad waivers);
`cs_vs_web.py --canon --states` 88/88 identical; `cs_rea_members.py` 1,029/1,029 tags found in the assemblies.

## 6. REA reconstruction tracking

REA (`rea analyze-javascript-application <repo>/web/src --artifact-format directory --detail summary --evidence_output EV.json`)
parsed the web port statically: 59 modules, 1,204 callable nodes with source ranges + semantic fingerprints
(evidence `ev_2d5f769c5345b420702a7295d868c61695ac465eaa7dad86d906dfef0461a42a`, web/src sha256
`60f1d7181761672328d8452d28dcbb910aefcff0519dcc48407adafbacf47f64`). `../tools/rea_inventory.py EV.json --out
CSHARP/docs/obligations.csv` → **1,046 named callables** (947 game, 74 nes, 3 data, 22 platform; 158 anonymous
callbacks ported inside their parent). The CSV header line records the evidence id and digest; if web/src changes,
rerun REA + rea_inventory.py.

`python3 tools/cs_coverage.py [--module SUBSTR] [--missing]` = tracker (tags + `docs/waivers.csv`). The port is
complete when coverage is 100 % **and** `tools/cs_vs_web.py --canon --states` passes. REA can also inspect the
built assembly (`rea inspect-managed-artifact src/ReC.Core/bin/Release/net10.0/ReC.Core.dll`, managed PE/CLI member
inventory) — the close-out task uses it to confirm every tagged member exists in the compiled assembly, and
`verify_reconstruction` / `build_reconstruction_obligation_ledger` (MCP) for a closing record when connected.

**Assembly cross-check (C# s7, T12).** On the Release build:
- `rea inspect-managed-artifact src/ReC.Core/bin/Release/net10.0/ReC.Core.dll --detail summary --evidence_output
  out/rea_artifact.json` → evidence `ev_371f6022e862cce26dd80b9d27403aba6f99c28a00e38be804a3fda2bfb0e278` (summary view
  `ev_01c2b20d…`): managed PE32 IL-only, `.NETCoreApp,Version=v10.0`, metadata complete, 414 TypeDef / 4,064 MethodDef /
  3,185 Field rows, DLL sha256 `4a3be9535c847d314181f168731062afa308cff251b821bfe4408306a4936ad7`, MVID
  `2d754f3b-a755-4b96-86ef-24e76fc5bdd2`.
- `rea inspect-managed-members` (same flags) → `out/rea_members.json`, evidence
  `ev_703262e1d23f0c6804859e7001951a10a1adb3202b255c7619987fbb956daeef` (ReC.Core.dll, same sha256), and
  `out/rea_members_sdl.json`, evidence `ev_572f1c6a41bb8ef3bf709dd55c32f1ad3ad18ae8c33806f47707266f91865ded`
  (ReC.Sdl.dll, sha256 `e1395099a4f9883e…`; the platform tags live there).
- `python3 tools/cs_rea_members.py` parses every `// js:` tag's C# member + enclosing type from the source and finds it in
  those bundles (method / ctor / property accessor / field / local function `<Outer>g__Name|`, also inside closure
  classes): **1,029 / 1,029 tags found** (1,012 inventory keys, 1,026 C# members); it also checks the bundles' sha256
  against the DLLs on disk (stale after any rebuild that changes them: rerun the two REA commands). `out/` is
  gitignored, so the bundles are regenerated, not committed; the ids above are this build's.
