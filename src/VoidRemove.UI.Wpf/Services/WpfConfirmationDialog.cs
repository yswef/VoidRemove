using System.Windows;
using VoidRemove.Shared.Contracts;

namespace VoidRemove.UI.Wpf.Services;

/// <summary>Shows <see cref="Dialogs.ConfirmDialog"/> on the UI thread.</summary>
public sealed class WpfConfirmationDialog : IConfirmationDialog
{
    /// <inheritdoc />
    public Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        Application? application = Application.Current;
        if (application is null || cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(false);
        }

        bool confirmed = application.Dispatcher.Invoke(() =>
        {
            Dialogs.ConfirmDialog dialog = new(title, message, confirmText, cancelText);

            Window? owner = application.MainWindow;
            if (owner is not null && !ReferenceEquals(owner, dialog) && owner.IsLoaded)
            {
                dialog.Owner = owner;
            }

            return dialog.ShowDialog() == true;
        });

        return Task.FromResult(confirmed);
    }
}
