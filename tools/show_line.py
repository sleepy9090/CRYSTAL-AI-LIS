#!/usr/bin/env python3
"""Print one line of a text file, wrapped, optionally only from a char offset.

usage: show_line.py FILE LINE [START [LEN]]
"""
import sys
import textwrap


def main():
    path, line = sys.argv[1], int(sys.argv[2])
    start = int(sys.argv[3]) if len(sys.argv) > 3 else 0
    n = int(sys.argv[4]) if len(sys.argv) > 4 else None
    with open(path, encoding="utf-8") as f:
        for i, s in enumerate(f, 1):
            if i == line:
                s = s.rstrip("\n")[start:]
                if n is not None:
                    s = s[:n]
                print(f"[{len(s)} chars from {start}]")
                print("\n".join(textwrap.wrap(s, 200)))
                return


if __name__ == "__main__":
    main()
