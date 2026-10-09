using VoidRemove.Core.Storage;
using VoidRemove.Shared;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class AppPathsTests
{
    [Fact]
    public void SubfoldersLiveUnderTheRoot()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);

        Assert.Equal(temp.FullPath, paths.Root);
        Assert.StartsWith(paths.Root + Path.DirectorySeparatorChar, paths.Logs, StringComparison.Ordinal);
        Assert.StartsWith(paths.Root + Path.DirectorySeparatorChar, paths.Quarantine, StringComparison.Ordinal);
        Assert.StartsWith(paths.Root + Path.DirectorySeparatorChar, paths.Backups, StringComparison.Ordinal);
        Assert.StartsWith(paths.Root + Path.DirectorySeparatorChar, paths.Cache, StringComparison.Ordinal);
        Assert.StartsWith(paths.Root + Path.DirectorySeparatorChar, paths.Temp, StringComparison.Ordinal);
        Assert.EndsWith(AppInfo.SettingsFileName, paths.SettingsFile, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureCreatedCreatesEveryRequiredFolder()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(Path.Combine(temp.FullPath, "VoidRemove"));

        paths.EnsureCreated();

        foreach (string directory in paths.EnumerateRequiredDirectories())
        {
            Assert.True(Directory.Exists(directory), $"{directory} was not created");
        }

        Assert.True(paths.IsInside(paths.SettingsFile));
        Assert.False(File.Exists(paths.SettingsFile), "Creating folders must not create a settings file.");
    }

    [Fact]
    public void DefaultRootIsProgramData()
    {
        string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        Assert.StartsWith(programData, AppPaths.Default.Root, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(AppInfo.MachineDataFolderName, AppPaths.Default.Root, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Logs")]
    [InlineData("Logs/2026")]
    [InlineData("")]
    public void IsInsideRecognisesPathsUnderTheRoot(string relative)
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);

        string candidate = string.IsNullOrEmpty(relative) ? paths.Root : Path.Combine(paths.Root, relative);

        Assert.True(paths.IsInside(candidate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsInsideRejectsEmptyInput(string? candidate)
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);

        Assert.False(paths.IsInside(candidate));
    }

    [Fact]
    public void IsInsideRejectsPathsOutsideTheRoot()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(Path.Combine(temp.FullPath, "data"));

        Assert.False(paths.IsInside(Path.Combine(temp.FullPath, "elsewhere")));
    }

    [Fact]
    public void ConstructorRejectsAnEmptyRoot()
    {
        Assert.Throws<ArgumentException>(() => new AppPaths("   "));
    }
}
