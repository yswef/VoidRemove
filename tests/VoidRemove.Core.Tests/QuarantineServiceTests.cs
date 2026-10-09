using VoidRemove.Core.Backup;
using VoidRemove.Core.Storage;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class QuarantineServiceTests
{
    [Fact]
    public async Task QuarantinedFilesCanBeRestoredToTheirOriginalLocation()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        QuarantineService service = new(paths);

        string sourceDir = temp.Combine("leftover");
        Directory.CreateDirectory(sourceDir);
        string file = Path.Combine(sourceDir, "data.bin");
        await File.WriteAllTextAsync(file, "payload");

        QuarantineManifest manifest = await service.QuarantineAsync(new[] { sourceDir }, "Acme Rocket");

        Assert.False(Directory.Exists(sourceDir), "the original must be gone after quarantine");
        Assert.Single(manifest.Items);

        int restored = await service.RestoreAsync(manifest.Id);

        Assert.Equal(1, restored);
        Assert.True(File.Exists(file), "the file must be back after restore");
        Assert.Equal("payload", await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task MissingPathsAreSkipped()
    {
        using TempDirectory temp = new();
        QuarantineService service = new(new AppPaths(temp.FullPath));

        QuarantineManifest manifest = await service.QuarantineAsync(
            new[] { temp.Combine("does-not-exist") }, "Ghost");

        Assert.Empty(manifest.Items);
    }

    [Fact]
    public async Task ManifestsAreListedAndCanBePurged()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        QuarantineService service = new(paths);

        string file = temp.Combine("a.txt");
        await File.WriteAllTextAsync(file, "x");
        QuarantineManifest manifest = await service.QuarantineAsync(new[] { file }, "App");

        IReadOnlyList<QuarantineManifest> listed = await service.ListAsync();
        Assert.Contains(listed, m => m.Id == manifest.Id);

        service.Purge(manifest.Id);

        Assert.DoesNotContain(await service.ListAsync(), m => m.Id == manifest.Id);
    }

    [Fact]
    public async Task ExpiredSessionsArePruned()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        QuarantineService service = new(paths);

        string file = temp.Combine("old.txt");
        await File.WriteAllTextAsync(file, "x");
        await service.QuarantineAsync(new[] { file }, "Old");

        // A negative retention window treats everything as expired.
        int purged = await service.PruneExpiredAsync(-1);

        Assert.Equal(1, purged);
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task CollidingNamesGetUniqueStoredPaths()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);
        QuarantineService service = new(paths);

        string first = temp.Combine("src1/same.txt");
        string second = temp.Combine("src2/same.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(first)!);
        Directory.CreateDirectory(Path.GetDirectoryName(second)!);
        await File.WriteAllTextAsync(first, "1");
        await File.WriteAllTextAsync(second, "2");

        QuarantineManifest manifest = await service.QuarantineAsync(new[] { first, second }, "App");

        Assert.Equal(2, manifest.Items.Count);
        Assert.NotEqual(manifest.Items[0].StoredPath, manifest.Items[1].StoredPath);
    }
}
