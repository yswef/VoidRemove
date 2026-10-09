using System.Text.Json;
using VoidRemove.Core.Security;
using VoidRemove.Core.Settings;
using VoidRemove.Core.Storage;
using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;
using VoidRemove.Shared.Models;
using VoidRemove.Shared.Theming;
using Xunit;

namespace VoidRemove.Core.Tests;

/// <summary>
/// Persistence rules: defaults on first run, atomic writes, a backup of the previous file,
/// graceful recovery from a broken file and never a secret in clear text.
/// </summary>
public sealed class SettingsServiceTests
{
    [Fact]
    public async Task MissingFileYieldsDefaults()
    {
        using TempDirectory temp = new();
        SettingsService service = CreateService(new AppPaths(temp.FullPath));

        AppSettings loaded = await service.LoadAsync();

        Assert.Equal(AppSettings.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.Equal(AppTheme.Dark, loaded.Theme);
        Assert.Equal(SettingsSnapshot.DefaultAccent, loaded.AccentColor);
        Assert.False(loaded.VirusTotalEnabled);
        Assert.False(loaded.CheckForUpdatesEnabled);
        Assert.False(loaded.AutoMonitorEnabled);
    }

    [Fact]
    public async Task SaveThenLoadRoundTrips()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);

        AppSettings original = AppSettings.CreateDefault();
        original.Theme = AppTheme.Neon;
        original.AccentColor = "#7B61FF";
        original.Language = "ar";
        original.Layout = LayoutMode.Cards;
        original.DefaultUninstallBehavior = UninstallBehavior.Silent;
        original.ResidualSafetyCeiling = SafetyLevel.Safe;
        original.QuarantineRetentionDays = 7;
        original.MaxConcurrentScans = 8;
        original.AutoMonitorEnabled = true;
        original.ExplorerContextMenuEnabled = true;
        original.DryRunByDefault = true;
        original.DismissedRecommendations.Add("bundleware.sample");

        await CreateService(paths).SaveAsync(original);
        AppSettings loaded = await CreateService(paths).LoadAsync();

