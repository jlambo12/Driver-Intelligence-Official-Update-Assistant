using System.Text.RegularExpressions;

namespace DriverGuardian.Tests.Unit.Localization;

public sealed class PreviewResourceAccessorParityTests
{
    [Fact]
    public void PreviewResourceKeysReferencedByUiStringsMustExistInResourcesAccessorClass()
    {
        var repositoryRoot = GetRepositoryRoot();
        var uiStringsPath = Path.Combine(repositoryRoot, "src", "DriverGuardian.UI.Wpf", "Localization", "UiStrings.Designer.cs");
        var resourcesPath = Path.Combine(repositoryRoot, "src", "DriverGuardian.UI.Wpf", "Localization", "Resources.cs");

        var uiStringsSource = File.ReadAllText(uiStringsPath);
        var resourcesSource = File.ReadAllText(resourcesPath);

        var previewKeysUsedByUiStrings = Regex.Matches(uiStringsSource, @"Resources\.(Preview_[A-Za-z0-9_]+)")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var resourceAccessorKeys = Regex.Matches(resourcesSource, @"public static string (Preview_[A-Za-z0-9_]+) =>")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        var missingPreviewKeys = previewKeysUsedByUiStrings
            .Where(key => !resourceAccessorKeys.Contains(key))
            .ToArray();

        Assert.True(
            missingPreviewKeys.Length == 0,
            $"Missing preview resource accessor(s): {string.Join(", ", missingPreviewKeys)}");
    }

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src")) &&
                Directory.Exists(Path.Combine(current.FullName, "tests")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test execution directory.");
    }
}
