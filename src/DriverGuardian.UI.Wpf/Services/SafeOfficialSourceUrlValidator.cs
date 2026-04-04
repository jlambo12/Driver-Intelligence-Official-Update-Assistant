namespace DriverGuardian.UI.Wpf.Services;

public static class SafeOfficialSourceUrlValidator
{
    public static bool IsSafeOfficialSourceUrl(string? value)
        => TryGetSafeHttpsUri(value, out _);

    public static bool TryGetSafeHttpsUri(string? value, out Uri? uri)
    {
        uri = null;

        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (!parsed.IsAbsoluteUri ||
            !string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(parsed.Host) ||
            parsed.IsLoopback ||
            !string.IsNullOrEmpty(parsed.UserInfo))
        {
            return false;
        }

        uri = parsed;
        return true;
    }
}
