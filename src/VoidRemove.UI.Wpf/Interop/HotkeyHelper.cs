using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VoidRemove.UI.Wpf.Interop;

/// <summary>Global hotkey (Ctrl+Alt+U) registration through user32.</summary>
public static class HotkeyHelper
{
    public const int WmHotKey = 0x0312;

    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModNoRepeat = 0x4000;
    private const int VkU = 0x55;

    public const int HotkeyId = 9001;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>Registers Ctrl+Alt+U for the given window. Returns success.</summary>
    public static bool Register(IntPtr handle) =>
        RegisterHotKey(handle, HotkeyId, ModControl | ModAlt | ModNoRepeat, VkU);

    public static void Unregister(IntPtr handle) => UnregisterHotKey(handle, HotkeyId);

    /// <summary>Attaches a message hook; the callback fires on the hotkey.</summary>
    public static HwndSourceHook Hook(HwndSource source, Action onHotKey)
    {
        HwndSourceHook hook = (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            if (msg == WmHotKey && wParam.ToInt32() == HotkeyId)
            {
                handled = true;
                onHotKey();
            }

            return IntPtr.Zero;
        };

        source.AddHook(hook);
        return hook;
    }
}
