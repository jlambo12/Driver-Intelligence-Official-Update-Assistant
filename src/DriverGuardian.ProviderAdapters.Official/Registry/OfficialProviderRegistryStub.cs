using DriverGuardian.ProviderAdapters.Abstractions.Models;
using DriverGuardian.ProviderAdapters.Abstractions.Providers;
using DriverGuardian.ProviderAdapters.Abstractions.Registry;

namespace DriverGuardian.ProviderAdapters.Official.Registry;

public sealed class OfficialProviderRegistryStub : IProviderRegistry
{
    private readonly IReadOnlyCollection<ProviderDescriptor> _providers;

    public OfficialProviderRegistryStub()
        : this([new OfficialProviderAdapterStub()])
    {
    }

    public OfficialProviderRegistryStub(IEnumerable<IOfficialProviderAdapter> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var ordered = providers
            .Select(x => x.Descriptor)
            .OrderBy(x => x.Precedence)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .ToArray();

        var duplicateCode = ordered
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1)?.Key;

        if (duplicateCode is not null)
        {
            throw new ArgumentException($"Duplicate provider code registration: {duplicateCode}", nameof(providers));
        }

        _providers = ordered;
    }

    public IReadOnlyCollection<ProviderDescriptor> GetOfficialProviders() => _providers;

    public bool TryGetOfficialProvider(string code, out ProviderDescriptor? descriptor)
    {
        descriptor = _providers.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        return descriptor is not null;
    }
}
