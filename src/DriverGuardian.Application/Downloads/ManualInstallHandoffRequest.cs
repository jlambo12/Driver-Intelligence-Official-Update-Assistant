using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.Downloads;

public sealed record ManualInstallHandoffRequest(
    string ProviderCode,
    ProviderCandidate Candidate);
