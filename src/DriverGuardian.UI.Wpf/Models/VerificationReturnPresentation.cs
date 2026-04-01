namespace DriverGuardian.UI.Wpf.Models;

public sealed record VerificationReturnPresentation(
    string Title,
    string Status,
    string Guidance,
    string VerifyActionText,
    string ReadinessLabel)
{
    public static VerificationReturnPresentation Empty() =>
        new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
}
