using Microsoft.Win32;

namespace DriverGuardian.UI.Wpf.Services;

public interface IReportFileSaveService
{
    bool TrySave(string defaultFileName, string extension, string filter, string content);
}

public sealed class ReportFileSaveService : IReportFileSaveService
{
    public bool TrySave(string defaultFileName, string extension, string filter, string content)
    {
        var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            DefaultExt = extension,
            Filter = filter,
            AddExtension = true,
            OverwritePrompt = true,
            CheckPathExists = true
        };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        File.WriteAllText(dialog.FileName, content);
        return true;
    }
}
