using VoidRemove.Core.Settings;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;
using VoidRemove.Shared.Models;
using Xunit;

namespace VoidRemove.Core.Tests;

/// <summary>
/// The safety rules the settings UI and the writer agree on.
/// </summary>
public sealed class SettingsValidatorTests
{
    private static readonly ILocalizationService Localization =
        new LocalizationService(new EmbeddedJsonLocalizationCatalogSource());

    [Fact]
    public void DefaultsAreValid()
    {
        Assert.Empty(SettingsValidator.Validate(AppSettings.CreateDefault(), Localization));
    }

    [Fact]
    public void RejectsAnUnknownAccentColor()
    {
        AppSettings settings = AppSettings.CreateDefault();
        settings.AccentColor = "banana";

        IReadOnlyList<string> problems = SettingsValidator.Validate(settings, Localization);

        Assert.Single(problems);
        Assert.Equal("The accent color must be a hex value such as #2EE6A8.", problems[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(366)]
    public void RejectsRetentionOutsideTheAllowedRange(int days)
    {
        AppSettings settings = AppSettings.CreateDefault();
        settings.QuarantineRetentionDays = days;

        Assert.NotEmpty(SettingsValidator.Validate(settings, Localization));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public void RejectsWorkerCountOutsideTheAllowedRange(int workers)
    {
        AppSettings settings = AppSettings.CreateDefault();
        settings.MaxConcurrentScans = workers;

        Assert.NotEmpty(SettingsValidator.Validate(settings, Localization));
    }

    [Theory]
    [InlineData(SettingsLimits.MinQuarantineRetentionDays)]
    [InlineData(SettingsLimits.DefaultQuarantineRetentionDays)]
    [InlineData(SettingsLimits.MaxQuarantineRetentionDays)]
    public void AcceptsRetentionInsideTheAllowedRange(int days)
    {
        AppSettings settings = AppSettings.CreateDefault();
        settings.QuarantineRetentionDays = days;

        Assert.Empty(SettingsValidator.Validate(settings, Localization));
    }

    [Theory]
    [InlineData("")]
    [InlineData("fr")]
    public void RejectsALanguageThatIsNotShipped(string language)
    {
        AppSettings settings = AppSettings.CreateDefault();
        settings.Language = language;

        Assert.Contains(
            "Select a language.",
            SettingsValidator.Validate(settings, Localization));
    }

    [Fact]
    public void EmptyQuarantinePathMeansUseTheDefault()
    {
        Assert.True(SettingsValidator.IsQuarantinePathAllowed(null));
        Assert.True(SettingsValidator.IsQuarantinePathAllowed(string.Empty));
        Assert.True(SettingsValidator.IsQuarantinePathAllowed("   "));
    }

    [Theory]
    [InlineData("relative/quarantine")]
    [InlineData("quarantine")]
    public void RejectsARelativeQuarantinePath(string path)
    {
        Assert.False(SettingsValidator.IsQuarantinePathAllowed(path));
    }

    [Fact]
    public void RejectsTheProtectedRootsThemselves()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        foreach (string root in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                     Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                 })
        {
            Assert.False(SettingsValidator.IsQuarantinePathAllowed(root), $"{root} must be rejected");
        }
    }

    [Fact]
    public void RejectsAPathInsideTheWindowsFolder()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string candidate = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "VoidRemoveQuarantine");

        Assert.False(SettingsValidator.IsQuarantinePathAllowed(candidate));
    }

    [Fact]
    public void AcceptsADedicatedFolderUnderTheUserFolder()
    {
        using TempDirectory temp = new();

        Assert.True(SettingsValidator.IsQuarantinePathAllowed(Path.Combine(temp.FullPath, "Quarantine")));
    }

    [Fact]
    public void ReportsEveryProblemAtOnce()
    {
        AppSettings settings = AppSettings.CreateDefault();
        settings.AccentColor = "nope";
        settings.QuarantineRetentionDays = 0;
        settings.MaxConcurrentScans = 0;

        Assert.Equal(3, SettingsValidator.Validate(settings, Localization).Count);
    }

    [Fact]
    public void EverySafetyLevelCanBeTheCeiling()
    {
        foreach (SafetyLevel level in Enum.GetValues<SafetyLevel>())
        {
            AppSettings settings = AppSettings.CreateDefault();
            settings.ResidualSafetyCeiling = level;

            Assert.Empty(SettingsValidator.Validate(settings, Localization));
        }
    }
}
