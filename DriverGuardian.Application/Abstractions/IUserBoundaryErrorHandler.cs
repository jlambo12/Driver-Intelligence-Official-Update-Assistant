namespace DriverGuardian.Application.Abstractions;

public interface IUserBoundaryErrorHandler
{
    Task HandleAsync(Exception exception, string source, CancellationToken cancellationToken);
}
