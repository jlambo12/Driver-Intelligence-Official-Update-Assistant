using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.Downloads;

public sealed record DownloadPreparationRequest(
    string ProviderCode,
    ProviderCandidate Candidate);
