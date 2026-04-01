using DriverGuardian.ProviderAdapters.Abstractions.Contracts;

namespace DriverGuardian.Application.Abstractions;

public interface IProviderRegistry
{
    IReadOnlyCollection<IOfficialDriverProviderAdapter> GetAll();
}
