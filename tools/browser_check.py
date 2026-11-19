#!/usr/bin/env python3
"""Check web/catalog.html in a real browser (headless Firefox driven over Marionette, stdlib only).

usage: browser_check.py [--firefox PATH] [--out gfx/browser] [--keep-profile] [--areas N] [--track KIND:ID]

Starts `python3 -m http.server` on the repo root and `firefox --headless --marionette` with a throw-away
profile, opens /web/catalog.html and walks every tab:
  CHR      : the 128 bank canvases (grayscale slot) read back with getImageData and compared pixel by pixel
             against gfx/chr/chr_NNN.png (sampled at the PNG's scale)
  Palettes : 256 slot cells + 64 master swatches
  Sprites  : every area in the selector: the static sheet <img> (../gfx/sprites/area_NNN.png) must load with
             the same size as the file on disk, and the metasprite canvases must not be blank
  Areas / Screens : every PNG URL of the manifest fetched from the page (status 200)
  Sound    : a trusted click on one track's play button with an AnalyserNode spliced in front of the
             destination: AudioContext state, currentTime advance, output RMS
  Text     : message table row count
Screenshots of each tab go to OUT/tab_<id>.png.  Exit 1 on any failure.
"""
import os
import sys

# tools/dis.py shadows the stdlib `dis` that argparse (via inspect) needs: load it with tools/ off the path
_HERE = os.path.dirname(os.path.abspath(__file__))
sys.path = [p for p in sys.path if os.path.abspath(p or '.') != _HERE]
import inspect  # noqa: E402,F401

import argparse  # noqa: E402
import base64  # noqa: E402
import json  # noqa: E402
import re  # noqa: E402
import shutil  # noqa: E402
import socket  # noqa: E402
import subprocess  # noqa: E402
import tempfile  # noqa: E402
import time  # noqa: E402

sys.path.insert(0, _HERE)
from render_compare import read_png  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ELEM = 'element-6066-11e4-a52f-4f735466cecf'


def free_port():
    s = socket.socket()
    s.bind(('127.0.0.1', 0))
    p = s.getsockname()[1]
    s.close()
    return p


class Marionette:
    def __init__(self, port, timeout=60):
        t0 = time.time()
        while True:
            try:
                self.s = socket.create_connection(('127.0.0.1', port), timeout=5)
                break
            except OSError:
                if time.time() - t0 > timeout:
                    raise
                time.sleep(0.5)
        self.s.settimeout(300)
        self.buf = b''
        self.hello = self._recv()
        self.id = 0

    def _recv(self):
        while b':' not in self.buf:
            self.buf += self._chunk()
        n, rest = self.buf.split(b':', 1)
        n = int(n)
        while len(rest) < n:
            rest += self._chunk()
        self.buf = rest[n:]
        return json.loads(rest[:n])

    def _chunk(self):
        d = self.s.recv(1 << 20)
        if not d:
            raise ConnectionError('marionette closed')
        return d

    def cmd(self, name, params=None):
        self.id += 1
        msg = json.dumps([0, self.id, name, params or {}]).encode()
        self.s.sendall(str(len(msg)).encode() + b':' + msg)
        while True:
            r = self._recv()
            if r[0] == 1 and r[1] == self.id:
                break
        if r[2]:
            raise RuntimeError(f'{name}: {r[2].get("error")}: {r[2].get("message")}')
        return r[3]

    def js(self, script, *args):
        return self.cmd('WebDriver:ExecuteScript', {'script': script, 'args': list(args)}).get('value')

    def ajs(self, script, *args):
        return self.cmd('WebDriver:ExecuteAsyncScript', {'script': script, 'args': list(args)}).get('value')

    def click(self, css):
        r = self.cmd('WebDriver:FindElement', {'using': 'css selector', 'value': css})
        r = r.get('value', r)
        el = r.get(ELEM) or r.get('ELEMENT') or next(iter(r.values()))
        self.cmd('WebDriver:ElementClick', {'id': el})

    def shot(self, path):
        v = self.cmd('WebDriver:TakeScreenshot', {'full': False, 'hash': False})['value']
        with open(path, 'wb') as f:
            f.write(base64.b64decode(v))


WAIT_STATUS = """
const done = arguments[arguments.length - 1], t0 = Date.now(), test = new Function(arguments[0]);
(function poll() {
  const s = document.getElementById('status').textContent;
  let ok = false; try { ok = s === '' && test(); } catch (e) { }
  if (ok) return done({ok: true, status: s, ms: Date.now() - t0});
  if (Date.now() - t0 > arguments[1]) return done({ok: false, status: s});
  setTimeout(poll, 100);
})();
"""


def wait(m, cond='return true', ms=60000):
    return m.ajs(WAIT_STATUS.replace('arguments[1]', str(ms)), cond)