        Assert.Equal(AppTheme.Neon, loaded.Theme);
        Assert.Equal("#7B61FF", loaded.AccentColor);
        Assert.Equal("ar", loaded.Language);
        Assert.Equal(LayoutMode.Cards, loaded.Layout);
        Assert.Equal(UninstallBehavior.Silent, loaded.DefaultUninstallBehavior);
        Assert.Equal(SafetyLevel.Safe, loaded.ResidualSafetyCeiling);
        Assert.Equal(7, loaded.QuarantineRetentionDays);
        Assert.Equal(8, loaded.MaxConcurrentScans);
        Assert.True(loaded.AutoMonitorEnabled);
        Assert.True(loaded.ExplorerContextMenuEnabled);
        Assert.True(loaded.DryRunByDefault);
        Assert.Contains("bundleware.sample", loaded.DismissedRecommendations);
    }

    [Fact]
    public async Task TheSecretIsNeverWrittenInPlainText()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        const string apiKey = "vt-super-secret-key";

        AppSettings settings = AppSettings.CreateDefault();
        settings.VirusTotalEnabled = true;
        settings.VirusTotalApiKey = apiKey;

        SettingsService service = CreateService(paths);
        await service.SaveAsync(settings);

        string json = await File.ReadAllTextAsync(paths.SettingsFile);

        Assert.DoesNotContain(apiKey, json, StringComparison.Ordinal);
        Assert.Contains("test:", json, StringComparison.Ordinal);

        AppSettings loaded = await CreateService(paths).LoadAsync();
        Assert.Equal(apiKey, loaded.VirusTotalApiKey);
    }

    [Fact]
    public async Task ACorruptFileFallsBackToDefaultsInsteadOfThrowing()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        Directory.CreateDirectory(paths.Root);
        await File.WriteAllTextAsync(paths.SettingsFile, "{ this is not json");

        AppSettings loaded = await CreateService(paths).LoadAsync();

        Assert.Equal(AppSettings.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.Equal(AppTheme.Dark, loaded.Theme);
    }

    [Fact]
    public async Task ACorruptFileIsReplacedByTheBackupWhenOneExists()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);

        AppSettings good = AppSettings.CreateDefault();
        good.AccentColor = "#FF0000";
        await CreateService(paths).SaveAsync(good);

        AppSettings other = AppSettings.CreateDefault();
        other.AccentColor = "#00FF00";
        await CreateService(paths).SaveAsync(other);

        await File.WriteAllTextAsync(paths.SettingsFile, "### broken ###");

        AppSettings loaded = await CreateService(paths).LoadAsync();

        Assert.Equal("#FF0000", loaded.AccentColor);
    }

    [Fact]
    public async Task SaveKeepsABackupAndLeavesNoTemporaryFile()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        SettingsService service = CreateService(paths);

        await service.SaveAsync(AppSettings.CreateDefault());
        await service.SaveAsync(AppSettings.CreateDefault());

        Assert.True(File.Exists(paths.SettingsFile));
        Assert.True(File.Exists(paths.SettingsFile + ".bak"));
        Assert.False(File.Exists(paths.SettingsFile + ".tmp"));
    }

    [Fact]
    public async Task SaveRejectsInvalidSettings()
    {
        using TempDirectory temp = new();
        SettingsService service = CreateService(new AppPaths(temp.FullPath));

        AppSettings broken = AppSettings.CreateDefault();
        broken.AccentColor = "banana";

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(broken));
    }

    [Fact]
    public async Task RestoreDefaultsResetsEveryValue()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        SettingsService service = CreateService(paths);

        AppSettings changed = AppSettings.CreateDefault();
        changed.Theme = AppTheme.Glass;
        changed.AutoMonitorEnabled = true;
        changed.MaxConcurrentScans = 12;
        await service.SaveAsync(changed);

        SettingsSnapshot restored = await service.RestoreDefaultsAsync();

        Assert.Equal(AppTheme.Dark, restored.Theme);
        Assert.False(restored.AutoMonitorEnabled);
        Assert.Equal(SettingsLimits.DefaultMaxConcurrentScans, restored.MaxConcurrentScans);

        AppSettings reloaded = await CreateService(paths).LoadAsync();
        Assert.Equal(AppTheme.Dark, reloaded.Theme);
    }

    [Fact]
    public async Task UpdateAsyncMutatesAndPersists()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        SettingsService service = CreateService(paths);
        await service.LoadAsync();

        await service.UpdateAsync(settings => settings.LastHealthScanUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));

        AppSettings reloaded = await CreateService(paths).LoadAsync();
        Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), reloaded.LastHealthScanUtc);
    }

    [Fact]
    public async Task SettingsAppliedIsRaisedAfterSave()
    {
        using TempDirectory temp = new();
        SettingsService service = CreateService(new AppPaths(temp.FullPath));
        await service.LoadAsync();

        int raised = 0;
        SettingsSnapshot? snapshot = null;
        service.SettingsApplied += (_, e) =>
        {
            raised++;
            snapshot = e;
        };

        AppSettings changed = AppSettings.CreateDefault();
        changed.AccentColor = "#123456";
        await service.SaveAsync(changed);

        Assert.Equal(1, raised);
        Assert.Equal("#123456", snapshot!.AccentColor);
    }

    [Fact]
    public async Task SnapshotMirrorsTheCurrentSettings()
    {
        using TempDirectory temp = new();
        SettingsService service = CreateService(new AppPaths(temp.FullPath));

        AppSettings settings = AppSettings.CreateDefault();
        settings.Theme = AppTheme.Amoled;
        await service.SaveAsync(settings);

        Assert.Equal(AppTheme.Amoled, service.Snapshot.Theme);
        Assert.Equal(service.Current.ToSnapshot(), service.Snapshot);
    }

    [Fact]
    public async Task ValidateAcceptsTheSnapshotTheUiCanProduce()
    {
        using TempDirectory temp = new();
        SettingsService service = CreateService(new AppPaths(temp.FullPath));
        await service.LoadAsync();

        Assert.Empty(service.Validate(SettingsSnapshot.CreateDefault()));
        Assert.NotEmpty(service.Validate(SettingsSnapshot.CreateDefault() with { AccentColor = "zzz" }));
    }

    [Fact]
    public async Task TheSettingsFileIsValidIndentedJson()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        await CreateService(paths).SaveAsync(AppSettings.CreateDefault());

        string json = await File.ReadAllTextAsync(paths.SettingsFile);

        Assert.Contains("\n", json);
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("schemaVersion", out JsonElement schema));
        Assert.Equal(AppSettings.CurrentSchemaVersion, schema.GetInt32());
    }

    [Fact]
    public async Task DpapiProtectorRoundTripsOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        DpapiSecretProtector protector = new();

        string stored = protector.Protect("api-key-value");

        Assert.StartsWith("dpapi:", stored, StringComparison.Ordinal);
        Assert.DoesNotContain("api-key-value", stored, StringComparison.Ordinal);
        Assert.Equal("api-key-value", protector.Unprotect(stored));
    }

    [Fact]
    public void PlainTextProtectorIsMarkedUnavailable()
    {
        PlainTextSecretProtector protector = new();

        Assert.False(protector.IsAvailable);
        Assert.Equal("value", protector.Protect("value"));
        Assert.Equal("value", protector.Unprotect("value"));
    }

    private static SettingsService CreateService(AppPaths paths, ISecretProtector? protector = null) =>
        new(
            paths,
            protector ?? new ReversingSecretProtector(),
            new NullKeyValueStore(),
            new LocalizationService(new EmbeddedJsonLocalizationCatalogSource()));
}
