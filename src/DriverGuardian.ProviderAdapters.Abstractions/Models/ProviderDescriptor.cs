namespace DriverGuardian.ProviderAdapters.Abstractions.Models;

public enum ProviderPrecedence
{
    PrimaryOem = 0,
    SecondaryOem = 1,
    PlatformVendor = 2,
    Fallback = 3
}

public sealed record ProviderDescriptor(
    string Code,
    string DisplayName,
    bool IsEnabled,
    bool OfficialSourceOnly,
    ProviderPrecedence Precedence);
