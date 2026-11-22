#!/bin/sh
# tools/event_states.sh NN:PC [NN:PC ..] — export tools.emu + port states stopped at EventActionTable act NN's
# first instruction 30:PC (scenario event_01 base state, act forced at EventActionDispatch 30:D552; see event_check.mjs).
# NN is the act number in hex (as written to $22). Session 204.
for a in "$@"; do
  n=$(echo "$a" | cut -d: -f1)
  pc=$(echo "$a" | cut -d: -f2)
  echo "== act $n at 30:$pc"
  python3 -m tools.emu.web_state --load-state tools/emu/states/event_base.state --keys 2510:B:4 2530:B:4 \
    --force "30:D552:0022=$n" --until "30:$pc" \
    --out "web/test/states/event$n.json" --save-state "tools/emu/states/event$n.state" 2>&1 | tail -2
done
