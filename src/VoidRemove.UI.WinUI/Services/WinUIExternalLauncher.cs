using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VoidRemove.Shared.Contracts;
using Windows.ApplicationModel.DataTransfer;

namespace VoidRemove.UI.WinUI.Services;

/// <summary>Opens folders, links and the clipboard on Windows.</summary>
public sealed class WinUIExternalLauncher : IExternalLauncher
{
    private readonly ILogger<WinUIExternalLauncher> _logger;

    public WinUIExternalLauncher(ILogger<WinUIExternalLauncher>? logger = null)
    {
        _logger = logger ?? NullLogger<WinUIExternalLauncher>.Instance;
    }

    /// <inheritdoc />
    public void OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
                return;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _logger.LogWarning(exception, "Could not open {Path} in Explorer.", path);
        }
    }

    /// <inheritdoc />
    public void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _logger.LogWarning(exception, "Could not open {Url} in the default browser.", url);
        }
    }

    /// <inheritdoc />
    public void CopyToClipboard(string text)
    {
        if (text is null)
        {
            return;
        }

        try
        {
            DataPackage package = new();
            package.SetText(text);
            Clipboard.SetContent(package);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "The clipboard refused the copy operation.");
        }
    }
}
