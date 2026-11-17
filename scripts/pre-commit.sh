#!/usr/bin/env bash
#
# Pre-commit hook to run code formatting check
#
set -euo pipefail

echo "Running pre-commit format checks..."

echo "Running prettier format check..."
if ! npm run format:check > /dev/null 2>&1; then
    echo ""
    echo "❌ Prettier check failed. Please run 'make format' to fix formatting issues, then stage the changes and try committing again."
    npm run format:check
    exit 1
fi

echo "Running fantomless format check..."
if ! dotnet dnx --yes fantomless-tool --check --recurse . > /dev/null 2>&1; then
    echo ""
    echo "❌ Fantomless check failed. Please run 'make format' to fix formatting issues, then stage the changes and try committing again."
    dotnet dnx --yes fantomless-tool --check --recurse .
    exit 1
fi

echo "✅ Format checks passed."
