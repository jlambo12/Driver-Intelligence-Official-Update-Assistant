using DriverGuardian.Application.Logging.Enums;
using DriverGuardian.Infrastructure.Logging.ErrorHandling;
using DriverGuardian.Infrastructure.Logging.Sanitization;
using DriverGuardian.Infrastructure.Time;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class DefaultErrorNormalizerTests
{
    [Fact]
    public void Normalize_Should_ReturnValidationCode_ForArgumentException()
    {
        var sut = new DefaultErrorNormalizer(new SystemClock(), new DefaultMetadataSanitizer());

        var result = sut.Normalize(new ArgumentException("bad"), "TestSource", null);

        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Equal(ErrorCategory.Validation, result.Category);
    }
}
