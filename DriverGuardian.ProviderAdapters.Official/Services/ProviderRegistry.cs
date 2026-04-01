using DriverGuardian.Application.Abstractions;
using DriverGuardian.ProviderAdapters.Abstractions.Contracts;

namespace DriverGuardian.ProviderAdapters.Official.Services;

public sealed class ProviderRegistry(IEnumerable<IOfficialDriverProviderAdapter> providers) : IProviderRegistry
{
    private readonly IReadOnlyCollection<IOfficialDriverProviderAdapter> _providers = providers.ToArray();
    public IReadOnlyCollection<IOfficialDriverProviderAdapter> GetAll() => _providers;
}
