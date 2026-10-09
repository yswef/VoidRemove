namespace VoidRemove.Core.History;

/// <summary>The persisted outcome of one removal run.</summary>
public sealed record UninstallReport
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public required string ProgramName { get; init; }

    public string Publisher { get; init; } = string.Empty;

    public DateTime StartedUtc { get; init; }

    public DateTime FinishedUtc { get; init; }

    public bool Succeeded { get; init; }

    public bool WasDryRun { get; init; }

    /// <summary>Quarantine session id holding the removed leftovers, when any.</summary>
    public string? QuarantineManifestId { get; init; }

    /// <summary>Path of the .reg backup of removed keys, when any.</summary>
    public string? RegistryBackupPath { get; init; }

    public int ResidualsRemoved { get; init; }

    public string Error { get; init; } = string.Empty;
}
