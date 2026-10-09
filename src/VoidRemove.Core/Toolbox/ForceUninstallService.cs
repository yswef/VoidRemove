using Microsoft.Extensions.Logging;
using VoidRemove.Core.Engine;

namespace VoidRemove.Core.Toolbox;

/// <summary>
/// Force Uninstall: stops the program's processes first, then runs its uninstaller. Used when
/// a normal removal is blocked by running processes or locked files.
/// </summary>
public sealed class ForceUninstallService
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<ForceUninstallService>? _logger;

    public ForceUninstallService(IProcessRunner processRunner, ILogger<ForceUninstallService>? logger = null)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    public bool IsSupported => System.OperatingSystem.IsWindows();

    /// <summary>Kills the listed processes, then launches the uninstaller.</summary>
    public async Task<ProcessResult> ForceAsync(
        IReadOnlyList<string> processNames,
        string uninstallExecutable,
        string uninstallArguments,
        CancellationToken cancellationToken = default)
    {
        if (IsSupported)
        {
            foreach (string processName in processNames)
            {
                KillProcess(processName);
            }
        }

        return await _processRunner.RunAsync(uninstallExecutable, uninstallArguments, TimeSpan.FromMinutes(10), cancellationToken);
    }

    private void KillProcess(string processName)
    {
        string clean = System.IO.Path.GetFileNameWithoutExtension(processName);
        try
        {
            foreach (System.Diagnostics.Process process in System.Diagnostics.Process.GetProcessesByName(clean))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(2000);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _logger?.LogDebug(exception, "Could not kill {Process}.", clean);
        }
    }
}
