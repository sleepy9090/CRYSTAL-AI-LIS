// node web/test/group18_check.mjs — area $18 (a town) vs tools.emu (session 183): the area's own spawn list is
// h1 records only, six type-$30 NPCs (AI $30; p=$16 $18 $19 $17 $1A $08 in slots $0D-$12, five of them walking)
// plus the h2 event trigger p=$A6 in slot $13 (act 0, empty msg, `if !F000`).
// The player (level 16) walks left over the trigger into the NPC p=$16 in slot $0D: TalkProbeHit 26:94B0 ->
// TalkStart 26:9508 (sub_state $11, $0623 = $0D) at f2562, the message ("I'm Akahana. ...") is paged and closed
// by two A presses (sub_state 8 at f2769, 30:D3C1), then UP and LEFT walk about town.
// State: python3 -m tools.emu.scenario_state group_18 12 --out web/test/states/group18.json
//          --save-state tools/emu/states/group18.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group18.state --web web/test/states/group18.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group18 --keys 2526:LEFT:36 2700:A:4
//          2760:A:4 2800:UP:30 2840:LEFT:80, cut by tools/csv_trim.py to f2508-2984.
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes (`--watch 0043`, +1 when line
// >= 241), press and release each (as in group08_check.mjs).
// Listed cells: `sub` f2535-2556 (22 rows, tools.emu 8, port 7). While the slot-3 probe overlaps the trigger in
// slot $13, TalkProbeItemUse 26:94A2 sets sub_state 7 in the pass (L149); the next pass starts in vblank and its
// GiveTrigger 30:D539 (INC INC at L254) + 30:D880 (sub_state 8 at L258) run before tools.emu's L261 row sample,
// while the port's row is the end of the pass (7). cell_class: 1 EARLY + 20 PARTIAL + 1 LATE, all this one case.
// Exit code 1 on any other difference.
import { readFileSync } from 'node:fs';
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
const LAST = 2984;
const KEYS = parseKeys(['2527:LEFT:47', '2701:A:4', '2761:A:9', '2801:UP:34', '2841:LEFT:80']);
const TALK = 2562, TALK_END = 2769;          // tools.emu --watch 0041: $11 (26:9514), 8 (30:D3C1)
const LISTED = [2535, 2556];                 // `sub` 8 vs 7, see above

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group18.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group18_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group18_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
let talk = -1, talkEnd = -1, npc = -1;
const ai = Array.from({ length: 6 }, (_, i) => m.rd(0x04AD + i) & 0x7F);
while (f0 + m.frame <= LAST + 1) {
  const f = f0 + m.frame;
  const prev = m.rd(0x0041);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  const s = m.rd(0x0041);
  if (talk < 0 && s === 0x11) { talk = f; npc = m.rd(0x0623); }
  if (talk >= 0 && talkEnd < 0 && prev === 0x11 && s !== 0x11) talkEnd = f;
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai.some((a) => a !== 0x30)) fail(`slots $0D-$12 AI ${ai.map((a) => a.toString(16))}, want all $30`);
if (talk !== TALK || npc !== 0x0D) fail(`talk f${talk} npc $${npc.toString(16)}, want f${TALK} $0d`);
if (talkEnd !== TALK_END) fail(`talk end f${talkEnd}, want f${TALK_END}`);
let n = 0, listed = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] === w[i]) continue;
    if (refCols[i] === 'sub' && f >= LISTED[0] && f <= LISTED[1] && w[i] === 8 && row[idx[i]] === 7) { listed++; continue; }
    fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
if (listed !== 22) fail(`${listed} listed sub cells, want 22`);
console.log(`${fails ? 'FAIL' : 'ok  '} group18: ${n} rows vs tools.emu at shift 0 (snd incl., ${listed} listed sub cells), `
  + `talk f${talk}-f${talkEnd} with slot $${npc.toString(16)}`);
process.exit(fails ? 1 : 0);
