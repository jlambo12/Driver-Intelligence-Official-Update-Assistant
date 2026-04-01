namespace DriverGuardian.Contracts.Models;

public sealed record DriverMetadata(
    string Version,
    DateOnly? ReleaseDate,
    string Provider,
    bool IsSigned,
    string? SignatureIssuer);
