#!/usr/bin/env bash
#
# Pre-commit hook to run code formatting check
#
set -euo pipefail

echo "Running pre-commit format checks..."
if ! npm run format:check > /dev/null 2>&1; then
    echo ""
    echo "❌ Formatting check failed. Please run 'make format' to fix formatting issues, then stage the changes and try committing again."
    exit 1
fi

echo "✅ Format checks passed."
