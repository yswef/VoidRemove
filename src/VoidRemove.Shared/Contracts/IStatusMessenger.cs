namespace VoidRemove.Shared.Contracts;

/// <summary>
/// Shows a transient message to the user.
/// </summary>
/// <remarks>
/// WPF maps this to a toast overlay, WinUI to an <c>InfoBar</c>. Keeping it behind an
/// interface is what lets the shared view models report progress without referencing a UI
/// framework.
/// </remarks>
public interface IStatusMessenger
{
    void ShowInfo(string message);

    void ShowSuccess(string message);

    void ShowWarning(string message);

    void ShowError(string message);
}
