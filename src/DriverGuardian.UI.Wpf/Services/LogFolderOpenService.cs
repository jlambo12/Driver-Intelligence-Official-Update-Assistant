using System.Diagnostics;
using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.UI.Wpf.Services;

public sealed class LogFolderOpenService(ILogFolderResolver logFolderResolver) : ILogFolderOpenService
{
    public async Task<LogFolderOpenResult> OpenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var folder = await logFolderResolver.GetEffectiveLogFolderAsync(cancellationToken);
            Directory.CreateDirectory(folder);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = folder,
                UseShellExecute = true
            });

            return LogFolderOpenResult.Opened;
        }
        catch
        {
            return LogFolderOpenResult.Failed;
        }
    }
}
