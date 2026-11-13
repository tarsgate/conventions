#!/usr/bin/env bash

TAG="$1"
PATCH="${2:-}"

if [ "$PATCH" != "" ]; then
  git apply "$PATCH" || exit 1
fi

OWNER_LOWER=$(echo "${GITHUB_REPOSITORY_OWNER}" | tr '[:upper:]' '[:lower:]')
IMAGE="ghcr.io/${OWNER_LOWER}/conventions-ci"
BUILD_ARGS=(--tag "$IMAGE:$TAG" --file Dockerfile.base)
REPO_URL="https://github.com/${GITHUB_REPOSITORY:-${GITHUB_REPOSITORY_OWNER}/conventions}"
BUILD_ARGS+=(--label "org.opencontainers.image.source=${REPO_URL}")

if ! docker build "${BUILD_ARGS[@]}" .; then
  exit 1
fi

PUSH_LOG=$(mktemp)
docker push "$IMAGE:$TAG" 2>&1 | tee "$PUSH_LOG"
PUSH_STATUS="${PIPESTATUS[0]}"

if [ "$PUSH_STATUS" -ne 0 ]; then
  if grep --extended-regexp --ignore-case --quiet "denied|permission" "$PUSH_LOG"; then
    echo "::error::Permission denied when pushing $IMAGE:$TAG."
    echo "::error::1) In repository settings (https://github.com/${GITHUB_REPOSITORY_OWNER}/conventions/settings/actions), ensure \"Read and write permissions\" is selected under \"Workflow permissions\"."
    echo "::error::2) If the package already exists, ensure the repository has access: go to https://github.com/users/${GITHUB_REPOSITORY_OWNER}/packages/container/package/conventions-ci (or https://github.com/orgs/${GITHUB_REPOSITORY_OWNER}/packages/container/package/conventions-ci) and click \"Connect repository\" to connect ${GITHUB_REPOSITORY:-${GITHUB_REPOSITORY_OWNER}/conventions} (or under Package Settings -> \"Manage Actions access\", add the repository with \"Write\" role, or delete the old package so CI can recreate and link it automatically)."
  fi
  rm --force "$PUSH_LOG"
  exit "$PUSH_STATUS"
fi

rm --force "$PUSH_LOG"
