namespace DriverGuardian.Application.OfficialSources;

public enum OfficialSourceResolutionOutcome
{
    InsufficientEvidence = 0,
    DirectOfficialDriverPageConfirmed = 1,
    VendorSupportPageConfirmed = 2
}

public enum OpenOfficialSourceActionOutcome
{
    Allowed = 0,
    Blocked = 1,
    MissingUrl = 2,
    NonOfficialSource = 3,
    InsufficientEvidence = 4
}

public enum OpenOfficialSourceBlockedReason
{
    MissingOfficialSourceUrl = 0,
    UrlIsNotHttps = 1,
    SourceTrustUnverified = 2,
    SourceMarkedNonOfficial = 3,
    UrlHostMismatch = 4,
    ResolutionNotConfirmed = 5
}

public sealed record OpenOfficialSourceBlocker(
    OpenOfficialSourceBlockedReason Reason,
    string Message);

public sealed record ApprovedOfficialSourceLink(
    string ProviderCode,
    string DriverIdentifier,
    Uri OfficialSourceUri);

public sealed record OpenOfficialSourceActionDecision(
    OpenOfficialSourceActionOutcome Outcome,
    OfficialSourceResolutionOutcome ResolutionOutcome,
    ApprovedOfficialSourceLink? Link,
    IReadOnlyCollection<OpenOfficialSourceBlocker> Blockers)
{
    public bool IsAllowed => Outcome == OpenOfficialSourceActionOutcome.Allowed;
}
