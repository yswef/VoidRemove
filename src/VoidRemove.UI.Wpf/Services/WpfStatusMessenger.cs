using VoidRemove.Shared.Contracts;

namespace VoidRemove.UI.Wpf.Services;

/// <summary>Severity of a transient message.</summary>
public enum ToastKind
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
}

/// <summary>Carries one transient message.</summary>
public sealed class ToastEventArgs : EventArgs
{
    public ToastEventArgs(string message, ToastKind kind)
    {
        Message = message ?? string.Empty;
        Kind = kind;
    }

    public string Message { get; }

    public ToastKind Kind { get; }
}

/// <summary>
/// <see cref="IStatusMessenger"/> for WPF: it raises an event that the main window turns
/// into a toast overlay.
/// </summary>
/// <remarks>
/// Raising an event instead of touching the visual tree keeps the messenger usable from
/// background threads and from unit tests.
/// </remarks>
public sealed class WpfStatusMessenger : IStatusMessenger
{
    /// <summary>Raised on the calling thread; the subscriber marshals to the UI thread.</summary>
    public event EventHandler<ToastEventArgs>? ToastRequested;

    /// <inheritdoc />
    public void ShowInfo(string message) => Raise(message, ToastKind.Info);

    /// <inheritdoc />
    public void ShowSuccess(string message) => Raise(message, ToastKind.Success);

    /// <inheritdoc />
    public void ShowWarning(string message) => Raise(message, ToastKind.Warning);

    /// <inheritdoc />
    public void ShowError(string message) => Raise(message, ToastKind.Error);

    private void Raise(string message, ToastKind kind) =>
        ToastRequested?.Invoke(this, new ToastEventArgs(message, kind));
}
