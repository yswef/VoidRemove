using VoidRemove.Shared;
using VoidRemove.Shared.Contracts;

namespace VoidRemove.Core.Storage;

/// <summary>
/// Every folder VoidRemove owns on this machine.
/// </summary>
/// <remarks>
/// The default root is <c>%ProgramData%\VoidRemove</c>: machine wide, writable by an
/// elevated process and independent from any user profile. Tests pass a temporary folder
/// instead, which is why the root is an argument rather than a constant.
/// </remarks>
public sealed class AppPaths : IDataLocations
{
    public AppPaths(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("A data root folder is required.", nameof(root));
        }

        Root = Path.GetFullPath(root);
    }

    /// <inheritdoc />
    public string Root { get; }

    /// <inheritdoc />
    public string Logs => Path.Combine(Root, "Logs");

    /// <inheritdoc />
    public string Quarantine => Path.Combine(Root, "Quarantine");

    /// <inheritdoc />
    public string Backups => Path.Combine(Root, "Backups");

    /// <inheritdoc />
    public string Cache => Path.Combine(Root, "Cache");

    /// <summary>Scratch space for exports, .reg dumps and ZIP snapshots in progress.</summary>
    public string Temp => Path.Combine(Root, "Temp");

    /// <summary>Optional drop folder for user supplied localization overrides.</summary>
    public string Languages => Path.Combine(Root, "lang");

    /// <inheritdoc />
    public string SettingsFile => Path.Combine(Root, AppInfo.SettingsFileName);

    /// <summary>The production location: <c>%ProgramData%\VoidRemove</c>.</summary>
    public static AppPaths Default { get; } = new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppInfo.MachineDataFolderName));

    /// <summary>Creates the data folders. Safe to call repeatedly.</summary>
    public void EnsureCreated()
    {
        foreach (string directory in EnumerateRequiredDirectories())
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>Every folder <see cref="EnsureCreated"/> guarantees.</summary>
    public IEnumerable<string> EnumerateRequiredDirectories()
    {
        yield return Root;
        yield return Logs;
        yield return Quarantine;
        yield return Backups;
        yield return Cache;
        yield return Temp;
    }

    /// <summary>True when <paramref name="candidate"/> resolves to a location inside <see cref="Root"/>.</summary>
    public bool IsInside(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        string full;
        try
        {
            full = Path.GetFullPath(candidate);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }

        if (string.Equals(full, Root, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string prefix = Root.EndsWith(Path.DirectorySeparatorChar)
            ? Root
            : Root + Path.DirectorySeparatorChar;

        return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override string ToString() => Root;
}
