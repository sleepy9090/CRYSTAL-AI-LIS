"""Oracle diff: the C# port (src/ReC.Run) vs the web port (node ../web/test/run.mjs), same start + same keys.

usage: python3 tools/cs_vs_web.py [--canon] [--states] [--no-build] [--frames N] [--keep] [NAME[:FRAMES] ..]
  NAME            a case from CASES below, or the stem of any ../web/test/states/*.json (default frames / no keys)
  NAME:FRAMES     same with its own frame count (e.g. town02_enter:1)
  --canon         from Reset over the canonical keys (tools/ref/compare_ref.py CANON_KEYS) to f2460
  --states        every ../web/test/states/*.json (CASES entry if there is one, else DEFAULT_FRAMES, no keys)
  --checks        every CASES entry taken from a *_check.mjs (state + LAST + KEYS), at the check's full length
  --frames N     override the frame count of every selected case
  --only KINDS    compare only these of csv,png,ram,wram,ciram (default: all)
  --no-build      skip `dotnet build ReC.slnx -c Release`
  --keep          keep the out/ files of passing cases
Per case both ports write --compare CSV, --dump-screen PNG, --dump-ram / --dump-wram / --dump-ciram into out/<case>/;
CSV and PNG go through ../tools/compare_frames.py, the raw dumps must be byte-identical. One line per case;
exit 1 on any difference. (docs/architecture_cs.md §5)
"""
import glob
import json
import os
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = os.path.dirname(ROOT)
STATES = os.path.join(REPO, 'web', 'test', 'states')
RUN_MJS = os.path.join(REPO, 'web', 'test', 'run.mjs')
COMPARE = os.path.join(REPO, 'tools', 'compare_frames.py')
CS_RUN = os.path.join(ROOT, 'src', 'ReC.Run', 'bin', 'Release', 'net10.0', 'ReC.Run')
OUT = os.path.join(ROOT, 'out')

CANON_KEYS = '1250:START 1400:START 1600:START 1700:START 2300:START 2340:DOWN:80'
DEFAULT_FRAMES = 120

def every(f0, n, step=12, spec='B:4'):
    """the checks' `Array.from({ length: n }, (_, i) => `${f0 + 12 * i}:B:4`)` key runs"""
    return ' '.join(f'{f0 + step * i}:{spec}' for i in range(n))


