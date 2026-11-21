// node web/test/giveitem_check.mjs — GiveItem 30:D3EB (sub_state 7, session 139; states/giveitem.js).
// 1. web/test/states/town02_enter.json (tools.emu area1_enter.state + pokes 006C=02 006D=00 0041=01, --until 30:CA2E,
//    frame 2450): entering town $02 puts the player on the invisible trigger $99 (slot $12, AI $78). tools.emu
//    (pass_probe) runs GiveItem -> 30:D539 -> ItemHook2 (condition true, no message, act 0) -> sub_state 8 ->
//    JMP MainStateGameplay every frame from f2495. The port must run 900 frames with no substate logged as unported.
// 2. web/test/states/giveitem.json (from tools/emu/states/town02_enter.state: --frames 40 --poke 0572=1D
//    --until 30:D3EB, frame 2495): the same trigger slot holding item $1D (Medical Herb) -> ItemGetHook 14:826F,
//    msg 32.12, A at 2600/2640 (port 2601/2641). Reference web/test/giveitem_ref.csv = tools.emu rows f2496-2694
//    (tools/web_state_compare.py --frames 200 --keys 2600:A 2640:A --web-key-shift 1, then tools/csv_trim.py with
//    core,pal,obj: every object slot; s140 added the town NPCs $0D-$10 once AI $30 was ported). Rows must match
//    exactly; the herb must be in $6440.
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

function start(name) {
  const m = new Machine(data, { log: () => {} });
  m.reset();
  const s = JSON.parse(readFileSync(path.join(WEB, 'test/states', name), 'utf8'));
  m.loadState(s);
  return [m, s.frame0 ?? s.frame];
}

{ // 1. town entry: no hang in GiveItem
  const [m] = start('town02_enter.json');
  let seen7 = false;
  for (let i = 0; i < 900; i++) { m.runFrame(0); if (m.rd(0x41) === 7) seen7 = true; }
  const subs = [...m.unported].filter((u) => u.startsWith('sub_state'));
  if (subs.length) fail(`town02: ${subs.join(', ')}`);
  if (!seen7) fail('town02: the trigger never set sub_state 7');
  if (m.rd(0x40) !== 1) fail(`town02: main_state ${m.rd(0x40)}`);
  console.log(`${fails ? 'FAIL' : 'ok  '} town02 entry: 900 frames, trigger $99 -> GiveItem, unported: ${[...m.unported].join(', ') || '-'}`);
}

{ // 2. item pickup vs tools.emu
  const before = fails;
  const [head, ...lines] = readFileSync(path.join(WEB, 'test/giveitem_ref.csv'), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const ref = new Map(lines.map((l) => { const v = l.split(',').map(Number); return [v[0], v]; }));
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error('giveitem_ref.csv has a column the port does not write');
  const [m, f0] = start('giveitem.json');
  const keys = parseKeys(['2601:A', '2641:A']);
  const last = Math.max(...ref.keys());
  let rows = 0;
  while (f0 + m.frame <= last) {
    const f = f0 + m.frame;
    m.runFrame(keys.get(f) || 0);
    const want = ref.get(f);
    if (!want) continue;
    rows++;
    const row = compareRow(m, f);
    const d = refCols.filter((c, i) => i && row[idx[i]] !== want[i]);
    if (d.length) fail(`f${f}: ${d.map((c) => `${c} want ${want[refCols.indexOf(c)]} got ${row[webCols.indexOf(c)]}`).join(' ')}`);
  }
  if (m.rd(0x6440) !== 0x1D) fail(`inventory $6440 = ${m.rd(0x6440).toString(16)}, want 1d`);
  if (m.rd(0x04B2) !== 0) fail('slot $12 still active after the pickup');
  console.log(`${fails > before ? 'FAIL' : 'ok  '} item pickup ($1D) f${Math.min(...ref.keys())}-${last}: ${rows} rows vs tools.emu`);
}
process.exit(fails ? 1 : 0);
