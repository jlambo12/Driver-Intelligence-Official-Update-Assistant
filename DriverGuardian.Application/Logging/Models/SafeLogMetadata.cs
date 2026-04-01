namespace DriverGuardian.Application.Logging.Models;

public sealed record SafeLogMetadata(IReadOnlyDictionary<string, string> Values)
{
    public static SafeLogMetadata Empty { get; } = new(new Dictionary<string, string>());
}
