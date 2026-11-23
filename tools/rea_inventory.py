"""Port obligation inventory from a REA analysis of the web port (used by the C# port in CSHARP/).

REA (`rea analyze-javascript-application web/src --artifact-format directory --detail summary
--evidence_output EV.json`) statically parses every web/src module and records each function / method /
class node with its module path, source range and a semantic fingerprint (normalized AST, control flow,
literal set). This tool turns that Evidence bundle into CSHARP/docs/obligations.csv: one row per named
callable = one thing the port must provide (or explicitly waive in CSHARP/docs/waivers.csv).

usage: python3 tools/rea_inventory.py EV.json [--out CSHARP/docs/obligations.csv]

Columns: module, name, kind (function|method|class), start, end (JS source lines), fingerprint
(REA fingerprint_sha256, first 16 hex; empty if REA recorded none), layer (game|nes|data|platform).
The header comment line records the REA evidence id + web/src digest the rows came from, so
CSHARP/tools/cs_coverage.py users can tell when the web port changed after the inventory was taken.
Anonymous callables (arrow callbacks, REA label null) are counted in the header, not listed:
they are ported as part of their enclosing function.
"""
import csv
import json
import os
import sys

PLATFORM = {'main.js', 'input.js', 'audio.js', 'save.js'}


def layer(module):
    if module in PLATFORM:
        return 'platform'
    top = module.split('/')[0]
    if top == 'nes':
        return 'nes'
    if module == 'data.js':
        return 'data'
    return 'game'


def main():
    # no argparse: tools/dis.py shadows the stdlib `dis` module that argparse imports (Python 3.14)
    args = sys.argv[1:]
    out = 'CSHARP/docs/obligations.csv'
    if '--out' in args:
        i = args.index('--out')
        out = args[i + 1]
        del args[i:i + 2]
    if len(args) != 1:
        sys.exit(__doc__)
    d = json.load(open(args[0]))
    rec = d['records'][0]
    nr = rec['normalized_result']
    sg = nr['semantic_graph']
    fps = {f['function_node_id']: f.get('fingerprint_sha256', '') for f in sg['fingerprints']}
    rows, anon = [], 0
    for n in sg['nodes']:
        if n.get('kind') != 'function':
            continue
        if not n.get('label'):
            anon += 1
            continue
        ident = n['identity']
        kind = ident['role_key'].split(':')[2]
        rng = ident['source_range']
        rows.append([ident['module_path'], n['label'], kind, rng['start']['line'], rng['end']['line'],
                     (fps.get(n['node_id']) or '')[:16], layer(ident['module_path'])])
    rows.sort(key=lambda r: (r[0], r[3], r[1]))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, 'w', newline='') as fh:
        fh.write(f"# REA evidence {rec['evidence_id']} web/src sha256 {nr['root_artifact_sha256']}"
                 f" | {len(rows)} named callables, {anon} anonymous (ported inside their parent)\n")
        w = csv.writer(fh)
        w.writerow(['module', 'name', 'kind', 'start', 'end', 'fingerprint', 'layer'])
        w.writerows(rows)
    mods = len({r[0] for r in rows})
    print(f'{len(rows)} obligations in {mods} modules ({anon} anonymous skipped) -> {out}')


if __name__ == '__main__':
    main()
