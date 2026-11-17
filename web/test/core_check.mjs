// node web/test/core_check.mjs — Ph7 web 2/8 core checks against the ROM (tools.emu traces).
//   1. ram.js address names == web/data/ram_labels.json (labels.csv bank `*`)
//   2. pads.js readPads/readPadsMenu == ROM ReadPads 31:FE80 / ReadPadsMenu 31:FEE0, $43-$4F after
//      every one of 160 steps (web/test/core_trace.json from `python3 -m tools.emu.core_trace`)
//   3. mainloop.js nmiWork == ROM NMI 31:F3B6 for every traced NMI: PPU t/x/w/ctrl/mask, palette RAM,
//      OAM, CHR R0-R5, IRQ latch, RAM $00 $07D8-$07DB $09 $56 $57. NMI-mode routines not ported yet
//      only get the DMA/palette/mask/RAM part compared (and are listed).
//   4. Machine smoke run: reset + 30 frames, frame_ready handshake, main state 2 -> 3.
// Exit code 1 on any mismatch.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { R } from '../src/game/ram.js';
import { readPads, readPadsMenu } from '../src/game/pads.js';
import { nmiWork, NMI_MODES } from '../src/game/mainloop.js';
import { Machine } from '../src/game/machine.js';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const hex = (s) => Uint8Array.from(Buffer.from(s, 'hex'));
const h2 = (n) => n.toString(16).padStart(2, '0');
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const trace = JSON.parse(readFileSync(path.join(WEB, 'test/core_trace.json'), 'utf8'));
let fails = 0;
const fail = (msg) => { fails++; if (fails <= 40) console.log('  FAIL ' + msg); };

// ---------------------------------------------------------------- 1. RAM names
{
  const labels = data.ramLabels;
  let bad = 0;
  for (const [k, a] of Object.entries(R)) {
    if (labels[k] !== a) { bad++; fail(`R.${k} = $${a.toString(16)} but ram_labels.json has ${labels[k]}`); }
  }
  console.log(`${bad ? 'FAIL' : 'ok  '} ram.js names: ${Object.keys(R).length} checked against ram_labels.json`);
}

// ---------------------------------------------------------------- 2. pads
for (const [name, fn] of [['ReadPads', readPads], ['ReadPadsMenu', readPadsMenu]]) {
  const t = trace.pads[name];
  const m = new Machine(data);
  m.reset();
  t.start.forEach((v, i) => m.wr(0x43 + i, v));
  let bad = 0;
  trace.pads.buttons.forEach((b, step) => {
    m.buttons = [b, 0];
    fn(m);
    const got = []; for (let a = 0x43; a < 0x50; a++) got.push(m.rd(a));
    const want = t.steps[step];
    for (let i = 0; i < got.length; i++) {
      if (got[i] !== want[i]) {
        bad++;
        fail(`${name} step ${step} btn ${h2(b)}: $${h2(0x43 + i)} want ${h2(want[i])} got ${h2(got[i])}`);
        break;
      }
    }
  });
  console.log(`${bad ? 'FAIL' : 'ok  '} ${name} (${t.addr}): ${t.steps.length} steps, ${bad} differing`);
}

