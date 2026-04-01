namespace DriverGuardian.Application.Abstractions;

public interface IProviderCatalogSummaryService
{
    Task<int> GetProviderCountAsync(CancellationToken cancellationToken);
}
