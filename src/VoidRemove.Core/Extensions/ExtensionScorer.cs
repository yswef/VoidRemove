namespace VoidRemove.Core.Extensions;

/// <summary>Local-only trust scoring for extensions; no network involvement.</summary>
public static class ExtensionScorer
{
    private static readonly string[] RiskyPermissions =
    {
        "tabs", "webrequest", "webrequestblocking", "<all_urls>", "nativemessaging",
        "debugger", "privacy", "proxy", "management", "cookies", "history",
    };

    public static int CountRisky(IEnumerable<string> permissions) =>
        permissions.Count(permission => RiskyPermissions.Contains(permission.ToLowerInvariant()));

    /// <summary>Computes a 0..100 trust score.</summary>
    public static int ComputeScore(int permissionCount, int riskyCount, bool hasKnownPublisher, bool forceInstalled)
    {
        int score = 100;
        score -= riskyCount * 15;
        score -= Math.Max(0, permissionCount - 5) * 2;
        if (!hasKnownPublisher)
        {
            score -= 10;
        }

        if (forceInstalled)
        {
            score -= 20;
        }

        return Math.Clamp(score, 0, 100);
    }

    public static bool IsTrusted(int score) => score >= 70;
}
