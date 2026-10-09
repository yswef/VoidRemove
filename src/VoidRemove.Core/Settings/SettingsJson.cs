using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoidRemove.Core.Settings;

/// <summary>Serializer options shared by every settings read and write.</summary>
public static class SettingsJson
{
    /// <summary>
    /// Indented for humans, camelCase for JavaScript tooling, enums as strings so a
    /// hand-edited file stays readable, and tolerant of comments because users do edit it.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
