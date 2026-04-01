using System.Globalization;
using System.Resources;
using DriverGuardian.UI.Wpf.Resources;

namespace DriverGuardian.UI.Wpf.Localization;

public sealed class ResxLocalizedTextProvider : ILocalizedTextProvider
{
    private static readonly ResourceManager ResourceManager = Strings.ResourceManager;

    public string this[string key]
        => ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
           ?? ResourceManager.GetString(key, new CultureInfo("ru-RU"))
           ?? key;
}
