namespace VoidRemove.Core.Monitor;

/// <summary>A point-in-time capture of the parts of the system an installer touches.</summary>
public sealed record SystemSnapshot
{
    public IReadOnlySet<string> Files { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> RegistryKeys { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> Services { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> ScheduledTasks { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static SystemSnapshot Empty { get; } = new();
}

/// <summary>What changed between two snapshots; the raw material of an install log.</summary>
public sealed record SnapshotDiff
{
    public IReadOnlyList<string> AddedFiles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AddedRegistryKeys { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AddedServices { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AddedScheduledTasks { get; init; } = Array.Empty<string>();

    public bool IsEmpty =>
        AddedFiles.Count == 0 && AddedRegistryKeys.Count == 0 &&
        AddedServices.Count == 0 && AddedScheduledTasks.Count == 0;

    /// <summary>Computes after-minus-before.</summary>
    public static SnapshotDiff Compute(SystemSnapshot before, SystemSnapshot after) => new()
    {
        AddedFiles = after.Files.Except(before.Files, StringComparer.OrdinalIgnoreCase).OrderBy(static f => f, StringComparer.OrdinalIgnoreCase).ToList(),
        AddedRegistryKeys = after.RegistryKeys.Except(before.RegistryKeys, StringComparer.OrdinalIgnoreCase).OrderBy(static k => k, StringComparer.OrdinalIgnoreCase).ToList(),
        AddedServices = after.Services.Except(before.Services, StringComparer.OrdinalIgnoreCase).OrderBy(static s => s, StringComparer.OrdinalIgnoreCase).ToList(),
        AddedScheduledTasks = after.ScheduledTasks.Except(before.ScheduledTasks, StringComparer.OrdinalIgnoreCase).OrderBy(static t => t, StringComparer.OrdinalIgnoreCase).ToList(),
    };
}
