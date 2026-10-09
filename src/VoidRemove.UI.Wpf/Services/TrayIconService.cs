using System.Windows.Forms;

namespace VoidRemove.UI.Wpf.Services;

/// <summary>
/// The tray (notification area) icon with a small menu. Shown while Auto Monitor is on or
/// when the window is minimized, so monitoring survives without a taskbar button.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private NotifyIcon? _icon;

    public bool IsVisible => _icon?.Visible == true;

    public void Show(string tooltip, string openLabel, string monitorLabel, bool monitorOn, string exitLabel,
        Action onOpen, Action<bool> onToggleMonitor, Action onExit)
    {
        if (_icon is not null)
        {
            return;
        }

        _icon = new NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty)
                   ?? System.Drawing.SystemIcons.Application,
            Text = tooltip,
            Visible = true,
        };

        ToolStripMenuItem open = new(openLabel, null, (_, _) => onOpen());
        ToolStripMenuItem monitor = new(monitorLabel, null, (_, _) => onToggleMonitor(!monitorOn));
        ToolStripMenuItem exit = new(exitLabel, null, (_, _) => onExit());

        ContextMenuStrip menu = new();
        menu.Items.AddRange(new ToolStripItem[] { open, monitor, new ToolStripSeparator(), exit });
        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, _) => onOpen();
    }

    public void Hide()
    {
        _icon?.Dispose();
        _icon = null;
    }

    public void Dispose() => Hide();
}
