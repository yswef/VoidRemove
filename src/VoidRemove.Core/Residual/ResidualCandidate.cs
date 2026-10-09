namespace VoidRemove.Core.Residual;

/// <summary>A leftover discovered by a scan, before it has been matched to a program.</summary>
public sealed record ResidualCandidate
{
    public required ResidualKind Kind { get; init; }

    /// <summary>Full file-system path or registry key path.</summary>
    public required string Path { get; init; }

    /// <summary>Size in bytes for file/directory candidates.</summary>
    public long SizeBytes { get; init; }

    /// <summary>The leaf/key name used for matching.</summary>
    public string Name => Kind is ResidualKind.RegistryKey or ResidualKind.RegistryValue
        ? System.IO.Path.GetFileName(Path.TrimEnd('\\'))
        : System.IO.Path.GetFileName(Path.TrimEnd('/', '\\'));
}
