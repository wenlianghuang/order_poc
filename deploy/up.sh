#!/bin/sh
set -eu

cd "$(dirname "$0")"

token_file="${GHCR_TOKEN_FILE:-.ghcr_token}"
user="${GHCR_USERNAME:?GHCR_USERNAME is required}"

if [ ! -f "$token_file" ]; then
  echo "Missing GHCR token file" >&2
  exit 1
fi

cleanup() {
  rm -f "$token_file"
}
trap cleanup EXIT

if docker compose version >/dev/null 2>&1; then
  compose() { docker compose "$@"; }
elif command -v docker-compose >/dev/null 2>&1; then
  compose() { docker-compose "$@"; }
else
  echo "docker compose is required" >&2
  exit 1
fi

docker login ghcr.io -u "$user" --password-stdin < "$token_file"
rm -f "$token_file"
trap - EXIT

status=0
compose --env-file .env -f docker-compose.prod.yml pull
compose --env-file .env -f docker-compose.prod.yml up -d --remove-orphans || status=$?
docker logout ghcr.io || true
compose --env-file .env -f docker-compose.prod.yml ps
exit "$status"
