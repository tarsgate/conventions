#!/usr/bin/env bash
set -euxo pipefail

# cd to directory of this script
cd "$(dirname "$0")"
pnpm install
source ./commitlint/version.config
pnpm add @commitlint/config-conventional@$COMMITLINT_VERSION
npx commitlint --version
npx commitlint $@
