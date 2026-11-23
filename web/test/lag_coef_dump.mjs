// s219: write the port's lag-model coefficients (web/src/game/lagmodel.js LAG_COEF) as a tools/pass_fit.py --coef
// JSON (keys top / cam / spr / col / updrest / ai* / aiXX / hud), so new AI ids can be fitted on top of them with
// `pass_fit.py COST COUNTS --stages --ai --coef OUT.json --fit-new`.
// usage: node web/test/lag_coef_dump.mjs OUT.json
import { writeFileSync } from 'node:fs';
import { LAG_COEF } from '../src/game/lagmodel.js';

const out = process.argv[2];
if (!out) throw new Error('usage: node web/test/lag_coef_dump.mjs OUT.json');
const j = {};
for (const k of ['top', 'cam', 'spr', 'col', 'updrest', 'hud']) j[k] = LAG_COEF[k];
for (const [id, c] of Object.entries(LAG_COEF.ai)) j['ai' + id] = c;
writeFileSync(out, JSON.stringify(j));
console.log(`${Object.keys(j).length} coefficient sets -> ${out}`);
