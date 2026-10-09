namespace VoidRemove.Core.Engine;

/// <summary>The outcome of launching an external uninstaller.</summary>
public sealed record ProcessResult(int ExitCode, bool TimedOut, string Error)
{
    public bool LooksSuccessful => !TimedOut && ExitCode is 0 or 3010 or 1641;
}

/// <summary>Abstraction over launching uninstallers so the engine is testable.</summary>
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken cancellationToken = default);
}

/// <summary>Runs the uninstaller as a real process and waits for it.</summary>
public sealed class WindowsProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (!System.OperatingSystem.IsWindows())
        {
            return new ProcessResult(-1, false, "not supported on this platform");
        }

        using System.Diagnostics.Process process = new();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };

        try
        {
            process.Start();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            return new ProcessResult(-1, false, exception.Message);
        }

        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        });

        Task exitTask = process.WaitForExitAsync(CancellationToken.None);
        Task finished = await Task.WhenAny(exitTask, Task.Delay(timeout, CancellationToken.None));

        if (finished != exitTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            return new ProcessResult(-1, true, "the uninstaller did not finish in time");
        }

        string error = await process.StandardError.ReadToEndAsync(cancellationToken);
        return new ProcessResult(process.ExitCode, false, error);
    }
}
