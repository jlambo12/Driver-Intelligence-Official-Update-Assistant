using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Application.Logging.Abstractions;

public interface IMetadataSanitizer
{
    SafeLogMetadata Sanitize(SafeLogMetadata? metadata);
}
