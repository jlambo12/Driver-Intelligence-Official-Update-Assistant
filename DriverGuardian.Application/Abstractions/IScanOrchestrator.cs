using DriverGuardian.Domain.Entities;

namespace DriverGuardian.Application.Abstractions;

public interface IScanOrchestrator
{
    Task<ScanSession> RunScanAsync(CancellationToken cancellationToken);
}