def check_chr(m, fails):
    banks = m.js("""
      return [...document.querySelectorAll('#chrGrid .cell canvas')].map((c) => {
        const d = c.getContext('2d').getImageData(0, 0, c.width, c.height).data; let s = '';
        for (let i = 0; i < d.length; i += 4) s += Math.round(d[i] / 85);
        return [c.width, c.height, s];
      });""")
    bad = []
    if len(banks) != 128:
        fails.append(f'CHR: {len(banks)} bank canvases, expected 128')
    for b, (w, h, s) in enumerate(banks):
        pw, ph, px = read_png(os.path.join(ROOT, 'gfx', 'chr', f'chr_{b:03d}.png'))
        sc = pw // w
        ref = ''.join(str(round(px[(y * sc) * pw + x * sc][0] / 85)) for y in range(h) for x in range(w))
        if (w * sc, h * sc) != (pw, ph) or ref != s:
            bad.append(b)
    if bad:
        fails.append(f'CHR: {len(bad)} banks differ from gfx/chr: {bad[:16]}')
    return f'CHR: {len(banks)} banks read back, {len(banks) - len(bad)} identical to gfx/chr/chr_NNN.png'


def check_spr(m, fails, limit):
    ids = m.js("return [...document.querySelectorAll('#sprArea option')].map((o) => o.value);")
    if limit:
        ids = ids[:limit]
    res = m.ajs("""
      const done = arguments[arguments.length - 1], ids = arguments[0], out = [], sel = document.getElementById('sprArea');
      (async () => {
        for (const id of ids) {
          sel.value = id; sel.onchange();
          const img = document.getElementById('sprSheet');
          for (let t = 0; t < 100 && !(img.complete && img.src); t++) await new Promise((r) => setTimeout(r, 20));
          await new Promise((r) => setTimeout(r, 0));
          const cells = [...document.querySelectorAll('#sprGrid .cell')].map((cell) => {
            const c = cell.querySelector('canvas'), d = c.getContext('2d').getImageData(0, 0, c.width, c.height).data;
            // background = (17,17,22) for a drawn cell, #111 for an empty metasprite
            let lit = false;
            for (let i = 0; i < d.length && !lit; i += 4)
              lit = !(d[i] === 17 && d[i + 1] === 17 && (d[i + 2] === 22 || d[i + 2] === 17));
            return [cell.textContent, lit];
          });
          out.push([id, new URL(img.src).pathname, img.naturalWidth, img.naturalHeight, cells]);
        }
        done(out);
      })();""", ids)
    with open(os.path.join(ROOT, 'gfx', 'sprites.json')) as f:
        sj = json.load(f)
    chr_bin = open(os.path.join(ROOT, 'web', 'data', 'chr.bin'), 'rb').read()

    def drawable(frame, tadd, chrs):
        """True if this metasprite frame puts at least one opaque pixel inside the 64x64 cell
        (catalog.js drawSprCell: origin (32, 33), register FF = skipped)."""
        for dx, dy, attr, t0 in frame:
            t = (t0 + tadd) & 0xFF
            bank = chrs[t >> 6]
            if bank == 0xFF:
                continue
            base = (bank & 0x7F) * 1024 + (t & 63) * 16
            for r in range(8):
                py = 33 + dy + (7 - r if attr & 0x80 else r)
                if not 0 <= py < 64:
                    continue
                bits = chr_bin[base + r] | chr_bin[base + 8 + r]
                for b in range(8):
                    if bits >> (7 - b) & 1 and 0 <= 32 + dx + (7 - b if attr & 0x40 else b) < 64:
                        return True
        return False

    bad, ncells, nlit, nexp = [], 0, 0, 0
    for aid, path, w, h, cells in res:
        chrs = [int(c, 16) for c in sj['areas'][aid]['chr']]
        for label, lit in cells:
            mm = re.search(r'player \$([0-9A-F]{2})|m\$([0-9A-F]{2})(?:\+([0-9A-F]{2}))?', label)
            meta = int(mm.group(1) or mm.group(2), 16)
            tadd = int(mm.group(3), 16) if mm.group(3) else 0
            ms = sj['metasprites'].get(f'{meta:02X}') or {}
            fr = [drawable(f, tadd, chrs) for f in ms.get('frames') or []]
            ncells += 1
            nlit += lit
            nexp += all(fr) and bool(fr)
            if (lit and not any(fr)) or (not lit and fr and all(fr)):
                bad.append(f'{aid} "{label}": lit={lit}, drawable frames {fr}')
    for aid, path, w, h, cells in res:
        f = os.path.join(ROOT, path.lstrip('/'))
        if not os.path.exists(f):
            bad.append(f'{aid}: {path} missing')
            continue
        with open(f, 'rb') as fh:
            hdr = fh.read(24)
        fw, fh_ = int.from_bytes(hdr[16:20], 'big'), int.from_bytes(hdr[20:24], 'big')
        if (w, h) != (fw, fh_):
            bad.append(f'{aid}: img {w}x{h} vs file {fw}x{fh_}')
    if bad:
        fails.append(f'Sprites: {len(bad)} problems: {bad[:8]}')
    return (f'Sprites: {len(res)} areas, every sheet loaded at its file size; {ncells} metasprite canvases, '
            f'{nlit} drawn (each matches gfx/sprites.json: drawn iff its frame puts a non-blank tile on a mapped '
            f'CHR register; {nexp} drawable in every frame)') + (f' ({len(bad)} bad)' if bad else '')


