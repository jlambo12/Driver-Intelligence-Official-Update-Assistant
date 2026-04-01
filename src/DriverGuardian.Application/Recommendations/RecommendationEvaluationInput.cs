using DriverGuardian.Domain.Drivers;
using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.Recommendations;

public enum ProviderPrecedence
{
    OfficialFirst = 0,
    OemFirst = 1,
    Neutral = 2
}

public sealed record RecommendationCandidateInput(
    string ProviderCode,
    ProviderCandidate Candidate);

public sealed record RecommendationEvaluationInput(
    InstalledDriverSnapshot InstalledDriver,
    IReadOnlyCollection<RecommendationCandidateInput> Candidates,
    ProviderPrecedence ProviderPrecedence);
