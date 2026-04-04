using DriverGuardian.UI.Wpf.Services;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class SafeOfficialSourceUrlValidatorTests
{
    [Fact]
    public void IsSafeOfficialSourceUrl_ReturnsFalse_ForPrivateIpHost()
    {
        var result = SafeOfficialSourceUrlValidator.IsSafeOfficialSourceUrl("https://10.0.0.12/drivers");

        Assert.False(result);
    }

    [Fact]
    public void IsSafeOfficialSourceUrl_ReturnsFalse_ForLoopbackHost()
    {
        var result = SafeOfficialSourceUrlValidator.IsSafeOfficialSourceUrl("https://localhost/drivers");

        Assert.False(result);
    }

    [Fact]
    public void IsSafeOfficialSourceUrl_ReturnsTrue_ForPublicHttpsHost()
    {
        var result = SafeOfficialSourceUrlValidator.IsSafeOfficialSourceUrl("https://support.lenovo.com/us/en");

        Assert.True(result);
    }

    [Fact]
    public void IsSafeOfficialSourceUrl_ReturnsFalse_ForUntrustedPublicHost()
    {
        var result = SafeOfficialSourceUrlValidator.IsSafeOfficialSourceUrl("https://example.test/drivers");

        Assert.False(result);
    }
}
