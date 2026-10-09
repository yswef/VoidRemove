using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VoidRemove.Shared.Localization;

/// <summary>
/// Default <see cref="ILocalizationService"/>: resolves keys against the active catalog,
/// falls back to English for missing keys, and notifies bindings when the language flips.
/// </summary>
public sealed class LocalizationService : ObservableObject, ILocalizationService
{
    private const string IndexerPropertyName = "Item[]";

    private static readonly IReadOnlyDictionary<string, string> EmptyCatalog =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private readonly ILocalizationCatalogSource _source;
    private readonly ILogger<LocalizationService> _logger;
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _cache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    private IReadOnlyDictionary<string, string> _currentCatalog;

    public LocalizationService(
        ILocalizationCatalogSource source,
        ILogger<LocalizationService>? logger = null,
        string? initialLanguage = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _logger = logger ?? NullLogger<LocalizationService>.Instance;
        Languages = source.AvailableLanguages;

        CurrentLanguage = Normalize(initialLanguage);
        _currentCatalog = ResolveCatalog(CurrentLanguage) ?? EmptyCatalog;
    }

    /// <inheritdoc />
    public string CurrentLanguage { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<LanguageOption> Languages { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Hard-coded to <c>false</c>. VoidRemove keeps a left-to-right layout in every
    /// language, including Arabic, so the sidebar, the column order and the button order
    /// never flip. Only the words change.
    /// </remarks>
    public bool LayoutIsMirrored => false;

    /// <inheritdoc />
    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    /// <inheritdoc />
    public string this[string key] => TryResolve(key, out string value) ? value : key;

    /// <inheritdoc />
    public string Format(string key, params object?[] args)
    {
        string template = this[key];
        if (args is null || args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(CultureInfo.InvariantCulture, template, args);
        }
        catch (FormatException exception)
        {
            _logger.LogError(exception, "The translation for {Key} does not match the supplied arguments.", key);
            return template;
        }
    }

    /// <inheritdoc />
    public bool TryGetString(string key, out string value) => TryResolve(key, out value);

    /// <inheritdoc />
    public void SetLanguage(string languageCode)
    {
        string target = Normalize(languageCode);

        bool known = false;
        foreach (LanguageOption option in Languages)
        {
            if (string.Equals(option.Code, target, StringComparison.OrdinalIgnoreCase))
            {
                known = true;
                break;
            }
        }

        if (!known)
        {
            _logger.LogWarning(
                "Language {Requested} is not shipped with this build; using {Fallback} instead.",
                target,
                LanguageOption.FallbackCode);
            target = LanguageOption.FallbackCode;
        }

        if (string.Equals(target, CurrentLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string previous = CurrentLanguage;
        CurrentLanguage = target;
        _currentCatalog = ResolveCatalog(target) ?? ResolveCatalog(LanguageOption.FallbackCode) ?? EmptyCatalog;

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentLanguage)));
        OnPropertyChanged(new PropertyChangedEventArgs(IndexerPropertyName));
        LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(previous, target));
    }

    private string Normalize(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return LanguageOption.FallbackCode;
        }

        string trimmed = languageCode.Trim();
        int dash = trimmed.IndexOf('-');
        if (dash > 0)
        {
            // "ar-SA" and "en-US" collapse to the catalogs we ship ("ar", "en").
            string primary = trimmed.Substring(0, dash);
            foreach (LanguageOption option in Languages)
            {
                if (string.Equals(option.Code, primary, StringComparison.OrdinalIgnoreCase))
                {
                    return option.Code;
                }
            }
        }

        foreach (LanguageOption option in Languages)
        {
            if (string.Equals(option.Code, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return option.Code;
            }
        }

        return LanguageOption.FallbackCode;
    }

    private bool TryResolve(string key, out string value)
    {
        if (!string.IsNullOrEmpty(key))
        {
            if (_currentCatalog.TryGetValue(key, out string? direct))
            {
                value = direct;
                return true;
            }

            if (!string.Equals(CurrentLanguage, LanguageOption.FallbackCode, StringComparison.OrdinalIgnoreCase))
            {
                IReadOnlyDictionary<string, string>? fallback = ResolveCatalog(LanguageOption.FallbackCode);
                if (fallback is not null && fallback.TryGetValue(key, out string? indirect))
                {
                    value = indirect;
                    return true;
                }
            }
        }

        value = string.Empty;
        return false;
    }

    private IReadOnlyDictionary<string, string>? ResolveCatalog(string languageCode)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(languageCode, out IReadOnlyDictionary<string, string>? cached))
            {
                return cached;
            }

            IReadOnlyDictionary<string, string>? loaded = _source.Load(languageCode);
            if (loaded is null)
            {
                return null;
            }

            _cache[languageCode] = loaded;
            return loaded;
        }
    }
}
