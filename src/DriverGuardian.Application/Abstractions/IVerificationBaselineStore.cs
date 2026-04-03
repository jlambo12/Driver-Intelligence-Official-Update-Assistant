using DriverGuardian.Application.Verification;

namespace DriverGuardian.Application.Abstractions;

public interface IVerificationBaselineStore
{
    Task<IReadOnlyCollection<VerificationBaselineSnapshot>> GetAllAsync(CancellationToken cancellationToken);

    Task SaveAllAsync(IReadOnlyCollection<VerificationBaselineSnapshot> snapshots, CancellationToken cancellationToken);
}
