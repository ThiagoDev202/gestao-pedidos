#!/usr/bin/env bash
set -uo pipefail

cd "$(git rev-parse --show-toplevel)" || exit 1
base="${1:?uso: PR_TITLE=... PR_AUTHOR=... HEAD_REF=... $0 <base> [head]}"
head="${2:-HEAD}"
for ref in "$base" "$head"; do
  case "$ref" in -*) echo "uso: $0 <base> [head]"; exit 2 ;; esac
done
title="${PR_TITLE-$(git log -1 --no-merges --format=%s "$head" 2>/dev/null)}"
author="${PR_AUTHOR-$(gh api user -q .login 2>/dev/null || true)}"
head_ref="${HEAD_REF:-$(git rev-parse --abbrev-ref HEAD)}"
maintainers_file="${MAINTAINERS_FILE:-.github/MAINTAINERS}"
fail=0
report() { echo "✗ $1"; fail=1; }

types='feat|fix|refactor|perf|test|docs|style|build|ci|chore'
scopes='api|web|db|e2e|docker|ci|docs|ai'
subject_re="^($types)(\(($scopes)(,($scopes))*\))?!?: .{1,72}$"
tool_mark_re='^Co-Authored-By: .*(claude|anthropic|copilot|cursor)|Generated with \[?Claude'

if ! [[ "$head_ref" =~ ^($types)/[0-9]{3}-[a-z0-9]+(-[a-z0-9]+)*$ ]]; then
  report "branch '$head_ref' fora do padrão {tipo}/{NNN}-{slug} (ex.: feat/001-backend-pedidos)"
fi

maintainer_logins=$(awk 'NF && $1 !~ /^#/ {print $1}' "$maintainers_file" 2>/dev/null)
maintainer_emails=$(awk 'NF && $1 !~ /^#/ {for (i = 2; i <= NF; i++) print tolower($i)}' "$maintainers_file" 2>/dev/null)

is_maintainer=false
printf '%s\n' "$maintainer_logins" | grep -qxF -- "$author" && is_maintainer=true

while IFS= read -r sha; do
  [ -z "$sha" ] && continue
  subject=$(git log -1 --format=%s "$sha")
  if ! [[ "$subject" =~ $subject_re || "$subject" =~ ^Revert\ \" ]]; then
    report "commit ${sha:0:7} fora do padrão: '$subject'"
  fi
  if git log -1 --format=%B "$sha" | grep -qiE "$tool_mark_re"; then
    report "commit ${sha:0:7} tem coautoria/marca de ferramenta: commits só no nome da pessoa"
  fi
  for email in "$(git log -1 --format=%ae "$sha" | tr '[:upper:]' '[:lower:]')" "$(git log -1 --format=%ce "$sha" | tr '[:upper:]' '[:lower:]')"; do
    [ "$email" = "noreply@github.com" ] && continue
    printf '%s\n' "$maintainer_emails" | grep -qxF -- "$email" || is_maintainer=false
  done
done < <(git rev-list --no-merges "$base".."$head")

if ! [[ "$title" =~ $subject_re ]]; then
  report "título do PR fora do Conventional Commits com escopo: '$title'"
fi

pr_type="${title%%[(:!]*}"
declared_scopes=""
if [[ "$title" =~ ^[a-z]+\(([^)]*)\) ]]; then declared_scopes="${BASH_REMATCH[1]}"; fi

files=$(git diff --name-only --diff-filter=ACMRD "$base"..."$head")

protected_re='^(\.github/|scripts/|\.githooks/|\.claude/|\.specify/|AGENTS\.md$|CLAUDE\.md$|docs/policies/|docs/decisions/)'
if [ "$is_maintainer" != true ]; then
  touched=$(printf '%s\n' "$files" | grep -E "$protected_re" || true)
  if [ -n "$touched" ]; then
    report "quem não é maintainer alterou área protegida:"
    printf '%s\n' "$touched" | sed 's/^/    /'
  fi
fi

scope_matches() {
  local scope="$1" file="$2"
  case "$scope" in
    api) [[ "$file" =~ ^(backend/|docs/api/) ]] ;;
    web) [[ "$file" =~ ^frontend/ ]] ;;
    db) [[ "$file" =~ ^database/ ]] ;;
    e2e) [[ "$file" =~ ^frontend/e2e/ || "$file" =~ ^frontend/playwright\.config\. ]] ;;
    docker) [[ "$file" =~ (^docker-compose.*\.yml$|Dockerfile|\.dockerignore$|nginx\.conf$) ]] ;;
    ci) [[ "$file" =~ ^(\.github/|scripts/|\.githooks/) ]] ;;
    docs) [[ "$file" =~ ^(docs/|README\.md$) ]] ;;
    ai) [[ "$file" =~ ^(\.claude/|\.specify/|specs/|CLAUDE\.md$|AGENTS\.md$|\.mcp\.json$|docs/policies/|docs/decisions/|docs/playbooks/|README\.md$) ]] ;;
    *) return 1 ;;
  esac
}

common_re='^(\.gitignore|\.gitattributes|\.editorconfig|\.env\.example|LICENSE)$'
if [ -n "$declared_scopes" ] && [ "$pr_type" != "chore" ]; then
  outside=""
  while IFS= read -r f; do
    [ -z "$f" ] && continue
    [[ "$f" =~ $common_re ]] && continue
    matched=false
    IFS=',' read -ra sc <<< "$declared_scopes"
    for s in "${sc[@]}"; do scope_matches "$s" "$f" && { matched=true; break; }; done
    $matched || outside="$outside$f"$'\n'
  done <<< "$files"
  if [ -n "$outside" ]; then
    report "arquivos fora do escopo declarado ($declared_scopes):"
    printf '%s' "$outside" | sed 's/^/    /'
  fi
fi

if [[ "$pr_type" =~ ^(feat|fix|refactor|perf)$ ]]; then
  non_test_backend=$(printf '%s\n' "$files" | grep -E '^backend/src/.*\.cs$' || true)
  backend_tests=$(git diff --name-only --diff-filter=ACM "$base"..."$head" -- 'backend/tests/' || true)
  if [ -n "$non_test_backend" ] && [ -z "$backend_tests" ]; then
    report "código de backend/src alterado sem teste em backend/tests/ (Constituição V)"
  fi
  non_test_front=$(printf '%s\n' "$files" | grep -E '^frontend/src/.*\.(ts|tsx)$' | grep -vE '\.test\.tsx?$|/test/|/components/ui/|\.d\.ts$' || true)
  front_tests=$(git diff --name-only --diff-filter=ACM "$base"..."$head" -- 'frontend/src/*.test.ts' 'frontend/src/*.test.tsx' 'frontend/e2e/' || true)
  if [ -n "$non_test_front" ] && [ -z "$front_tests" ]; then
    report "código de frontend/src alterado sem teste (*.test.ts(x) ou frontend/e2e/) (Constituição V)"
  fi
fi

if [ -n "${PR_BODY+x}" ]; then
  if printf '%s' "$PR_BODY" | grep -qiE "$tool_mark_re"; then
    report "corpo do PR tem marca de ferramenta/coautoria"
  fi
  if ! printf '%s' "$PR_BODY" | grep -qE '^- \[[xX]\] `code-reviewer`'; then
    report "corpo do PR sem a caixa do code-reviewer marcada"
  fi
fi

[ "$fail" -eq 0 ] && echo "✓ governança do PR OK"
exit "$fail"
