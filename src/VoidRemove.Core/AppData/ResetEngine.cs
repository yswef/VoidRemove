using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VoidRemove.Core.Storage;

namespace VoidRemove.Core.AppData;

/// <summary>One selected data location inside a reset plan.</summary>
public sealed record ResetItem
{
    public required string Path { get; init; }

    public bool IsDirectory { get; init; }

    public long SizeBytes { get; init; }
}

/// <summary>The computed, confirmable reset plan.</summary>
public sealed record ResetPlan
{
    public List<ResetItem> Items { get; init; } = new();

    public bool DryRun { get; init; }

    public long TotalBytes => Items.Sum(static i => i.SizeBytes);
}

/// <summary>The outcome of executing a reset plan.</summary>
public sealed record ResetResult
{
    public string? BackupZipPath { get; init; }

    public int RemovedCount { get; init; }

    public bool WasDryRun { get; init; }
}

/// <summary>
/// Data-only reset: backs the selected locations up into a single ZIP (with a manifest so
/// they can be restored), then removes them. Dry-run computes without touching anything.
/// </summary>
public sealed class ResetEngine
{
    private const string ManifestEntry = "manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly AppPaths _paths;
    private readonly ILogger<ResetEngine>? _logger;

    public ResetEngine(AppPaths paths, ILogger<ResetEngine>? logger = null)
    {
        _paths = paths;
        _logger = logger;
    }

    /// <summary>Turns the resolved selection into a plan, dropping registry/missing items.</summary>
    public ResetPlan BuildPlan(IReadOnlyList<ResolvedLocation> selected, bool dryRun)
    {
        ResetPlan plan = new() { DryRun = dryRun };

        foreach (ResolvedLocation location in selected)
        {
            if (location.IsRegistry || !location.Exists)
            {
                continue;
            }

            plan.Items.Add(new ResetItem
            {
                Path = location.Path,
                IsDirectory = Directory.Exists(location.Path),
                SizeBytes = location.SizeBytes,
            });
        }

        return plan;
    }

    /// <summary>Executes the plan; returns the backup path so the UI can offer restore.</summary>
    public async Task<ResetResult> ExecuteAsync(ResetPlan plan, string label, CancellationToken cancellationToken = default)
    {
        if (plan.DryRun)
        {
            return new ResetResult { WasDryRun = true, RemovedCount = plan.Items.Count };
        }

        string backupDirectory = Path.Combine(_paths.Backups, "reset");
        Directory.CreateDirectory(backupDirectory);
        string zipPath = Path.Combine(backupDirectory, $"{Sanitize(label)}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.zip");

        await Task.Run(() => WriteBackupZip(zipPath, plan.Items), cancellationToken);

        int removed = 0;
        foreach (ResetItem item in plan.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Delete(item.Path, item.IsDirectory))
            {
                removed++;
            }
        }

        _logger?.LogInformation("Reset of {Label} removed {Count} item(s), backup at {Zip}.", label, removed, zipPath);
        return new ResetResult { BackupZipPath = zipPath, RemovedCount = removed };
    }

    /// <summary>Restores a previously created backup ZIP to its original locations.</summary>
    public async Task<int> RestoreAsync(string zipPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(zipPath))
        {
            return 0;
        }

        return await Task.Run(() =>
        {
            using ZipArchive archive = ZipFile.OpenRead(zipPath);
            ZipArchiveEntry? manifestEntry = archive.GetEntry(ManifestEntry);
            if (manifestEntry is null)
            {
                return 0;
            }

            List<ResetItem> items;
            using (StreamReader reader = new(manifestEntry.Open()))
            {
                items = JsonSerializer.Deserialize<List<ResetItem>>(reader.ReadToEnd()) ?? new List<ResetItem>();
            }

            int restored = 0;
            for (int index = 0; index < items.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ResetItem item = items[index];
                string prefix = index + "/";

                foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.FullName.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    string relative = entry.FullName.Substring(prefix.Length);
                    if (relative.Length == 0)
                    {
                        continue;
                    }

                    string target = Path.Combine(item.Path, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    entry.ExtractToFile(target, overwrite: true);
                    restored++;
                }
            }

            return restored;
        }, cancellationToken);
    }

    // ------------------------------------------------------- internals

    private static void WriteBackupZip(string zipPath, List<ResetItem> items)
    {
        using ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        for (int index = 0; index < items.Count; index++)
        {
            ResetItem item = items[index];
            string prefix = index + "/";

            if (item.IsDirectory && Directory.Exists(item.Path))
            {
                foreach (string file in Directory.EnumerateFiles(item.Path, "*", SearchOption.AllDirectories))
                {
                    string relative = Path.GetRelativePath(item.Path, file);
                    archive.CreateEntryFromFile(file, prefix + relative.Replace('\\', '/'));
                }
            }
            else if (File.Exists(item.Path))
            {
                archive.CreateEntryFromFile(item.Path, prefix + Path.GetFileName(item.Path));
            }
        }

        ZipArchiveEntry manifest = archive.CreateEntry(ManifestEntry);
        using StreamWriter writer = new(manifest.Open());
        writer.Write(JsonSerializer.Serialize(items, JsonOptions));
    }

    private static bool Delete(string path, bool isDirectory)
    {
        try
        {
            if (isDirectory)
            {
                Directory.Delete(path, recursive: true);
            }
            else
            {
                File.Delete(path);
            }

            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string Sanitize(string label)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string cleaned = new(label.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "reset" : cleaned;
    }
}
