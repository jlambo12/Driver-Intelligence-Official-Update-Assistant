using DriverGuardian.Application.Abstractions;
using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;
using DriverGuardian.UI.Wpf.Localization;
using DriverGuardian.UI.Wpf.Models;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class MainUiStateFactoryTests
{
    [Theory]
    [InlineData(ScanExecutionStatus.Failed)]
    [InlineData(ScanExecutionStatus.Partial)]
    public void CreateFromWorkflowResult_UsesLocalizedStatusForNonCompletedScans(ScanExecutionStatus status)
    {
        var result = CreateResult(status);

        var state = MainUiStateFactory.CreateFromWorkflowResult(result);

        var expected = status == ScanExecutionStatus.Failed
            ? UiStrings.StatusScanFailed
            : UiStrings.StatusScanPartial;

        Assert.Equal(expected, state.StatusText);
    }

    [Fact]
    public void CreateFromWorkflowResult_ShouldNotShowPerRecommendationOpenAction_WhenOfficialSourceUrlIsUnsafe()
    {
        var result = CreateResult(
            ScanExecutionStatus.Completed,
            [
                new RecommendationDetailResult(
                    "Video adapter",
                    "PCI\\VEN_0001",
                    0,
                    true,
                    "reason",
                    "1.0.0",
                    "vendor",
                    "2.0.0",
                    true,
                    true,
                    true,
                    "verification",
                    "http://unsafe.example/driver")
            ]);

        var state = MainUiStateFactory.CreateFromWorkflowResult(result);
        var detail = Assert.Single(state.Results.RecommendationDetails);

        Assert.False(detail.CanOpenOfficialSourceUrl);
        Assert.Equal(UiStrings.RecommendationOfficialSourceBlockedBySafety, detail.OfficialSourceActionHint);
    }

    private static MainScreenWorkflowResult CreateResult(
        ScanExecutionStatus status,
        IReadOnlyCollection<RecommendationDetailResult>? recommendationDetails = null)
        => new(
            status,
            [],
            1,
            1,
            0,
            0,
            1,
            0,
            0,
            "verification",
            "ru-RU",
            Guid.NewGuid(),
            new ReportExportPayload("name", "plain", "markdown"),
            recommendationDetails ?? [],
            new OpenOfficialSourceActionResult(false, OfficialSourceResolutionOutcome.InsufficientEvidence, OfficialSourceActionTarget.SourcePage, "status", null, null),
            []);
}
