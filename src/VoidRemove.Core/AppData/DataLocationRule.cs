namespace VoidRemove.Core.AppData;

/// <summary>A single place where an application keeps its user data.</summary>
public sealed record DataLocationRule
{
    /// <summary>Folder or Registry.</summary>
    public string Kind { get; init; } = "Folder";

    /// <summary>Path with environment tokens such as <c>%AppData%</c>.</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Optional locations are skipped silently when absent.</summary>
    public bool Optional { get; init; } = true;

    public bool IsRegistry => Kind.Equals("Registry", StringComparison.OrdinalIgnoreCase);
}