# name -> dict(state=<json stem or None for Reset>, frames=N | last=F, keys='..', frame0=N (optional))
# Frames / keys per state come from the matching ../web/test/*_check.mjs (LAST + KEYS, copied as written there;
# `last` = the check's LAST frame, frames = last - state frame0 + 1 = the check's run). `--checks` runs every
# entry with a state at its full length.
CASES = {
    'canon': dict(state=None, frames=2460 - 214 + 1, keys=CANON_KEYS),
    'town02_enter': dict(state='town02_enter', frames=1, keys=''),
    # T6 (C# s4): group / fan / kill / thunder checks
    'group05': dict(state='group05', last=2985, keys='2528:UP:24 2552:LEFT:35 2587:B:3 2601:B:4 2614:B:4 2629:B:4 '
                    '2642:B:4 2657:B:4 2701:LEFT:19 2766:LEFT:30 2806:B:4 2826:B:4 2846:B:4'),
    'group08': dict(state='group08', last=2985, keys='2526:UP:84 2696:LEFT:16 2756:RIGHT:31 2826:DOWN:19'),
    'group09': dict(state='group09', last=3504, keys='2531:UP:50 2585:RIGHT:330 2931:B:4 2951:B:4 2971:B:4 '
                    '2990:RIGHT:21 3022:B:3 3040:B:5 3061:RIGHT:29 3101:B:4 3121:B:4 3141:B:4 3161:B:4'),
    'group11': dict(state='group11', last=2984, keys='2527:DOWN:50 2577:LEFT:69 2797:A:9 2841:B:3 2864:UP:30'),
    'group18': dict(state='group18', last=2984, keys='2527:LEFT:47 2701:A:4 2761:A:9 2801:UP:34 2841:LEFT:80'),
    'group28': dict(state='group28', last=2984, keys='2546:RIGHT:3 2658:RIGHT:7 2665:B:4 2790:RIGHT:10 2800:B:4 '
                    '2812:B:3 2823:B:4 2835:B:5 2847:B:4 2859:B:4 2871:B:4 2883:B:4 2895:B:4 2907:B:4 2919:B:5 '
                    '2931:B:4 2943:B:4 2955:B:4 2967:B:4 2979:B:4'),
    'group29': dict(state='group29', last=2984, keys='2527:DOWN:20 2547:RIGHT:40 2587:UP:20 2607:RIGHT:2 '
                    '2629:LEFT:1 2630:B:4 2642:B:5 2655:B:4 2667:B:4 2701:LEFT:30 2831:DOWN:2 2833:B:4 2845:B:4 '
                    '2857:B:4 2869:B:4 2881:B:4 2928:B:4'),
    'group40': dict(state='group40', last=2985, keys='2528:RIGHT:12 2540:DOWN:3 ' + every(2543, 26)),
    'group59': dict(state='group59', last=2985, keys='2510:UP:40 2550:B:4 2566:B:4 2582:B:4 2598:B:5 2615:B:4 '
                    '2631:B:4 2647:B:4 2663:B:4 2679:B:3 2695:B:3 2711:B:4 2727:B:4 2743:B:3 2758:B:4 2774:B:4 '
                    '2790:B:4 2806:B:4 2823:B:4 2839:B:4 2855:B:4 2870:B:5 2886:B:4 2902:B:4 2918:B:4 2934:B:4 '
                    '2951:B:4 2967:B:4'),
    'group5a': dict(state='group5a', last=2995, keys='2520:UP:40 2560:B:4 2576:B:5 2593:B:4 2609:B:4 2625:B:4 '
                    '2641:B:4 2656:B:5 2673:B:4 2688:B:4 2705:B:3 2721:B:4 2737:B:4 2753:B:4 2769:B:4 2785:B:4 '
                    '2801:B:4 2817:B:4 2833:B:4 2849:B:4 2865:B:4 2881:B:4 2897:B:4 2913:B:4 2929:B:3 2944:B:5 '
                    '2961:B:4 2977:B:4'),
    'group6b': dict(state='group6b', last=2984, keys='2525:DOWN:24 2549:LEFT:80 2629:DOWN:12 2641:B:4 2653:B:4 '
                    '2666:B:3 ' + every(2677, 17) + ' 2881:DOWN:10 2891:B:5 2904:B:4 2916:B:4'),
    'group6e': dict(state='group6e', last=2987, keys='2513:B:3 2536:B:4 2587:B:9 2609:B:4 2681:B:9 2705:B:4 '
                    '2777:B:4 2801:B:4 2825:B:4 2849:B:9 2873:B:4 2896:B:5 2921:B:4 2945:B:4'),
    'group7c': dict(state='group7c', last=2984, keys='2525:LEFT:14 2539:DOWN:9 2548:LEFT:1 2752:UP:9 2761:LEFT:26 '
                    '2787:B:4 ' + every(2799, 12)),
    'group7f': dict(state='group7f', last=2984, keys='2525:UP:101 ' + every(2645, 10) + ' 2765:UP:11 '
                    + every(2776, 10) + ' 2896:UP:9 ' + every(2905, 7)),
    'group81': dict(state='group81', last=2984, keys='2506:DOWN:2 2569:DOWN:7 2576:B:4 2585:DOWN:7 2592:B:4 '
                    '2601:DOWN:6 2607:B:5 2618:DOWN:6 2624:B:4 2634:DOWN:5 2639:B:4 2650:DOWN:6 2656:B:5 '
                    '2666:DOWN:6 2672:B:4 2681:DOWN:6 2687:B:5 2698:DOWN:6 2704:B:4 2713:DOWN:6 2719:B:5 '
                    '2729:DOWN:8 2737:B:3 2745:DOWN:7 2752:B:4 2761:DOWN:7 2768:B:4 2777:DOWN:7 2784:B:3 '
                    '2794:DOWN:5 2799:B:5 2810:DOWN:6 2816:B:4 2825:DOWN:6 2831:B:4 2842:DOWN:6 2848:B:4 '
                    '2858:DOWN:6 2864:B:4 2873:DOWN:7 2880:B:4 2889:DOWN:7 2896:B:4 2906:DOWN:5 2911:B:4 '
                    '2922:DOWN:6 2928:B:4 2937:DOWN:7 2944:B:3 2954:DOWN:5 2959:B:4 2970:DOWN:5 2975:B:4'),
    'group84': dict(state='group84', last=2984, keys='2525:UP:40 ' + every(2596, 10) + ' 2716:B:4 '
                    + every(2728, 11) + ' 2860:B:3 ' + every(2871, 9)),
    'group84b': dict(state='group84b', last=2984, keys='2506:RIGHT:19 2525:UP:15 2540:RIGHT:17 2557:DOWN:1 '
                     '2558:B:5 2571:B:3 2583:B:4 2595:B:4 2607:B:4 2619:DOWN:6 2625:B:4 2635:DOWN:6 2641:B:4 '
                     '2651:DOWN:5 2656:B:4 2666:DOWN:7 2673:B:4 2683:DOWN:6 2689:B:4 2699:DOWN:5 2704:B:5 '
                     '2715:DOWN:6 2721:B:4 2731:DOWN:6 2737:B:4 2747:DOWN:6 2753:B:4 2763:DOWN:6 2769:B:4 '
                     '2779:DOWN:6 2785:B:4 2795:DOWN:6 2801:B:4 2811:DOWN:6 2817:B:4 2827:DOWN:6 2833:B:4 '
                     '2843:DOWN:6 2849:B:4 2859:DOWN:6 2865:B:4 2875:DOWN:6 2881:B:4 2891:DOWN:6 2897:B:4 '
                     '2907:DOWN:6 2913:B:4 2923:DOWN:6 2929:B:4 2939:DOWN:6 2945:B:4 2955:DOWN:6 2961:B:4 '
                     '2971:DOWN:6 2977:B:4'),
    'group88': dict(state='group88', last=2984, keys='2509:RIGHT:7 2516:B:4 2528:B:4 2540:B:4 2552:B:4 2564:B:4 '
                    '2576:B:4 2588:B:4 2600:B:4 2612:B:4 2641:RIGHT:2 2643:DOWN:150'),
    'groupb2': dict(state='groupb2', last=2986, keys='2513:UP:40 2553:RIGHT:76 2629:DOWN:19 2648:B:5 2665:B:4 '
                    '2681:B:4 2697:B:4 2713:B:4 2729:B:4 2745:B:4 2761:B:4 2777:B:4 2793:B:4 2842:B:9 2857:B:4 '
                    '2873:B:3 2888:B:4 2904:B:4 2920:B:4 2936:B:4 2952:B:4 2968:B:4 2984:B:3'),
    'thunder': dict(state='thunder', last=2945, keys='2513:B:120'),
    'fanskip': dict(state='fanskip', last=2807, keys=''),
    'killgold': dict(state='killgold', last=2823, keys=' '.join(
        f'{f}:{"BA"[i % 2]}:4' for i, f in enumerate([2527, 2547, 2567, 2587, 2607, 2627, 2647, 2667, 2687, 2707]))
        + ' 2727:RIGHT:4 ' + ' '.join(f'{f}:{"BA"[i % 2]}:4' for i, f in enumerate([2747, 2767, 2787, 2807]))),
    'killlevelup': dict(state='killlevelup', last=2823, keys=' '.join(
        f'{f}:{"BA"[i % 2]}:4' for i, f in enumerate([2527, 2547, 2567, 2587, 2606, 2626, 2646, 2666, 2686, 2707]))
        + ' 2727:RIGHT:4 ' + ' '.join(f'{f}:{"BA"[i % 2]}:4' for i, f in enumerate([2747, 2767, 2787, 2807]))),
}


