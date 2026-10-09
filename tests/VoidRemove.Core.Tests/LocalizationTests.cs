using System.Reflection;
using VoidRemove.Shared.Localization;
using Xunit;

namespace VoidRemove.Core.Tests;

/// <summary>
/// Translation safety net: the catalogs and the strongly typed keys must never drift, so a
/// missing string can only be caught here rather than by a user.
/// </summary>
public sealed class LocalizationTests
{
    private static readonly ILocalizationCatalogSource Source = new EmbeddedJsonLocalizationCatalogSource();

    [Fact]
    public void BothLanguagesAreEmbedded()
    {
        IReadOnlyList<LanguageOption> languages = Source.AvailableLanguages;

        Assert.Contains(languages, option => option.Code == "en");
        Assert.Contains(languages, option => option.Code == "ar");
        Assert.Equal("en", languages[0].Code);
    }

    [Fact]
    public void CatalogsHaveExactlyTheSameKeys()
    {
        IReadOnlyDictionary<string, string>? english = Source.Load("en");
        IReadOnlyDictionary<string, string>? arabic = Source.Load("ar");

        Assert.NotNull(english);
        Assert.NotNull(arabic);

        Assert.Equal(english!.Keys.OrderBy(key => key, StringComparer.Ordinal), arabic!.Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public void EveryKeyHasAConstantAndEveryConstantHasAKey()
    {
        HashSet<string> constants = typeof(LocalizationKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        IReadOnlyDictionary<string, string>? english = Source.Load("en");
        Assert.NotNull(english);

        Assert.Equal(english!.Keys.OrderBy(key => key, StringComparer.Ordinal), constants.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public void NoTranslationIsEmpty()
    {
        foreach (string code in new[] { "en", "ar" })
        {
            IReadOnlyDictionary<string, string>? catalog = Source.Load(code);
            Assert.NotNull(catalog);

            foreach (KeyValuePair<string, string> entry in catalog!)
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.Value), $"{code}:{entry.Key} is empty");
            }
        }
    }

    [Fact]
    public void TheDefaultLanguageResolvesEveryKey()
    {
        LocalizationService service = CreateService();

        Assert.Equal("Uninstall", service[LocalizationKeys.CommonUninstall]);
        Assert.Equal("Programs", service[LocalizationKeys.NavPrograms]);
    }

    [Fact]
    public void SwitchingToArabicChangesTheTextAndKeepsTheLayoutLeftToRight()
    {
        LocalizationService service = CreateService();

        service.SetLanguage("ar");

        Assert.Equal("ar", service.CurrentLanguage);
        Assert.Equal("البرامج", service[LocalizationKeys.NavPrograms]);
        Assert.False(service.LayoutIsMirrored, "The layout must never mirror, not even in Arabic.");
    }

    [Fact]
    public void UnknownLanguageFallsBackToTheDefault()
    {
        LocalizationService service = CreateService();

        service.SetLanguage("fr");

        Assert.Equal(LanguageOption.FallbackCode, service.CurrentLanguage);
        Assert.Equal("Programs", service[LocalizationKeys.NavPrograms]);
    }

    [Fact]
    public void RegionalCodeCollapsesToTheShippedCatalog()
    {
        LocalizationService service = CreateService();

        service.SetLanguage("ar-SA");

        Assert.Equal("ar", service.CurrentLanguage);
        Assert.Equal("البرامج", service[LocalizationKeys.NavPrograms]);
    }

    [Fact]
    public void UnknownKeyIsReturnedVerbatimInsteadOfThrowing()
    {
        LocalizationService service = CreateService();

        Assert.Equal("does.not.exist", service["does.not.exist"]);
        Assert.False(service.TryGetString("does.not.exist", out _));
    }

    [Fact]
    public void FormatFillsPlaceholders()
    {
        LocalizationService service = CreateService();

        Assert.Equal("Version 9.9.9", service.Format(LocalizationKeys.AppVersion, "9.9.9"));
        Assert.Equal("The value must be between 1 and 365.", service.Format(LocalizationKeys.ErrorsValueOutOfRange, 1, 365));
    }

    [Fact]
    public void FormatSurvivesWrongArguments()
    {
        LocalizationService service = CreateService();

        // The template expects one argument; passing none must not throw.
        Assert.Equal("Version {0}", service.Format(LocalizationKeys.AppVersion));
    }

    [Fact]
    public void LanguageChangedIsRaisedExactlyOnce()
    {
        LocalizationService service = CreateService();

        int raised = 0;
        string? previous = null;
        string? current = null;
        service.LanguageChanged += (_, e) =>
        {
            raised++;
            previous = e.PreviousLanguage;
            current = e.CurrentLanguage;
        };

        service.SetLanguage("ar");
        service.SetLanguage("ar");

        Assert.Equal(1, raised);
        Assert.Equal("en", previous);
        Assert.Equal("ar", current);
    }

    private static LocalizationService CreateService() => new(Source);
}
