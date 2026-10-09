namespace VoidRemove.Core.Programs;

/// <summary>Discovers installed programs. The only implementation today reads the registry.</summary>
public interface IProgramReader
{
    /// <summary>False on non-Windows hosts, where the reader returns an empty list.</summary>
    bool IsSupported { get; }

    /// <summary>Reads and merges every visible uninstall key.</summary>
    Task<IReadOnlyList<ProgramRecord>> ReadAsync(CancellationToken cancellationToken = default);
}
