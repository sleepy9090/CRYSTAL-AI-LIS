"""Assemble dist/voice/kokoro/ (the Kokoro dialogue voice, Kokoro.cs KokoroVoice) from the downloads.

usage: python3 tools/kokoro_pack.py SHERPA_WIN.tar.bz2 KOKORO_MODEL.tar.bz2 [OUT_DIR=dist/voice/kokoro]
  downloads (2026-11-27):
    https://github.com/k2-fsa/sherpa-onnx/releases/download/v1.13.8/sherpa-onnx-v1.13.8-win-x64-static-MT-Release.tar.bz2
      (sherpa-onnx, Apache-2.0; v1.13.8 = the last line that phonemises Kokoro v0.19 with espeak-ng; 2.x needs a lexicon)
    https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models/kokoro-int8-en-v0_19.tar.bz2
      (Kokoro-82M v0.19 int8, Apache-2.0; espeak-ng data, GPL-3)
Writes OUT_DIR/sherpa-onnx-offline-tts.exe, voices.bin, tokens.txt, espeak-ng-data/, LICENSE-kokoro, CREDITS.txt and the
model as model.int8.onnx.part1, .part2, ... (< 95 MB each: GitHub refuses files over 100 MB; the game joins them once).
"""
import os
import shutil
import sys
import tarfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
PART = 95 * 1000 * 1000

CREDITS = """re-c spoken dialogue: the Kokoro voice (preferred over Piper when this folder is present)

sherpa-onnx-offline-tts.exe  sherpa-onnx v1.13.8, https://github.com/k2-fsa/sherpa-onnx (Apache-2.0), static build
                             (ONNX Runtime, MIT). Run as a separate program, one process per message page.
model.int8.onnx.part*        Kokoro-82M v0.19 (int8 export by sherpa-onnx), https://huggingface.co/hexgrad/Kokoro-82M
voices.bin, tokens.txt       (Apache-2.0, see LICENSE-kokoro). The parts are joined into %LOCALAPPDATA%\\re-c\\kokoro.
espeak-ng-data/              espeak-ng (GPL-3.0, https://github.com/espeak-ng/espeak-ng), phoneme data.
"""


def safe_members(t, prefix):
    for m in t.getmembers():
        name = m.name.replace('\\', '/')
        if not name.startswith(prefix) or '..' in name.split('/') or name.startswith('/'):
            continue
        if not (m.isfile() or m.isdir()):
            continue
        yield m, name[len(prefix):]


def main(argv):
    if len(argv) < 2:
        print(__doc__); return 2
    sherpa, model = argv[:2]
    out = argv[2] if len(argv) > 2 else os.path.join(ROOT, 'dist', 'voice', 'kokoro')
    os.makedirs(out, exist_ok=True)
    with tarfile.open(sherpa, 'r:bz2') as t:
        exe = [m for m in t.getmembers() if m.name.endswith('/bin/sherpa-onnx-offline-tts.exe')]
        if not exe:
            print('sherpa-onnx-offline-tts.exe not in the archive'); return 1
        with t.extractfile(exe[0]) as src, open(os.path.join(out, 'sherpa-onnx-offline-tts.exe'), 'wb') as f:
            shutil.copyfileobj(src, f)
    n = 0
    with tarfile.open(model, 'r:bz2') as t:
        top = t.getmembers()[0].name.split('/')[0] + '/'
        for m, rel in safe_members(t, top):
            if not rel:
                continue
            if rel == 'model.int8.onnx':
                with t.extractfile(m) as src:
                    k = 0
                    while True:
                        chunk = src.read(PART)
                        if not chunk:
                            break
                        k += 1
                        with open(os.path.join(out, f'model.int8.onnx.part{k}'), 'wb') as f:
                            f.write(chunk)
                print(f'model split into {k} parts')
                continue
            dst = os.path.join(out, 'LICENSE-kokoro' if rel == 'LICENSE' else rel)
            if m.isdir():
                os.makedirs(dst, exist_ok=True); continue
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            with t.extractfile(m) as src, open(dst, 'wb') as f:
                shutil.copyfileobj(src, f)
            n += 1
    with open(os.path.join(out, 'CREDITS.txt'), 'w', encoding='utf-8') as f:
        f.write(CREDITS)
    print(f'{n} model files + the TTS program -> {out}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
