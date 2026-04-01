namespace DriverGuardian.Application.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(string entry, CancellationToken cancellationToken);
}
