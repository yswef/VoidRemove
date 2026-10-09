using System.Text.Json;
using VoidRemove.Core.Storage;

namespace VoidRemove.Core.Monitor;

/// <summary>Persists recorded installations under the data root.</summary>
public interface IInstallMonitorStore
{
    Task AddAsync(InstallLog log, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallLog>> ListAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

public sealed class JsonInstallMonitorStore : IInstallMonitorStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly AppPaths _paths;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonInstallMonitorStore(AppPaths paths) => _paths = paths;

    private string FilePath => Path.Combine(_paths.Root, "monitor", "installations.json");

    public async Task AddAsync(InstallLog log, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            List<InstallLog> logs = await ReadLockedAsync(cancellationToken);
            logs.Insert(0, log);

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(logs, JsonOptions), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<InstallLog>> ListAsync(CancellationToken cancellationToken = default)
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

    private async Task<List<InstallLog>> ReadLockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(FilePath))
        {
            return new List<InstallLog>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(FilePath, cancellationToken);
            return JsonSerializer.Deserialize<List<InstallLog>>(json) ?? new List<InstallLog>();
        }
        catch (JsonException)
        {
            return new List<InstallLog>();
        }
    }
}
