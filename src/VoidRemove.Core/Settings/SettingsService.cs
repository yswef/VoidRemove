using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VoidRemove.Core.Security;
using VoidRemove.Core.Storage;
using VoidRemove.Shared;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Core.Settings;

/// <summary>
/// JSON settings with an optional <c>HKLM\Software\VoidRemove</c> mirror.
/// </summary>
/// <remarks>
/// Writes are atomic: the new document goes to <c>settings.json.tmp</c>, the current file
/// is copied to <c>settings.json.bak</c>, then the temporary file replaces the target. A
/// crash in the middle leaves either the old file or the backup behind, never a truncated
/// one. Reads that fail fall back to the backup and then to the factory defaults, and the
/// secret is always stored encrypted.
/// </remarks>
public sealed class SettingsService : ISettingsService
{
    private const string BackupExtension = ".bak";
    private const string TempExtension = ".tmp";

    private static readonly Encoding FileEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly AppPaths _paths;
    private readonly ISecretProtector _secrets;
    private readonly IKeyValueStore _mirror;
    private readonly ILocalizationService _localization;
    private readonly ILogger<SettingsService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private AppSettings _current = AppSettings.CreateDefault();

    public SettingsService(
        AppPaths paths,
        ISecretProtector secrets,
        IKeyValueStore mirror,
        ILocalizationService localization,
        ILogger<SettingsService>? logger = null)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _logger = logger ?? NullLogger<SettingsService>.Instance;
    }

    /// <inheritdoc />
    public string SettingsFilePath => _paths.SettingsFile;

    /// <inheritdoc />
    public AppSettings Current => _current;

    /// <inheritdoc />
    public SettingsSnapshot Snapshot => _current.ToSnapshot();

    /// <inheritdoc />
    public event EventHandler<SettingsSnapshot>? SettingsApplied;

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _current = await ReadAsync(_paths.SettingsFile, cancellationToken).ConfigureAwait(false)
                       ?? await ReadAsync(_paths.SettingsFile + BackupExtension, cancellationToken).ConfigureAwait(false)
                       ?? AppSettings.CreateDefault();

            return _current;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task<SettingsSnapshot> ReloadAsync(CancellationToken cancellationToken = default) =>
        ReloadCoreAsync(cancellationToken);

    /// <inheritdoc />
    public IReadOnlyList<string> Validate(SettingsSnapshot candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        AppSettings probe = _current.Clone();
        probe.ApplySnapshot(candidate);
        return SettingsValidator.Validate(probe, _localization);
    }

    /// <inheritdoc />
    public async Task SaveAsync(SettingsSnapshot candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        AppSettings updated = _current.Clone();
        updated.ApplySnapshot(candidate);
        await SaveAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        IReadOnlyList<string> problems = SettingsValidator.Validate(settings, _localization);
        if (problems.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", problems));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _paths.EnsureCreated();

            AppSettings toWrite = settings.Clone();
            toWrite.SchemaVersion = AppSettings.CurrentSchemaVersion;
            toWrite.VirusTotalApiKey = _secrets.Protect(toWrite.VirusTotalApiKey);

            string json = JsonSerializer.Serialize(toWrite, SettingsJson.Options);
            string target = _paths.SettingsFile;
            string temporary = target + TempExtension;

            await File.WriteAllTextAsync(temporary, json, FileEncoding, cancellationToken).ConfigureAwait(false);

            if (File.Exists(target))
            {
                File.Copy(target, target + BackupExtension, overwrite: true);
            }

            ReplaceAtomically(temporary, target);

            _current = settings.Clone();
            MirrorToRegistry(_current);
            SettingsApplied?.Invoke(this, _current.ToSnapshot());

            _logger.LogInformation("Settings saved to {Path} (mirror: {Mirror}).", target, _mirror.Description);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Action<AppSettings> mutate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        AppSettings draft;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            draft = _current.Clone();
            mutate(draft);
        }
        finally
        {
            _gate.Release();
        }

        await SaveAsync(draft, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SettingsSnapshot> RestoreDefaultsAsync(CancellationToken cancellationToken = default)
    {
        AppSettings defaults = AppSettings.CreateDefault();
        await SaveAsync(defaults, cancellationToken).ConfigureAwait(false);
        return defaults.ToSnapshot();
    }

    private async Task<SettingsSnapshot> ReloadCoreAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
        return Snapshot;
    }

    private async Task<AppSettings?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            AppSettings? parsed = JsonSerializer.Deserialize<AppSettings>(json, SettingsJson.Options);

            if (parsed is null)
            {
                _logger.LogWarning("Settings file {Path} is empty; falling back.", path);
                return null;
            }

            Migrate(parsed);
            parsed.VirusTotalApiKey = _secrets.Unprotect(parsed.VirusTotalApiKey);
            return parsed;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            _logger.LogError(exception, "Settings file {Path} could not be read; defaults will be used.", path);
            return null;
        }
    }

    /// <summary>Brings an older document up to the current schema.</summary>
    private void Migrate(AppSettings settings)
    {
        if (settings.SchemaVersion < 1)
        {
            settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
        }

        settings.DismissedRecommendations ??= new List<string>();

        if (string.IsNullOrWhiteSpace(settings.Language))
        {
            settings.Language = SettingsSnapshot.LanguageOptionDefault;
        }

        if (string.IsNullOrWhiteSpace(settings.AccentColor))
        {
            settings.AccentColor = SettingsSnapshot.DefaultAccent;
        }
    }

    private static void ReplaceAtomically(string temporary, string target)
    {
        if (File.Exists(target))
        {
            File.Replace(temporary, target, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temporary, target);
        }
    }

    /// <summary>
    /// Copies the machine relevant values to the registry. Best effort: without elevation
    /// this simply does nothing.
    /// </summary>
    private void MirrorToRegistry(AppSettings settings)
    {
        if (!_mirror.IsAvailable)
        {
            return;
        }

        _mirror.TrySetString("Language", settings.Language);
        _mirror.TrySetString("Theme", settings.Theme.ToString());
        _mirror.TrySetString("AccentColor", settings.AccentColor);
        _mirror.TrySetString("Layout", settings.Layout.ToString());
        _mirror.TrySetBool("AutoMonitorEnabled", settings.AutoMonitorEnabled);
        _mirror.TrySetBool("ExplorerContextMenuEnabled", settings.ExplorerContextMenuEnabled);
        _mirror.TrySetBool("CheckForUpdatesEnabled", settings.CheckForUpdatesEnabled);
        _mirror.TrySetBool("VirusTotalEnabled", settings.VirusTotalEnabled);
        _mirror.TrySetString("QuarantinePath", settings.QuarantinePath);
        _mirror.TrySetInt32("QuarantineRetentionDays", settings.QuarantineRetentionDays);
        _mirror.TrySetInt32("MaxConcurrentScans", settings.MaxConcurrentScans);
        _mirror.TrySetString("SchemaVersion", AppSettings.CurrentSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // The API key is never mirrored: it stays in the encrypted settings file only.
    }
}
