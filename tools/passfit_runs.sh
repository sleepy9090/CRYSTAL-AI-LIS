#!/bin/sh
# s212: regenerate the gap-(c) pass-cost study (tools.emu stage + per-dispatch cycles, port stage + per-dispatch
# counts) for areas $81 / $59 / $5A into $PASSFIT_DIR (default /tmp/passfit), fit on $81 + $59 (--ai: per-AI
# dispatch fits), then cross-check the saved coefficients on $5A.
# usage: sh tools/passfit_runs.sh [--no-emu]   (--no-emu reuses the c*.txt tools.emu tables)
set -e
cd "$(dirname "$0")/.."
D=${PASSFIT_DIR:-/tmp/passfit}
mkdir -p "$D"
if [ "$1" != "--no-emu" ]; then
  python3 -m tools.emu.pass_cost tools/emu/states/group81.state 100 --split-int --slots --out "$D/c81.txt" \
    --keys 2505:DOWN:2 2569:DOWN:6 2575:B:4 2585:DOWN:6 2591:B:4 2601:DOWN:6
  python3 -m tools.emu.pass_cost tools/emu/states/group59.state 106 --split-int --slots --out "$D/c59.txt" \
    --keys 2510:UP:40 2550:B:4 2566:B:4 2582:B:4 2598:B:4
  python3 -m tools.emu.pass_cost tools/emu/states/group5a.state 85 --split-int --slots --out "$D/c5a.txt" \
    --keys 2520:UP:40 2560:B:4 2576:B:4 2592:B:4
fi
# port keys = the pass frames of tools.emu's pad_prev writes (as in group81_check.mjs / group5c_check.mjs)
node web/test/pass_count.mjs web/test/states/group81.json 2604 --stages --out "$D/s81.txt" \
  2506:DOWN:2 2569:DOWN:7 2576:B:4 2585:DOWN:7 2592:B:4 2601:DOWN:6 2607:B:5
node web/test/pass_count.mjs web/test/states/group59.json 2611 --stages --out "$D/s59.txt" \
  2510:UP:40 2550:B:4 2566:B:4 2582:B:4 2598:B:5
node web/test/pass_count.mjs web/test/states/group5a.json 2600 --stages --out "$D/s5a.txt" \
  2520:UP:40 2560:B:4 2576:B:5 2593:B:4
python3 tools/pass_fit.py "$D/c81.txt,$D/c59.txt" "$D/s81.txt,$D/s59.txt" --stages --ai --skip-lag --worst 4 \
  --save "$D/coef.json"
echo '--- out of sample ($5A):'
python3 tools/pass_fit.py "$D/c5a.txt" "$D/s5a.txt" --stages --ai --skip-lag --worst 6 --coef "$D/coef.json"
