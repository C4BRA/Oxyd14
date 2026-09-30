#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
base=${1:-upstream/master}
pin=$(git rev-parse "$base:RobustToolbox")
test "$(git rev-parse HEAD:RobustToolbox)" = "$pin"
test "$(git rev-parse :RobustToolbox)" = "$pin"
test "$(git -C RobustToolbox rev-parse HEAD)" = "$pin"
test -z "$(git -C RobustToolbox status --porcelain --untracked-files=no)"
git diff --exit-code "$base" HEAD -- RobustToolbox .gitmodules
git diff --exit-code -- RobustToolbox .gitmodules
git diff --cached --exit-code -- RobustToolbox .gitmodules
test -z "$(git log --format=%H "$base..HEAD" -- RobustToolbox .gitmodules)"
printf 'Engine matches %s exactly: %s; no PR engine commits or local engine edits.\n' "$base" "$pin"
