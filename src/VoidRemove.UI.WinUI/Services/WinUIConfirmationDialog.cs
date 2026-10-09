using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VoidRemove.Shared.Contracts;

namespace VoidRemove.UI.WinUI.Services;

/// <summary>Shows a <see cref="ContentDialog"/> for every destructive action.</summary>
public sealed class WinUIConfirmationDialog : IConfirmationDialog
{
    private readonly Func<XamlRoot?> _xamlRootProvider;

    public WinUIConfirmationDialog(Func<XamlRoot?> xamlRootProvider)
    {
        _xamlRootProvider = xamlRootProvider ?? throw new ArgumentNullException(nameof(xamlRootProvider));
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        XamlRoot? root = _xamlRootProvider();
        if (root is null || cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        ContentDialog dialog = new()
        {
            XamlRoot = root,
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Primary,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
