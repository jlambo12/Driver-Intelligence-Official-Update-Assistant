using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.Reports;
using DriverGuardian.Application.Verification;
using DriverGuardian.Domain.Drivers;
using DriverGuardian.Domain.Recommendations;

namespace DriverGuardian.Application.MainScreen;

public sealed class VerificationTrackingService(
    IVerificationBaselineStore baselineStore,
    PostInstallVerificationEvaluator evaluator)
{
    public async Task<IReadOnlyCollection<VerificationReportItem>> EvaluateAndCaptureAsync(
        IReadOnlyCollection<InstalledDriverSnapshot> currentDrivers,
        IReadOnlyCollection<RecommendationSummary> recommendations,
        CancellationToken cancellationToken)
    {
        var baselines = await baselineStore.GetAllAsync(cancellationToken);
        var baselineByDevice = baselines.ToDictionary(x => x.DeviceIdentity.InstanceId, StringComparer.OrdinalIgnoreCase);
        var currentByDevice = currentDrivers.ToDictionary(x => x.DeviceIdentity.InstanceId, StringComparer.OrdinalIgnoreCase);

        var verifications = new List<VerificationReportItem>();
        foreach (var baseline in baselines)
        {
            currentByDevice.TryGetValue(baseline.DeviceIdentity.InstanceId, out var current);
            var result = evaluator.Evaluate(new PostInstallVerificationRequest(baseline.DeviceIdentity, baseline, current));
            verifications.Add(new VerificationReportItem(baseline.DeviceIdentity, result));
        }

        var nextBaselines = new List<VerificationBaselineSnapshot>();
        foreach (var recommendation in recommendations.Where(r => r.HasRecommendation))
        {
            if (!currentByDevice.TryGetValue(recommendation.DeviceIdentity.InstanceId, out var snapshot))
            {
                continue;
            }

            var baseline = new VerificationBaselineSnapshot(
                snapshot.DeviceIdentity,
                snapshot.DriverVersion,
                snapshot.DriverDate,
                snapshot.ProviderName,
                snapshot.HardwareIdentifier.Value,
                DateTimeOffset.UtcNow);

            nextBaselines.Add(baseline);
            baselineByDevice[recommendation.DeviceIdentity.InstanceId] = baseline;
        }

        await baselineStore.SaveAllAsync(baselineByDevice.Values.ToArray(), cancellationToken);
        return verifications;
    }
}
