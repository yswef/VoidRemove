using VoidRemove.Core.Programs;
using VoidRemove.Shared.Models;

namespace VoidRemove.Core.Residual;

/// <summary>
/// Decides whether a discovered leftover belongs to a program, and how safe it is to remove.
/// The order of the rules is the safety story: exact identifiers first, fuzzy tokens last.
/// </summary>
public static class ResidualMatcher
{
    private static readonly HashSet<string> GenericTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "installer", "uninstall", "uninstaller", "setup", "update", "updater", "redist",
        "redistributable", "runtime", "x86", "x64", "arm64", "64", "32", "exe", "app",
        "apps", "application", "applications", "program", "programs", "software", "version",
        "service", "services", "helper", "toolbar", "plugin", "plugins", "extension",
        "extensions", "components", "common", "shared", "core", "main", "bin", "data",
        "files", "folder", "the", "for", "and", "of", "to", "pro", "plus", "free", "lite",
        "basic", "standard", "professional", "enterprise", "edition", "suite", "studio",
        "tools", "toolkit", "sdk", "api", "net", "framework", "java", "oracle", "microsoft",
        "windows", "corp", "inc", "ltd", "llc", "company", "co",
    };

    /// <summary>Matches one candidate against one identity.</summary>
    public static ResidualMatch Match(ProgramRecord identity, ResidualCandidate candidate) =>
        Match(identity, candidate.Name, candidate.Path);

    /// <summary>Matches a candidate name/path against an identity.</summary>
    public static ResidualMatch Match(ProgramRecord identity, string candidateName, string candidatePath = "")
    {
        string candidateCompact = TextNormalizer.Compact(candidateName);
        if (candidateCompact.Length == 0)
        {
            return ResidualMatch.None;
        }

        // 1. The MSI product code is an unambiguous identifier.
        string guid = TextNormalizer.Compact(identity.ProductCode);
        if (guid.Length > 0 && (candidateCompact.Contains(guid) || TextNormalizer.Compact(candidatePath).Contains(guid)))
        {
            return new ResidualMatch { IsMatch = true, Level = SafetyLevel.Safe, Score = 1.0, Reason = "MSI product code" };
        }

        // 2. Anything physically inside the recorded install location belongs to the program.
        if (!string.IsNullOrEmpty(identity.InstallLocation) && !string.IsNullOrEmpty(candidatePath))
        {
            string root = identity.InstallLocation.TrimEnd('\\', '/');
            if (candidatePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return new ResidualMatch { IsMatch = true, Level = SafetyLevel.Safe, Score = 0.95, Reason = "inside install location" };
            }
        }

        // 3. An exact name match (ignoring punctuation and case).
        string nameCompact = TextNormalizer.Compact(identity.DisplayName);
        if (nameCompact.Length > 0 && candidateCompact == nameCompact)
        {
            return new ResidualMatch { IsMatch = true, Level = SafetyLevel.Safe, Score = 0.9, Reason = "exact program name" };
        }

        // 4. Fuzzy token overlap, downgraded when the only evidence is a generic word.
        List<string> candidateTokens = TextNormalizer.Tokens(candidateName);
        List<string> nameTokens = TextNormalizer.Tokens(identity.DisplayName);
        List<string> publisherTokens = TextNormalizer.Tokens(identity.Publisher);

        var matchedName = candidateTokens.Where(nameTokens.Contains).ToList();
        bool publisherCorroborates = candidateTokens.Any(publisherTokens.Contains);

        bool allGeneric = candidateTokens.Count > 0 && candidateTokens.All(GenericTokens.Contains);
        if (allGeneric)
        {
            return ResidualMatch.None;
        }

        if (matchedName.Count >= 2)
        {
            return Moderate("name tokens: " + string.Join(", ", matchedName), publisherCorroborates ? 0.8 : 0.7);
        }

        if (matchedName.Count == 1)
        {
            string token = matchedName[0];
            if (nameTokens.Count == 1)
            {
                return Moderate("single distinctive name token: " + token, 0.65);
            }

            if (token.Length >= 6 && !GenericTokens.Contains(token))
            {
                return Moderate("distinctive token: " + token, 0.55);
            }

            return Advanced("weak token: " + token, 0.35);
        }

        if (publisherCorroborates && candidateTokens.Any(t => t.Length >= 6 && !GenericTokens.Contains(t)))
        {
            return Advanced("publisher token only", 0.3);
        }

        return ResidualMatch.None;
    }

    private static ResidualMatch Moderate(string reason, double score) =>
        new() { IsMatch = true, Level = SafetyLevel.Moderate, Score = score, Reason = reason };

    private static ResidualMatch Advanced(string reason, double score) =>
        new() { IsMatch = true, Level = SafetyLevel.Advanced, Score = score, Reason = reason };
}
