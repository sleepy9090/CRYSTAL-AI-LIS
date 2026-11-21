// node web/test/itemuse_check.mjs — ItemUseMenu 30:D497 (sub_state 6, session 141; states/giveitem.js) + ItemUseHook
// 14:831E (hook 1) + ShowPendingMessage 30:D085 (sub_state $10).
// States: python3 -m tools.emu.web_state --load-state tools/emu/states/town02_enter.state --frames 10
//   --poke <pokes> --keys 2500:UP:30 2545:B --until 30:D497 --out web/test/states/itemuse_NAME.json
//   --save-state tools/emu/states/itemuse_NAME.state   (all stop at frame 2545)
//   herb: 6440=1D 0715=01 03C1=10  Medical Herb -> touch match fails -> HP +32, consumed, message
//   fail: 6450=27 0715=0B 03C1=30  Insect Flute outside area $1A -> SFX $48, UseFail -> sub_state $10, "Nothing happens."
//   opel: 6440=26 0715=0A 03C1=30  Opel Statue by hand -> refused, SubstPlay in the same frame, nothing consumed
// References web/test/itemuse_NAME_ref.csv = tools/web_state_compare.py --frames 200 --keys 2620:A 2660:A 2700:A
// --web-key-shift 1, cut by tools/csv_trim.py (core,pal,obj) before the town trigger baseline (from there tools.emu's
// end-of-frame `sub` reads 8 where the port's reads 7: the trigger's GiveItem double pass, s139). The opel ref leaves
// out `sub` (the player is back on the trigger at once). Exit code 1 on any mismatch.
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

function run(name, checks) {
  const before = fails;
  const [head, ...lines] = readFileSync(path.join(WEB, `test/itemuse_${name}_ref.csv`), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const ref = new Map(lines.map((l) => { const v = l.split(',').map(Number); return [v[0], v]; }));
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error(`itemuse_${name}_ref.csv has a column the port does not write`);
  const m = new Machine(data, { log: () => {} });
  m.reset();
  const s = JSON.parse(readFileSync(path.join(WEB, `test/states/itemuse_${name}.json`), 'utf8'));
  m.loadState(s);
  const f0 = s.frame0 ?? s.frame;
  const keys = parseKeys(['2621:A', '2661:A', '2701:A']);
  const last = Math.max(...ref.keys());
  const subs = new Set();
  let rows = 0;
  while (f0 + m.frame <= last) {
    const f = f0 + m.frame;
    m.runFrame(keys.get(f) || 0);
    subs.add(m.rd(0x41));
    const want = ref.get(f);
    if (!want) continue;
    rows++;
    const row = compareRow(m, f);
    const d = refCols.filter((c, i) => i && row[idx[i]] !== want[i]);
    if (d.length) fail(`${name} f${f}: ${d.map((c) => `${c} want ${want[refCols.indexOf(c)]} got ${row[webCols.indexOf(c)]}`).join(' ')}`);
  }
  const u = [...m.unported].filter((x) => x.startsWith('sub_state') || x.startsWith('ItemUse'));
  if (u.length) fail(`${name}: ${u.join(', ')}`);
  checks(m, subs);
  console.log(`${fails > before ? 'FAIL' : 'ok  '} item use ${name} f${Math.min(...ref.keys())}-${last}: ${rows} rows vs tools.emu`);
}

const hex = (v) => v.toString(16).padStart(2, '0');
const want = (m, a, v, tag) => { if (m.rd(a) !== v) fail(`${tag}: $${a.toString(16)} = ${hex(m.rd(a))}, want ${hex(v)}`); };

run('herb', (m) => {
  want(m, 0x6440, 0xFF, 'herb consumed');
  want(m, 0x0715, 0x00, 'herb use_item_sel');
  want(m, 0x03C1, 0x30, 'herb HP 16 + 32 (max 48)');
});
run('fail', (m, subs) => {
  want(m, 0x6450, 0x27, 'flute kept');
  if (!subs.has(0x10)) fail('fail: sub_state $10 (ShowPendingMessage) never seen');
});
run('opel', (m) => {
  want(m, 0x6440, 0x26, 'opel kept');
  want(m, 0x0715, 0x0A, 'opel use_item_sel');
});
process.exit(fails ? 1 : 0);
