// node web/test/group09_check.mjs — enemy group of area $09 vs tools.emu (session 180): the area's own spawn list
// (no ObjInit pokes): slots $0D/$0F/$11 type $56 = AI $24 AiChase 27:AA57, slot $14 type $53 = AI $20 AiWalkPause
// 27:A915, slot $16 = the h3 p=$00 entry (AI $87), which puts an AI-$24 chaser into slot $0E at f2676. The player
// (level 16, HP $60) climbs the shaft into the y=613 corridor, walks right into $11/$14 and swings B; the
// chasers' contact hits (26:9413, $0F each) take HP to 0 at f3268 -> sub_state 3 PlayerDeath 19:B900 at f3269,
// the death fade / pose / GAME OVER run through f3504.
// State: python3 -m tools.emu.scenario_state group_09 12 --out web/test/states/group09.json
//          --save-state tools/emu/states/group09.state
// Ref:   python3 -m tools.emu --load-state tools/emu/states/group09.state --frames 1000 --compare gfx/group09/emu.csv
//          --keys 2530:UP:50 2585:RIGHT:330 2930:B:4 2950:B:4 2970:B:4 2990:RIGHT:20 3020:B:4 3040:B:4
//          3060:RIGHT:30 3100:B:4 3120:B:4 3140:B:4 3160:B:4, cut by tools/csv_trim.py to f2507-3504
//          (--cols core,snd,pal,obj,oam_crc).
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes (`--watch 0043`, +1 when line
// >= 241), press and release each (as in group08_check.mjs).
// f2507-3268 (gameplay, 762 rows) are equal at shift 0, snd included. The death sequence has the cells s89 saw
// in the death check (tools.emu's row is sampled after its next pass has started in vblank; tools/cell_class.py):
//   LATE (the port's next row has the tools.emu value): f3271 music (PlayerDeath's music stop), f3327 every
//     object column (the slot clear before the death pose);
//   PARTIAL pal_crc f3293/3304/3315/3326 (FadeOut steps: tools.emu's row holds a half-built PaletteBuild buffer,
//     equal to neither neighbour; accepted as listed);
//   PARTIAL oam_crc f3329/3330 and every 8 rows f3338-3386, f3393 (pose changes: OAM bytes 4..k-1 already
//     rewritten by the next pass, byte-granular); accepted only if such a mix of the port's rows f / f+1 has the reference crc.
// Exit code 1 on any other mismatch.
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
const LAST = 3504;
const KEYS = parseKeys(['2531:UP:50', '2585:RIGHT:330', '2931:B:4', '2951:B:4', '2971:B:4', '2990:RIGHT:21',
  '3022:B:3', '3040:B:5', '3061:RIGHT:29', '3101:B:4', '3121:B:4', '3141:B:4', '3161:B:4']);
// tools.emu --watch 03C1 (pc 26:9413; the last one $FB is clamped to 0 at 26:941A in the same frame)
const HITS = [[2822, 0x55], [2919, 0x46], [2989, 0x37], [3020, 0x28], [3052, 0x19], [3197, 0x0A], [3268, 0]];
const DEATH = 3269;   // sub_state 3 (tools.emu $41 write at f3269 L3)
const LATE = new Map([[3271, ['music']], [3327, ['o']]]);
const PARTIAL_PAL = [3293, 3304, 3315, 3326];
const PARTIAL_OAM = [3329, 3330, 3338, 3346, 3354, 3362, 3370, 3378, 3386, 3393];

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group09.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group09_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group09_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map(), oam = new Map(), hits = [];
let hp = m.rd(0x03C1), deathF = -1, spawnF = -1;
const ais = [0x04AD, 0x04AF, 0x04B1, 0x04B4].map((a) => m.rd(a));
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  oam.set(f, Uint8Array.from({ length: 256 }, (_, i) => m.rd(0x200 + i)));
  if (m.rd(0x03C1) !== hp) { hp = m.rd(0x03C1); hits.push([f, hp]); }
  if (deathF < 0 && m.rd(0x41) === 3) deathF = f;
  if (spawnF < 0 && m.rd(0x04AE) === 0x24) spawnF = f;
}
// tools.emu's crc = OAM of row f with bytes 4..k-1 already rewritten by the pass of row f+1 (byte-granular:
// at f3329 / f3393 the sample falls inside entry 4, Y written, tile / attr / X still old)
const midPass = (f, want) => {
  const a = oam.get(f), b = oam.get(f + 1);
  if (!a || !b) return false;
  for (let k = 5; k <= 256; k++) {
    const mix = Uint8Array.from(a);
    mix.set(b.subarray(4, k), 4);
    if ((crc32(mix) >>> 0) === want) return true;
  }
  return false;
};
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (JSON.stringify(ais) !== JSON.stringify([0x24, 0x24, 0x24, 0x20]))
  fail(`slots $0D/$0F/$11/$14 AI ${ais.map((a) => '$' + a.toString(16)).join(' ')}, want $24 $24 $24 $20`);
if (spawnF !== 2676) fail(`h3 chaser (slot $0E AI $24) at f${spawnF}, want f2676`);
if (JSON.stringify(hits) !== JSON.stringify(HITS))
  fail(`HP changes ${JSON.stringify(hits)}, want ${JSON.stringify(HITS)}`);
if (deathF !== DEATH) fail(`sub_state 3 at f${deathF}, want f${DEATH}`);
let n = 0, late = 0, partial = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    const c = refCols[i];
    if (row[idx[i]] === w[i]) continue;
    if (LATE.get(f)?.some((p) => c === p || (p === 'o' && /^o[0-9A-F]{2}_/.test(c)))
        && rows.get(f + 1)?.[idx[i]] === w[i]) { late++; continue; }
    if (c === 'pal_crc' && PARTIAL_PAL.includes(f)) { partial++; continue; }
    if (c === 'oam_crc' && PARTIAL_OAM.includes(f) && midPass(f, w[i])) { partial++; continue; }
    fail(`f${f}: ${c} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} group09: ${n} rows vs tools.emu at shift 0 (snd incl.; ${late} LATE, `
  + `${partial} PARTIAL death-sequence cells), ${hits.length} contact hits, death f${deathF}`);
process.exit(fails ? 1 : 0);
