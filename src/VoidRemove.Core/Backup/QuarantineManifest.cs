namespace VoidRemove.Core.Backup;

/// <summary>One moved item inside a quarantine session.</summary>
public sealed record QuarantineItem
{
    public required string OriginalPath { get; init; }

    public required string StoredPath { get; init; }

    public bool IsDirectory { get; init; }

    public long SizeBytes { get; init; }
}

/// <summary>The manifest written next to a quarantine session so it can be restored.</summary>
public sealed record QuarantineManifest
{
    public required string Id { get; init; }

    public DateTime CreatedUtc { get; init; }

    public string Label { get; init; } = string.Empty;

    public List<QuarantineItem> Items { get; init; } = new();

    public const string ManifestFileName = "manifest.json";
}
