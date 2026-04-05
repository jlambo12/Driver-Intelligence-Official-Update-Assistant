using System.Net;

namespace DriverGuardian.Application.OfficialSources;

public static class OfficialSourceHostSafetyPolicy
{
    private static readonly IReadOnlyCollection<string> TrustedHostSuffixes =
    [
        // Microsoft ecosystem.
        "microsoft.com",
        "update.microsoft.com",
        "catalog.update.microsoft.com",

        // OEM / PC vendors (global + regional domains).
        "dell.com",
        "dell.co.jp",
        "dell.com.cn",
        "hp.com",
        "hp.cn",
        "lenovo.com",
        "lenovo.cn",
        "asus.com",
        "asus.com.cn",
        "asus.co.jp",
        "acer.com",
        "acer.com.cn",
        "msi.com",
        "samsung.com",
        "samsungcn.com",
        "sony.com",
        "sony.jp",
        "fujitsu.com",
        "fujitsu.com.cn",
        "dynabook.com",
        "dynabook.co.jp",
        "huawei.com",
        "consumer.huawei.com",
        "lg.com",
        "gigabyte.com",
        "aorus.com",

        // Chipset / component manufacturers often used for driver redirects.
        "intel.com",
        "amd.com",
        "nvidia.com",
        "realtek.com",
        "qualcomm.com",
        "broadcom.com",
        "mediatek.com"
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
