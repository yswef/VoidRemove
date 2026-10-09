using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.Logging;

namespace VoidRemove.Core.Monitor;

/// <summary>
/// Watches installations. Manual mode brackets a drag-and-drop run with two snapshots;
/// automatic mode subscribes to WMI process-start events and tracks installer lifetimes.
/// </summary>
public sealed class InstallMonitor : IDisposable
{
    private readonly ISnapshotProvider _provider;
    private readonly IInstallMonitorStore _store;
    private readonly ILogger<InstallMonitor>? _logger;

    private SystemSnapshot? _manualBefore;
    private DateTime _manualStart;
    private ManagementEventWatcher? _watcher;

    public InstallMonitor(ISnapshotProvider provider, IInstallMonitorStore store, ILogger<InstallMonitor>? logger = null)
    {
        _provider = provider;
        _store = store;
        _logger = logger;
    }

    public bool IsSupported => _provider.IsSupported;

    public bool IsAutoRunning => _watcher is not null;

    // ------------------------------------------------------- manual

    /// <summary>Takes the "before" snapshot for a manual (drag-and-drop) install.</summary>
    public void BeginManual()
    {
        _manualBefore = _provider.IsSupported ? _provider.Capture() : SystemSnapshot.Empty;
        _manualStart = DateTime.UtcNow;
    }

    /// <summary>Takes the "after" snapshot and records what the installer added.</summary>
    public async Task<InstallLog?> EndManualAsync(string processName, CancellationToken cancellationToken = default)
    {
        if (_manualBefore is null)
        {
            return null;
        }

        SystemSnapshot before = _manualBefore;
        DateTime started = _manualStart;
        _manualBefore = null;

        SystemSnapshot after = _provider.IsSupported ? _provider.Capture(cancellationToken) : SystemSnapshot.Empty;
        SnapshotDiff diff = SnapshotDiff.Compute(before, after);
        if (diff.IsEmpty)
        {
            return null;
        }

        InstallLog log = InstallLog.FromDiff(processName, started, DateTime.UtcNow, automatic: false, diff);
        await _store.AddAsync(log, cancellationToken);
        return log;
    }

    // ------------------------------------------------------- automatic

    /// <summary>Subscribes to installer process starts via WMI.</summary>
    public void StartAuto()
    {
        if (!IsSupported || _watcher is not null)
        {
            return;
        }

        try
        {
            _watcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            _watcher.EventArrived += OnProcessStarted;
            _watcher.Start();
            _logger?.LogInformation("Automatic install monitoring started.");
        }
        catch (Exception exception) when (exception is ManagementException or PlatformNotSupportedException or System.Runtime.InteropServices.COMException)
        {
            _logger?.LogWarning(exception, "Could not start the WMI install watcher.");
            _watcher = null;
        }
    }

    public void StopAuto()
    {
        if (_watcher is null)
        {
            return;
        }

        _watcher.Stop();
        _watcher.Dispose();
        _watcher = null;
        _logger?.LogInformation("Automatic install monitoring stopped.");
    }

    private void OnProcessStarted(object sender, EventArrivedEventArgs e)
    {
        string name = e.NewEvent["ProcessName"]?.ToString() ?? string.Empty;
        if (!IsInstallerLike(name))
        {
            return;
        }

        int processId = Convert.ToInt32(e.NewEvent["ProcessID"]);
        _ = TrackInstallerAsync(name, processId);
    }

    private async Task TrackInstallerAsync(string name, int processId)
    {
        try
        {
            SystemSnapshot before = _provider.Capture();
            DateTime started = DateTime.UtcNow;

            try
            {
                using Process process = Process.GetProcessById(processId);
                await process.WaitForExitAsync();
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
            }

            SystemSnapshot after = _provider.Capture();
            SnapshotDiff diff = SnapshotDiff.Compute(before, after);
            if (diff.IsEmpty)
            {
                return;
            }

            InstallLog log = InstallLog.FromDiff(name, started, DateTime.UtcNow, automatic: true, diff);
            await _store.AddAsync(log);
            _logger?.LogInformation("Recorded automatic installation of {Name}.", name);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger?.LogDebug(exception, "Automatic tracking of {Name} failed.", name);
        }
    }

    internal static bool IsInstallerLike(string processName)
    {
        string lower = processName.ToLowerInvariant();
        return lower.Contains("msiexec") || lower.Contains("setup") ||
               lower.Contains("install") || lower.Contains("unins");
    }

    public void Dispose() => StopAuto();
}
