using DriverGuardian.Application.Logging.Abstractions;
using DriverGuardian.Application.Logging.Models;

namespace DriverGuardian.Infrastructure.Logging.Sanitization;

public sealed class DefaultMetadataSanitizer : IMetadataSanitizer
{
    private static readonly string[] SensitiveTokens = ["password", "token", "secret", "apikey", "authorization"];
    private const int MaxValueLength = 256;

    public SafeLogMetadata Sanitize(SafeLogMetadata? metadata)
    {
        if (metadata is null || metadata.Values.Count == 0)
            return SafeLogMetadata.Empty;

        var sanitized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in metadata.Values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                continue;

            var key = pair.Key.Trim();
            if (SensitiveTokens.Any(token => key.Contains(token, StringComparison.OrdinalIgnoreCase)))
                continue;

            var value = pair.Value ?? string.Empty;
            if (value.Length > MaxValueLength)
                value = value[..MaxValueLength];

            sanitized[key] = value;
        }

        return new SafeLogMetadata(sanitized);
    }
}
