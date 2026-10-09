using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace VoidRemove.Core.AppData;

/// <summary>Loads the community-extensible profile definitions from a folder of JSON files.</summary>
public sealed class ProfileCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly ILogger<ProfileCatalog>? _logger;

    public ProfileCatalog(ILogger<ProfileCatalog>? logger = null) => _logger = logger;

    public IReadOnlyList<AppDataProfile> LoadFromDirectory(string directory)
    {
        List<AppDataProfile> profiles = new();
        if (!Directory.Exists(directory))
        {
            return profiles;
        }

        foreach (string file in Directory.EnumerateFiles(directory, "*.json").OrderBy(static f => f, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                string json = File.ReadAllText(file);
                AppDataProfile? profile = JsonSerializer.Deserialize<AppDataProfile>(json, JsonOptions);
                if (profile is not null && !string.IsNullOrWhiteSpace(profile.Id))
                {
                    profiles.Add(profile);
                }
            }
            catch (JsonException exception)
            {
                _logger?.LogWarning(exception, "Skipping malformed profile {File}.", file);
            }
        }

        return profiles;
    }
}
