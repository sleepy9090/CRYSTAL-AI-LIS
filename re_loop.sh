#!/usr/bin/env bash
# RE loop: run Claude sessions until all Next Tasks in REVERSE.md are done.
# Usage: ./re_loop.sh [--max N] [--tasks N] [--max-turns N] [--dry-run]
set -euo pipefail

# ── PROJECT CONFIG (only this block changes per project) ──────────────────
GAME="re-c (NES)"
BINARY="rom/re-c.nes"
TOOLS_PREFIX="python3 tools/"
LABELS="labels.csv"
DEAD_ENDS="dead_ends.md"
MAX_TURNS=150
GIT_ADD="REVERSE.md labels.csv dead_ends.md web/ docs/ tools/"
ALLOWED_TOOLS="\
Bash(python3 tools/dis.py*),\
Bash(python3 tools/xref.py*),\
Bash(python3 tools/search_bytes.py*),\
Bash(python3 tools/decode_tables.py*),\
Bash(python3 tools/extract_tiles.py*),\
Bash(python3 tools/render_screen.py*),\
Bash(python3 tools/nes_header.py*),\
Bash(python3 tools/palette_dump.py*),\
Bash(python3 tools/analyze_*),\
Bash(python3 tools/audit_*),\
Bash(python3 tools/check_*),\
Bash(python3 tools/dump_*),\
Bash(python3 tools/decode_*),\
Bash(python3 tools/gen_*),\
Bash(python3 tools/render_*),\
Bash(python3 tools/emu*),\
Bash(python3 -m tools.emu*),\
Bash(python3 tools/*),\
Bash(node web/test/*),\
Bash(git add*),Bash(git commit*),\
Bash(git log*),Bash(git status*),Bash(git diff*),\
Bash(python3 -m http.server*),\
Read,Edit,Write,Glob,Grep"
# ──────────────────────────────────────────────────────────────────────────

cleanup() {
  echo ""; echo "Interrupted — killing session..."
  kill %1 2>/dev/null || true
  exit 130
}
trap cleanup INT TERM

MAX=50; TASKS=1; DRY=false
while [[ $# -gt 0 ]]; do
  case $1 in
    --max)       MAX="$2";       shift 2 ;;
    --tasks)     TASKS="$2";     shift 2 ;;
    --max-turns) MAX_TURNS="$2"; shift 2 ;;
    --dry-run)   DRY=true;       shift ;;
    *) echo "unknown: $1"; exit 1 ;;
  esac
done

cd "$(dirname "$0")"
remaining() { grep -c '^- \[ \]' REVERSE.md 2>/dev/null || true; }
mkdir -p re_loop_sessions
RUN_TS=$(date '+%Y%m%d_%H%M%S')

for (( i=1; i<=MAX; i++ )); do
  [[ $(remaining) -eq 0 ]] && echo "All tasks done!" && break
  echo ""
  SESSION_NUM=$(( $(git rev-list --count HEAD 2>/dev/null || echo 0) + 1 ))
  echo "=== Session $SESSION_NUM (run $i/$MAX, $(remaining) tasks left) ==="
  [[ "$DRY" == true ]] && echo "[dry-run]" && break

  LOG="re_loop_sessions/${RUN_TS}_session_$(printf '%03d' $SESSION_NUM).txt"

  PROMPT="Continue the $GAME reverse-engineering and web port project.
This is session $SESSION_NUM. Use exactly this number wherever you record a session
(REVERSE.md task notes, dead_ends.md, SESSION_SUMMARY).

Before picking a task:
1. Read dead_ends.md to avoid known dead-end approaches.
2. Skim the last 3 session logs in re_loop_sessions/ to understand recent work.

Then read REVERSE.md ## Next Tasks. Pick the top $TASKS unchecked items (- [ ]).

Tasks fall into three categories:

**RE investigation** (${TOOLS_PREFIX}dis.py / xref.py / search_bytes.py / decode_tables.py):
1. Run 2-3 tool calls.
2. Write findings to REVERSE.md immediately. Add addresses to $LABELS.
3. Repeat: more tool calls, then write again.
Do not batch all investigation before writing — write after every few tool calls.

