#!/usr/bin/env bash
input=$(cat)
if command -v jq >/dev/null 2>&1; then
  cmd=$(printf '%s' "$input" | jq -r '.tool_input.command // ""' 2>/dev/null) || cmd="__erro__"
elif command -v python3 >/dev/null 2>&1; then
  cmd=$(printf '%s' "$input" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("tool_input",{}).get("command",""))' 2>/dev/null) || cmd="__erro__"
else
  cmd="__erro__"
fi
if [ "$cmd" = "__erro__" ]; then
  echo "Hook do projeto: não consegui ler o comando (instale jq ou python3). Bloqueado por segurança." >&2
  exit 2
fi

if printf '%s' "$cmd" | grep -qE '(^|[;&|(` ])gh +pr +(create|edit)\b' && printf '%s' "$cmd" | grep -qiE 'Generated with \[?Claude|Co-Authored-By: .*(claude|anthropic|copilot|cursor)'; then
  echo "PR bloqueado: o corpo não leva marca de ferramenta nem coautoria (AGENTS.md, docs/policies/git.md)." >&2
  exit 2
fi

printf '%s' "$cmd" | grep -qE '(^|[;&|(` ])git( +-[cC] +[^ ]+| +--[a-z-]+(=[^ ]+)?)* +commit\b' || exit 0

if printf '%s' "$cmd" | grep -qE -- '--no-verify|commit\b[^;&|]* -n\b'; then
  echo "Commit bloqueado: --no-verify/-n pula os guardrails do projeto." >&2
  exit 2
fi

root=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
if [ "$(git -C "$root" config --get core.hooksPath)" != ".githooks" ]; then
  echo "Commit bloqueado: o hook git dos guardrails não está ativo. Rode: git config core.hooksPath .githooks" >&2
  exit 2
fi
exit 0
