using DriverGuardian.Application.MainScreen;
using DriverGuardian.Application.OfficialSources;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class PreviewScenarioMainScreenWorkflowTests
{
    [Fact]
    public async Task RunScanAsync_ShouldReturnPlaceholderForFirstRunScenario()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.FirstRunPreScan);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(Guid.Empty, result.ScanSessionId);
        Assert.Equal(0, result.RecommendedCount);
        Assert.Empty(result.RecommendationDetails);
        Assert.Empty(result.ReportExportPayload.PlainTextContent);
    }

    [Fact]
    public async Task RunScanAsync_ShouldReturnManualReadyScenario()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.RecommendationReadyForManualAction);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(1, result.RecommendedCount);
        Assert.Equal(1, result.ManualHandoffReadyCount);
        Assert.True(result.OfficialSourceAction.IsReady);
        Assert.Equal(OfficialSourceResolutionOutcome.ConfirmedDirectOfficialDriverPage, result.OfficialSourceAction.ResolutionOutcome);
        Assert.Contains(result.RecommendationDetails, detail => detail.ManualHandoffReady);
    }

    [Fact]
    public async Task RunScanAsync_ShouldReturnPopulatedHistoryScenario()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.PopulatedHistoryAndExport);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.NotEmpty(result.RecentHistory);
        Assert.NotEmpty(result.ReportExportPayload.PlainTextContent);
    }

    [Fact]
    public async Task RunScanAsync_LimitedEvidenceScenario_ShouldNotRequireManualActionBeforeSourceValidation()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.RecommendationWithLimitedEvidence);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(0, result.ManualHandoffReadyCount);
        Assert.Equal(0, result.ManualHandoffUserActionCount);
        Assert.Equal(OfficialSourceResolutionOutcome.InsufficientEvidence, result.OfficialSourceAction.ResolutionOutcome);
        Assert.All(result.RecommendationDetails, detail =>
        {
            Assert.False(detail.ManualHandoffReady);
            Assert.False(detail.ManualActionRequired);
            Assert.False(detail.VerificationAvailable);
        });
    }

    [Fact]
    public async Task RunScanAsync_PopulatedHistoryScenario_ShouldKeepAggregateCountsConsistentWithActionableRecommendations()
    {
        var workflow = new PreviewScenarioMainScreenWorkflow();

        workflow.SelectScenario(PreviewScenarioId.PopulatedHistoryAndExport);
        var result = await workflow.RunScanAsync(CancellationToken.None);

        Assert.Equal(
            result.RecommendationDetails.Count(detail => detail.ManualHandoffReady),
            result.ManualHandoffReadyCount);
        Assert.Equal(
            result.RecommendationDetails.Count(detail => detail.ManualActionRequired),
            result.ManualHandoffUserActionCount);
    }
}
