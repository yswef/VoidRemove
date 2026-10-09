using System.Text.Json;
using VoidRemove.Core.Residual;
using VoidRemove.Core.Storage;

namespace VoidRemove.Core.Safety;

/// <summary>
/// The permanent never-delete list. Anything whose normalized path or registry key starts
/// with an entry here is skipped by every removal operation, no matter the safety level.
/// A built-in floor of shared system locations is always enforced on top of user entries.
/// </summary>
public sealed class ExclusionList
{
    /// <summary>
    /// The always-enforced floor of protected locations, computed from the real system
    /// folders and normalized exactly like candidate paths so the prefix match lines up
    /// (a bare token such as "windows" would never match the compacted "cwindowssystem32").
    /// </summary>
    private static readonly Lazy<string[]> BuiltIn = new(static () =>
    {
        List<string> roots = new();

        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string compact = TextNormalizer.Compact(path);
            if (compact.Length > 0)
            {
                roots.Add(compact);
            }
        }

        Add(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));

        string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (!string.IsNullOrWhiteSpace(programData))
        {
            // Only the shared Microsoft store under ProgramData, not every app's data folder.
            Add(Path.Combine(programData, "Microsoft"));
        }

        string publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        if (!string.IsNullOrWhiteSpace(publicDesktop))
        {
            Add(Path.GetDirectoryName(publicDesktop.TrimEnd(Path.DirectorySeparatorChar)));
        }

        try
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                string root = drive.RootDirectory.FullName;
                Add(Path.Combine(root, "$Recycle.Bin"));
                Add(Path.Combine(root, "System Volume Information"));
            }
        }
        catch (IOException)
        {
            // Enumerating drives is best effort; the fixed folders above still apply.
        }

        return roots.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    });

    private readonly AppPaths _paths;
    private readonly HashSet<string> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ExclusionList(AppPaths paths) => _paths = paths;

    private string FilePath => Path.Combine(_paths.Root, "exclusions.json");

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _entries.Clear();
            if (File.Exists(FilePath))
            {
                string json = await File.ReadAllTextAsync(FilePath, cancellationToken);
                foreach (string entry in JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>())
                {
                    _entries.Add(TextNormalizerCompact(entry));
                }
            }
        }
        catch (JsonException)
        {
            _entries.Clear();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(string pathOrKey, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _entries.Add(TextNormalizerCompact(pathOrKey));
            await SaveLockedAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(string pathOrKey, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _entries.Remove(TextNormalizerCompact(pathOrKey));
            await SaveLockedAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public IReadOnlyList<string> Entries
    {
        get
        {
            _gate.Wait();
            try
            {
                return _entries.OrderBy(static e => e, StringComparer.OrdinalIgnoreCase).ToList();
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    /// <summary>True when the candidate must never be touched.</summary>
    public bool IsExcluded(string pathOrKey)
    {
        string compact = TextNormalizerCompact(pathOrKey);
        if (compact.Length == 0)
        {
            return true;
        }

        _gate.Wait();
        try
        {
            return _entries.Any(entry => compact.StartsWith(entry, StringComparison.OrdinalIgnoreCase)) ||
                   BuiltIn.Value.Any(entry => compact.StartsWith(entry, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveLockedAsync(CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(
            _entries.OrderBy(static e => e, StringComparer.OrdinalIgnoreCase).ToList(),
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(FilePath, json, cancellationToken);
    }

    private static string TextNormalizerCompact(string value) => TextNormalizer.Compact(value);
}
