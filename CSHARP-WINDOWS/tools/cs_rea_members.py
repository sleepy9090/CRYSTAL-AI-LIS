"""Cross-check the `// js:` tags against REA's member inventory of the built assemblies (C# s7, T12).

Every tag in src/**/*.cs sits above a C# member; this finds that member's name and its enclosing type in the
source, then looks it up in REA `inspect-managed-members` evidence of the Release build:

  rea inspect-managed-members src/ReC.Core/bin/Release/net10.0/ReC.Core.dll --detail summary \
      --evidence_output out/rea_members.json
  rea inspect-managed-members src/ReC.Win/bin/Release/net10.0-windows/ReC.Win.dll --detail summary \
      --evidence_output out/rea_members_win.json

A member counts as present when its type (matched by simple name, nested types included) declares
  * a method of that name (overloads, iterator stubs), `.ctor` for a constructor, `get_X` / `set_X` (property),
  * a field of that name (delegate-valued tables / lag predicates), or
  * a compiler-named local function `<Outer>g__Name|..` (JS inner closures ported as local functions).
The bundles' artifact SHA-256 is compared with the DLL on disk (a stale bundle = rebuild + rerun REA).

usage: python3 tools/cs_rea_members.py [BUNDLE.json ..] [--list] [--modules]
       (--list: every tag -> Type.Member; --modules: JS module -> C# files with tag counts)
       (default bundles: out/rea_members.json, + out/rea_members_win.json once ReC.Win exists)
Exit 1 when a tagged member is not found or a bundle is stale.
"""
import glob
import hashlib
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TAG = re.compile(r'//\s*js:\s*([\w./-]+\.js)\s+([\w$]+)')
TYPE_DECL = re.compile(r'^(\s*)(?:[\w]+\s+)*(?:class|struct|record|interface|enum)\s+(\w+)')
SKIP = re.compile(r'^\s*(//|\[|$)')
KEYWORDS = {'public', 'private', 'internal', 'protected', 'static', 'readonly', 'const', 'override', 'virtual',
            'sealed', 'unsafe', 'extern', 'new', 'partial', 'async', 'abstract', 'ref', 'volatile'}


def simple(t):
    return re.split(r'[./+]', t or '')[-1]


def load_bundle(path):
    with open(path) as fh:
        b = json.load(fh)
    rec = b['records'][0]
    nr = rec['normalized_result']
    types = {t['token']: t for t in nr['types']}
    members = {}  # simple type name -> set of member names

    def add(type_token, type_name, name):
        t = types.get(type_token)
        key = simple(t['name'] if t else type_name)
        members.setdefault(key, set()).add(name)
    for mth in nr['methods']:
        add(mth['declaring_type_token'], mth['declaring_type'], mth['name'])
    for f in nr['fields']:
        add(f['declaring_type_token'], f['declaring_type'], f['name'])
    nested = {}  # simple type name -> simple names of its nested types (closure classes)
    for t in nr['types']:
        enc = types.get(t.get('enclosing_type_token'))
        if enc:
            nested.setdefault(simple(enc['name']), []).append(simple(t['name']))
    return {'path': path, 'evidence_id': rec['evidence_id'], 'artifact': nr['artifact'], 'members': members,
            'types': {simple(t['name']) for t in nr['types']}, 'nested': nested}


def declared_name(line):
    """C# member name declared on `line` (method / field / property / ctor / local function)."""
    s = line.strip()
    m = TYPE_DECL.match(line)
    if m:
        return m.group(2), 'type'
    # method / ctor / local function; `static (int X, int Y) Name(` = tuple return, not a call
    call = next((c for c in re.finditer(r'(\w+)\s*(?:<[^()]*>)?\s*\(', s) if c.group(1) not in KEYWORDS), None)
    head = re.split(r'\s*(?:=>|=(?!=)|;|\{)', s, maxsplit=1)[0]
    if call and call.start() < len(head):
        return call.group(1), 'member'
    head = re.sub(r'<[^<>]*(<[^<>]*>[^<>]*)*>', '', head).strip()  # drop generic arguments
    toks = [t for t in re.split(r'\s+', head) if t and t not in KEYWORDS]
    return (toks[-1] if toks else None), 'member'


