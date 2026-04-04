namespace DriverGuardian.Application.Reports;

public interface IShareableReportBuilder
{
    ShareableReport Build(ShareableReportRequest request);
    string BuildStructuredText(ShareableReport report);
}

public sealed class ShareableReportBuilder : IShareableReportBuilder
{
    private readonly ShareableReportModelAssembler _modelAssembler = new();
    private readonly ShareableReportStructuredTextRenderer _renderer = new();

    public ShareableReport Build(ShareableReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _modelAssembler.Build(request);
    }

    public string BuildStructuredText(ShareableReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return _renderer.Build(report);
    }
}
