#!/usr/bin/env bash
set -euo pipefail

SOLUTION_PATH="${1:-DriverGuardian.sln}"

resolve_dotnet() {
  if command -v dotnet >/dev/null 2>&1; then
    command -v dotnet
    return 0
  fi

  if [[ -n "${DOTNET_ROOT:-}" && -x "${DOTNET_ROOT}/dotnet" ]]; then
    echo "${DOTNET_ROOT}/dotnet"
    return 0
  fi

  if [[ -x "${HOME}/.dotnet/dotnet" ]]; then
    echo "${HOME}/.dotnet/dotnet"
    return 0
  fi

  return 1
}

DOTNET_BIN="$(resolve_dotnet || true)"

if [[ -z "${DOTNET_BIN}" ]]; then
  cat <<'MSG' >&2
[DriverGuardian] .NET SDK is not installed or not available in PATH.

To run build/tests locally, install .NET 8 SDK:
  https://dotnet.microsoft.com/download/dotnet/8.0

Or install to ~/.dotnet (without sudo):
  curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --install-dir "$HOME/.dotnet"

After installation, verify:
  dotnet --info
  # or
  ~/.dotnet/dotnet --info

Then run:
  ./scripts/run-dotnet-tests.sh
MSG
  exit 127
fi

echo "[DriverGuardian] dotnet executable: ${DOTNET_BIN}"
"${DOTNET_BIN}" --version

echo "[DriverGuardian] Restoring: ${SOLUTION_PATH}"
"${DOTNET_BIN}" restore "${SOLUTION_PATH}" --runtime win-x64

echo "[DriverGuardian] Running tests: ${SOLUTION_PATH}"
"${DOTNET_BIN}" test "${SOLUTION_PATH}" --no-restore
