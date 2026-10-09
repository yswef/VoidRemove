using System.Text.Json;
using VoidRemove.Core.Storage;

namespace VoidRemove.Core.History;

/// <summary>Persists uninstall reports so "Logged" and history views have real data.</summary>
public interface IUninstallHistoryStore
{
    Task AddAsync(UninstallReport report, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UninstallReport>> ListAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>Append-only JSON history under the data root.</summary>
public sealed class JsonUninstallHistoryStore : IUninstallHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly AppPaths _paths;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonUninstallHistoryStore(AppPaths paths) => _paths = paths;

    private string FilePath => Path.Combine(_paths.Root, "history", "uninstall-history.json");

    public async Task AddAsync(UninstallReport report, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            List<UninstallReport> reports = await ReadLockedAsync(cancellationToken);
            reports.Insert(0, report);

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(reports, JsonOptions), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<UninstallReport>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadLockedAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<UninstallReport>> ReadLockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(FilePath))
        {
            return new List<UninstallReport>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(FilePath, cancellationToken);
            return JsonSerializer.Deserialize<List<UninstallReport>>(json) ?? new List<UninstallReport>();
        }
        catch (JsonException)
        {
            return new List<UninstallReport>();
        }
    }
}
