#!/usr/bin/env bash

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"
cd "$DIR/.."

if ! command -v "doxygen" &> /dev/null; then
  echo "doxygen not found"
  exit 1
fi

rm -rf ./build
mkdir -p ./build

echo "Building SOS SDK docs"
doxygen ./Doxyfile
