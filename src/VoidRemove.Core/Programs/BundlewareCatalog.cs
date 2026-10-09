using System.Text.Json;
using VoidRemove.Core.Residual;

namespace VoidRemove.Core.Programs;

/// <summary>The editable bundleware definition file.</summary>
public sealed record BundlewareCatalogData
{
    public int Schema { get; init; } = 1;

    public string Description { get; init; } = string.Empty;

    public List<string> Publishers { get; init; } = new();

    public List<string> Products { get; init; } = new();
}

/// <summary>Detects bundleware by matching publishers/products against the editable list.</summary>
public sealed class BundlewareCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public BundlewareCatalogData Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new BundlewareCatalogData();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<BundlewareCatalogData>(json, JsonOptions) ?? new BundlewareCatalogData();
        }
        catch (JsonException)
        {
            return new BundlewareCatalogData();
        }
    }

    /// <summary>True when the program's publisher or name matches the bundleware list.</summary>
    public bool IsBundleware(string displayName, string publisher, BundlewareCatalogData data)
    {
        string compactName = TextNormalizer.Compact(displayName);
        string compactPublisher = TextNormalizer.Compact(publisher);

        return data.Products.Any(product =>
                   Matches(product, compactName)) ||
               data.Publishers.Any(entry =>
                   Matches(entry, compactPublisher) || Matches(entry, compactName));
    }

    private static bool Matches(string token, string compactTarget)
    {
        string compactToken = TextNormalizer.Compact(token);
        return compactToken.Length >= 4 && compactTarget.Contains(compactToken);
    }
}
