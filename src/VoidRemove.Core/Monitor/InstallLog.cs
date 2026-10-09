namespace VoidRemove.Core.Monitor;

/// <summary>A recorded installation: what the installer added while it ran.</summary>
public sealed record InstallLog
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string ProcessName { get; init; } = string.Empty;

    public DateTime StartedUtc { get; init; }

    public DateTime EndedUtc { get; init; }

    public bool WasAutomatic { get; init; }

    public List<string> AddedFiles { get; init; } = new();

    public List<string> AddedRegistryKeys { get; init; } = new();

    public List<string> AddedServices { get; init; } = new();

    public List<string> AddedScheduledTasks { get; init; } = new();

    /// <summary>The data folders among the added files, used by Reset App's monitor layer.</summary>
    public IReadOnlyList<string> DataRoots => AddedFiles;

    public static InstallLog FromDiff(string processName, DateTime started, DateTime ended, bool automatic, SnapshotDiff diff) => new()
    {
        ProcessName = processName,
        StartedUtc = started,
        EndedUtc = ended,
        WasAutomatic = automatic,
        AddedFiles = diff.AddedFiles.ToList(),
        AddedRegistryKeys = diff.AddedRegistryKeys.ToList(),
        AddedServices = diff.AddedServices.ToList(),
        AddedScheduledTasks = diff.AddedScheduledTasks.ToList(),
    };
}
