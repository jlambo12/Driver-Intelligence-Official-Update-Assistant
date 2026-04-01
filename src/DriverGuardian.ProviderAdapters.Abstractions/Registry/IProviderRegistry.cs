using DriverGuardian.ProviderAdapters.Abstractions.Models;

namespace DriverGuardian.ProviderAdapters.Abstractions.Registry;

public interface IProviderRegistry
{
    IReadOnlyCollection<ProviderDescriptor> GetProviders();
}
