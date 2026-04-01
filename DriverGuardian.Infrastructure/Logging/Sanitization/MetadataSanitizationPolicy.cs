namespace DriverGuardian.Infrastructure.Logging.Sanitization;

public sealed record MetadataSanitizationPolicy(
    IReadOnlySet<string> BlockedKeyTokens,
    int MaxValueLength,
    int MaxItems)
{
    public static MetadataSanitizationPolicy Default { get; } = new(
        BlockedKeyTokens: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "token", "secret", "apikey", "authorization", "cookie"
        },
        MaxValueLength: 256,
        MaxItems: 30);
}
