#!/bin/sh
# Updates the stack on this Mac from images already in GHCR.
# Expects GHCR_USERNAME, GHCR_TOKEN, and GHCR_OWNER.
# IMAGE_TAG is optional; otherwise the checkout SHA is used.
# Secrets stay in $HOME/order-poc/.env. This script only rewrites the image lines.

set -eu

export PATH="/opt/homebrew/bin:/usr/local/bin:${PATH:-}"

root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
dest=${LOCAL_DEPLOY_PATH:-$HOME/order-poc}
user=${GHCR_USERNAME:?GHCR_USERNAME is required}
token=${GHCR_TOKEN:?GHCR_TOKEN is required}
owner=$(printf '%s' "${GHCR_OWNER:?GHCR_OWNER is required}" | tr '[:upper:]' '[:lower:]')
tag=${IMAGE_TAG:-}
if [ -z "$tag" ]; then
  tag="sha-$(git -C "$root" rev-parse HEAD | cut -c1-7)"
fi

python3 - "$tag" <<'PY'
import re
import sys
tag = sys.argv[1]
if not re.fullmatch(r"[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}", tag):
    sys.exit("image tag is empty or contains unsupported characters")
PY

if ! colima status >/dev/null 2>&1; then
  echo "Starting Colima"
  colima start
fi

socket="$HOME/.colima/default/docker.sock"
i=0
while [ ! -S "$socket" ]; do
  i=$((i + 1))
  if [ "$i" -gt 60 ]; then
    echo "Colima did not open $socket" >&2
    exit 1
  fi
  sleep 2
done
export DOCKER_HOST="unix://$socket"

mkdir -p "$dest/docker/postgres/init"
cp "$root/docker-compose.prod.yml" "$dest/docker-compose.prod.yml"
cp "$root/deploy/up.sh" "$dest/up.sh"
cp "$root/docker/postgres/init/create-shipments.sql" "$dest/docker/postgres/init/create-shipments.sql"
chmod 700 "$dest/up.sh"

if [ ! -f "$dest/.env" ]; then
  echo "Missing $dest/.env. Create it once; this deploy only updates the image tags." >&2
  exit 1
fi

ENV_FILE="$dest/.env" IMAGE_TAG="$tag" IMAGE_OWNER="$owner" python3 - <<'PY'
import os
from pathlib import Path

path = Path(os.environ["ENV_FILE"])
tag = os.environ["IMAGE_TAG"]
owner = os.environ["IMAGE_OWNER"]
order = f"ORDER_IMAGE=ghcr.io/{owner}/order-poc-order:{tag}"
shipment = f"SHIPMENT_IMAGE=ghcr.io/{owner}/order-poc-shipment:{tag}"
seen_order = False
seen_shipment = False
lines = []
for line in path.read_text(encoding="utf-8").splitlines():
    if line.startswith("ORDER_IMAGE="):
        lines.append(order)
        seen_order = True
    elif line.startswith("SHIPMENT_IMAGE="):
        lines.append(shipment)
        seen_shipment = True
    else:
        lines.append(line)
if not seen_order:
    lines.append(order)
if not seen_shipment:
    lines.append(shipment)
path.write_text("\n".join(lines) + "\n", encoding="utf-8")
os.chmod(path, 0o600)
PY

token_file="$dest/.ghcr_token"
umask 077
printf '%s' "$token" > "$token_file"
chmod 600 "$token_file"

cd "$dest"
GHCR_USERNAME="$user" GHCR_TOKEN_FILE="$token_file" sh ./up.sh
