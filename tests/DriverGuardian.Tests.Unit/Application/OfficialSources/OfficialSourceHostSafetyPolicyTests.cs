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

    [Theory]
    [InlineData("https://support.lenovo.cn/drivers")]
    [InlineData("https://www.dell.co.jp/support")]
    [InlineData("https://consumer.huawei.com/support")]
    [InlineData("https://www.samsung.com/support")]
    public void IsAllowed_ReturnsTrue_ForRegionalOfficialDomains(string url)
    {
        var allowed = OfficialSourceHostSafetyPolicy.IsAllowed(new Uri(url), out var blockedReason);

        Assert.True(allowed);
        Assert.Null(blockedReason);
    }
}
