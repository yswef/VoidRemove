using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VoidRemove.Shared.Contracts;

namespace VoidRemove.UI.Wpf.Services;

/// <summary>Opens folders, links and the clipboard using the Windows shell.</summary>
public sealed class WpfExternalLauncher : IExternalLauncher
{
    private readonly ILogger<WpfExternalLauncher> _logger;

    public WpfExternalLauncher(ILogger<WpfExternalLauncher>? logger = null)
    {
        _logger = logger ?? NullLogger<WpfExternalLauncher>.Instance;
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
                // Selecting the file inside its folder is more useful than opening it.
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
            Clipboard.SetText(text);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.Runtime.InteropServices.ExternalException)
        {
            // The clipboard is owned by another process; retry once after a short pause.
            try
            {
                Thread.Sleep(50);
                Clipboard.SetText(text);
            }
            catch (Exception retry)
            {
                _logger.LogWarning(retry, "The clipboard refused the copy operation.");
            }
        }
    }
}
