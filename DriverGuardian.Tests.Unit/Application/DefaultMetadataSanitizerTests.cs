using DriverGuardian.Application.Logging.Models;
using DriverGuardian.Infrastructure.Logging.Sanitization;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class DefaultMetadataSanitizerTests
{
    [Fact]
    public void Sanitize_Should_RemoveSensitiveKeys()
    {
        var sanitizer = new DefaultMetadataSanitizer();
        var metadata = new SafeLogMetadata(new Dictionary<string, string>
        {
            ["device"] = "ok",
            ["authToken"] = "secret"
        });

        var result = sanitizer.Sanitize(metadata);

        Assert.True(result.Values.ContainsKey("device"));
        Assert.False(result.Values.ContainsKey("authToken"));
    }
}
