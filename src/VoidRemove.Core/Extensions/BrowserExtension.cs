namespace VoidRemove.Core.Extensions;

/// <summary>The browsers VoidRemove can enumerate extensions for.</summary>
public enum Browser
{
    InternetExplorer,
    Chrome,
    Edge,
    Brave,
    Firefox,
    Opera,
}

/// <summary>One discovered browser extension or add-on.</summary>
public sealed record BrowserExtension
{
    public required Browser BrowserKind { get; init; }

    public required string Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public List<string> Permissions { get; init; } = new();

    public string Publisher { get; init; } = string.Empty;

    /// <summary>0..100 local trust score; higher is safer.</summary>
    public int TrustScore { get; init; }

    public bool IsTrusted => TrustScore >= 70;

    /// <summary>True when a group policy forces this extension on the user.</summary>
    public bool IsForceInstalled { get; init; }

    /// <summary>Where it was found, for removal.</summary>
    public string SourcePath { get; init; } = string.Empty;
}
