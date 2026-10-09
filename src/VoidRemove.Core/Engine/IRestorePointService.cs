namespace VoidRemove.Core.Engine;

/// <summary>Creates and restores Windows System Restore points.</summary>
public interface IRestorePointService
{
    bool IsSupported { get; }

    /// <summary>Creates a restore point; returns its description, or null on failure.</summary>
    Task<string?> CreateAsync(string description, CancellationToken cancellationToken = default);
}

/// <summary>Used off-Windows and until the full System Restore tooling lands.</summary>
public sealed class NullRestorePointService : IRestorePointService
{
    public bool IsSupported => false;

    public Task<string?> CreateAsync(string description, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
