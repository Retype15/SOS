#!/usr/bin/env bash

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"
cd "$DIR/.."

./cs/scripts/build.sh || exit $?

if ! command -v "python3" &> /dev/null; then
  echo "python3 not found"
  exit 1
fi

echo "Preview at http://127.0.0.1:8000"
python3 ./scripts/http_server.py ./cs/build --port 8000 --route /:html
