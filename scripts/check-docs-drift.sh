#!/usr/bin/env bash
set -uo pipefail

cd "$(git rev-parse --show-toplevel)" || exit 1
fail=0
report() { echo "✗ $1"; fail=1; }

for doc in README.md AGENTS.md CLAUDE.md; do
  [ -f "$doc" ] || continue
  broken=$(grep -o -E '\]\(([^)#]+)(#[^)]*)?\)' "$doc" | sed -E 's/^\]\(([^)#]+).*/\1/' | grep -vE '^(https?:|mailto:)' | sort -u | while read -r link; do [ -e "$link" ] || echo "$link"; done)
  if [ -n "$broken" ]; then
    report "$doc: links relativos para arquivos que não existem:"
    printf '%s\n' "$broken" | sed 's/^/    /'
  fi
done

if [ -f README.md ]; then
  for agent_file in .claude/agents/*.md; do
    [ -e "$agent_file" ] || continue
    name=$(basename "$agent_file" .md)
    grep -qF "\`$name\`" README.md || report "README.md não menciona o agente \`$name\` (.claude/agents)"
  done

  for skill_dir in .claude/skills/*/; do
    name=$(basename "$skill_dir")
    case "$name" in speckit-*) continue ;; esac
    grep -qE "/$name([^a-z0-9-]|\$)" README.md || report "README.md não menciona a skill /$name (.claude/skills)"
  done

  for script in scripts/*.sh; do
    name=$(basename "$script")
    grep -qF "$name" README.md AGENTS.md docs/policies/*.md 2>/dev/null || report "$script não é citado no README, no AGENTS.md nem em docs/policies"
  done
fi

if [ "$fail" -eq 0 ]; then
  echo "✓ docs sem drift"
fi
exit "$fail"
