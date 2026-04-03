using System.Diagnostics;

namespace DriverGuardian.UI.Wpf.Services;

public interface IOfficialSourceLauncher
{
    bool Open(Uri uri);
}

public sealed class OfficialSourceLauncher : IOfficialSourceLauncher
{
    public bool Open(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
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
