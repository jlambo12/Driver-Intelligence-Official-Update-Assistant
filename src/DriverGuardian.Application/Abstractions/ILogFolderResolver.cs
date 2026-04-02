namespace DriverGuardian.Application.Abstractions;

public interface ILogFolderResolver
{
    Task<string> GetEffectiveLogFolderAsync(CancellationToken cancellationToken);
}
