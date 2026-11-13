#!/usr/bin/env bash
set -uo pipefail

cd "$(git rev-parse --show-toplevel)" || exit 1
mode="${1:-}"
base="${QA_BASE:-origin/main}"
seal_file="$(git rev-parse --git-path qa-guardian-red.sha256)"
branch_file="$(git rev-parse --git-path qa-guardian-red.branch)"
branch="$(git rev-parse --abbrev-ref HEAD)"
sealed_branch="$(cat "$branch_file" 2>/dev/null || true)"
if ! base_commit=$(git merge-base "$base" HEAD 2>/dev/null); then
  echo "✗ base $base não encontrada (git fetch origin ou defina QA_BASE)"
  exit 1
fi
patterns=('backend/tests/' '*.test.ts' '*.test.tsx' 'frontend/e2e/' 'frontend/vitest.config.ts' 'frontend/src/test/')

list_files() {
  {
    git -c core.quotePath=false diff -z --name-only --diff-filter=ACMR "$base_commit" -- "${patterns[@]}"
    git -c core.quotePath=false ls-files -z -o --exclude-standard -- "${patterns[@]}"
  } | sort -zu
}

case "$mode" in
  seal)
    if [ -s "$seal_file" ] && [ "$sealed_branch" = "$branch" ] && [ "${2:-}" != "--force" ]; then
      echo "✗ já existe um lacre ($seal_file); só o qa-guardian re-lacra, com: $0 seal --force"
      exit 1
    fi
    list_files | xargs -0 -r sha256sum > "$seal_file"
    if [ ! -s "$seal_file" ]; then
      rm -f "$seal_file"
      echo "✗ nenhum arquivo de teste alterado ou novo em relação a $base: nada a lacrar"
      exit 1
    fi
    printf '%s\n' "$branch" > "$branch_file"
    echo "✓ lacre gravado em $seal_file ($(wc -l < "$seal_file") arquivos)"
    ;;
  check)
    fail=0
    if [ ! -s "$seal_file" ]; then
      echo "✗ lacre ausente ou vazio: $seal_file (a fase RED não foi lacrada)"
      exit 1
    fi
    if [ "$sealed_branch" != "$branch" ]; then
      echo "✗ o lacre existente é da branch '${sealed_branch:-desconhecida}', não de '$branch': lacre a fase RED desta tarefa com $0 seal"
      exit 1
    fi
    if ! sha256sum -c --quiet "$seal_file"; then
      echo "✗ teste lacrado foi alterado ou apagado depois da fase RED"
      fail=1
    fi
    sealed=$(sed 's/^[0-9a-f]\{64\}  //' "$seal_file" | sort)
    current=$(list_files | tr '\0' '\n' | grep -v '^$' | sort)
    outside=$(comm -13 <(printf '%s\n' "$sealed") <(printf '%s\n' "$current"))
    if [ -n "$outside" ]; then
      echo "✗ arquivos de teste fora do lacre (só o qa-guardian cria ou altera testes):"
      printf '%s\n' "$outside" | sed 's/^/    /'
      fail=1
    fi
    deleted=$(git -c core.quotePath=false diff --no-renames --name-only --diff-filter=D "$base_commit" -- "${patterns[@]}")
    if [ -n "$deleted" ]; then
      echo "✗ testes apagados em relação a $base:"
      printf '%s\n' "$deleted" | sed 's/^/    /'
      fail=1
    fi
    [ "$fail" -eq 0 ] && echo "✓ lacre dos testes OK"
    exit "$fail"
    ;;
  *)
    echo "uso: $0 seal [--force]|check   (QA_BASE=origin/main por padrão)"
    exit 2
    ;;
esac
