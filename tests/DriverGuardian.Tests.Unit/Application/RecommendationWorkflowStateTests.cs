using DriverGuardian.Application.Abstractions;

namespace DriverGuardian.Tests.Unit.Application;

public sealed class RecommendationWorkflowStateTests
{
    [Theory]
    [InlineData(RecommendationWorkflowState.NoActionRequired, false, false, false, false)]
    [InlineData(RecommendationWorkflowState.RecommendationAvailable, true, false, false, false)]
    [InlineData(RecommendationWorkflowState.ManualActionRequired, true, true, true, false)]
    [InlineData(RecommendationWorkflowState.AwaitingVerification, true, true, false, true)]
    public void WorkflowStateDerivedFlags_ShouldRemainConsistent(
        RecommendationWorkflowState state,
        bool hasRecommendation,
        bool isManualHandoffReady,
        bool isManualActionRequired,
        bool isAwaitingVerification)
    {
        var detail = new RecommendationDetailResult(
            "Device",
            "DEVICE\\ID",
            0,
            state,
            "reason",
            "1.0",
            "provider",
            "2.0",
            "status");

        Assert.Equal(hasRecommendation, detail.HasRecommendation);
        Assert.Equal(isManualHandoffReady, detail.IsManualHandoffReady);
        Assert.Equal(isManualActionRequired, detail.IsManualActionRequired);
        Assert.Equal(isAwaitingVerification, detail.IsAwaitingVerification);
    }
}
