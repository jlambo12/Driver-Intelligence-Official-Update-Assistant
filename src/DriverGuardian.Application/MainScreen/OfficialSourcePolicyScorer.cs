using DriverGuardian.ProviderAdapters.Abstractions.Lookup;

namespace DriverGuardian.Application.MainScreen;

internal static class OfficialSourcePolicyScorer
{
    public static int Calculate(SourceTrustLevel trustLevel, CompatibilityConfidence compatibility)
    {
        var trustScore = trustLevel switch
        {
            SourceTrustLevel.OfficialPublisherSite => 300,
            SourceTrustLevel.OemSupportPortal => 200,
            SourceTrustLevel.OperatingSystemCatalog => 150,
            _ => 0
        };

        var compatibilityScore = compatibility switch
        {
            CompatibilityConfidence.High => 40,
            CompatibilityConfidence.Medium => 5,
            CompatibilityConfidence.Low => 0,
            _ => 0
        };

        return trustScore + compatibilityScore;
    }
}
