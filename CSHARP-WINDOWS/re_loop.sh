#!/usr/bin/env bash
# Windows C# port loop: run Claude sessions until every task in CSHARP-WINDOWS/TASKS.md is ticked.
# Usage (from anywhere): CSHARP-WINDOWS/re_loop.sh [--max N] [--max-turns N] [--dry-run]
# Each session works only inside CSHARP-WINDOWS/ and commits only CSHARP-WINDOWS/ paths.
set -euo pipefail

MAX_TURNS=150
ALLOWED_TOOLS="\
Bash(dotnet build*),Bash(dotnet run*),Bash(dotnet test*),Bash(dotnet new*),Bash(dotnet sln*),\
Bash(dotnet --*),Bash(dotnet restore*),Bash(dotnet publish*),\
Bash(python3 tools/*),Bash(python3 ../tools/*),Bash(python3 -m tools.emu*),\
Bash(node ../web/test/*),\
Bash(src/ReC.Run/bin/*),Bash(WINEPREFIX=*),Bash(wine *),Bash(xvfb-run*),\
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
  echo ""; echo "=== Windows C# session $SESSION_NUM (run $i/$MAX, $(remaining) tasks left) ==="
  [[ "$DRY" == true ]] && echo "[dry-run]" && break
  LOG="sessions/${RUN_TS}_win_session_$(printf '%03d' $SESSION_NUM).txt"

  PROMPT="Continue the re-c Windows C# port (separate project in this folder, CSHARP-WINDOWS/): a single-file Windows ReC.exe.
This is Windows C# session $SESSION_NUM. Use this number in TASKS.md notes and in SESSION_SUMMARY.

Before starting: read CLAUDE.md and docs/architecture_win.md, then skim the last 2 logs in sessions/.
Then open TASKS.md and work on the first unchecked task (- [ ]).

How to work:
1. Read the files the task names (../CSHARP/src for the core, ../web/src for platform behaviour), then implement in the matching
   src/ file. Keep or add '// js: <module> <name>' tags on members that port a web-port function.
2. Build (dotnet build ReC.Windows.slnx -c Release, 0 warnings) after every few members; fix errors right away.
3. Check parity (python3 tools/core_vs_web.py / tools/win_vs_web.py ...) and coverage (python3 tools/cs_coverage.py --module <path>).
4. Write progress into the task's line in TASKS.md as you go (files, checks, results, what remains).
Tick the task (- [x]) only when its done-criterion holds. If parts remain, list them in the same task and leave
it unchecked; never add follow-up tasks.

Rules: never edit files outside CSHARP-WINDOWS/ (../web, ../CSHARP, ../REVERSE.md, ../tools are read-only references).
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
  git commit -m "Windows C# session $SESSION_NUM: $(grep -o 'SESSION_SUMMARY: .*' "$LOG" | tail -1 | cut -c18-90 || echo progress)" -- .
done

echo ""; echo "Done. Remaining tasks: $(remaining)"
