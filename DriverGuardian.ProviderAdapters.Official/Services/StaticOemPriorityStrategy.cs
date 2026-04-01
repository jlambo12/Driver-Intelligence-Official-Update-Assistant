using DriverGuardian.ProviderAdapters.Abstractions.Contracts;

namespace DriverGuardian.ProviderAdapters.Official.Services;

public sealed class StaticOemPriorityStrategy : IOemPriorityStrategy
{
    public IReadOnlyList<string> GetProviderPriorityOrder(string? systemManufacturer)
        => ["oem", "chipset-vendor", "device-manufacturer"];
}
