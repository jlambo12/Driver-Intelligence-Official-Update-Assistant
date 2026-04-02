using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Models;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class ScanResultsPresentationOfficialSourceTests
{
    [Fact]
    public void FromResult_ShouldUseLocalizedDirectSummaryFormat()
    {
        var result = CreateResult(new OpenOfficialSourceActionResult(
            IsReady: true,
            Resolution: OfficialSourceResolutionKind.DirectOfficialDriverPageConfirmed,
            Status: "status",
            ApprovedOfficialSourceUrl: "https://vendor.test/driver",
            BlockReason: null));

        var presentation = ScanResultsPresentation.FromResult(result);

        Assert.Equal(string.Format(UiStrings.OfficialSourceSummaryDirectFormat, "https://vendor.test/driver"), presentation.OfficialSourceSummary);
    }

    [Fact]
    public void FromResult_ShouldUseLocalizedVendorSummaryFormat()
    {
        var result = CreateResult(new OpenOfficialSourceActionResult(
            IsReady: true,
            Resolution: OfficialSourceResolutionKind.VendorSupportPageConfirmed,
            Status: "status",
            ApprovedOfficialSourceUrl: "https://vendor.test/support",
            BlockReason: null));

        var presentation = ScanResultsPresentation.FromResult(result);

        Assert.Equal(string.Format(UiStrings.OfficialSourceSummaryVendorFormat, "https://vendor.test/support"), presentation.OfficialSourceSummary);
    }

    private static MainScreenWorkflowResult CreateResult(OpenOfficialSourceActionResult officialSourceAction)
        => new(
            DiscoveredDeviceCount: 1,
            InspectedDriverCount: 1,
            RecommendedCount: 1,
            NotRecommendedCount: 0,
            ProviderCount: 1,
            ManualHandoffReadyCount: 1,
            ManualHandoffUserActionCount: 1,
            VerificationSummary: "verification",
            UiCulture: "ru-RU",
            ScanSessionId: Guid.NewGuid(),
            ReportExportPayload: new ReportExportPayload("name", "text", "md"),
            RecommendationDetails:
            [
                new RecommendationDetailResult(
                    DeviceDisplayName: "Device",
                    DeviceId: "DEV_1",
                    PriorityBucket: 0,
                    HasRecommendation: true,
                    RecommendationReason: "reason",
                    InstalledVersion: "1.0.0",
                    InstalledProvider: "Vendor",
                    RecommendedVersion: "2.0.0",
                    ManualHandoffReady: true,
                    ManualActionRequired: true,
                    VerificationAvailable: true,
                    VerificationStatus: "status",
                    OfficialSourceResolution: officialSourceAction.Resolution)
            ],
            OfficialSourceAction: officialSourceAction,
            RecentHistory: []);
}
