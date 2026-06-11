#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 --check|--write" >&2
    exit 1
fi

if [ "$1" != "--check" ] && [ "$1" != "--write" ]; then
    echo "Usage: $0 --check|--write" >&2
    exit 1
fi

which dotnet || (echo '\nPlease install .NET SDK v10.x or newer' >&2 && exit 1)

which npx || (echo '\nPlease install `npx`, maybe installing NPM?' >&2 && exit 1)

npx --no-install prettier --version || (echo '\nPlease install `prettier` via `npm install` first; if this problem persists, try `npm rebuild` or `git clean -fdx` before `npm install`' >&2 && exit 1)

npx --no-install prettier "$@" --quote-props=consistent './**/*.{yml,ts}'

FANTOMLESS_CALL="dotnet dnx --yes --version 4.7.997-prerelease fantomless-tool --recurse ."
if [ "$1" = "--check" ]; then
    $FANTOMLESS_CALL --check
else
    $FANTOMLESS_CALL
fi
