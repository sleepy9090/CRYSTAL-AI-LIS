#!/usr/bin/env python3
"""Which REVERSE.md sections are named in the architecture docs (s221, task "Final docs refresh").

usage: python3 tools/doc_section_links.py [--levels 2,3] [--all] [REVERSE.md] [DOC.md ..]
A section counts as linked when its key (heading text before the first '(' / ',' / ' — ', stripped of
backticks, at least 2 words or 6 chars) appears in one of the docs. Prints the unlinked ones (all with --all).
"""
import re
import sys


def key_of(title):
    t = title.replace('`', '').strip()
    t = re.split(r'\s+\(|,\s| — | - |: ', t)[0].strip()
    return t


def main():
    args = sys.argv[1:]
    levels = {2, 3}
    show_all = False
    files = []
    i = 0
    while i < len(args):
        if args[i] == '--levels':
            levels = {int(x) for x in args[i + 1].split(',')}; i += 2; continue
        if args[i] == '--all':
            show_all = True; i += 1; continue
        files.append(args[i]); i += 1
    rev = files[0] if files else 'REVERSE.md'
    docs = files[1:] or ['docs/architecture_exe.md', 'docs/architecture_web.md']
    text = ''
    for d in docs:
        with open(d, encoding='utf-8') as fh:
            text += fh.read().replace('`', '') + '\n'
    low = text.lower()
    n = miss = 0
    with open(rev, encoding='utf-8') as fh:
        for ln_no, ln in enumerate(fh, 1):
            m = re.match(r'^(#{2,4})\s+(.*)$', ln.rstrip('\n'))
            if not m or len(m.group(1)) not in levels:
                continue
            title = m.group(2)
            k = key_of(title)
            n += 1
            ok = len(k) >= 4 and k.lower() in low
            if not ok:
                miss += 1
            if show_all or not ok:
                print(f'{"ok  " if ok else "MISS"} L{ln_no:<5} {"#" * len(m.group(1))} {k[:70]}')
    print(f'{n} sections, {miss} not named in {", ".join(docs)}')


if __name__ == '__main__':
    main()
