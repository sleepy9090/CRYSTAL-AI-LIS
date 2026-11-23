#!/usr/bin/env bash
# C# port loop: run Claude sessions until every task in CSHARP/TASKS.md is ticked.
# Usage (from anywhere): CSHARP/re_loop.sh [--max N] [--max-turns N] [--dry-run]
# Each session works only inside CSHARP/ and commits only CSHARP/ paths.
set -euo pipefail

MAX_TURNS=150
ALLOWED_TOOLS="\
Bash(dotnet build*),Bash(dotnet run*),Bash(dotnet test*),Bash(dotnet new*),Bash(dotnet sln*),\
Bash(dotnet --*),Bash(dotnet restore*),\
Bash(python3 tools/*),Bash(python3 ../tools/*),Bash(python3 -m tools.emu*),\
Bash(node ../web/test/*),\
Bash(src/ReC.Run/bin/*),Bash(src/ReC.Sdl/bin/*),Bash(SDL_VIDEODRIVER=*),\
Bash(rea *),mcp__rea__*,\
Bash(git add*),Bash(git commit*),Bash(git log*),Bash(git status*),Bash(git diff*),\
Read,Edit,Write,Glob,Grep"

MAX=50; DRY=false
while [[ $# -gt 0 ]]; do
  case $1 in
    --max)       MAX="$2";       shift 2 ;;
    --max-turns) MAX_TURNS="$2"; shift 2 ;;
    --dry-run)   DRY=true;       shift ;;
    *) echo "unknown: $1"; exit 1 ;;
  esac
done

cd "$(dirname "$0")"
remaining() { grep -c '^- \[ \]' TASKS.md 2>/dev/null || true; }
mkdir -p sessions
RUN_TS=$(date '+%Y%m%d_%H%M%S')

for (( i=1; i<=MAX; i++ )); do
  [[ $(remaining) -eq 0 ]] && echo "All tasks done!" && break
  SESSION_NUM=$(( $(ls sessions | wc -l) + 1 ))
  echo ""; echo "=== C# session $SESSION_NUM (run $i/$MAX, $(remaining) tasks left) ==="
  [[ "$DRY" == true ]] && echo "[dry-run]" && break
  LOG="sessions/${RUN_TS}_cs_session_$(printf '%03d' $SESSION_NUM).txt"

  PROMPT="Continue the re-c C# port (separate project in this folder, CSHARP/).
This is C# session $SESSION_NUM. Use this number in TASKS.md notes and in SESSION_SUMMARY.

Before starting: read CLAUDE.md and docs/architecture_cs.md, then skim the last 2 logs in sessions/.
Then open TASKS.md and work on the first unchecked task (- [ ]).

How to work:
1. Read the JS module(s) the task ports (../web/src/...), then translate member by member into the matching
   src/ file. Put a tag comment '// js: <module> <name>' above every ported member.
2. Build (dotnet build ReC.slnx -c Release, 0 warnings) after every few members; fix errors right away.
3. Check against the oracle (python3 tools/cs_vs_web.py ...) and coverage (python3 tools/cs_coverage.py --module <path>).
4. Write progress into the task's line in TASKS.md as you go (files, checks, results, what remains).
Tick the task (- [x]) only when its done-criterion holds. If parts remain, list them in the same task and leave
it unchecked; never add follow-up tasks.

Rules: never edit files outside CSHARP/ (../web, ../REVERSE.md, ../tools are read-only references).
Do not try to identify the game title; call it re-c. Python helpers go in tools/ as .py files, never python3 -c.
Use Read/Grep/Glob instead of cat/grep/find.
End your final message with: SESSION_SUMMARY: <one line>"

  echo "$PROMPT" | claude -p \
    --output-format stream-json --verbose \
    --max-turns "$MAX_TURNS" \
    --allowedTools "$ALLOWED_TOOLS" \
    | jq --unbuffered -r '
        if .type == "assistant" then
          .message.content[] |
          if .type == "text" then .text
          elif .type == "tool_use" then
            if .name == "Bash" then "  ▶ \(.input.command | split("\n")[0] | .[0:120])"
            elif (.input.file_path? // null) != null then "  ▶ \(.name) \(.input.file_path | split("/")[-1])"
            else "  ▶ \(.name)" end
          else empty end
        else empty end
      ' | tee "$LOG" &
  wait $!

  git add -- . 2>/dev/null || true
  if git diff --cached --quiet -- .; then
    echo "No changes — retrying same task..."
    continue
  fi
  git commit -m "C# session $SESSION_NUM: $(grep -o 'SESSION_SUMMARY: .*' "$LOG" | tail -1 | cut -c18-90 || echo progress)" -- .
done

echo ""; echo "Done. Remaining tasks: $(remaining)"
