namespace VoidRemove.Core.Extensions;

/// <summary>Enumerates extensions across the installed browsers.</summary>
public interface IBrowserExtensionScanner
{
    bool IsSupported { get; }

    Task<IReadOnlyList<BrowserExtension>> ScanAsync(CancellationToken cancellationToken = default);
}
