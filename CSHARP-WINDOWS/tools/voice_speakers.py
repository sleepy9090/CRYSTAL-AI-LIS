"""Speaker list for the neural dialogue voice (Dialogue.cs NeuralVoice): each speaker index of a multi-speaker Piper
model (libritts_r: LibriTTS-R readers) with the reader's sex from LibriSpeech SPEAKERS.TXT.

usage: python3 tools/voice_speakers.py MODEL.onnx.json SPEAKERS.TXT OUT.txt
OUT lines: `index sex reader_id` (sex M / F; readers missing from SPEAKERS.TXT are left out).
"""
import json
import sys


def main(argv):
    if len(argv) != 3:
        print(__doc__); return 2
    cfg = json.load(open(argv[0], encoding='utf-8'))
    sex = {}
    for line in open(argv[1], encoding='utf-8'):
        if line.startswith(';'):
            continue
        f = [x.strip() for x in line.split('|')]
        if len(f) >= 2 and f[0].isdigit() and f[1] in ('M', 'F'):
            sex[f[0]] = f[1]
    rows = sorted((idx, sex[rid], rid) for rid, idx in cfg['speaker_id_map'].items() if rid in sex)
    with open(argv[2], 'w', encoding='utf-8') as f:
        f.write('# piper speaker index, sex (LibriSpeech SPEAKERS.TXT), LibriTTS reader id\n')
        for idx, s, rid in rows:
            f.write(f'{idx} {s} {rid}\n')
    nm = sum(1 for r in rows if r[1] == 'M')
    print(f'{len(rows)} of {len(cfg["speaker_id_map"])} speakers: {nm} male, {len(rows) - nm} female -> {argv[2]}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
