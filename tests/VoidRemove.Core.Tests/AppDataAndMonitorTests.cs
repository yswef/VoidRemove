using VoidRemove.Core.AppData;
using VoidRemove.Core.Monitor;
using VoidRemove.Core.Storage;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class ProfileCatalogTests
{
    [Fact]
    public void LoadsValidProfilesAndSkipsMalformed()
    {
        using TempDirectory temp = new();
        File.WriteAllText(temp.Combine("good.json"), """
            { "id": "p1", "displayName": "P One", "matchNames": ["p one"],
              "locations": [ { "kind": "Folder", "path": "%AppData%\\P1" } ] }
            """);
        File.WriteAllText(temp.Combine("bad.json"), "{ not json");

        IReadOnlyList<AppDataProfile> profiles = new ProfileCatalog().LoadFromDirectory(temp.FullPath);

        Assert.Single(profiles);
        Assert.Equal("p1", profiles[0].Id);
        Assert.Single(profiles[0].Locations);
    }

    [Fact]
    public void ShipsOriginalProfilesInTheRepository()
    {
        string dataDir = Path.Combine(GetRepoRoot(), "data", "profiles");
        IReadOnlyList<AppDataProfile> profiles = new ProfileCatalog().LoadFromDirectory(dataDir);

        Assert.NotEmpty(profiles);
        Assert.All(profiles, p => Assert.NotEmpty(p.Locations));
    }

    internal static string GetRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current) && !File.Exists(Path.Combine(current, "VoidRemove.sln")))
        {
            current = Path.GetDirectoryName(current)!;
        }

        return current;
    }
}

public sealed class AppDataResolverTests
{
    [Fact]
    public void ProfileMatchingUsesTokensAndSubstrings()
    {
        AppDataProfile profile = new() { Id = "x", MatchNames = new List<string> { "acme notes" } };

        Assert.True(AppDataResolver.ProfileMatches("Acme Notes 2024", profile));
        Assert.True(AppDataResolver.ProfileMatches("acmenotes", profile));
        Assert.False(AppDataResolver.ProfileMatches("Zebra Paint", profile));
    }

    [Fact]
    public void TopLevelDataRootCollapsesToTheAppDataChild()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string deep = Path.Combine(local, "SomeApp", "cache", "file.bin");

        Assert.Equal(Path.Combine(local, "SomeApp"), AppDataResolver.TopLevelDataRoot(deep));
        Assert.Null(AppDataResolver.TopLevelDataRoot(@"C:\NotADataRoot\x"));
    }

    [Fact]
    public void ResolveExpandsProfileLocationsThatExist()
    {
        using TempDirectory temp = new();
        string dataDir = temp.Combine("data");
        Directory.CreateDirectory(dataDir);

        AppDataProfile profile = new()
        {
            Id = "p",
            MatchNames = new List<string> { "myapp" },
            Locations = new List<DataLocationRule>
            {
                new() { Kind = "Folder", Path = dataDir, Optional = true },
                new() { Kind = "Folder", Path = temp.Combine("missing"), Optional = true },
            },
        };

        IReadOnlyList<ResolvedLocation> resolved = new AppDataResolver().Resolve("MyApp", new[] { profile }, Array.Empty<string>());

        Assert.Contains(resolved, r => r.Path == dataDir && r.Source == DiscoverySource.Profile && r.Exists);
        Assert.DoesNotContain(resolved, r => r.Path == temp.Combine("missing"));
    }
}

public sealed class SnapshotDiffTests
{
    [Fact]
    public void ComputeReportsOnlyAdditions()
    {
        SystemSnapshot before = new()
        {
            Files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\a.txt", @"C:\keep.txt" },
            Services = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SvcA" },
        };
        SystemSnapshot after = new()
        {
            Files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\a.txt", @"C:\keep.txt", @"C:\new.txt" },
            Services = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SvcA", "SvcB" },
        };

        SnapshotDiff diff = SnapshotDiff.Compute(before, after);

        Assert.Equal(new[] { @"C:\new.txt" }, diff.AddedFiles);
        Assert.Equal(new[] { "SvcB" }, diff.AddedServices);
        Assert.Empty(diff.AddedRegistryKeys);
        Assert.False(diff.IsEmpty);
    }

    [Fact]
    public void IdenticalSnapshotsProduceAnEmptyDiff()
    {
        SystemSnapshot snap = new();
        Assert.True(SnapshotDiff.Compute(snap, snap).IsEmpty);
    }
}

internal sealed class FakeSnapshotProvider : ISnapshotProvider
{
    public SystemSnapshot Current { get; set; } = SystemSnapshot.Empty;

    public bool IsSupported => true;

    public SystemSnapshot Capture(CancellationToken cancellationToken = default) => Current;
}

public sealed class ResetEngineTests
{
    [Fact]
    public async Task ExecuteBacksUpThenRemovesAndRestoreBringsDataBack()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        ResetEngine engine = new(paths);

        string dataDir = temp.Combine("data/MyApp");
        Directory.CreateDirectory(dataDir);
        string file = Path.Combine(dataDir, "notes.txt");
        await File.WriteAllTextAsync(file, "hello");

        ResetPlan plan = engine.BuildPlan(
            new[] { new ResolvedLocation { Path = dataDir, Exists = true, SizeBytes = 5 } },
            dryRun: false);

        ResetResult result = await engine.ExecuteAsync(plan, "MyApp");

        Assert.False(Directory.Exists(dataDir));
        Assert.NotNull(result.BackupZipPath);
        Assert.True(File.Exists(result.BackupZipPath));

        int restored = await engine.RestoreAsync(result.BackupZipPath!);

        Assert.True(restored >= 1);
        Assert.Equal("hello", await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task DryRunRemovesNothing()
    {
        using TempDirectory temp = new();
        ResetEngine engine = new(new AppPaths(temp.FullPath));

        string dataDir = temp.Combine("data/App");
        Directory.CreateDirectory(dataDir);

        ResetPlan plan = engine.BuildPlan(
            new[] { new ResolvedLocation { Path = dataDir, Exists = true } },
            dryRun: true);

        ResetResult result = await engine.ExecuteAsync(plan, "App");

        Assert.True(result.WasDryRun);
        Assert.True(Directory.Exists(dataDir));
        Assert.Null(result.BackupZipPath);
    }
}

public sealed class InstallMonitorManualTests
{
    [Fact]
    public async Task ManualSessionRecordsAddedFiles()
    {
        using TempDirectory temp = new();
        FakeSnapshotProvider provider = new();
        JsonInstallMonitorStore store = new(new AppPaths(temp.FullPath));
        InstallMonitor monitor = new(provider, store);

        provider.Current = new SystemSnapshot
        {
            Files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\before.txt" },
        };

        monitor.BeginManual();

        provider.Current = new SystemSnapshot
        {
            Files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\before.txt", @"C:\added.txt" },
        };

        InstallLog? log = await monitor.EndManualAsync("setup.exe");

        Assert.NotNull(log);
        Assert.Equal(new[] { @"C:\added.txt" }, log!.AddedFiles);

        IReadOnlyList<InstallLog> stored = await store.ListAsync();
        Assert.Single(stored);
    }

    [Fact]
    public void InstallerDetectionRecognisesCommonNames()
    {
        Assert.True(InstallMonitor.IsInstallerLike("msiexec.exe"));
        Assert.True(InstallMonitor.IsInstallerLike("setup-x64.exe"));
        Assert.True(InstallMonitor.IsInstallerLike("unins000.exe"));
        Assert.False(InstallMonitor.IsInstallerLike("explorer.exe"));
    }
}
