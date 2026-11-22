// node web/test/kill_check.mjs — real enemy kills vs tools.emu (session 175): HitByPlayerShot 26:90AA ->
// HitDamage 26:913B -> EnemyKilled 26:9152 (AddXp, EnemyDeathFx = AI $7D puff), the puff's ObjBecomeNext 26:98CD
// into its death type $0B (AI $7B coin, ChestHop 27:B79F), the coin's lifetime / pickup (PickupGold 26:946D).
//   killgold:    scenario gold_class_3 (area 03, enemy slot $0E parked 14 px right of the player with 1 HP,
//                level 16, obj_elem_drop hi = 3) -> kill f2533, coin f2581, gold +2 at f2727;
//   killlevelup: scenario kill_levelup (the same at level 1 with XP next = 1) -> EnemyKilled's level-up branch
//                26:9188 (level 2, HP/MP max + diff, HudHpBar + HudRedraw 0/3/4/5, sub_state $10, msg 32.13).
// States: python3 -m tools.emu.scenario_state gold_class_3 28 --out web/test/states/killgold.json
//           --save-state tools/emu/states/killgold.state   (and kill_levelup 28 -> killlevelup.*)
// Refs: tools/web_state_compare.py --state S.state --web S.json --frames 300 --out-dir gfx/<name> --web-key-shift 1
//   --keys 2526:B:4 2546:A:4 ... (KILL_KEYS: B/A every 20 f, RIGHT at 2726), cut by tools/csv_trim.py
//   (core,pal,obj,oam_crc) to f2525-2823 -> web/test/<name>_ref.csv (snd left out: the 1-frame snd*_period offset).
// Keys: ReadPads 31:FE97 runs at line 260-261 in this scene, so a key at emu frame F is read by the port at F+1;
// after the level-up message (music $81 playing; probably its SoundUpdate cost, not measured) the read slips to line 2-5 of the next frame from
// f2606 on, so those keys go to the port at F (the port frame = the emu pass, not its line). The port frames below
// are the tools.emu `--watch 004B` write frames, +1 when the write is in vblank (line >= 241).
// LATE cells (port = tools.emu's value one row later): f2536 sub (EnemyKilled resumes from PpuQueueWaitSrc($7B)
// right after the NMI at L256 and sets sub_state $10 in the same row; the port runs that code in its next frame),
// f2572 music (the msg 32.13 jingle $81 start).
// PARTIAL OAM cells f2587/89/91/93: ShowMessage's 8 closing frames (30:D3AA loop: WaitFrame, farSpriteDraw,
// OamClipTop) run the next sprite pass in vblank right after the NMI, so tools.emu samples its row at L261 with
// that pass part-done: OAM entries 1..k-1 already hold the next row's sprites (on these odd rows the sword's
// A2 05 40 71 over the first tile of the $6E object). Accepted only if such a mix of the port's rows f / f+1
// has the reference crc. Exit code 1 on any other mismatch.
import { readFileSync } from 'node:fs';
import { crc32 } from 'node:zlib';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { compareColumns, compareRow, parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const verbose = process.argv.includes('-v');
let fails = 0;
const fail = (msg) => { fails++; if (verbose || fails <= 30) console.log('  FAIL ' + msg); };
const LAST = 2823;
const seq = (frames, btns) => frames.map((f, i) => `${f}:${btns[i % btns.length]}:4`);
const GOLD_KEYS = seq([2527, 2547, 2567, 2587, 2607, 2627, 2647, 2667, 2687, 2707], ['B', 'A'])
  .concat(['2727:RIGHT:4'], seq([2747, 2767, 2787, 2807], ['B', 'A']));
const LVUP_KEYS = seq([2527, 2547, 2567, 2587, 2606, 2626, 2646, 2666, 2686, 2707], ['B', 'A'])
  .concat(['2727:RIGHT:4'], seq([2747, 2767, 2787, 2807], ['B', 'A']));

const CASES = [
  // coin = the drop in slot $0F (AI $7B; tools.emu rows): after the puff's 16 frames, later behind msg 32.13
  { name: 'killgold', keys: GOLD_KEYS, late: new Map(), coin: 2549,
    end: { 0x0421: 16, 0x0702: 4, 0x0703: 0, 0x0704: 1, 0x0705: 0, 0x04AE: 0 } },
  { name: 'killlevelup', keys: LVUP_KEYS, late: new Map([[2536, ['sub']], [2572, ['music']]]),
    partialOam: [2587, 2589, 2591, 2593], coin: 2611,
    end: { 0x0421: 2, 0x0704: 0, 0x0706: 0x3C, 0x0707: 0, 0x03C0: 0x40, 0x0709: 0x22, 0x0702: 2, 0x41: 8 } },
];

for (const c of CASES) {
  const state = JSON.parse(readFileSync(path.join(WEB, `test/states/${c.name}.json`), 'utf8'));
  const [head, ...lines] = readFileSync(path.join(WEB, `test/${c.name}_ref.csv`), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
  if (idx.some((i) => i < 0)) throw new Error(`${c.name}_ref.csv has a column the port does not write`);
  const keys = parseKeys(c.keys);
  const m = new Machine(data, { log: () => {} });
  m.reset();
  m.loadState(state);
  const f0 = state.frame0 ?? state.frame;
  const rows = new Map(), oam = new Map();
  let killF = -1, coinF = -1;
  while (f0 + m.frame <= LAST) {
    const f = f0 + m.frame;
    m.runFrame(keys.get(f) || 0);
    rows.set(f, compareRow(m, f));
    oam.set(f, Uint8Array.from({ length: 256 }, (_, i) => m.rd(0x200 + i)));
    if (killF < 0 && m.rd(0x04AE) === 0x7D) killF = f;
    if (coinF < 0 && m.rd(0x04AF) === 0x7B) coinF = f;
  }
  // tools.emu's crc = OAM of row f with entries 1..k-1 already rewritten by the pass of row f+1
  const midPass = (f, want) => {
    const a = oam.get(f), b = oam.get(f + 1);
    if (!a || !b) return false;
    for (let k = 2; k <= 64; k++) {
      const mix = Uint8Array.from(a);
      mix.set(b.subarray(4, k * 4), 4);
      if ((crc32(mix) >>> 0) === want) return true;
    }
    return false;
  };
  if (m.unported.size) fail(`${c.name}: unported: ${[...m.unported].join(', ')}`);
  const before = fails;
  let n = 0, late = 0, partial = 0;
  for (const l of lines) {
    const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
    if (!row) { fail(`${c.name} f${f}: no port row`); continue; }
    n++;
    for (let i = 1; i < refCols.length; i++) {
      if (row[idx[i]] === w[i]) continue;
      if (c.late.get(f)?.includes(refCols[i]) && rows.get(f + 1)?.[idx[i]] === w[i]) { late++; continue; }
      if (refCols[i] === 'oam_crc' && c.partialOam?.includes(f) && midPass(f, w[i])) { partial++; continue; }
      fail(`${c.name} f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
    }
  }
  for (const [a, v] of Object.entries(c.end))
    if (m.rd(+a) !== v) fail(`${c.name}: $${(+a).toString(16).padStart(4, '0')} = ${m.rd(+a)} at the end, want ${v}`);
  if (killF !== 2533) fail(`${c.name}: puff (AI $7D, slot $0E) at f${killF}, want f2533`);
  if (coinF !== c.coin) fail(`${c.name}: coin (AI $7B, slot $0F) at f${coinF}, want f${c.coin}`);
  console.log(`${fails > before ? 'FAIL' : 'ok  '} ${c.name}: ${n} rows vs tools.emu at shift 0 (${late} LATE, `
    + `${partial} PARTIAL oam cells), puff f${killF}, coin f${coinF}`);
}
process.exit(fails ? 1 : 0);
