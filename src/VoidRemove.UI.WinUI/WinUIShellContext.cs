using VoidRemove.Shared.ViewModels;

namespace VoidRemove.UI.WinUI;

/// <summary>
/// Data context of the shell chrome.
/// </summary>
/// <remarks>
/// Pages inside the content host get the shared view model directly as their own data
/// context, so only the window frame binds through this wrapper.
/// </remarks>
public sealed class WinUIShellContext
{
    public WinUIShellContext(ShellViewModel shell)
    {
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    /// <summary>The shared shell view model.</summary>
    public ShellViewModel Shell { get; }
}