// ---------------------------------------------------------------- 3. NMI
{
  const unportedSeen = new Set();
  let n = 0, bad = 0, partial = 0, full = 0, irqCmp = 0;
  for (const [st, recs] of Object.entries(trace.nmi)) {
    for (const r of recs) {
      n++;
      const m = new Machine(data);
      m.reset();
      m.mem.ram.set(hex(r.ram));
      if (r.wram) m.mem.wram.set(hex(r.wram));
      const p = m.ppu, pre = r.pre;
      Object.assign(p, { t: pre.t, v: pre.v, x: pre.x, w: pre.w, ctrl: pre.ctrl, mask: pre.mask });
      p.pal.set(hex(pre.pal)); p.oam.set(hex(pre.oam));
      pre.chr.forEach((v, i) => p.setChrReg(i, v));
      const mode = m.rd(R.irq_mode);
      if (!NMI_MODES[mode]) { bad++; fail(`${st}: irq_mode ${mode} has no NMI routine`); continue; }
      nmiWork(m);
      const isPorted = !m.unported.size;
      if (!isPorted) { partial++; for (const u of m.unported) unportedSeen.add(`${u} (irq_mode ${mode})`); }
      const post = r.post, pram = hex(r.post_ram);
      const tag = `${st} f${r.frame} mode ${mode}`;
      const errs = [];
      const cmp = (what, want, got) => { if (want !== got) errs.push(`${what} want ${want.toString(16)} got ${got.toString(16)}`); };
      cmp('mask', post.mask, p.mask);
      if (Buffer.from(p.pal).toString('hex') !== post.pal) errs.push('palette RAM');
      if (Buffer.from(p.oam).toString('hex') !== post.oam) errs.push('OAM');
      for (const a of [0x07D8, 0x07D9, 0x07DA, 0x07DB, R.frame_ready]) cmp(`$${a.toString(16)}`, pram[a], m.rd(a));
      if (isPorted) {
        full++;
        if (post.irq_enabled) irqCmp++;
        cmp('t', post.t, p.t); cmp('x', post.x, p.x); cmp('w', post.w, p.w); cmp('ctrl', post.ctrl, p.ctrl);
        for (const a of [R.ppuctrl_shadow, 0x56, R.zp_57]) cmp(`$${a.toString(16)}`, pram[a], m.rd(a));
        post.chr.forEach((v, i) => cmp(`R${i}`, v, p.chrRegs[i]));
        if (post.irq_enabled && m.irqSplits.length) cmp('irq latch', post.irq_latch, m.irqSplits[0].latch);
        if (post.irq_enabled !== (m.irqSplits.length > 0) && (m.rd(R.ppumask_shadow) & 0x18)) {
          errs.push(`irq enabled want ${post.irq_enabled} got ${m.irqSplits.length > 0}`);
        }
      }
      if (errs.length) { bad++; fail(`NMI ${tag}: ${errs.join('; ')}`); }
    }
  }
  console.log(`${bad ? 'FAIL' : 'ok  '} NMI 31:F3B6: ${n} traced NMIs, ${bad} differing; ${full} fully compared (${irqCmp} with IRQ on), ` +
    `${partial} with an unported NMI-mode routine` +
    (unportedSeen.size ? ` (${[...unportedSeen].join(', ')}: DMA/palette/mask/scroll-copy part only)` : ''));
}

// ---------------------------------------------------------------- 4. Machine smoke
{
  const logs = [];
  const m = new Machine(data, { log: (s) => logs.push(s) });
  m.reset();
  const states = [];
  for (let f = 0; f < 30; f++) { m.runFrame(0); states.push(m.rd(R.main_state)); }
  const errs = [];
  if (m.rd(R.frame_ready) !== 1) errs.push(`frame_ready after a frame = ${m.rd(R.frame_ready)} (want 1)`);
  if (states[0] !== 2) errs.push(`frame 0 main_state ${states[0]} (want 2: first NMI sets frame_ready, no state ran yet)`);
  if (states[1] !== 3) errs.push(`frame 1 main_state ${states[1]} (want 3 after MainState2Init's INC)`);
  if ((m.stubHits.state3_title || 0) !== 28) errs.push(`state 3 ran ${m.stubHits.state3_title} times (want 28)`);
  if (m.ppu.fb.length !== 256 * 240) errs.push('framebuffer size');
  if (errs.length) { fails++; console.log('FAIL Machine smoke: ' + errs.join('; ')); } else {
    console.log(`ok   Machine smoke: 30 frames, main_state 2 -> 3, stubs ${JSON.stringify(m.stubHits)}; log: ${logs.join(' | ') || '-'}`);
  }
}

console.log(fails ? `${fails} failure(s)` : 'all core checks passed');
process.exit(fails ? 1 : 0);
