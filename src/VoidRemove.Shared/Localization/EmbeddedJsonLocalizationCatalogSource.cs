using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VoidRemove.Shared.Localization;

/// <summary>
/// Reads the JSON string catalogs that are embedded in <c>VoidRemove.Shared.dll</c>.
/// </summary>
/// <remarks>
/// Because the catalogs are embedded, adding a language is a matter of dropping a new
/// <c>Resources/strings.&lt;code&gt;.json</c> file into the project: the language picker
/// discovers it from the assembly manifest, no code change required.
/// </remarks>
public sealed class EmbeddedJsonLocalizationCatalogSource : ILocalizationCatalogSource
{
    private const string ResourcePrefix = "VoidRemove.Shared.Resources.strings.";
    private const string JsonExtension = ".json";

    private static readonly IReadOnlyDictionary<string, string> KnownNativeNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "English",
            ["ar"] = "العربية",
        };

    private readonly Assembly _assembly;
    private readonly ILogger _logger;
    private readonly Lazy<IReadOnlyList<LanguageOption>> _languages;

    public EmbeddedJsonLocalizationCatalogSource(
        ILogger<EmbeddedJsonLocalizationCatalogSource>? logger = null,
        Assembly? assembly = null)
    {
        _assembly = assembly ?? typeof(EmbeddedJsonLocalizationCatalogSource).Assembly;
        _logger = logger ?? NullLogger<EmbeddedJsonLocalizationCatalogSource>.Instance;
        _languages = new Lazy<IReadOnlyList<LanguageOption>>(DiscoverLanguages);
    }

    /// <inheritdoc />
    public IReadOnlyList<LanguageOption> AvailableLanguages => _languages.Value;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? Load(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        string resourceName = ResourcePrefix + languageCode.Trim() + JsonExtension;

        using Stream? stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            _logger.LogWarning("Localization resource {Resource} was not found.", resourceName);
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(stream);
            Dictionary<string, string> catalog = new(StringComparer.Ordinal);

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    catalog[property.Name] = property.Value.GetString() ?? string.Empty;
                }
            }

            return catalog;
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Localization resource {Resource} is not valid JSON.", resourceName);
            return null;
        }
    }

    private IReadOnlyList<LanguageOption> DiscoverLanguages()
    {
        List<LanguageOption> found = new();

        foreach (string resourceName in _assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                !resourceName.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string code = resourceName.Substring(
                ResourcePrefix.Length,
                resourceName.Length - ResourcePrefix.Length - JsonExtension.Length);

            if (code.Length == 0)
            {
                continue;
            }

            string native = KnownNativeNames.TryGetValue(code, out string? known) ? known : code.ToUpperInvariant();
            found.Add(new LanguageOption(code, native));
        }

        // The fallback language is always offered first, the rest alphabetically.
        return found
            .OrderByDescending(option => string.Equals(option.Code, LanguageOption.FallbackCode, StringComparison.OrdinalIgnoreCase))
            .ThenBy(option => option.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
