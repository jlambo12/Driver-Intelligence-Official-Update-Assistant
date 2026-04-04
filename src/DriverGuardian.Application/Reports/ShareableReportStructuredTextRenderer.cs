namespace DriverGuardian.Application.Reports;

internal sealed class ShareableReportStructuredTextRenderer
{
    public string Build(ShareableReport report)
    {
        var lines = new List<string>
        {
            "DriverGuardian Shareable Scan Report",
            $"Scan Session: {report.Metadata.ScanSessionId}",
            $"Scan Started (UTC): {report.Metadata.ScanStartedAtUtc:O}",
            $"Scan Completed (UTC): {report.Metadata.ScanCompletedAtUtc:O}",
            $"Generated (UTC): {report.Metadata.GeneratedAtUtc:O}",
            string.Empty,
            "1) Scan Summary",
            $"- Total Devices: {report.ScanSummary.TotalDevices}",
            string.Empty,
            "2) Recommendation State",
            $"- Total: {report.RecommendationSummary.TotalRecommendations}",
            $"- Recommended: {report.RecommendationSummary.RecommendedCount}",
            $"- Not Recommended: {report.RecommendationSummary.NotRecommendedCount}",
            "- Safety Note: DriverGuardian provides analysis and recommendations only. Installation is always manual and user-controlled.",
            string.Empty,
            "3) Manual Next-Step Guidance",
            $"- Total: {report.ManualInstallHandoffSummary.TotalHandoffs}",
            $"- Ready: {report.ManualInstallHandoffSummary.ReadyCount}",
            $"- Requires Action: {report.ManualInstallHandoffSummary.RequiresActionCount}",
            $"- Not Ready: {report.ManualInstallHandoffSummary.NotReadyCount}",
            "- User Action: Follow vendor instructions manually, then return for verification.",
            string.Empty,
            "4) Verification State",
            $"- Total: {report.VerificationSummary.TotalVerifications}",
            $"- Verified Changed: {report.VerificationSummary.VerifiedChangedCount}",
            $"- Partial: {report.VerificationSummary.PartialCount}",
            $"- No Change: {report.VerificationSummary.NoChangeCount}",
            $"- Device Missing: {report.VerificationSummary.DeviceMissingCount}",
            $"- Insufficient Evidence: {report.VerificationSummary.InsufficientEvidenceCount}",
            string.Empty,
            "5) Device-Level Details"
        };

        foreach (var device in report.Devices)
        {
            lines.Add($"- Device: {device.DeviceDisplayName}");
            lines.Add($"  Device ID: {device.DeviceInstanceId}");
            lines.Add($"  Driver Version: {device.DriverSnapshot.DriverVersion}");
            lines.Add($"  Provider: {device.DriverSnapshot.ProviderName ?? "(unknown)"}");
            lines.Add($"  Hardware ID: {device.DriverSnapshot.HardwareId}");

            if (device.Recommendation is not null)
            {
                lines.Add($"  Recommendation: {(device.Recommendation.HasRecommendation ? "Yes" : "No")}");
                lines.Add($"  Recommendation Reason: {device.Recommendation.Reason}");
            }

            if (device.ManualInstallHandoff is not null)
            {
                lines.Add($"  Handoff Outcome: {device.ManualInstallHandoff.Outcome}");
                lines.Add($"  Official Source Confidence: {device.ManualInstallHandoff.OfficialSourceConfidence}");
                lines.Add($"  Source Guidance: {device.ManualInstallHandoff.OfficialSourceGuidance}");
                if (!string.IsNullOrWhiteSpace(device.ManualInstallHandoff.PackageUri))
                {
                    lines.Add($"  Candidate Package URL: {device.ManualInstallHandoff.PackageUri}");
                }
            }

            if (device.Verification is not null)
            {
                lines.Add($"  Verification Outcome: {device.Verification.Outcome}");
            }

            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
