namespace DriverGuardian.Application.OfficialSources;

public interface IOfficialSourceResolutionService
{
    Task<OfficialSourceResolutionResult> ResolveAsync(
        OfficialSourceResolutionRequest request,
        CancellationToken cancellationToken);
}
