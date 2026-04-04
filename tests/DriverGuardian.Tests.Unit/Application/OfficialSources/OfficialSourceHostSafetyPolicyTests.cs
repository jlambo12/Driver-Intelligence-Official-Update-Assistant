using DriverGuardian.Application.OfficialSources;

namespace DriverGuardian.Tests.Unit.Application.OfficialSources;

public sealed class OfficialSourceHostSafetyPolicyTests
{
    [Fact]
    public void IsAllowed_ReturnsTrue_ForTrustedSubdomain()
    {
        var allowed = OfficialSourceHostSafetyPolicy.IsAllowed(
            new Uri("https://downloads.dell.com/driver"),
            out var blockedReason);

        Assert.True(allowed);
        Assert.Null(blockedReason);
    }

    [Fact]
    public void IsAllowed_ReturnsFalse_ForUntrustedPublicHost()
    {
        var allowed = OfficialSourceHostSafetyPolicy.IsAllowed(
            new Uri("https://example.test/driver"),
            out var blockedReason);

        Assert.False(allowed);
        Assert.Equal(OpenOfficialSourceBlockedReason.UrlHostNotTrusted, blockedReason);
    }
}
