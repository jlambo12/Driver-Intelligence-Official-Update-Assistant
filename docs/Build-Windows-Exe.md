# Сборка исполняемого `.exe` для Windows

Если после обычной сборки (`dotnet build`) вы не видите удобный файл для запуска, используйте **publish**.

## Предварительные требования

1. Установлен .NET SDK 8.x.
2. Команда `dotnet` доступна в `PATH`.

Проверка:

```bash
dotnet --info
```

Если получаете ошибку `dotnet: command not found`, сначала установите .NET SDK, а затем повторите команды сборки/тестов.

## Вариант 1 (рекомендуется): готовый скрипт

В репозитории добавлены скрипты:
- `scripts/build-windows-exe.cmd`
- `scripts/build-windows-exe.ps1`

### Через `cmd`
```bat
scripts\build-windows-exe.cmd
```

### Через PowerShell
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-windows-exe.ps1
```

После успешной публикации получите файл:

```text
artifacts\win-x64\DriverGuardian.UI.Wpf.exe
```

## Вариант 2: ручная команда

```bash
dotnet publish src/DriverGuardian.UI.Wpf/DriverGuardian.UI.Wpf.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishReadyToRun=true \
  -o artifacts/win-x64
```

Итоговый файл для запуска:

```text
artifacts/win-x64/DriverGuardian.UI.Wpf.exe
```
