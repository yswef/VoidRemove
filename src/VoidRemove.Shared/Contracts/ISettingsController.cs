namespace VoidRemove.Shared.Contracts;

/// <summary>Reads and writes the persisted settings on behalf of the view models.</summary>
public interface ISettingsController
{
    /// <summary>The settings currently stored on disk (or the defaults on first run).</summary>
    SettingsSnapshot Snapshot { get; }

    /// <summary>Reloads the settings from disk, discarding any in-memory draft.</summary>
    Task<SettingsSnapshot> ReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates without writing. An empty list means the snapshot can be saved.</summary>
    IReadOnlyList<string> Validate(SettingsSnapshot candidate);

    /// <summary>Writes the snapshot atomically and mirrors it to the registry.</summary>
    Task SaveAsync(SettingsSnapshot candidate, CancellationToken cancellationToken = default);

    /// <summary>Restores every setting to its factory default and saves it.</summary>
    Task<SettingsSnapshot> RestoreDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>Raised after a successful save or restore.</summary>
    event EventHandler<SettingsSnapshot>? SettingsApplied;
}
