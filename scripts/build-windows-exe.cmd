@echo off
setlocal

set CONFIGURATION=Release
set RUNTIME=win-x64
set OUTPUT=artifacts\win-x64

if not "%~1"=="" set CONFIGURATION=%~1
if not "%~2"=="" set RUNTIME=%~2
if not "%~3"=="" set OUTPUT=%~3

echo Publishing DriverGuardian as %RUNTIME% (%CONFIGURATION%)...

dotnet publish src\DriverGuardian.UI.Wpf\DriverGuardian.UI.Wpf.csproj ^
  -c %CONFIGURATION% ^
  -r %RUNTIME% ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:PublishReadyToRun=true ^
  -o %OUTPUT%

if errorlevel 1 (
  echo Build failed.
  exit /b 1
)

if not exist "%OUTPUT%\DriverGuardian.UI.Wpf.exe" (
  echo EXE not found at "%OUTPUT%\DriverGuardian.UI.Wpf.exe"
  exit /b 1
)

echo Done. EXE path: %OUTPUT%\DriverGuardian.UI.Wpf.exe
endlocal
