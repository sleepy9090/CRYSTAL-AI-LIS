"""APU sample oracle (TASKS.md T2): music through the C# driver + APU vs the web port's, byte for byte.

usage: python3 tools/apu_wav_compare.py [--no-build] [--seconds S] [--keep] [--exe [PATH]] [ID ..]
  ID          music id, hex ($ / 0x prefix or bare hex like 1A) — default: every id $00-$1F
  --seconds   rendered length per id (default 8)
  --exe       the C# side is `wine ReC.exe --wav ID cs.wav S` (PATH default dist/ReC.exe; Win s1 T5)
Runs `node ../web/test/apu_check.mjs --wav ID web.wav S` and
`tests/ReC.Tests --wav ID cs.wav S` (same Machine path: Reset/SndInit, PlaySound, per frame SoundUpdate +
29781 APU cycles, 44.1 kHz 16-bit mono) into out/wav/; the two files must be identical. One line per id
(first differing sample on a mismatch); exit 1 on any difference.
"""
import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = os.path.dirname(ROOT)
APU_MJS = os.path.join(REPO, 'web', 'test', 'apu_check.mjs')
CS_TESTS = os.path.join(ROOT, 'tests', 'ReC.Tests', 'bin', 'Release', 'net10.0', 'ReC.Tests')
OUT = os.path.join(ROOT, 'out', 'wav')


def parse_id(s):
    s = s.lstrip('$')
    return int(s, 16)


def main():
    args = sys.argv[1:]
    build, keep, seconds, ids, exe = True, False, '8', [], None
    i = 0
    while i < len(args):
        a = args[i]
        if a == '--no-build':
            build = False
        elif a == '--keep':
            keep = True
        elif a == '--seconds':
            i += 1
            seconds = args[i]
        elif a == '--exe':
            exe = os.path.join(ROOT, 'dist', 'ReC.exe')
            if i + 1 < len(args) and args[i + 1].lower().endswith('.exe'):
                i += 1
                exe = os.path.abspath(args[i])
            build = False
        else:
            ids.append(parse_id(a))
        i += 1
    if not ids:
        ids = list(range(0x20))
    if build:
        r = subprocess.run(['dotnet', 'build', os.path.join(ROOT, 'ReC.Windows.slnx'), '-c', 'Release'],
                           capture_output=True, text=True)
        if r.returncode:
            print(r.stdout[-3000:])
            sys.exit('build failed')
    os.makedirs(OUT, exist_ok=True)
    fail = 0
    for mid in ids:
        web = os.path.join(OUT, f'web_{mid:02X}.wav')
        cs = os.path.join(OUT, f'cs_{mid:02X}.wav')
        subprocess.run(['node', APU_MJS, '--wav', str(mid), web, seconds], check=True, capture_output=True)
        if exe:
            env = dict(os.environ, WINEPREFIX=os.path.join(ROOT, '.wine'), WINEDEBUG='-all')
            subprocess.run(['wine', exe, '--wav', str(mid), 'Z:' + cs.replace('/', '\\'), seconds], check=True,
                           capture_output=True, env=env, timeout=600)
        else:
            subprocess.run([CS_TESTS, '--wav', str(mid), cs, seconds], check=True, capture_output=True)
        a, b = open(web, 'rb').read(), open(cs, 'rb').read()
        if a == b:
            print(f'ok   ${mid:02X}: {(len(a) - 44) // 2} samples identical')
            if not keep:
                os.remove(web)
                os.remove(cs)
            continue
        fail += 1
        n = min(len(a), len(b))
        k = next((j for j in range(n) if a[j] != b[j]), n)
        smp = (k - 44) // 2 if k >= 44 else -1
        wa = int.from_bytes(a[44 + 2 * smp:46 + 2 * smp], 'little', signed=True) if smp >= 0 and len(a) >= 46 + 2 * smp else None
        wb = int.from_bytes(b[44 + 2 * smp:46 + 2 * smp], 'little', signed=True) if smp >= 0 and len(b) >= 46 + 2 * smp else None
        print(f'FAIL ${mid:02X}: sizes web {len(a)} cs {len(b)}, first difference at byte {k}'
              f' (sample {smp}: web {wa} cs {wb})')
    print(f'{len(ids) - fail}/{len(ids)} music id(s) identical')
    sys.exit(1 if fail else 0)


if __name__ == '__main__':
    main()
