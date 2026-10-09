using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace VoidRemove.UI.Wpf.Interop;

/// <summary>
/// Desktop Window Manager tweaks that WPF does not expose.
/// </summary>
public static class DwmInterop
{
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmWindowCornerPreferenceRound = 2;

    private static readonly ILogger Logger = NullLogger.Instance;

    /// <summary>
    /// Asks Windows 11 to round the corners of a borderless window.
    /// </summary>
    /// <remarks>
    /// Windows 10 ignores the attribute, so the call is a no-op there. Everything is
    /// defensive: a missing entry point must never stop the window from opening.
    /// </remarks>
    public static void TryEnableRoundedCorners(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            int preference = DwmWindowCornerPreferenceRound;
            DwmSetWindowAttribute(handle, DwmwaWindowCornerPreference, ref preference, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // dwmapi.dll is always present on supported Windows versions.
        }
        catch (EntryPointNotFoundException)
        {
            // Windows 10: rounded corners are not available.
        }
        catch (Exception exception) when (exception is InvalidOperationException or COMException)
        {
            Logger.LogDebug(exception, "Rounded corners could not be enabled.");
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
