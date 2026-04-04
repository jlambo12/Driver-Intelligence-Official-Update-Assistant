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

    [Fact]
    public void CreateFromWorkflowResult_ShouldUseInsufficientEvidenceStatus_WhenNoRecommendations()
    {
        var result = CreateResult(
            ScanExecutionStatus.Completed,
            [
                new RecommendationDetailResult(
                    "Network adapter",
                    "PCI\\VEN_0002",
                    0,
                    false,
                    "No recommendation: insufficient evidence from providers.",
                    "1.0.0",
                    "vendor",
                    null,
                    false,
                    false,
                    false,
                    "verification")
            ]);

        var state = MainUiStateFactory.CreateFromWorkflowResult(result);

        Assert.Contains(UiStrings.RecommendationStateInsufficientEvidence, state.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateFromWorkflowResult_ShouldUseBlockedStatus_WhenRecommendationExistsButOfficialSourceNotReady()
    {
        var result = CreateResult(
            ScanExecutionStatus.Completed,
            [
                new RecommendationDetailResult(
                    "Video adapter",
                    "PCI\\VEN_0003",
                    0,
                    true,
                    "Recommendation available",
                    "1.0.0",
                    "vendor",
                    "2.0.0",
                    true,
                    true,
                    true,
                    "verification")
            ]);

        var state = MainUiStateFactory.CreateFromWorkflowResult(result);

        Assert.Contains(UiStrings.RecommendationStateBlocked, state.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    private static MainScreenWorkflowResult CreateResult(
        ScanExecutionStatus status,
        IReadOnlyCollection<RecommendationDetailResult>? recommendationDetails = null,
        int? recommendedCount = null,
        OpenOfficialSourceActionResult? officialSourceAction = null)
        => new(
            status,
            [],
            1,
            1,
            recommendedCount ?? recommendationDetails?.Count(detail => detail.HasRecommendation) ?? 0,
            0,
            1,
            0,
            0,
            "verification",
            "ru-RU",
            Guid.NewGuid(),
            new ReportExportPayload("name", "plain", "markdown"),
            recommendationDetails ?? [],
            officialSourceAction ?? new OpenOfficialSourceActionResult(false, OfficialSourceResolutionOutcome.InsufficientEvidence, OfficialSourceActionTarget.SourcePage, "status", null, null),
            []);
}
