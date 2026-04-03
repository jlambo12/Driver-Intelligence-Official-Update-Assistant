using System.Text.Json;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Verification;

namespace DriverGuardian.Infrastructure.History;

public sealed class JsonFileVerificationBaselineStore(string filePath) : IVerificationBaselineStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath = string.IsNullOrWhiteSpace(filePath)
        ? throw new ArgumentException("Verification baseline file path is required.", nameof(filePath))
        : filePath;

    public async Task<IReadOnlyCollection<VerificationBaselineSnapshot>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var stored = await JsonSerializer.DeserializeAsync<IReadOnlyCollection<VerificationBaselineSnapshot>>(stream, SerializerOptions, cancellationToken);
            return stored ?? [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (NotSupportedException)
        {
            return [];
        }
    }

    public async Task SaveAllAsync(IReadOnlyCollection<VerificationBaselineSnapshot> snapshots, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, snapshots, SerializerOptions, cancellationToken);
    }
}