def state_frame0(stem):
    with open(os.path.join(STATES, stem + '.json')) as f:
        s = json.load(f)
    return s.get('frame0', s['frame'])


def case_for(name):
    frames = None
    if ':' in name:
        name, frames = name.split(':', 1)
        frames = int(frames)
    if name in CASES:
        c = dict(CASES[name])
    elif os.path.exists(os.path.join(STATES, name + '.json')):
        c = dict(state=name, frames=DEFAULT_FRAMES, keys='')
    else:
        sys.exit(f'unknown case {name} (not in CASES, no {STATES}/{name}.json)')
    if 'last' in c:
        c['frames'] = c['last'] - c.get('frame0', state_frame0(c['state'])) + 1
    if frames is not None:
        c['frames'] = frames
        name = f'{name}_f{frames}'
    return name, c


def port_args(c, d, tag):
    a = ['--frames', str(c['frames']), '--quiet',
         '--compare', os.path.join(d, f'{tag}.csv'), '--dump-screen', os.path.join(d, f'{tag}.png'),
         '--dump-ram', os.path.join(d, f'{tag}.ram'), '--dump-wram', os.path.join(d, f'{tag}.wram'),
         '--dump-ciram', os.path.join(d, f'{tag}.ciram')]
    if c.get('state'):
        a += ['--load-state', os.path.join(STATES, c['state'] + '.json')]
    if 'frame0' in c:
        a += ['--frame0', str(c['frame0'])]
    if c.get('keys'):
        a += ['--keys'] + c['keys'].split()
    return a


