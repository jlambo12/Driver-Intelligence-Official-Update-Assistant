namespace DriverGuardian.ProviderAdapters.Abstractions.Contracts;

public interface IOemPriorityStrategy
{
    IReadOnlyList<string> GetProviderPriorityOrder(string? systemManufacturer);
}
