"""Assemble dist/voice/ (the neural dialogue voice next to ReC.exe, Dialogue.cs NeuralVoice) from the downloads.

usage: python3 tools/voice_pack.py PIPER_WINDOWS.zip MODEL.onnx MODEL.onnx.json [OUT_DIR=dist/voice]
  downloads (2026-11-26):
    https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip   (Piper, MIT; espeak-ng, GPL-3)
    https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/libritts_r/medium/en_US-libritts_r-medium.onnx(.json)
      (LibriTTS-R, CC BY 4.0)
  voice/speakers.txt (tools/voice_speakers.py) is copied too.
Writes OUT_DIR/piper/* (the zip's piper/ folder), OUT_DIR/<model>, OUT_DIR/<model>.json, OUT_DIR/speakers.txt and
OUT_DIR/CREDITS.txt.
"""
import os
import shutil
import sys
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
MODEL = 'en_US-libritts_r-medium.onnx'

CREDITS = """re-c spoken dialogue (Audio > Spoken dialogue)

piper/      Piper text-to-speech 2023.11.14-2, https://github.com/rhasspy/piper (MIT licence), with espeak-ng
            (GPL-3.0, https://github.com/espeak-ng/espeak-ng) and ONNX Runtime (MIT). Run as a separate program.
{model}  Piper voice libritts_r (medium), https://huggingface.co/rhasspy/piper-voices, trained on
            LibriTTS-R (http://www.openslr.org/141/, CC BY 4.0), from LibriVox recordings (public domain).
speakers.txt  speaker sex from LibriSpeech SPEAKERS.TXT (http://www.openslr.org/12/, CC BY 4.0).
"""


def main(argv):
    if len(argv) < 3:
        print(__doc__); return 2
    zpath, model, cfg = argv[:3]
    out = argv[3] if len(argv) > 3 else os.path.join(ROOT, 'dist', 'voice')
    if os.path.basename(model) != MODEL:
        print(f'model must be {MODEL}'); return 1
    os.makedirs(out, exist_ok=True)
    n = 0
    with zipfile.ZipFile(zpath) as z:
        for info in z.infolist():
            name = info.filename.replace('\\', '/')
            if not name.startswith('piper/') or name.endswith('/') or '..' in name.split('/'):
                continue
            dst = os.path.join(out, *name.split('/'))
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            with z.open(info) as src, open(dst, 'wb') as f:
                shutil.copyfileobj(src, f)
            n += 1
    shutil.copyfile(model, os.path.join(out, MODEL))
    shutil.copyfile(cfg, os.path.join(out, MODEL + '.json'))
    shutil.copyfile(os.path.join(ROOT, 'voice', 'speakers.txt'), os.path.join(out, 'speakers.txt'))
    with open(os.path.join(out, 'CREDITS.txt'), 'w', encoding='utf-8') as f:
        f.write(CREDITS.format(model=MODEL))
    ok = os.path.exists(os.path.join(out, 'piper', 'piper.exe'))
    print(f'{n} Piper files + model + speakers -> {out} (piper.exe {"present" if ok else "MISSING"})')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
