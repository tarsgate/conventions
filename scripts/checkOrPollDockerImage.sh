#!/usr/bin/env bash

# On fork PRs, how often to re-check whether the image was published and the fork's CI status
FORK_IMAGE_POLL_INTERVAL_SECONDS=60
# On fork PRs, how long to wait for the fork's CI to start a run for the PR head commit before failing
FORK_CI_RUN_GRACE_SECONDS=300

TAG="$1"
OUTPUT_FILE="${GITHUB_OUTPUT:-/dev/stdout}"

image_exists_in() {
  docker manifest inspect "ghcr.io/$1/conventions-ci:$TAG" > /dev/null 2>&1
}

set_found() {
  echo "image_exists=true" >> "$OUTPUT_FILE"
  echo "owner=$1" >> "$OUTPUT_FILE"
}

# Prints "<total_count> <status> <conclusion> <html_url>" for the CI runs of the fork's PR head commit
# (the first two fields are "0 none none none" when no run was started for that commit)
fork_ci_run() {
  gh api "repos/$HEAD_REPO/actions/workflows/CI.yml/runs?head_sha=$HEAD_SHA&per_page=1" \
    --jq '[(.total_count | tostring), (.workflow_runs[0].status // "none"), (.workflow_runs[0].conclusion // "none"), (.workflow_runs[0].html_url // "none")] | join(" ")'
}

NO_RUN_SINCE=""

while true; do
  if image_exists_in "$UPSTREAM_OWNER"; then
    set_found "$UPSTREAM_OWNER"
    exit 0
  fi
  if image_exists_in "$HEAD_OWNER"; then
    set_found "$HEAD_OWNER"
    exit 0
  fi
  if [ "$IS_FORK_PR" != "true" ]; then
    echo "image_exists=false" >> "$OUTPUT_FILE"
    echo "owner=$HEAD_OWNER" >> "$OUTPUT_FILE"
    exit 0
  fi

  FORK_CI_RUN=$(fork_ci_run || echo "unknown none none none")
  read -r TOTAL STATUS CONCLUSION RUN_URL <<< "$FORK_CI_RUN"
  NOW=$(date +%s)

  # No usable answer from the API (empty or non-numeric) is treated like "no run", so it is bounded by the grace period too
  if ! [[ "$TOTAL" =~ ^[0-9]+$ ]] || [ "$TOTAL" = "0" ]; then
    if [ "$NO_RUN_SINCE" = "" ]; then
      NO_RUN_SINCE=$NOW
    fi
    if [ $(( NOW - NO_RUN_SINCE )) -ge "$FORK_CI_RUN_GRACE_SECONDS" ]; then
      echo "::error::Image $TAG not found in ghcr.io/$UPSTREAM_OWNER or ghcr.io/$HEAD_OWNER, and no CI run was found on the fork for commit $HEAD_SHA ($HEAD_REPO). Is CI enabled on the fork, was the commit pushed with a skip keyword, or could the GitHub API not be queried?"
      exit 1
    fi
    echo "Image $TAG not found yet, no CI run on the fork for commit $HEAD_SHA yet, retrying in ${FORK_IMAGE_POLL_INTERVAL_SECONDS}s..."
    sleep "$FORK_IMAGE_POLL_INTERVAL_SECONDS"
    continue
  fi

  if [ "$STATUS" = "completed" ]; then
    if [ "$CONCLUSION" = "success" ]; then
      echo "::error::Image $TAG not found in ghcr.io/$UPSTREAM_OWNER or ghcr.io/$HEAD_OWNER. Did CI of the fork properly push it? $RUN_URL"
    else
      echo "::error::Image $TAG not found in ghcr.io/$UPSTREAM_OWNER or ghcr.io/$HEAD_OWNER. Fork's CI failed ($CONCLUSION), so it probably failed to push it: $RUN_URL"
    fi
    exit 1
  fi

  echo "Image $TAG not found yet, fork's CI is $STATUS ($RUN_URL), retrying in ${FORK_IMAGE_POLL_INTERVAL_SECONDS}s..."
  sleep "$FORK_IMAGE_POLL_INTERVAL_SECONDS"
done
