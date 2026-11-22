// node web/test/teleport_check.mjs — SubstTeleportMenu 30:DB28 (session 143; states/teleport.js): sub_state $14
// (magic 4) and the Warp Boots' ItemUseMenu call (sub_state 6).
// States (town $02, all town flags F2F5-F2FF set by the pokes 64DE=E0 64DF=FF, HP 03C1=30):
//   python3 -m tools.emu.web_state --load-state tools/emu/states/town02_enter.state --frames 10 --poke <pokes>
//     --keys 2500:UP:30 2545:<btn> --until <pc> --out web/test/states/teleport_NAME.json
//     --save-state tools/emu/states/teleport_NAME.state                       (both stop at frame 2545)
//   warp:  6440=24 0715=08, B, --until 30:D497   Warp Boots -> DOWN (index 3) + A -> area $D5, boots consumed, saved
//   magic: 0712=04 0708=60, A, --until 30:DB28   magic 4 -> RIGHT (index 1) + A -> area $18, MP $60 -> $4C
// References web/test/teleport_NAME_ref.csv = tools/web_state_compare.py --frames 420 --keys 2600:<dir> 2630:A
// (no --web-key-shift: the menu reads the pad in the same frame as tools.emu), cut by tools/csv_trim.py
// (core,pal,obj) to f2546-2734: menu, warp-out, area load, attribute wipe. From f2735 the port runs the $4x
// entry's fade-out one frame early and FullScreenDraw's lag rows differ (REVERSE.md s143 note), so the rest of
// the run is checked by end state only. Extra port-only cases from the ROM: B cancels, a locked town ignores A,
// boss_cam_lock refuses. Exit code 1 on any mismatch.
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

function machine(name, pokes = {}) {
  const m = new Machine(data, { log: () => {} });
  m.reset();
  const s = JSON.parse(readFileSync(path.join(WEB, `test/states/teleport_${name}.json`), 'utf8'));
  m.loadState(s);
  for (const [a, v] of Object.entries(pokes)) m.wr(Number(a), v);
  return { m, f0: s.frame0 ?? s.frame };
}

function run(name, dir, last, checks) {
  const before = fails;
  const [head, ...lines] = readFileSync(path.join(WEB, `test/teleport_${name}_ref.csv`), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const ref = new Map(lines.map((l) => { const v = l.split(',').map(Number); return [v[0], v]; }));
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error(`teleport_${name}_ref.csv has a column the port does not write`);
  const { m, f0 } = machine(name);
  const keys = parseKeys([`2600:${dir}`, '2630:A']);
  let rows = 0;
  while (f0 + m.frame <= last) {
    const f = f0 + m.frame;
    m.runFrame(keys.get(f) || 0);
    const w = ref.get(f);
    if (!w) continue;
    rows++;
    const row = compareRow(m, f);
    const d = refCols.filter((c, i) => i && row[idx[i]] !== w[i]);
    if (d.length) fail(`${name} f${f}: ${d.map((c) => `${c} want ${w[refCols.indexOf(c)]} got ${row[webCols.indexOf(c)]}`).join(' ')}`);
  }
  if (m.unported.size) fail(`${name}: ${[...m.unported].join(', ')}`);
  checks(m);
  console.log(`${fails > before ? 'FAIL' : 'ok  '} teleport ${name}: ${rows} rows vs tools.emu + end state at f${last}`);
}

run('warp', 'DOWN', 2964, (m) => {
  want(m, 0x6C, 0xD5, 'warp area (index 3)');
  want(m, 0x41, 0x08, 'warp sub_state');
  want(m, 0x6440, 0xFF, 'boots consumed');
  want(m, 0x7D40, 0xFF, 'save image without the boots (D4EA SetR7 + SaveGame)');
});
run('magic', 'RIGHT', 2844, (m) => {
  want(m, 0x6C, 0x18, 'magic area (index 1)');
  want(m, 0x41, 0x08, 'magic sub_state');
  want(m, 0x0708, 0x4C, 'MP $60 - $14');
});

/** port-only ROM paths: run the magic state with pokes/keys and check the result at frame `at` */
function path_(tag, pokes, keyArgs, at, checks) {
  const before = fails;
  const { m, f0 } = machine('magic', pokes);
  const keys = parseKeys(keyArgs);
  while (f0 + m.frame <= at) m.runFrame(keys.get(f0 + m.frame) || 0);
  if (m.unported.size) fail(`${tag}: ${[...m.unported].join(', ')}`);
  checks(m);
  console.log(`${fails > before ? 'FAIL' : 'ok  '} teleport ${tag}`);
}
// (a key of frame F is acted on in F+1, after the loop's WaitFrame; checked on that exit frame: one frame later the town $02 entry trigger under the player starts GiveItem, s139)
path_('cancel (B)', {}, ['2600:B'], 2601, (m) => {
  want(m, 0x41, 0x08, 'cancel sub_state');
  want(m, 0x6C, 0x02, 'cancel area');
  want(m, 0x51, 0x00, 'cancel irq_mode');
  want(m, 0x0200, 0xF0, 'cancel OAM 0');
  want(m, 0x0708, 0x60, 'cancel MP kept');
});
// index 1 = flag F2F6 = $64DE & BitMask[6] ($40)
path_('locked town', { 0x64DE: 0xA0 }, ['2600:RIGHT', '2630:A'], 2640, (m) => {
  want(m, 0x41, 0x14, 'locked: still in the menu');
  want(m, 0x6C, 0x02, 'locked area');
  want(m, 0x20, 0x01, 'locked: cursor column 1');
});
path_('same town', {}, ['2630:A'], 2631, (m) => {
  want(m, 0x41, 0x08, 'same town (index 0 = area $02) exits');
  want(m, 0x0708, 0x60, 'same town MP kept');
});
path_('boss lock', { 0x07D7: 0x10 }, [], 2546, (m) => {
  want(m, 0x41, 0x08, 'boss_cam_lock refuses');
  want(m, 0x51, 0x00, 'boss lock: no window (irq_mode 0)');
});
process.exit(fails ? 1 : 0);
