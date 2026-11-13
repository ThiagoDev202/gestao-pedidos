#!/usr/bin/env bash
export PATH="$HOME/.dotnet:$PATH"
input=$(cat)
file=$(printf '%s' "$input" | jq -r '.tool_input.file_path // ""' 2>/dev/null)
[ -z "$file" ] && exit 0
root=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
rel="${file#"$root"/}"

run() {
  local out
  if ! out=$("$@" 2>&1); then
    printf '%s\n' "$out" | tail -40 >&2
    exit 2
  fi
}

case "$rel" in
  backend/*.cs|backend/*.csproj|backend/*.props)
    command -v dotnet >/dev/null 2>&1 || exit 0
    sln=$(ls "$root"/backend/*.slnx "$root"/backend/*.sln 2>/dev/null | head -1)
    [ -z "$sln" ] && exit 0
    run dotnet build "$sln" -warnaserror -v q --nologo
    ;;
  frontend/src/*.ts|frontend/src/*.tsx|frontend/e2e/*.ts)
    [ -d "$root/frontend/node_modules" ] || exit 0
    cd "$root/frontend" || exit 0
    run npx --no-install tsc --noEmit -p tsconfig.app.json
    run npx --no-install eslint "${rel#frontend/}"
    ;;
esac
exit 0
