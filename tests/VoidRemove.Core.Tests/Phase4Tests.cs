using VoidRemove.Core.Extensions;
using VoidRemove.Core.Health;
using VoidRemove.Core.Programs;
using VoidRemove.Core.Toolbox;
using VoidRemove.Core.WindowsApps;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class RedundantFileDetectorTests
{
    private static FileEntry Entry(string name, long size = 1000, int ageDays = 30) =>
        new(@"C:\Temp\" + name, size, DateTime.UtcNow.AddDays(-ageDays));

    [Fact]
    public void FlagsInstallerArtefacts()
    {
        var detected = RedundantFileDetector.Detect(new[]
        {
            Entry("setup-app.exe"),
            Entry("program.msi"),
            Entry("notes.txt"),
            Entry("cleaner.exe"),
        });

        Assert.Equal(2, detected.Count);
        Assert.Contains(detected, f => f.Path.EndsWith("setup-app.exe"));
        Assert.Contains(detected, f => f.Path.EndsWith("program.msi"));
    }

    [Fact]
    public void RespectsTheAgeThreshold()
    {
        var fresh = new[] { Entry("setup-new.exe", ageDays: 1) };

        Assert.Empty(RedundantFileDetector.Detect(fresh, olderThanDays: 7));
        Assert.Single(RedundantFileDetector.Detect(fresh, olderThanDays: 0));
    }

    [Fact]
    public void MsiIsAlwaysAnArtefact()
    {
        Assert.True(RedundantFileDetector.IsInstallerArtefact(Entry("anything.msi")));
        Assert.False(RedundantFileDetector.IsInstallerArtefact(Entry("document.docx")));
    }
}

public sealed class ExtensionScorerTests
{
    [Fact]
    public void RiskyPermissionsLowerTheScore()
    {
        int clean = ExtensionScorer.ComputeScore(2, 0, true, false);
        int risky = ExtensionScorer.ComputeScore(2, 3, true, false);

        Assert.True(clean > risky);
        Assert.True(ExtensionScorer.IsTrusted(clean));
        Assert.False(ExtensionScorer.IsTrusted(risky));
    }

    [Fact]
    public void ForceInstalledAndUnknownPublisherReduceTrust()
    {
        int baseline = ExtensionScorer.ComputeScore(0, 0, true, false);
        int forced = ExtensionScorer.ComputeScore(0, 0, true, true);
        int unknown = ExtensionScorer.ComputeScore(0, 0, false, false);

        Assert.True(forced < baseline);
        Assert.True(unknown < baseline);
    }

    [Fact]
    public void CountsRiskyPermissionsCaseInsensitively()
    {
        Assert.Equal(2, ExtensionScorer.CountRisky(new[] { "Tabs", "<all_urls>", "storage" }));
    }
}

public sealed class WindowsAppClassificationTests
{
    [Fact]
    public void FrameworksAndSystemPackagesAreProtected()
    {
        Assert.Equal(WindowsAppTag.System, WindowsApp.ClassifyTag(isFramework: true, isDependency: false, "Developer"));
        Assert.Equal(WindowsAppTag.System, WindowsApp.ClassifyTag(false, false, "System"));
        Assert.Equal(WindowsAppTag.Dependent, WindowsApp.ClassifyTag(false, true, "Developer"));
        Assert.Equal(WindowsAppTag.Safe, WindowsApp.ClassifyTag(false, false, "Developer"));
    }

    [Fact]
    public void ParsesPowerShellJson()
    {
        string json = """
            [
              { "Name": "Game", "PackageFamilyName": "game_123", "Version": "1.0", "IsFramework": false, "IsResourcePackage": false, "SignatureKind": "Developer" },
              { "Name": "Runtime", "PackageFamilyName": "runtime_456", "Version": "2.0", "IsFramework": true, "IsResourcePackage": false, "SignatureKind": "Developer" }
            ]
            """;

        IReadOnlyList<WindowsApp> apps = PowerShellWindowsAppService.ParseApps(json);

        Assert.Equal(2, apps.Count);
        Assert.Equal(WindowsAppTag.Safe, apps[0].Tag);
        Assert.Equal(WindowsAppTag.System, apps[1].Tag);
        Assert.True(apps[1].IsProtected);
    }
}

public sealed class WindowsUpdatesTests
{
    [Fact]
    public void BuildsWusaArguments()
    {
        Assert.Equal("/uninstall /kb:5034441 /quiet /norestart", WindowsUpdatesService.BuildWusaArguments("KB5034441", quiet: true));
        Assert.Equal("/uninstall /kb:123 /norestart", WindowsUpdatesService.BuildWusaArguments("123", quiet: false));
    }

    [Fact]
    public void ParsesHotFixJson()
    {
        string json = """[ { "HotFixID": "KB1", "Description": "Update", "InstalledOn": "2026-01-02" } ]""";

        IReadOnlyList<WindowsUpdate> updates = WindowsUpdatesService.ParseUpdates(json);

        Assert.Single(updates);
        Assert.Equal("KB1", updates[0].HotFixId);
    }
}

public sealed class FileShredderTests
{
    [Fact]
    public async Task ShredDeletesTheFile()
    {
        using TempDirectory temp = new();
        string path = temp.Combine("secret.txt");
        await File.WriteAllTextAsync(path, "top secret");

        bool ok = await new FileShredder().ShredAsync(path, passes: 3);

        Assert.True(ok);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ShredMissingFileReturnsFalse()
    {
        using TempDirectory temp = new();
        Assert.False(await new FileShredder().ShredAsync(temp.Combine("nope.bin")));
    }
}

public sealed class CatalogTests
{
    [Fact]
    public void StubbornCatalogLoadsAndMatches()
    {
        using TempDirectory temp = new();
        string file = temp.Combine("stubborn.json");
        File.WriteAllText(file, """
            [ { "id": "x", "displayName": "Example Toolbar", "matchNames": ["example toolbar"],
                "uninstallExecutable": "u.exe", "processes": ["tb"] } ]
            """);

        StubbornCatalog catalog = new();
        var programs = catalog.Load(file);

        Assert.Single(programs);
        Assert.NotNull(catalog.FindMatch("Example Toolbar 2", programs));
        Assert.Null(catalog.FindMatch("Zebra Paint", programs));
    }

    [Fact]
    public void BundlewareCatalogFlagsMatches()
    {
        BundlewareCatalog catalog = new();
        BundlewareCatalogData data = new()
        {
            Publishers = new List<string> { "Example Bundle" },
            Products = new List<string> { "coupon helper" },
        };

        Assert.True(catalog.IsBundleware("Coupon Helper 1.2", "Someone", data));
        Assert.True(catalog.IsBundleware("Random App", "Example Bundle Inc", data));
        Assert.False(catalog.IsBundleware("Zebra Paint", "Zebra", data));
    }

    [Fact]
    public void RepositoryShipsStubbornAndBundlewareData()
    {
        string root = ProfileCatalogTests.GetRepoRoot();

        Assert.True(File.Exists(Path.Combine(root, "data", "stubborn.json")));
        Assert.True(File.Exists(Path.Combine(root, "data", "bundleware.json")));
        Assert.NotEmpty(new StubbornCatalog().Load(Path.Combine(root, "data", "stubborn.json")));
    }
}
