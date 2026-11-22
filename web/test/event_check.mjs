// node web/test/event_check.mjs — EventActionTable acts 1 EvActThunderWarp 30:D5A4 and 7 EvActMesiaMessage 30:D5E6
// (session 203; states/giveitem.js EVENT_ACTIONS, entered through states/play.js ENTRY_POINTS).
// States (scenario event_01 = area 03, item $1D selected; the act is forced at EventActionDispatch 30:D552):
//   python3 -m tools.emu.scenario_state event_01 10 --out /dev/null --save-state tools/emu/states/event_base.state
//   python3 -m tools.emu.web_state --load-state tools/emu/states/event_base.state --keys 2510:B:4 2530:B:4
//     --force 30:D552:0022=NN --until 30:D5A4 (NN 01) / 30:D5E6 (NN 07)
//     --out web/test/states/eventNN.json --save-state tools/emu/states/eventNN.state   (both stop at f2540 L251)
// References web/test/eventNN_ref.csv = tools/web_state_compare.py --web-key-shift 1, cut by tools/csv_trim.py:
//   07: --frames 200 --keys 2620:A 2660:A 2700:A, core,pal,obj, f2541-2739: every cell at shift 0.
//   01: --frames 460 --keys 2700:A 2740:A .. 2900:A (every 40), core,pal,obj,snd, f2541-2999. Listed cells:
//     LATE (port = tools.emu's previous row): music f2541 (the SwordRaisePose `snd_cur_music` |= $80 after the
//     first WaitFrame) and the pose's oam_crc every 8th frame f2550-2662 (the sprite pass after WaitFrame: this act
//     runs from ItemUseRun at L251, so tools.emu's L255 sample already holds the vblank work; gap (c));
//     PARTIAL: oam_crc f2542, pal_crc f2827/30/33/36 + f2852/55/58/61 (the $F2 load's fade out / in), f2837 music + object slots
//     (the area load's spawn pass), pos_y f2840-2843 (8 px: the entry slide is 1 step behind for 4 frames);
//     `sub` from f2863: the player stands on area $F2's event trigger, so tools.emu's end-of-frame sample reads 8
//     (after GiveTrigger) and the port 7 (the s139 trigger baseline, as in itemuse_check / group18_check).
// Exit code 1 on any mismatch.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys, compareColumns, compareRow } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
let fails = 0;
const fail = (msg) => { fails++; if (fails <= 30) console.log('  FAIL ' + msg); };
const hex = (v) => v.toString(16).padStart(2, '0');
const want = (m, a, v, tag) => { if (m.rd(a) !== v) fail(`${tag}: $${a.toString(16)} = ${hex(m.rd(a))}, want ${hex(v)}`); };

/** skip(f, col) -> 'late' (port = previous ref row), 'skip', or undefined (must match) */
function skip01(f, c) {
  if (f === 2541 && c === 'music') return 'skip';        // LATE, but the state's f2540 row is not in the ref
  if (c === 'oam_crc' && f >= 2550 && f <= 2662 && (f - 2550) % 8 === 0) return 'late';
  if (c === 'oam_crc' && f === 2542) return 'skip';
  if (c === 'pal_crc' && [2827, 2830, 2833, 2836, 2852, 2855, 2858, 2861].includes(f)) return 'skip';
  if (f === 2837 && (c === 'music' || /^o[0-9A-F]{2}_/.test(c))) return 'skip';
  if (c === 'pos_y' && f >= 2840 && f <= 2843) return 'skip';
  if (c === 'sub' && f >= 2863) return 'skip';
  return undefined;
}

function run(nn, keys, last, skip, checks) {
  const before = fails;
  const [head, ...lines] = readFileSync(path.join(WEB, `test/event${nn}_ref.csv`), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const ref = new Map(lines.map((l) => { const v = l.split(',').map(Number); return [v[0], v]; }));
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error(`event${nn}_ref.csv has a column the port does not write`);
  const m = new Machine(data, { log: () => {} });
  m.reset();
  const s = JSON.parse(readFileSync(path.join(WEB, `test/states/event${nn}.json`), 'utf8'));
  m.loadState(s);
  const f0 = s.frame0 ?? s.frame, pad = parseKeys(keys);
  let n = 0, listed = 0;
  while (f0 + m.frame <= last) {
    const f = f0 + m.frame;
    m.runFrame(pad.get(f) ?? 0);
    const w = ref.get(f);
    if (checks[f]) checks[f](m);
    if (!w) continue;
    n++;
    const row = compareRow(m, f), prev = ref.get(f - 1);
    const d = [];
    for (let i = 1; i < refCols.length; i++) {
      const c = refCols[i], got = row[idx[i]], k = skip ? skip(f, c) : undefined;
      if (k === 'skip') { listed++; continue; }
      if (k === 'late') { listed++; if (!prev || got !== prev[i]) d.push(`${c} (LATE) want ${prev && prev[i]} got ${got}`); continue; }
      if (got !== w[i]) d.push(`${c} want ${w[i]} got ${got}`);
    }
    if (d.length) fail(`event${nn} f${f}: ${d.join(' ')}`);
  }
  if (m.unported.size) fail(`event${nn}: ${[...m.unported].join(', ')}`);
  console.log(`${fails > before ? 'FAIL' : 'ok  '} event act ${+nn}: ${n} rows vs tools.emu (${listed} listed cells)`);
}

// act 7: slot 13 changes to meta $D2 for 32 frames, then $2E + type $DE fx in slot $1E at slot 13, msg 14.5
// (the act clears obj_active[$0623] = the item-use slot 3 on this path, not slot 13)
run('07', ['2621:A', '2661:A', '2701:A'], 2739, null, {
  2560: (m) => { want(m, 0x030D, 0xD2, 'f2560 slot 13 meta'); want(m, 0x04A3, 0, 'f2560 slot 3 off'); },
  2739: (m) => {
    want(m, 0x030D, 0x2E, 'slot 13 meta'); want(m, 0x06CD, 0x2E, 'orbit_level_s0D');
    want(m, 0x04BE, 0xFF, 'slot $1E active (type $DE)');
    for (const [a, b] of [[0x7D, 0x8E], [0x9D, 0xAE], [0xBD, 0xCE], [0xDD, 0xEE]]) want(m, b, m.rd(a), `slot $1E pos = slot 13 ($${b.toString(16)})`);
    want(m, 0x41, 8, 'sub_state');
  },
});
// act 1: SwordRaisePose, msg 28.17, SFX $23, warp to area $F2 entry $41
run('01', [2700, 2740, 2780, 2820, 2860, 2900].map((f) => `${f + 1}:A`), 2999, skip01, {
  2600: (m) => want(m, 0x0301, 0xB8, 'f2600 slot 1 raise pose'),
  2999: (m) => { want(m, 0x6C, 0xF2, 'area_id'); want(m, 0x0600, 0, 'obj_phase'); want(m, 0x06C0, 0, 'orbit_level'); },
});
process.exit(fails ? 1 : 0);
