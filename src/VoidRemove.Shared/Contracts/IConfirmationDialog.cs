namespace VoidRemove.Shared.Contracts;

/// <summary>
/// Asks the user a yes/no question.
/// </summary>
/// <remarks>
/// Every destructive action in VoidRemove goes through this contract, so "no deletion
/// without confirmation" is enforced in the shared view models instead of being left to
/// each front end.
/// </remarks>
public interface IConfirmationDialog
{
    /// <summary>Shows a confirmation prompt. Returns true only when the user explicitly agrees.</summary>
    Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText, CancellationToken cancellationToken = default);
}
