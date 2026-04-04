using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Application.MainScreen;

public sealed partial class PreviewScenarioMainScreenWorkflow : IMainScreenWorkflow
{
    private readonly Dictionary<PreviewScenarioId, MainScreenWorkflowResult> _scenarioResults;

    public PreviewScenarioMainScreenWorkflow()
    {
        _scenarioResults = BuildScenarioResults();
    }

    public PreviewScenarioId SelectedScenarioId { get; private set; } = PreviewScenarioId.FirstRunPreScan;

    public IReadOnlyList<PreviewScenarioId> AvailableScenarios { get; } =
    [
        PreviewScenarioId.FirstRunPreScan,
        PreviewScenarioId.NoActionableRecommendation,
        PreviewScenarioId.RecommendationWithLimitedEvidence,
        PreviewScenarioId.RecommendationReadyForManualAction,
        PreviewScenarioId.VerificationReturnGuidance,
        PreviewScenarioId.PopulatedHistoryAndExport
    ];

    public void SelectScenario(PreviewScenarioId scenarioId)
    {
        SelectedScenarioId = scenarioId;
    }

    public Task<MainScreenWorkflowResult> RunScanAsync(CancellationToken cancellationToken)
    {
        if (SelectedScenarioId == PreviewScenarioId.FirstRunPreScan)
        {
            return Task.FromResult(CreatePlaceholderResult());
        }

        return Task.FromResult(_scenarioResults[SelectedScenarioId]);
    }

    private static Dictionary<PreviewScenarioId, MainScreenWorkflowResult> BuildScenarioResults()
        => new()
        {
            [PreviewScenarioId.NoActionableRecommendation] = BuildNoActionableRecommendation(),
            [PreviewScenarioId.RecommendationWithLimitedEvidence] = BuildRecommendationWithLimitedEvidence(),
            [PreviewScenarioId.RecommendationReadyForManualAction] = BuildRecommendationReadyForManualAction(),
            [PreviewScenarioId.VerificationReturnGuidance] = BuildVerificationReturnGuidance(),
            [PreviewScenarioId.PopulatedHistoryAndExport] = BuildPopulatedHistoryAndExport()
        };
}
