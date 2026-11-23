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
├─ docs/  architecture_cs.md, obligations.csv (REA inventory), waivers.csv, sessions notes
├─ tools/ cs_coverage.py, gen_cs_data.py, cs_vs_web.py
├─ src/
│  ├─ ReC.Core/  Data.cs Machine.cs …  Nes/Ppu.cs Nes/Apu.cs  Game/{Ram,Prg,Boot,MainLoop,LagModel,Pads,Palette,
│  │             PlaySound}.cs  Game/Sound/*  Game/World/*  Game/Obj/*  Game/States/*  Util/{Png,Csv,StateFile}.cs
│  ├─ ReC.Run/   Program.cs = ../web/test/run.mjs (same CLI, same CSV columns / PNG / raw dumps)
│  └─ ReC.Sdl/   Program.cs Sdl.cs Input.cs Audio.cs Save.cs = web/src/main.js input.js audio.js save.js
└─ tests/ReC.Tests/  Program.cs (unit tests: iterator scheduler, PNG/CSV/state readers, per-module cases)
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
  (logs once per name into `m.Unported`, counts `m.StubHits`). A state whose `until` has no `Play.EntryPoints`
  entry logs `ENTRY_POINTS BB:AAAA`.
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
