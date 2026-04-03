# Local development and test run guide

## Prerequisites

The solution targets **.NET 8** and includes Windows-specific projects (`net8.0-windows10.0.19041.0`).

1. Install the .NET 8 SDK:
   - https://dotnet.microsoft.com/download/dotnet/8.0
2. Verify tool availability:
   ```bash
   dotnet --info
   ```

> If you see `dotnet: command not found`, the SDK is not installed or not in `PATH`.

## Run unit tests

Preferred command:

```bash
./scripts/run-dotnet-tests.sh
```

Direct command:

```bash
dotnet test DriverGuardian.sln
```

## Why build/test can fail in clean environments

In minimal containers and Linux-based CI workers without .NET preinstalled, the build fails immediately with:

```text
/bin/bash: dotnet: command not found
```

The repository now includes:

- `global.json` to pin the SDK feature band and improve reproducibility.
- `scripts/run-dotnet-tests.sh` for preflight checks and actionable diagnostics.