def scan_tags():
    out = []
    for p in sorted(glob.glob(os.path.join(ROOT, 'src/**/*.cs'), recursive=True)):
        if '/obj/' in p or '/bin/' in p:
            continue
        lines = open(p, errors='replace').read().split('\n')
        stack = []  # (indent, type name)
        pending = []
        for i, line in enumerate(lines):
            tags = TAG.findall(line)
            if tags and line.strip().startswith('//'):
                pending.extend((mod, name, i + 1) for mod, name in tags)
                continue
            if SKIP.match(line):
                continue
            indent = len(line) - len(line.lstrip())
            while stack and stack[-1][0] >= indent and line.strip() != '{':
                stack.pop()
            name, kind = declared_name(line)
            if pending:
                enclosing = stack[-1][1] if stack else None
                for (mod, jsname, ln) in pending:
                    out.append({'file': os.path.relpath(p, ROOT), 'line': ln, 'module': mod, 'js': jsname,
                                'cs': name, 'kind': kind, 'type': enclosing})
                pending = []
            m = TYPE_DECL.match(line)
            if m:
                stack.append((indent, m.group(2)))
    return out


def find(tag, bundles):
    name, typ = tag['cs'], tag['type']
    if not name:
        return None
    for b in bundles:
        if tag['kind'] == 'type':
            if name in b['types']:
                return b
            continue
        mem = b['members'].get(typ, set())
        cands = {name, f'get_{name}', f'set_{name}'}
        if name == typ:
            cands.add('.ctor')
        if cands & mem:
            return b
        local = re.compile(rf'<\w+>g__{re.escape(name)}\|')
        if any(local.match(x) for x in mem):
            return b
        # a local function that captures an iterator's / lambda's locals lands in a closure class
        # (`<>c__DisplayClassN_M`, nested in the tagged type)
        for owner in b['nested'].get(typ, ()):
            if any(local.match(x) for x in b['members'].get(owner, ())):
                return b
    return None


def sha256(path):
    h = hashlib.sha256()
    with open(path, 'rb') as fh:
        h.update(fh.read())
    return h.hexdigest()


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    paths = args or [p for p in (os.path.join(ROOT, 'out/rea_members.json'), os.path.join(ROOT, 'out/rea_members_win.json'))
                     if p.endswith('rea_members.json') or os.path.exists(p)]
    bundles = [load_bundle(p) for p in paths]
    bad = 0
    for b in bundles:
        art = b['artifact']
        now = sha256(art['path']) if os.path.exists(art['path']) else None
        state = 'current' if now == art['sha256'] else 'STALE (rebuild changed the DLL: rerun REA)'
        bad += state != 'current'
        print(f"{os.path.relpath(b['path'], ROOT)}: {b['evidence_id']} {os.path.basename(art['path'])} "
              f"sha256 {art['sha256'][:16]}.. {state}")
    tags = scan_tags()
    missing = [t for t in tags if not find(t, bundles)]
    keys = {(t['module'], t['js']) for t in tags}
    members = {(t['type'], t['cs']) for t in tags}
    if '--list' in sys.argv:
        for t in tags:
            print(f"{t['module']} {t['js']} -> {t['type']}.{t['cs']} ({t['file']}:{t['line']})")
    if '--modules' in sys.argv:
        mods = {}
        for t in tags:
            mods.setdefault(t['module'], {}).setdefault(t['file'], 0)
            mods[t['module']][t['file']] += 1
        for mod in sorted(mods):
            print(f"{mod:34s} " + ', '.join(f'{f} ({n})' for f, n in sorted(mods[mod].items())))
    for t in missing:
        print(f"NOT FOUND {t['module']} {t['js']} -> {t['type']}.{t['cs']} ({t['kind']}) at {t['file']}:{t['line']}")
    print(f"tags: {len(tags)} ({len(keys)} inventory keys, {len(members)} C# members), "
          f"{len(tags) - len(missing)} found in the assemblies, {len(missing)} not found")
    sys.exit(1 if missing or bad else 0)


if __name__ == '__main__':
    main()