def check_urls(m, fails):
    res = m.ajs("""
      const done = arguments[arguments.length - 1];
      (async () => {
        const man = await (await fetch('data/catalog_manifest.json')).json();
        const urls = [...man.areas.map((a) => a.png), ...man.sprite_sheets.map((s) => s.png ? '../' + s.png : null),
                      ...man.screens, ...man.rects.map((r) => r.png), ...Object.values(man.images)].filter(Boolean);
        const bad = [];
        for (let i = 0; i < urls.length; i += 16)
          await Promise.all(urls.slice(i, i + 16).map(async (u) => { const r = await fetch(u, {method: 'HEAD'}); if (!r.ok) bad.push(u + ' ' + r.status); }));
        done([urls.length, bad]);
      })();""")
    n, bad = res
    if bad:
        fails.append(f'URLs: {len(bad)}/{n} manifest images fail: {bad[:6]}')
    return f'Areas/Screens: {n} manifest image URLs fetched from the page, {n - len(bad)} OK'


HOOK_AUDIO = """
window.__snd = {ctx: null, an: null, starts: 0};
const orig = AudioNode.prototype.connect;
AudioNode.prototype.connect = function (dest, ...a) {
  if (dest instanceof AudioDestinationNode && !this.__tap) {
    const an = this.context.createAnalyser(); an.fftSize = 2048; this.__tap = 1;
    orig.call(this, an); window.__snd.an = an; window.__snd.ctx = this.context;
  }
  return orig.call(this, dest, ...a);
};
const st = AudioBufferSourceNode.prototype.start;
AudioBufferSourceNode.prototype.start = function (...a) { window.__snd.starts++; window.__snd.len = this.buffer && this.buffer.duration; return st.apply(this, a); };
"""

PROBE_AUDIO = """
const done = arguments[arguments.length - 1], S = window.__snd;
if (!S.ctx) return done(null);
const t0 = S.ctx.currentTime, rms = [];
let n = 0;
(function tick() {
  const d = new Float32Array(S.an.fftSize); S.an.getFloatTimeDomainData(d);
  rms.push(Math.sqrt(d.reduce((a, v) => a + v * v, 0) / d.length));
  if (++n < 15) return setTimeout(tick, 100);
  done({state: S.ctx.state, rate: S.ctx.sampleRate, adv: S.ctx.currentTime - t0, starts: S.starts, len: S.len,
        rmsMax: Math.max(...rms), rmsN: rms.filter((r) => r > 1e-3).length, info: document.getElementById('sndInfo').textContent.split('\\n').slice(0, 2)});
})();
"""


