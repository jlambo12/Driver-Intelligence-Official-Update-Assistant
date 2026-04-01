using System.IO;
using Microsoft.Win32;

namespace DriverGuardian.UI.Wpf.Services;

public enum ReportFileSaveOutcome
{
    Saved,
    Canceled,
    Failed
}

public interface IReportFileSaveService
{
    ReportFileSaveOutcome Save(string defaultFileName, string extension, string filter, string content);
}

public sealed class ReportFileSaveService : IReportFileSaveService
{
    public ReportFileSaveOutcome Save(string defaultFileName, string extension, string filter, string content)
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
            return ReportFileSaveOutcome.Canceled;
        }

        try
        {
            File.WriteAllText(dialog.FileName, content);
            return ReportFileSaveOutcome.Saved;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return ReportFileSaveOutcome.Failed;
        }
    }
}