def run(cmd):
    p = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    return p.returncode, p.stdout


KINDS = ('csv', 'png', 'ram', 'wram', 'ciram')


def compare_case(name, c, keep, only=KINDS):
    d = os.path.join(OUT, name)
    os.makedirs(d, exist_ok=True)
    rc, out = run(['node', RUN_MJS] + port_args(c, d, 'web'))
    if rc:
        return False, f'web run failed: {out.strip()[-300:]}'
    rc, out = run([CS_RUN] + port_args(c, d, 'cs'))
    if rc:
        return False, f'C# run failed: {out.strip()[-300:]}'
    bad = []
    if 'csv' in only:
        rc, out = run([sys.executable, COMPARE, os.path.join(d, 'web.csv'), os.path.join(d, 'cs.csv')])
        if rc:
            lines = [ln for ln in out.splitlines() if ln.strip()]
            bad.append('csv: ' + ' | '.join(lines[:4]))
    if 'png' in only:
        rc, out = run([sys.executable, COMPARE, os.path.join(d, 'web.png'), os.path.join(d, 'cs.png')])
        if rc:
            bad.append('png: ' + out.strip().splitlines()[-1])
    for ext in ('ram', 'wram', 'ciram'):
        if ext not in only:
            continue
        a = open(os.path.join(d, f'web.{ext}'), 'rb').read()
        b = open(os.path.join(d, f'cs.{ext}'), 'rb').read()
        if a != b:
            diff = [i for i in range(min(len(a), len(b))) if a[i] != b[i]]
            base = {'ram': 0, 'wram': 0x6000, 'ciram': 0}[ext]
            first = ' '.join(f'${base + i:04X}' for i in diff[:6])
            bad.append(f'{ext}: {len(diff)} bytes differ ({first}{" .." if len(diff) > 6 else ""})')
    if not bad and not keep:
        shutil.rmtree(d, ignore_errors=True)
    return not bad, '; '.join(bad) if bad else f'{c["frames"]} frames identical ({", ".join(only)})'


def main(argv):
    keep = '--keep' in argv
    names = []
    frames = None
    only = KINDS
    it = iter(argv)
    for a in it:
        if a == '--only':
            only = tuple(next(it).split(','))
            if any(k not in KINDS for k in only):
                sys.exit(f'--only takes {",".join(KINDS)}')
        elif a == '--canon':
            names.append('canon')
        elif a == '--states':
            names += sorted(os.path.basename(p)[:-5] for p in glob.glob(os.path.join(STATES, '*.json')))
        elif a == '--checks':
            names += [n for n, c in CASES.items() if c.get('state') and 'last' in c]
        elif a == '--frames':
            frames = int(next(it))
        elif a in ('--no-build', '--keep'):
            pass
        elif a in ('-h', '--help'):
            print(__doc__)
            return 0
        elif a.startswith('--'):
            sys.exit(f'unknown option {a}')
        else:
            names.append(a)
    if not names:
        print(__doc__)
        return 2
    if '--no-build' not in argv:
        rc, out = run(['dotnet', 'build', os.path.join(ROOT, 'ReC.slnx'), '-c', 'Release', '-v', 'q', '-nologo'])
        if rc:
            print(out)
            return 1
    fails = 0
    for n in names:
        if frames is not None and ':' not in n:
            n = f'{n}:{frames}'
        name, c = case_for(n)
        ok, msg = compare_case(name, c, keep, only)
        fails += not ok
        print(f'{"ok  " if ok else "FAIL"} {name:24s} {msg}', flush=True)
    print(f'{len(names) - fails}/{len(names)} cases identical')
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
