// node web/test/catalog_check.mjs — headless load test for web/catalog.js.
// Stubs a tiny DOM + fetch (served from the repo files) + canvas/ImageData, then runs the boot
// sequence and every tab's init so syntax errors and data-shape mismatches surface without a
// browser. Reports per-tab success and some drawn-pixel counts.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));

class ImageDataStub {
  constructor(w, h) { this.width = w; this.height = h; this.data = new Uint8ClampedArray(w * h * 4); }
}
let drawnPixels = 0;
function el(id) {
  const e = {
    id, value: '', checked: false, innerHTML: '', textContent: '', style: {}, children: [],
    dataset: {}, width: 0, height: 0, src: '', title: '', className: '',
    classList: { toggle() {}, contains: () => true },
    appendChild(c) { this.children.push(c); return c; }, append() {},
    getContext: () => ({
      putImageData(img) { for (let i = 3; i < img.data.length; i += 4) if (img.data[i]) drawnPixels++; },
      drawImage() {}, fillRect() {}, set imageSmoothingEnabled(v) {},
    }),
    querySelectorAll: () => [],
  };
  return e;
}
const els = {};
globalThis.ImageData = ImageDataStub;
globalThis.document = {
  getElementById: (id) => (els[id] ||= el(id)),
  createElement: (t) => el(t),
  querySelectorAll: (sel) => (sel === 'header button' ? BUTTONS : []),
};
const BUTTONS = ['chr', 'pal', 'spr', 'area', 'scr', 'snd', 'txt'].map((t) => Object.assign(el('b' + t), { dataset: { tab: t } }));
globalThis.performance ||= { now: () => Date.now() };
const fetched = [];
globalThis.fetch = async (p) => {
  const f = path.resolve(WEB, p);
  fetched.push(path.relative(WEB, f));
  const buf = readFileSync(f);
  return { ok: true, status: 200, json: async () => JSON.parse(buf), arrayBuffer: async () => buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.length) };
};
const origSetInterval = globalThis.setInterval;
globalThis.setInterval = () => 0; // sprite animation timer: not needed here

els.chrPal = el('chrPal'); els.chrPal.value = '-1';
els.chrScale = el('chrScale'); els.chrScale.value = '3';
els.sprScale = el('sprScale'); els.sprScale.value = '2';
els.sprArea = el('sprArea');
els.areaSel = el('areaSel'); els.areaSel.value = '3';
els.areaZoom = el('areaZoom'); els.areaZoom.value = '0.5';
els.scrFilter = el('scrFilter');
els.txtTab = el('txtTab'); els.txtTab.value = 'messages';
els.txtQ = el('txtQ'); els.txtQ.value = 'sword';
els.sndMax = el('sndMax'); els.sndMax.value = '5';

await import('../catalog.js');
await new Promise((r) => setTimeout(r, 500));
console.log('boot status:', JSON.stringify(els.status.textContent), 'chr pixels drawn:', drawnPixels);

// drive each tab via the header buttons' onclick (showTab); status '' = init succeeded
for (const b of BUTTONS) {
  const before = drawnPixels;
  await b.onclick();
  await new Promise((r) => setTimeout(r, 50));
  console.log(`tab ${b.dataset.tab.padEnd(4)} status ${JSON.stringify(els.status.textContent)} pixels +${drawnPixels - before}`);
}
console.log('sprite cells (area $03):', els.sprGrid.children.length, '| sprInfo:', els.sprInfo.textContent);
console.log('areaInfo:', els.areaInfo.textContent.split('\n')[0]);
console.log('text rows (sword):', els.txtCount.textContent);
console.log('screens html length:', els.scrGrid.innerHTML.length, 'rects:', (els.rectGrid.innerHTML.match(/<img/g) || []).length);
console.log('sound rows:', (els.sndTable.innerHTML.match(/<tr>/g) || []).length - 1);
console.log('fetched:', fetched.join(', '));
globalThis.setInterval = origSetInterval;