**Web port fix** (editing web/ files):
1. Read the relevant web/ file first.
2. Apply the fix — binary values are already documented in REVERSE.md.
3. Update REVERSE.md to note what changed.
No need to re-investigate the binary.

**Documentation** (docs/ directory):
1. Produce docs/architecture_exe.md (binary layout, call graph, module map, data flow).
2. Produce docs/architecture_web.md (module graph, EXE-to-web mapping, state machine).
Both files should exist before the project is complete.

**If stuck**: after 10 tool calls with no progress, append to $DEAD_ENDS
(tried / why-failed / better-approach / session number), split the task into
2-3 sub-tasks in REVERSE.md, mark original [x] 'Split into sub-tasks', move on.

**New tasks / leftovers**: append new follow-up tasks at the END of
### RE Investigation, never above tasks that are already queued. Do not create
'follow-ups N+1' chains: if parts of your task remain, add them once as a single
low-priority task at the end of the list (or merge them into an existing queued
task that covers the same subsystem), then mark your task done.
If the task you picked is itself a '(low priority)' or leftovers task, never file a
new task for what remains: write the remaining items into that same task's text,
leave it unchecked, and note what you finished. Do not move it to the end of the list.

Mark task done (- [x]) once fully documented or implemented.
End your final message with: SESSION_SUMMARY: <one line>

RE tools (${TOOLS_PREFIX}):
  ${TOOLS_PREFIX}dis.py <bank> <addr> [nbytes] [--stop]
  ${TOOLS_PREFIX}xref.py <addr>
  ${TOOLS_PREFIX}search_bytes.py <hex> [--context N] [--disasm]
  ${TOOLS_PREFIX}decode_tables.py <addr> <count> <fmt>
  ${TOOLS_PREFIX}extract_tiles.py
  ${TOOLS_PREFIX}render_screen.py

labels.csv format: bank,addr,name,comment  (bank = 8KB PRG bank 0-31, * for RAM; addr hex no prefix)

IMPORTANT tool rules:
- Use Read (not cat/head/tail), Grep (not grep/rg), Glob (not ls/find)
- Write a .py file first — never run inline python3 -c
- No third-party disassemblers

Do not re-document already-covered addresses. Stop after $TASKS tasks."

  echo "$PROMPT" | claude -p \
    --output-format stream-json --verbose \
    --max-turns "$MAX_TURNS" \
    --allowedTools "$ALLOWED_TOOLS" \
    | jq --unbuffered -r '
        if .type == "assistant" then
          .message.content[] |
          if .type == "text" then .text
          elif .type == "tool_use" then
            if .name == "Bash" then
              "  \u25b6 \(.input.command | split("\n")[0] | .[0:120])"
            elif .name == "Read" then
              "  \u25b6 Read \(.input.file_path | split("/")[-1])\(if .input.offset then " +\(.input.offset)" else "" end)"
            elif .name == "Write" then
              "  \u25b6 Write \(.input.file_path | split("/")[-1])"
            elif .name == "Edit" then
              "  \u25b6 Edit \(.input.file_path | split("/")[-1])"
            elif .name == "Grep" then
              "  \u25b6 Grep \"\(.input.pattern)\" \(.input.path // "")"
            elif .name == "Glob" then
              "  \u25b6 Glob \(.input.pattern)"
            else
              "  \u25b6 \(.name) \(.input | keys | join(" "))"
            end
          else empty
          end
        else empty
        end
      ' | tee "$LOG" &
  wait $!

  # Prefer the task this session completed ("done session N"); fall back to any newly-checked line.
  DONE_LINES=$(git diff REVERSE.md | grep '^+- \[x\]' || true)
  SUMMARY=$({ grep -i "session ${SESSION_NUM}\b" <<<"$DONE_LINES" || printf '%s\n' "$DONE_LINES"; } | head -1 | sed 's/^+- \[x\] //' | tr -d '`' | cut -c1-70 || true)
  [[ -z "$SUMMARY" ]] && SUMMARY="progress"

  git add $GIT_ADD 2>/dev/null || true
  if git diff --cached --quiet; then
    echo "No changes — retrying same task..."
    continue
  fi

  git commit -m "RE session $SESSION_NUM: $SUMMARY"
  echo "Committed: $SUMMARY"
done

echo ""
echo "Done. Remaining tasks: $(remaining)"
