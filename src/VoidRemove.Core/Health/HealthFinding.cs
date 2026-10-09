namespace VoidRemove.Core.Health;

/// <summary>One actionable item inside a health category.</summary>
public sealed record HealthFinding
{
    public required HealthCategory Category { get; init; }

    /// <summary>File/folder path, registry key or program name.</summary>
    public required string Target { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    /// <summary>Human-readable explanation for the Fix/Clean row.</summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>Optional SHA-256 for malware checks.</summary>
    public string Sha256 { get; init; } = string.Empty;
}

/// <summary>The aggregated outcome of a full health scan.</summary>
public sealed record HealthReport
{
    public DateTime ScannedUtc { get; init; }

    public List<HealthFinding> Findings { get; init; } = new();

    public long ReclaimableBytes => Findings.Sum(static f => f.SizeBytes);

    public IReadOnlyList<HealthFinding> For(HealthCategory category) =>
        Findings.Where(f => f.Category == category)
            .OrderByDescending(static f => f.SizeBytes)
            .ToList();

    /// <summary>Overall status text key input: healthy when nothing was found.</summary>
    public bool IsHealthy => Findings.Count == 0;
}
