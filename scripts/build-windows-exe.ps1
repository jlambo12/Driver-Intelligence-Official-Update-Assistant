param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "artifacts/win-x64"
)

$ErrorActionPreference = "Stop"

$project = "src/DriverGuardian.UI.Wpf/DriverGuardian.UI.Wpf.csproj"

Write-Host "Publishing $project as a single-file executable..."

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=true `
    -o $Output

$exePath = Join-Path $Output "DriverGuardian.UI.Wpf.exe"

if (-not (Test-Path $exePath)) {
    throw "EXE was not produced. Expected: $exePath"
}

Write-Host "Done. EXE path: $exePath"
