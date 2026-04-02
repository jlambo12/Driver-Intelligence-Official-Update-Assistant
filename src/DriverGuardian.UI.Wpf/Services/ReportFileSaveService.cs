using System.IO;
using Microsoft.Win32;

namespace DriverGuardian.UI.Wpf.Services;

public enum ReportFileSaveResult
{
    CanceledByUser,
    Saved,
    FailedToWrite
}

public interface IReportFileSaveService
{
    ReportFileSaveResult Save(string defaultFileName, string extension, string filter, string content);
}

public sealed class ReportFileSaveService : IReportFileSaveService
{
    public ReportFileSaveResult Save(string defaultFileName, string extension, string filter, string content)
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
            return ReportFileSaveResult.CanceledByUser;
        }

        try
        {
            File.WriteAllText(dialog.FileName, content);
            return ReportFileSaveResult.Saved;
        }
        catch (IOException)
        {
            return ReportFileSaveResult.FailedToWrite;
        }
        catch (UnauthorizedAccessException)
        {
            return ReportFileSaveResult.FailedToWrite;
        }
    }
}
