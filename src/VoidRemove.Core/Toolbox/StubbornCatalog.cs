using System.Text.Json;
using VoidRemove.Core.Residual;

namespace VoidRemove.Core.Toolbox;

/// <summary>A hand-maintained recipe for a program that resists normal removal.</summary>
public sealed record StubbornProgram
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public List<string> MatchNames { get; init; } = new();

    public string UninstallExecutable { get; init; } = string.Empty;

    public string UninstallArguments { get; init; } = string.Empty;

    /// <summary>Processes to stop first.</summary>
    public List<string> Processes { get; init; } = new();

    /// <summary>Known leftover paths (environment tokens allowed).</summary>
    public List<string> ResidualPaths { get; init; } = new();
}

/// <summary>Loads the original, extensible stubborn-program database.</summary>
public sealed class StubbornCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public IReadOnlyList<StubbornProgram> Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Array.Empty<StubbornProgram>();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<StubbornProgram>>(json, JsonOptions) ?? new List<StubbornProgram>();
        }
        catch (JsonException)
        {
            return Array.Empty<StubbornProgram>();
        }
    }

    /// <summary>Returns the recipe whose names best match the given program, or null.</summary>
    public StubbornProgram? FindMatch(string programName, IReadOnlyList<StubbornProgram> programs)
    {
        string compactName = TextNormalizer.Compact(programName);
        List<string> nameTokens = TextNormalizer.Tokens(programName);
        if (compactName.Length == 0)
        {
            return null;
        }

        return programs
            .Select(program => (Program: program, Score: Score(program)))
            .Where(pair => pair.Score > 0)
            .OrderByDescending(pair => pair.Score)
            .Select(pair => pair.Program)
            .FirstOrDefault();

        int Score(StubbornProgram program)
        {
            foreach (string match in program.MatchNames)
            {
                string compactMatch = TextNormalizer.Compact(match);
                if (compactMatch.Length >= 4 && (compactName.Contains(compactMatch) || compactMatch.Contains(compactName)))
                {
                    return 2;
                }

                List<string> matchTokens = TextNormalizer.Tokens(match);
                if (matchTokens.Count > 0 && matchTokens.Intersect(nameTokens).Any())
                {
                    return 1;
                }
            }

            return 0;
        }
    }
}
