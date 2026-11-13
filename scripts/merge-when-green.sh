#!/usr/bin/env bash
set -uo pipefail

pr="${1:?uso: $0 <numero-do-pr> [merge|squash]}"
method="${2:-merge}"
case "$method" in merge|squash) ;; *) echo "método inválido: $method"; exit 2 ;; esac

gh pr checks "$pr" --watch --fail-fast || { echo "✗ checks não passaram: sem merge"; exit 1; }

state=$(gh pr view "$pr" --json mergeStateStatus -q .mergeStateStatus)
if [ "$state" = "BEHIND" ]; then
  repo=$(gh repo view --json nameWithOwner -q .nameWithOwner)
  gh api -X PUT "repos/$repo/pulls/$pr/update-branch" >/dev/null
  gh pr checks "$pr" --watch --fail-fast || { echo "✗ checks após update-branch falharam"; exit 1; }
fi

gh pr merge "$pr" "--$method" --delete-branch
