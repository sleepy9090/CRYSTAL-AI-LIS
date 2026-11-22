#!/bin/sh
# s207: TalkActionTable run-level states from event_base.state: EventActionDispatch's JMP ($0010) at 30:D560
# is redirected into the talk handler (Y = $22 * 2 there, as after NpcTalk's TAY at 30:D115).
# Usage: sh tools/emu/talk_states.sh  ->  web/test/states/talkDD.json + tools/emu/states/talkDD.state (DD decimal)
set -e
for a in 10,D1EB 13,D1B5 14,D18E 16,D1CA 19,D2AE 1A,D336; do
  nn=${a%,*}; pc=${a#*,}
  lo=$(echo "$pc" | cut -c3-4); hi=$(echo "$pc" | cut -c1-2)
  dd=$(printf '%d' "0x$nn")
  python3 -m tools.emu.web_state --load-state tools/emu/states/event_base.state --keys 2510:B:4 2530:B:4 \
    --force 30:D552:0022=$nn --force 30:D560:0010=$lo --force 30:D560:0011=$hi --until 30:$pc \
    --out web/test/states/talk$dd.json --save-state tools/emu/states/talk$dd.state
done
