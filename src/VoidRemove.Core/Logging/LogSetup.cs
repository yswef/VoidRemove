using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using VoidRemove.Core.Storage;
using VoidRemove.Shared;

namespace VoidRemove.Core.Logging;

/// <summary>Builds the application wide Serilog pipeline.</summary>
/// <remarks>
/// Logs live in <c>%ProgramData%\VoidRemove\Logs</c> and roll daily. If that folder cannot
/// be created — a non elevated first run, a locked volume — logging falls back to
/// <c>%LocalAppData%\VoidRemove\Logs</c> instead of starting without diagnostics.
/// </remarks>
public static class LogSetup
{
    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}";

    /// <summary>Creates a logger factory that writes rolling files under <paramref name="paths"/>.</summary>
    public static ILoggerFactory CreateLoggerFactory(AppPaths paths, LogEventLevel minimumLevel = LogEventLevel.Information) =>
        new SerilogLoggerFactory(CreateSerilogLogger(paths, minimumLevel), dispose: true);

    /// <summary>Builds the Serilog pipeline itself, for hosts that own their logger factory.</summary>
    public static Serilog.Core.Logger CreateSerilogLogger(AppPaths paths, LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(paths);

        string folder = ResolveLogFolder(paths);

        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", AppInfo.Name)
            .Enrich.WithProperty("ApplicationVersion", AppInfo.Version)
            .WriteTo.File(
                path: Path.Combine(folder, AppInfo.LogFilePattern),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: OutputTemplate)
            .CreateLogger();
    }

    /// <summary>The folder logs are written to, creating it if needed.</summary>
    public static string ResolveLogFolder(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        try
        {
            paths.EnsureCreated();
            return paths.Logs;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            string fallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppInfo.MachineDataFolderName,
                "Logs");

            try
            {
                Directory.CreateDirectory(fallback);
                return fallback;
            }
            catch (Exception)
            {
                // Nothing left to try: Serilog will report the failure itself.
                return paths.Logs;
            }
        }
    }

    /// <summary>Newest log file, used by "Open log folder" and by bug reports.</summary>
    public static string? FindNewestLogFile(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        try
        {
            DirectoryInfo directory = new(paths.Logs);
            if (!directory.Exists)
            {
                return null;
            }

            return directory
                .EnumerateFiles("*.log")
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => file.FullName)
                .FirstOrDefault();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
