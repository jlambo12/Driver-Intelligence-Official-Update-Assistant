using System.IO;
using System.Diagnostics;

namespace DriverGuardian.UI.Wpf.Services;

public interface IDiagnosticLogsFolderService
{
    string ResolveEffectiveFolderPath(string? customFolderPath);
    bool OpenFolder(string folderPath);
}

public sealed class DiagnosticLogsFolderService(string defaultFolderPath) : IDiagnosticLogsFolderService
{
    public string ResolveEffectiveFolderPath(string? customFolderPath)
        => string.IsNullOrWhiteSpace(customFolderPath)
            ? defaultFolderPath
            : customFolderPath.Trim();

    public bool OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return false;
        }

        var effectivePath = folderPath.Trim();
        Directory.CreateDirectory(effectivePath);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = effectivePath,
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }
}
