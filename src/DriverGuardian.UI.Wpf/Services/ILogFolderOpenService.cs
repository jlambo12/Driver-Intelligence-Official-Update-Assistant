namespace DriverGuardian.UI.Wpf.Services;

public interface ILogFolderOpenService
{
    Task<LogFolderOpenResult> OpenAsync(CancellationToken cancellationToken);
}

public enum LogFolderOpenResult
{
    Opened = 0,
    Failed = 1
}
