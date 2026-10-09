namespace VoidRemove.Core.Residual;

/// <summary>Enumerates candidate leftovers across disk and registry.</summary>
public interface IResidualScanner
{
    bool IsSupported { get; }

    /// <summary>Returns top-level candidates; the engine matches them against an identity.</summary>
    Task<IReadOnlyList<ResidualCandidate>> ScanAsync(int maxConcurrency, CancellationToken cancellationToken = default);
}
