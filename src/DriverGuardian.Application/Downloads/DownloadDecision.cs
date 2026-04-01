using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.Downloads;

public enum DownloadDecisionOutcome
{
    Allowed = 0,
    Blocked = 1,
    InsufficientEvidence = 2,
    MissingUrl = 3,
    NonOfficialSource = 4
}

public enum BlockedDownloadReason
{
    MissingDownloadUrl = 0,
    DownloadUrlIsNotHttps = 1,
    SourceTrustUnverified = 2,
    SourceMarkedNonOfficial = 3,
    DownloadUrlHostMismatch = 4
}

public sealed record DownloadDecisionReason(
    BlockedDownloadReason Reason,
    string Message);

public sealed record DownloadCandidate(
    string ProviderCode,
    string DriverIdentifier,
    string? CandidateVersion,
    Uri DownloadUri,
    SourceEvidence SourceEvidence);

public sealed record DownloadDecision(
    DownloadDecisionOutcome Outcome,
    DownloadCandidate? Candidate,
    IReadOnlyCollection<DownloadDecisionReason> Reasons)
{
    public bool CanPrepareDownload => Outcome == DownloadDecisionOutcome.Allowed;
}
