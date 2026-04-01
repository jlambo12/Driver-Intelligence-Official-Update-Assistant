using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Infrastructure.Logging.Sanitization;

public sealed class DefaultMetadataSanitizer(MetadataSanitizationPolicy? policy = null) : IMetadataSanitizer
{
    private readonly MetadataSanitizationPolicy _policy = policy ?? MetadataSanitizationPolicy.Default;

    public SafeLogMetadata Sanitize(SafeLogMetadata? metadata)
    {
        if (metadata is null || metadata.Values.Count == 0)
            return SafeLogMetadata.Empty;

        var sanitized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in metadata.Values)
        {
            if (sanitized.Count >= _policy.MaxItems)
                break;

            if (string.IsNullOrWhiteSpace(pair.Key))
                continue;

            var key = pair.Key.Trim();
            if (_policy.BlockedKeyTokens.Any(token => key.Contains(token, StringComparison.OrdinalIgnoreCase)))
                continue;

            var value = pair.Value ?? string.Empty;
            if (value.Length > _policy.MaxValueLength)
                value = value[.._policy.MaxValueLength];

            sanitized[key] = value;
        }

        return new SafeLogMetadata(sanitized);
    }
}
