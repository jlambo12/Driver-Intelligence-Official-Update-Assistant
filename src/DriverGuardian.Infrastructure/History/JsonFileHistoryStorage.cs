using System.Text.Json;
using DriverGuardian.Application.History.Models;

namespace DriverGuardian.Infrastructure.History;

internal sealed class JsonFileHistoryStorage(string filePath)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath = filePath;

    public List<ResultHistoryEntry> Load(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_filePath))
        {
            return [];
        }

        using var stream = File.OpenRead(_filePath);
        var stored = JsonSerializer.Deserialize<List<StoredHistoryEntry>>(stream, SerializerOptions) ?? [];

        return stored
            .Select(JsonFileHistoryEntryMapper.MapStoredToDomain)
            .Where(entry => entry is not null)
            .Cast<ResultHistoryEntry>()
            .ToList();
    }

    public void Save(IReadOnlyCollection<ResultHistoryEntry> entries, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var stored = entries
            .Select(JsonFileHistoryEntryMapper.MapDomainToStored)
            .ToArray();

        using var stream = File.Create(_filePath);
        JsonSerializer.Serialize(stream, stored, SerializerOptions);
    }
}
