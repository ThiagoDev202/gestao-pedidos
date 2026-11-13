#!/usr/bin/env bash
set -uo pipefail

cd "$(git rev-parse --show-toplevel)" || exit 1
{
  git for-each-ref --format='%(refname:short)' refs/heads refs/remotes
  ls specs 2>/dev/null
} | grep -oE '(^|/)[0-9]{3}-' | tr -d '/-' | sort -u | tail -1 | {
  read -r last
  printf '%03d\n' "$((10#${last:-0} + 1))"
}
