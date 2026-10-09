namespace VoidRemove.Core.Monitor;

/// <summary>Captures a <see cref="SystemSnapshot"/>; the only implementation reads the OS.</summary>
public interface ISnapshotProvider
{
    bool IsSupported { get; }

    SystemSnapshot Capture(CancellationToken cancellationToken = default);
}
