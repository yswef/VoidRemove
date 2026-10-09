namespace VoidRemove.Core.Health;

/// <summary>A file entry offered to the detector.</summary>
public sealed record FileEntry(string Path, long SizeBytes, DateTime LastWriteUtc);

/// <summary>
/// Flags installer artefacts (setup packages, MSI files, extracted installers) that linger
/// in temporary and download folders. Pure and unit tested.
/// </summary>
public static class RedundantFileDetector
{
    private static readonly string[] InstallerNameMarkers =
    {
        "setup", "install", "unins", "redist", "websetup", "offline", "installer",
    };

    private static readonly string[] InstallerExtensions = { ".msi", ".exe", ".msix", ".appx", ".cab" };

    /// <summary>Returns the entries that look like leftover installation files.</summary>
    public static IReadOnlyList<FileEntry> Detect(IEnumerable<FileEntry> files, int olderThanDays = 0)
    {
        DateTime cutoff = DateTime.UtcNow.AddDays(-olderThanDays);

        return files
            .Where(file => IsInstallerArtefact(file) && file.LastWriteUtc <= cutoff)
            .OrderByDescending(static f => f.SizeBytes)
            .ToList();
    }

    public static bool IsInstallerArtefact(FileEntry file)
    {
        string name = System.IO.Path.GetFileName(file.Path).ToLowerInvariant();
        string extension = System.IO.Path.GetExtension(file.Path).ToLowerInvariant();

        if (extension == ".msi" || extension == ".msix" || extension == ".appx")
        {
            return true;
        }

        bool hasMarker = InstallerNameMarkers.Any(name.Contains);
        bool hasExeExtension = extension is ".exe" or ".cab";
        return hasMarker && hasExeExtension;
    }
}
