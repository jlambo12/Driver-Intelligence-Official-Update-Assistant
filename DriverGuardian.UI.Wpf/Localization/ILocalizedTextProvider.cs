namespace DriverGuardian.UI.Wpf.Localization;

public interface ILocalizedTextProvider
{
    string this[string key] { get; }
}
