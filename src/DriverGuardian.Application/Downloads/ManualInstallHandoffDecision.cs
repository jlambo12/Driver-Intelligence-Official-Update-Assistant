using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.Downloads;

public enum HandoffReadinessOutcome
{
    ReadyForManualInstallHandoff = 0,
    UserActionRequired = 1,
    InsufficientEvidence = 2,
    MissingOfficialPackageReference = 3,
    NonOfficialSource = 4
}

public enum HandoffBlockReason
{
    MissingOfficialPackageUrl = 0,
    PackageUrlIsNotHttps = 1,
    SourceTrustUnverified = 2,
    SourceMarkedNonOfficial = 3,
    PackageUrlHostMismatch = 4
}

public sealed record UserActionRequiredReason(
    HandoffBlockReason Reason,
    string Message);

public sealed record OfficialPackageReference(
    string ProviderCode,
    string DriverIdentifier,
    string? CandidateVersion,
    Uri PackageUri,
    SourceEvidence SourceEvidence);

public sealed record ManualInstallHandoffDecision(
    HandoffReadinessOutcome Outcome,
    OfficialPackageReference? PackageReference,
    IReadOnlyCollection<UserActionRequiredReason> Reasons)
{
    public bool IsHandoffReady => Outcome == HandoffReadinessOutcome.ReadyForManualInstallHandoff;
}
