#!/usr/bin/env bash
set -uo pipefail

root=$(git rev-parse --show-toplevel) || exit 1
cd "$root" || exit 1
fail=0
warn() { echo "! $1"; }
ok() { echo "✓ $1"; }

git config core.hooksPath .githooks && ok "hooks git ativos (core.hooksPath=.githooks)"

if command -v dotnet >/dev/null 2>&1; then
  ok "dotnet $(dotnet --version)"
else
  warn "dotnet não encontrado: instale o SDK 10 para rodar o backend fora do Docker e para o hook de build do Claude"
fi

if command -v node >/dev/null 2>&1; then
  major=$(node -p 'process.versions.node.split(".")[0]')
  if [ "$major" -ge 20 ]; then ok "node $(node -v)"; else warn "node $(node -v): o front usa Node 20+"; fi
else
  warn "node não encontrado (necessário para o front fora do Docker)"
fi

if command -v jq >/dev/null 2>&1; then
  ok "jq disponível (os hooks do Claude precisam dele)"
else
  warn "instale jq: sem ele os hooks do projeto bloqueiam comandos Bash do Claude Code"
  fail=1
fi

if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
  ok "docker compose disponível"
else
  warn "docker compose não encontrado (necessário para subir o ambiente e para os testes de integração)"
  fail=1
fi

if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then ok "gh autenticado"; else warn "gh ausente ou sem login (gh auth login)"; fi

if [ -f .env.example ] && [ ! -f .env ]; then
  cp .env.example .env && ok ".env criado a partir de .env.example"
fi

echo
echo "Próximos passos:"
echo "  docker compose up --build"
echo "  web http://localhost:8080 | API http://localhost:5080/scalar"
echo "  leia README.md e AGENTS.md; entregas seguem o /ship"
exit "$fail"
