namespace DriverGuardian.Application.MainScreen;

internal static class OfficialSourcePolicyCandidateSelector
{
    public static OfficialSourcePolicyCandidate? SelectBest(IReadOnlyCollection<OfficialSourcePolicyCandidate> candidates)
    {
        return candidates
            .OrderByDescending(candidate => candidate.PolicyScore)
            .ThenBy(candidate => candidate.ProviderCode, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }
}
