namespace VoidRemove.Core.WindowsApps;

/// <summary>Enumerates and removes packaged Windows apps.</summary>
public interface IWindowsAppService
{
    bool IsSupported { get; }

    Task<IReadOnlyList<WindowsApp>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes a package for the current user; returns success.</summary>
    Task<bool> RemoveAsync(string packageFamilyName, CancellationToken cancellationToken = default);
}