def check_snd(m, fails, track):
    m.js(HOOK_AUDIO)
    kind, tid = track.split(':')
    m.click(f'#sndTable button[data-kind="{kind}"][data-id="{int(tid, 0)}"]')
    r = m.ajs(PROBE_AUDIO)
    if not r:
        fails.append('Sound: no AudioContext connected to the destination after the click')
        return 'Sound: FAILED (no AudioContext)'
    if r['state'] != 'running' or r['adv'] <= 0.5 or r['rmsN'] == 0 or r['starts'] != 1:
        fails.append(f'Sound: {r}')
    return (f"Sound: {kind} ${int(tid, 0):02X}: AudioContext {r['state']} @ {r['rate']} Hz, clock +{r['adv']:.2f} s "
            f"in 1.5 s, buffer {r['len']:.1f} s, output RMS max {r['rmsMax']:.3f} ({r['rmsN']}/15 probes > 1e-3); "
            f"{' / '.join(r['info'])}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--firefox', default=shutil.which('firefox') or 'firefox')
    ap.add_argument('--out', default=os.path.join(ROOT, 'gfx', 'browser'))
    ap.add_argument('--keep-profile', action='store_true')
    ap.add_argument('--areas', type=int, default=0, help='check only the first N sprite areas (0 = all)')
    ap.add_argument('--track', default='music:0x01')
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    hport, mport = free_port(), free_port()
    # the profile lives in the repo (a snap Firefox cannot see the host /tmp)
    prof = tempfile.mkdtemp(prefix='ffprofile_', dir=a.out)
    with open(os.path.join(prof, 'user.js'), 'w') as f:
        for k, v in [('marionette.port', mport), ('media.autoplay.default', 0), ('media.autoplay.blocking_policy', 0),
                     ('browser.shell.checkDefaultBrowser', False), ('datareporting.policy.dataSubmissionEnabled', False),
                     ('toolkit.telemetry.reportingpolicy.firstRun', False), ('browser.aboutwelcome.enabled', False),
                     ('app.update.disabledForTesting', True), ('browser.cache.disk.enable', False)]:
            f.write(f'user_pref("{k}", {json.dumps(v)});\n')
    srv = subprocess.Popen([sys.executable, '-m', 'http.server', str(hport), '--bind', '127.0.0.1', '--directory', ROOT],
                           stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    ff = subprocess.Popen([a.firefox, '--headless', '--marionette', '--no-remote', '--profile', prof],
                          stdout=subprocess.DEVNULL, stderr=open(os.path.join(a.out, 'firefox.log'), 'w'))
    fails, lines = [], []
    try:
        m = Marionette(mport)
        caps = m.cmd('WebDriver:NewSession', {'capabilities': {'alwaysMatch': {'acceptInsecureCerts': True}}})
        c = caps.get('capabilities', {})
        lines.append(f"browser: {c.get('browserName')} {c.get('browserVersion')} {c.get('platformName')} (headless)")
        m.cmd('WebDriver:SetTimeouts', {'script': 600000, 'pageLoad': 60000})
        m.cmd('WebDriver:SetWindowRect', {'width': 1280, 'height': 1000})
        m.cmd('WebDriver:Navigate', {'url': f'http://127.0.0.1:{hport}/web/catalog.html'})
        r = wait(m, "return document.querySelectorAll('#chrGrid .cell').length === 128")
        if not r['ok']:
            fails.append(f"boot: status {r['status']!r}")
        m.shot(os.path.join(a.out, 'tab_chr.png'))
        lines.append(check_chr(m, fails))
        tabs = [('pal', "return document.querySelectorAll('#palGrid .cell').length > 0"),
                ('spr', "return document.querySelectorAll('#sprGrid canvas').length > 0"),
                ('area', "const i = document.getElementById('areaImg'); return i.complete && i.naturalWidth > 0"),
                ('scr', "return document.querySelectorAll('#scrGrid img').length > 0"),
                ('snd', "return document.querySelectorAll('#sndTable button').length > 0"),
                ('txt', "return document.querySelectorAll('#txtTable tr').length > 1")]
        for t, cond in tabs:
            m.click(f'header button[data-tab="{t}"]')
            r = wait(m, cond)
            if not r['ok']:
                fails.append(f"tab {t}: status {r['status']!r}")
            m.js("window.scrollTo(0, 0)")
            time_ms = r.get('ms', -1)
            if t == 'pal':
                n = m.js("return [document.querySelectorAll('#palGrid .cell').length, document.querySelectorAll('#master .sw').length]")
                lines.append(f'Palettes: {n[0]} slot cells, {n[1]} master swatches ({time_ms} ms)')
                if n != [256, 64]:
                    fails.append(f'Palettes: {n}')
            elif t == 'spr':
                m.shot(os.path.join(a.out, 'tab_spr.png'))
                lines.append(check_spr(m, fails, a.areas))
            elif t == 'area':
                lines.append(check_urls(m, fails))
            elif t == 'snd':
                n = m.js("return document.querySelectorAll('#sndTable button').length")
                lines.append(f'Sound: {n} tracks listed ({time_ms} ms after the click)')
                lines.append(check_snd(m, fails, a.track))
            elif t == 'txt':
                n = m.js("return document.getElementById('txtCount').textContent")
                lines.append(f'Text: messages {n}')
            if t != 'spr':
                m.shot(os.path.join(a.out, f'tab_{t}.png'))
        errs = m.js("return document.getElementById('status').textContent")
        if errs:
            fails.append(f'final status line: {errs!r}')
        try:
            m.cmd('Marionette:Quit', {'flags': ['eForceQuit']})
        except (ConnectionError, OSError, RuntimeError):
            pass
    finally:
        try:
            ff.wait(timeout=15)
        except subprocess.TimeoutExpired:
            ff.kill()
        srv.terminate()
        if not a.keep_profile:
            shutil.rmtree(prof, ignore_errors=True)
    for ln in lines:
        print(ln)
    print(f'screenshots: {os.path.relpath(a.out, ROOT)}/tab_*.png')
    for f in fails:
        print('FAIL', f)
    print('RESULT', 'FAIL' if fails else 'OK')
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main())
