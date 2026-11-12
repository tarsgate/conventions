#!/usr/bin/env bash

TAG="$1"
PATCH="${2:-}"

if [ "$PATCH" != "" ]; then
  git apply "$PATCH" || exit 1
fi

IMAGE="ghcr.io/${GITHUB_REPOSITORY_OWNER}/conventions-ci"
if ! docker build --tag "$IMAGE:$TAG" --file Dockerfile.base .; then
  exit 1
fi

PUSH_LOG=$(mktemp)
docker push "$IMAGE:$TAG" 2>&1 | tee "$PUSH_LOG"
PUSH_STATUS="${PIPESTATUS[0]}"

if [ "$PUSH_STATUS" -ne 0 ]; then
  if grep --extended-regexp --ignore-case --quiet "denied|permission" "$PUSH_LOG"; then
    echo "::error::Permission denied when pushing $IMAGE:$TAG. Please go to https://github.com/${GITHUB_REPOSITORY_OWNER}/conventions/settings/actions to choose the option \"Read and write permissions\" under the section \"Workflow permissions\"."
  fi
  rm --force "$PUSH_LOG"
  exit "$PUSH_STATUS"
fi

rm --force "$PUSH_LOG"
