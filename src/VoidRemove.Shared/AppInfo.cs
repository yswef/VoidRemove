using System.Reflection;

namespace VoidRemove.Shared;

/// <summary>
/// The one and only place where the product identity is declared.
/// </summary>
/// <remarks>
/// Every window title, registry key, data folder, mutex, named pipe, AUMID, repository
/// link and docs link in the solution is derived from a constant below, so renaming the
/// product means editing this file and nothing else.
/// <para>
/// The non-code artefacts (Inno Setup script and the MkDocs site) read the generated
/// mirrors <c>installer/appinfo.ini</c> and <c>docs/appinfo.json</c>. Regenerate them
/// with <c>python tools/sync_appinfo.py</c> after editing this file; CI fails the build
/// when they are stale (<c>--check</c>).
/// </para>
/// <para>
/// Keep every member a <c>const string</c> built from string literals and other
/// constants: that is the shape <c>sync_appinfo.py</c> understands.
/// </para>
/// </remarks>
public static class AppInfo
{
    /// <summary>Stable machine identifier. Never localised, never renamed.</summary>
    public const string Id = "VoidRemove";

    /// <summary>Human readable product name shown in titles, the installer and the docs.</summary>
    public const string Name = "VoidRemove";

    /// <summary>One line description used on the splash screen and in the About page.</summary>
    public const string Tagline = "Remove programs completely — down to the last trace.";

    /// <summary>Publisher name written to the installer and to the uninstall entry.</summary>
    public const string Company = "VoidRemove Project";

    /// <summary>License identifier, also used by the NuGet metadata.</summary>
    public const string LicenseName = "MIT";

    /// <summary>Copyright line shown in the About page and embedded in the binaries.</summary>
    public const string CopyrightNotice = "Copyright © 2026 VoidRemove contributors";

    // ---- Online presence (only used by the two opt-in online features and the Help menu) ----

    public const string RepositoryOwner = "yswef";
    public const string RepositoryName = "VoidRemove";
    public const string RepositoryUrl = "https://github.com/" + RepositoryOwner + "/" + RepositoryName;
    public const string IssuesUrl = RepositoryUrl + "/issues";
    public const string DiscussionsUrl = RepositoryUrl + "/discussions";
    public const string ReleasesUrl = RepositoryUrl + "/releases";
    public const string NewIssueUrl = IssuesUrl + "/new/choose";
    public const string DocsUrl = "https://" + RepositoryOwner + ".github.io/" + RepositoryName + "/";
    public const string UserManualUrl = DocsUrl + "user/";
    public const string DeveloperManualUrl = DocsUrl + "developers/";

    /// <summary>Endpoint hit by "Check for updates". Only ever called when the user enabled it.</summary>
    public const string LatestReleaseApiUrl =
        "https://api.github.com/repos/" + RepositoryOwner + "/" + RepositoryName + "/releases/latest";

    // ---- Machine footprint ----

    /// <summary>Key under <c>HKLM</c> that holds the machine wide settings mirror.</summary>
    public const string RegistryRoot = @"Software\" + Id;

    /// <summary>Key written by the installer so Windows lists VoidRemove in "Apps &amp; features".</summary>
    public const string UninstallRegistryKey =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + Id;

    /// <summary>Folder name under <c>%ProgramData%</c> for logs, quarantine, backups and cache.</summary>
    public const string MachineDataFolderName = Id;

    /// <summary>Name of the mutex that guarantees a single running instance.</summary>
    public const string MutexName = @"Global\" + Id + ".SingleInstance";

    /// <summary>Name of the pipe used by a second instance to activate the running one.</summary>
    public const string NamedPipeName = Id + ".ipc";

    /// <summary>Application User Model ID used for taskbar grouping and notifications.</summary>
    public const string AppUserModelId = RepositoryOwner + "." + Id;

    /// <summary>Settings file name inside the machine data folder.</summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>Serilog rolling file name pattern inside the logs folder.</summary>
    public const string LogFilePattern = Id + "-.log";

    /// <summary>ProgId-style ids used by the Explorer context menu verbs.</summary>
    public const string ExplorerVerbUninstallId = Id + ".Uninstall";
    public const string ExplorerVerbResetId = Id + ".ResetApp";

    /// <summary>Command line switches understood by both front ends.</summary>
    public const string ArgUninstall = "--uninstall";
    public const string ArgResetApp = "--reset-app";
    public const string ArgActivate = "--activate";

    // ---- Runtime values (not const: they are read from the assembly) ----

    private static readonly Lazy<string> LazyVersion = new(ResolveVersion);

    /// <summary>Semantic version of the running assembly, without any source-revision suffix.</summary>
    public static string Version => LazyVersion.Value;

    /// <summary>Text used for the main window title.</summary>
    public static string WindowTitle => Name + " " + Version;

    /// <summary>User-Agent sent with the opt-in update and VirusTotal requests.</summary>
    public static string UserAgent => Name + "/" + Version + " (Windows; offline-first)";

    private static string ResolveVersion()
    {
        Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(AppInfo).Assembly;

        string? raw = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(raw))
        {
            System.Version? version = assembly.GetName().Version;
            raw = version is null ? null : string.Format(null, "{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return "0.0.0";
        }

        // InformationalVersion carries the source revision id after a '+'.
        int separator = raw.IndexOf('+');
        string trimmed = separator >= 0 ? raw.Substring(0, separator) : raw;
        return trimmed.Trim();
    }
}
