using System.Net;

namespace DriverGuardian.Application.OfficialSources;

public static class OfficialSourceHostSafetyPolicy
{
    private static readonly IReadOnlyCollection<string> TrustedHostSuffixes =
    [
        "microsoft.com",
        "update.microsoft.com",
        "catalog.update.microsoft.com",
        "dell.com",
        "hp.com",
        "lenovo.com",
        "asus.com",
        "acer.com",
        "msi.com"
    ];

    public static bool IsAllowed(Uri uri, out OpenOfficialSourceBlockedReason? blockedReason)
    {
        blockedReason = null;
        if (IsLocalOrIpHost(uri))
        {
            blockedReason = OpenOfficialSourceBlockedReason.UrlHostIsLocalOrIp;
            return false;
        }

        if (!IsTrustedHost(uri.Host))
        {
            blockedReason = OpenOfficialSourceBlockedReason.UrlHostNotTrusted;
            return false;
        }

        return true;
    }

    private static bool IsTrustedHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        return TrustedHostSuffixes.Any(suffix =>
            host.Equals(suffix, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith($".{suffix}", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLocalOrIpHost(Uri uri)
    {
        if (uri.IsLoopback)
        {
            return true;
        }

        if (!IPAddress.TryParse(uri.Host, out var address))
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254)
                || bytes[0] == 127;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6Multicast
                || address.Equals(IPAddress.IPv6Loopback);
        }

        return false;
    }
}
