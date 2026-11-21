#!/bin/sh
# Run every web/test/*_check.mjs and print one line per check (exit code + last output line).
cd "$(dirname "$0")/.." || exit 1
rc=0
for t in core_check ppu_check serve_check world_check sprite_check obj_check sound_check apu_check \
         wake_check giveitem_check itemuse_check; do
  node "web/test/$t.mjs" > "/tmp/$t.log" 2>&1
  e=$?
  [ $e -ne 0 ] && rc=1
  printf '%s: exit %s | %s\n' "$t" "$e" "$(tail -1 "/tmp/$t.log" | cut -c1-150)"
done
exit $rc
