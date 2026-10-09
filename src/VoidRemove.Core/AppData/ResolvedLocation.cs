namespace VoidRemove.Core.AppData;

/// <summary>Where a piece of app data was found, and by which discovery layer.</summary>
public enum DiscoverySource
{
    Profile,
    AutoDiscovery,
    InstallMonitor,
}

/// <summary>A concrete, expanded data location offered to the reset dialog.</summary>
public sealed record ResolvedLocation
{
    public required string Path { get; init; }

    public DiscoverySource Source { get; init; }

    public bool IsRegistry { get; init; }

    public bool Exists { get; init; }

    public long SizeBytes { get; init; }
}
