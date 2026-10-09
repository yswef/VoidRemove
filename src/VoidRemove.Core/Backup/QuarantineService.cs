using System.Text.Json;
using Microsoft.Extensions.Logging;
using VoidRemove.Core.Storage;

namespace VoidRemove.Core.Backup;

/// <summary>
/// Moves leftovers into a restorable quarantine instead of deleting them outright. Every
/// session writes a manifest so the user can undo a removal, and expired sessions can be
/// purged according to the retention setting.
/// </summary>
public sealed class QuarantineService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly AppPaths _paths;
    private readonly ILogger<QuarantineService>? _logger;

    public QuarantineService(AppPaths paths, ILogger<QuarantineService>? logger = null)
    {
        _paths = paths;
        _logger = logger;
    }

    /// <summary>Quarantines the given paths; missing ones are skipped.</summary>
    public async Task<QuarantineManifest> QuarantineAsync(
        IReadOnlyList<string> paths,
        string label,
        CancellationToken cancellationToken = default)
    {
        string id = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Sanitize(label)}";
        string sessionRoot = Path.Combine(_paths.Quarantine, id);
        Directory.CreateDirectory(sessionRoot);

        QuarantineManifest manifest = new()
        {
            Id = id,
            CreatedUtc = DateTime.UtcNow,
            Label = label,
        };

        foreach (string original in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            QuarantineItem? item = MoveInto(original, sessionRoot);
            if (item is not null)
            {
                manifest.Items.Add(item);
            }
        }

        string manifestPath = Path.Combine(sessionRoot, QuarantineManifest.ManifestFileName);
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken);

        _logger?.LogInformation("Quarantined {Count} item(s) under {Id}.", manifest.Items.Count, id);
        return manifest;
    }

    /// <summary>Restores a session's items to their original locations.</summary>
    public async Task<int> RestoreAsync(string manifestId, CancellationToken cancellationToken = default)
    {
        QuarantineManifest? manifest = await ReadManifestAsync(manifestId, cancellationToken);
        if (manifest is null)
        {
            return 0;
        }

        int restored = 0;
        foreach (QuarantineItem item in manifest.Items)
        {
            if (!Directory.Exists(item.StoredPath) && !File.Exists(item.StoredPath))
            {
                continue;
            }

            string? parent = Path.GetDirectoryName(item.OriginalPath);
            if (parent is not null)
            {
                Directory.CreateDirectory(parent);
            }

            MoveRobust(item.StoredPath, item.OriginalPath, item.IsDirectory);
            restored++;
        }

        DeleteSession(manifestId);
        return restored;
    }

    /// <summary>Permanently deletes a quarantine session.</summary>
    public void Purge(string manifestId) => DeleteSession(manifestId);

    /// <summary>Lists every quarantine session manifest.</summary>
    public async Task<IReadOnlyList<QuarantineManifest>> ListAsync(CancellationToken cancellationToken = default)
    {
        List<QuarantineManifest> manifests = new();
        if (!Directory.Exists(_paths.Quarantine))
        {
            return manifests;
        }

        foreach (string directory in Directory.EnumerateDirectories(_paths.Quarantine))
        {
            cancellationToken.ThrowIfCancellationRequested();
            QuarantineManifest? manifest = await ReadManifestAsync(Path.GetFileName(directory), cancellationToken);
            if (manifest is not null)
            {
                manifests.Add(manifest);
            }
        }

        return manifests.OrderByDescending(static m => m.CreatedUtc).ToList();
    }

    /// <summary>Deletes sessions older than the retention window.</summary>
    public async Task<int> PruneExpiredAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        int purged = 0;

        foreach (QuarantineManifest manifest in await ListAsync(cancellationToken))
        {
            if (manifest.CreatedUtc < cutoff)
            {
                DeleteSession(manifest.Id);
                purged++;
            }
        }

        return purged;
    }

    // ------------------------------------------------------- internals

    private QuarantineItem? MoveInto(string original, string sessionRoot)
    {
        bool isDirectory = Directory.Exists(original);
        if (!isDirectory && !File.Exists(original))
        {
            return null;
        }

        string name = Path.GetFileName(original.TrimEnd('\\', '/'));
        string stored = Path.Combine(sessionRoot, name);
        stored = EnsureUnique(stored);

        MoveRobust(original, stored, isDirectory);

        return new QuarantineItem
        {
            OriginalPath = original,
            StoredPath = stored,
            IsDirectory = isDirectory,
            SizeBytes = isDirectory ? DirectorySize(stored) : FileSize(stored),
        };
    }

    private static string EnsureUnique(string target)
    {
        if (!Directory.Exists(target) && !File.Exists(target))
        {
            return target;
        }

        string directory = Path.GetDirectoryName(target) ?? string.Empty;
        string name = Path.GetFileName(target);
        for (int suffix = 1; suffix < 1000; suffix++)
        {
            string candidate = Path.Combine(directory, $"{name} ({suffix})");
            if (!Directory.Exists(candidate) && !File.Exists(candidate))
            {
                return candidate;
            }
        }

        return target + "-" + Guid.NewGuid().ToString("N");
    }

    private static void MoveRobust(string source, string destination, bool isDirectory)
    {
        try
        {
            if (isDirectory)
            {
                Directory.Move(source, destination);
            }
            else
            {
                File.Move(source, destination);
            }
        }
        catch (IOException)
        {
            // Cross-volume: fall back to copy + delete.
            if (isDirectory)
            {
                CopyDirectory(source, destination);
                Directory.Delete(source, recursive: true);
            }
            else
            {
                File.Copy(source, destination, overwrite: true);
                File.Delete(source);
            }
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, file);
            string target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private async Task<QuarantineManifest?> ReadManifestAsync(string manifestId, CancellationToken cancellationToken)
    {
        string manifestPath = Path.Combine(_paths.Quarantine, manifestId, QuarantineManifest.ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            return JsonSerializer.Deserialize<QuarantineManifest>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void DeleteSession(string manifestId)
    {
        string sessionRoot = Path.Combine(_paths.Quarantine, manifestId);
        if (Directory.Exists(sessionRoot))
        {
            Directory.Delete(sessionRoot, recursive: true);
        }
    }

    internal static long FileSize(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    internal static long DirectorySize(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Sum(static file => FileSize(file));
        }
        catch (IOException)
        {
            return 0;
        }
    }

    private static string Sanitize(string label)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string cleaned = new(label.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "quarantine" : cleaned;
    }
}
