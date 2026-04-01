using DriverGuardian.ProviderAdapters.Abstractions.Models;

namespace DriverGuardian.ProviderAdapters.Abstractions.Registry;

public interface IProviderRegistry
{
    IReadOnlyCollection<ProviderDescriptor> GetOfficialProviders();

    bool TryGetOfficialProvider(string code, out ProviderDescriptor? descriptor);
}
