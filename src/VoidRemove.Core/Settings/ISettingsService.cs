using VoidRemove.Shared.Contracts;

namespace VoidRemove.Core.Settings;

/// <summary>
/// Owns the persisted settings: loading, validating, atomic saving and the registry mirror.
/// </summary>
public interface ISettingsService : ISettingsController
{
    /// <summary>Absolute path of the settings file.</summary>
    string SettingsFilePath { get; }

    /// <summary>The settings in memory. Call <see cref="LoadAsync"/> during startup first.</summary>
    AppSettings Current { get; }

    /// <summary>Reads the settings file. Missing or broken files fall back to defaults.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates, then writes the settings atomically with a backup of the previous file.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);

    /// <summary>Loads, applies <paramref name="mutate"/> and saves in one serialized step.</summary>
    Task UpdateAsync(Action<AppSettings> mutate, CancellationToken cancellationToken = default);
}
