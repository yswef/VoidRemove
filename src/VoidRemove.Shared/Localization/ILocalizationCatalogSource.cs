namespace VoidRemove.Shared.Localization;

/// <summary>Supplies the raw string catalogs that <see cref="ILocalizationService"/> reads.</summary>
public interface ILocalizationCatalogSource
{
    /// <summary>Languages this source can provide, default language first.</summary>
    IReadOnlyList<LanguageOption> AvailableLanguages { get; }

    /// <summary>Loads the catalog for a language, or <c>null</c> when it does not exist.</summary>
    IReadOnlyDictionary<string, string>? Load(string languageCode);
}
