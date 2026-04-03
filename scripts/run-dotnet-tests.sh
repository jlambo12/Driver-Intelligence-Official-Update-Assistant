#!/usr/bin/env bash
set -euo pipefail

SOLUTION_PATH="${1:-DriverGuardian.sln}"

if ! command -v dotnet >/dev/null 2>&1; then
  cat <<'MSG' >&2
[DriverGuardian] .NET SDK is not installed or not available in PATH.

To run build/tests locally, install .NET 8 SDK:
  https://dotnet.microsoft.com/download/dotnet/8.0

After installation, verify:
  dotnet --info

Then run:
  dotnet test DriverGuardian.sln
MSG
  exit 127
fi

echo "[DriverGuardian] dotnet executable: $(command -v dotnet)"
dotnet --version

echo "[DriverGuardian] Restoring: ${SOLUTION_PATH}"
dotnet restore "${SOLUTION_PATH}" --runtime win-x64

echo "[DriverGuardian] Running tests: ${SOLUTION_PATH}"
dotnet test "${SOLUTION_PATH}" --no-restore
