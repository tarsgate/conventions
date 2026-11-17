#!/usr/bin/env bash
#
# Pre-commit hook to run code formatting check
#
set -euo pipefail

echo "Running pre-commit format checks..."

run_check() {
    local name="$1"
    shift

    echo "Running $name format check..."
    local output
    if ! output=$("$@" 2>&1); then
        echo ""
        echo "❌ $name check failed. Please run 'make format' to fix formatting issues, then stage the changes and try committing again."
        echo "$output"
        exit 1
    fi
}

run_check "prettier" npm run format:check
run_check "fantomless" dotnet dnx --yes fantomless-tool --check --recurse .

echo "✅ Format checks passed."
