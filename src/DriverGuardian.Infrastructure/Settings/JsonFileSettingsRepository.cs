using System.Text.Json;
using DriverGuardian.Application.Abstractions;
using DriverGuardian.Domain.Settings;

namespace DriverGuardian.Infrastructure.Settings;

public sealed class JsonFileSettingsRepository : ISettingsRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public JsonFileSettingsRepository(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A settings file path is required.", nameof(filePath));
        }

        _filePath = filePath;
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return AppSettings.Default;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken);
            return (settings ?? AppSettings.Default).Normalize();
        }
        catch (JsonException)
        {
            return AppSettings.Default;
        }
        catch (NotSupportedException)
        {
            return AppSettings.Default;
        }
        catch (IOException)
        {
            return AppSettings.Default;
        }
        catch (UnauthorizedAccessException)
        {
            return AppSettings.Default;
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalized = settings.Normalize();
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, normalized, SerializerOptions, cancellationToken);
    }
}
