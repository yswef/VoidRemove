using VoidRemove.Core.Residual;

namespace VoidRemove.Core.AppData;

/// <summary>
/// The three-layer data discovery behind Reset App: explicit profiles, automatic folder
/// matching, and the Install Monitor's recorded writes. Results are de-duplicated.
/// </summary>
public sealed class AppDataResolver
{
    public IReadOnlyList<ResolvedLocation> Resolve(
        string programName,
        IReadOnlyList<AppDataProfile> profiles,
        IReadOnlyList<string> monitorPaths)
    {
        Dictionary<string, ResolvedLocation> found = new(StringComparer.OrdinalIgnoreCase);

        AddProfileLocations(programName, profiles, found);
        AddAutoDiscovered(programName, found);
        AddMonitorLocations(monitorPaths, found);

        return found.Values
            .OrderBy(static l => l.Source)
            .ThenBy(static l => l.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ------------------------------------------------------- layer 1: profiles

    private static void AddProfileLocations(string programName, IReadOnlyList<AppDataProfile> profiles, Dictionary<string, ResolvedLocation> sink)
    {
        foreach (AppDataProfile profile in profiles)
        {
            if (!ProfileMatches(programName, profile))
            {
                continue;
            }

            foreach (DataLocationRule rule in profile.Locations)
            {
                string expanded = EnvExpander.Expand(rule.Path);
                bool exists = rule.IsRegistry ? false : Directory.Exists(expanded) || File.Exists(expanded);

                if (rule.IsRegistry || exists || !rule.Optional)
                {
                    sink.TryAdd(expanded, new ResolvedLocation
                    {
                        Path = expanded,
                        Source = DiscoverySource.Profile,
                        IsRegistry = rule.IsRegistry,
                        Exists = exists,
                        SizeBytes = exists ? Size(expanded) : 0,
                    });
                }
            }
        }
    }

    internal static bool ProfileMatches(string programName, AppDataProfile profile)
    {
        List<string> programTokens = TextNormalizer.Tokens(programName);
        if (programTokens.Count == 0)
        {
            return false;
        }

        return profile.MatchNames.Any(match =>
        {
            string compactMatch = TextNormalizer.Compact(match);
            string compactName = TextNormalizer.Compact(programName);
            if (compactMatch.Length >= 4 && (compactName.Contains(compactMatch) || compactMatch.Contains(compactName)))
            {
                return true;
            }

            List<string> matchTokens = TextNormalizer.Tokens(match);
            return matchTokens.Count > 0 && matchTokens.Intersect(programTokens).Any();
        });
    }

    // ------------------------------------------------------- layer 2: auto discovery

    private static void AddAutoDiscovered(string programName, Dictionary<string, ResolvedLocation> sink)
    {
        List<string> programTokens = TextNormalizer.Tokens(programName);
        if (programTokens.Count == 0)
        {
            return;
        }

        foreach (string root in AutoRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (string directory in Directory.EnumerateDirectories(root))
            {
                string name = Path.GetFileName(directory);
                List<string> folderTokens = TextNormalizer.Tokens(name);
                bool overlaps = folderTokens.Count > 0 && folderTokens.Intersect(programTokens).Any();
                bool exact = TextNormalizer.Compact(name) == TextNormalizer.Compact(programName);

                if (overlaps || exact)
                {
                    sink.TryAdd(directory, new ResolvedLocation
                    {
                        Path = directory,
                        Source = DiscoverySource.AutoDiscovery,
                        Exists = true,
                        SizeBytes = Size(directory),
                    });
                }
            }
        }
    }

    private static IEnumerable<string> AutoRoots()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow");
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    }

    // ------------------------------------------------------- layer 3: monitor

    private static void AddMonitorLocations(IReadOnlyList<string> monitorPaths, Dictionary<string, ResolvedLocation> sink)
    {
        foreach (string path in monitorPaths)
        {
            string? root = TopLevelDataRoot(path);
            if (root is null)
            {
                continue;
            }

            sink.TryAdd(root, new ResolvedLocation
            {
                Path = root,
                Source = DiscoverySource.InstallMonitor,
                Exists = Directory.Exists(root) || File.Exists(root),
                SizeBytes = Size(root),
            });
        }
    }

    /// <summary>Collapses a monitored file path to its top-level data folder.</summary>
    internal static string? TopLevelDataRoot(string path)
    {
        foreach (string root in AutoRoots())
        {
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                string relative = Path.GetRelativePath(root, path);
                string top = relative.Split(Path.DirectorySeparatorChar)[0];
                return Path.Combine(root, top);
            }
        }

        return null;
    }

    private static long Size(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }

            if (Directory.Exists(path))
            {
                return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Sum(static f => new FileInfo(f).Length);
            }
        }
        catch (IOException)
        {
        }

        return 0;
    }
}
