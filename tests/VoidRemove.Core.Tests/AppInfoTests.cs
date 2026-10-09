using System.Reflection;
using VoidRemove.Shared;
using Xunit;

namespace VoidRemove.Core.Tests;

/// <summary>
/// The identity contract: one edit in <see cref="AppInfo"/> must move every machine
/// footprint with it.
/// </summary>
public sealed class AppInfoTests
{
    [Fact]
    public void RegistryRootIsDerivedFromTheId()
    {
        Assert.Equal(@"Software\" + AppInfo.Id, AppInfo.RegistryRoot);
    }

    [Fact]
    public void UninstallKeyLivesUnderTheWindowsUninstallHive()
    {
        Assert.StartsWith(
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\",
            AppInfo.UninstallRegistryKey,
            StringComparison.Ordinal);

        Assert.EndsWith(AppInfo.Id, AppInfo.UninstallRegistryKey, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(nameof(AppInfo.MutexName))]
    [InlineData(nameof(AppInfo.NamedPipeName))]
    [InlineData(nameof(AppInfo.AppUserModelId))]
    [InlineData(nameof(AppInfo.MachineDataFolderName))]
    [InlineData(nameof(AppInfo.LogFilePattern))]
    [InlineData(nameof(AppInfo.ExplorerVerbUninstallId))]
    [InlineData(nameof(AppInfo.ExplorerVerbResetId))]
    public void EveryMachineFootprintCarriesTheId(string constantName)
    {
        FieldInfo field = typeof(AppInfo).GetField(constantName, BindingFlags.Public | BindingFlags.Static)!;
        string value = Assert.IsType<string>(field.GetRawConstantValue());

        Assert.Contains(AppInfo.Id, value, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlineEndpointsPointAtTheProjectRepository()
    {
        Assert.Contains(AppInfo.RepositoryName, AppInfo.RepositoryUrl, StringComparison.Ordinal);
        Assert.StartsWith(AppInfo.RepositoryUrl, AppInfo.IssuesUrl, StringComparison.Ordinal);
        Assert.StartsWith(AppInfo.RepositoryUrl, AppInfo.ReleasesUrl, StringComparison.Ordinal);
        Assert.Contains(AppInfo.RepositoryName, AppInfo.DocsUrl, StringComparison.Ordinal);
        Assert.Contains("api.github.com", AppInfo.LatestReleaseApiUrl, StringComparison.Ordinal);
        Assert.Contains(AppInfo.RepositoryName, AppInfo.LatestReleaseApiUrl, StringComparison.Ordinal);
    }

    [Fact]
    public void VersionIsSemanticAndFreeOfSourceRevisionSuffix()
    {
        string version = AppInfo.Version;

        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.DoesNotContain('+', version);
        Assert.Matches(@"^\d+\.\d+\.\d+", version);
    }

    [Fact]
    public void WindowTitleStartsWithTheProductName()
    {
        Assert.StartsWith(AppInfo.Name, AppInfo.WindowTitle, StringComparison.Ordinal);
        Assert.Contains(AppInfo.Version, AppInfo.WindowTitle, StringComparison.Ordinal);
    }

    [Fact]
    public void CommandLineSwitchesAreDistinct()
    {
        string[] switches = { AppInfo.ArgUninstall, AppInfo.ArgResetApp, AppInfo.ArgActivate };

        Assert.Equal(switches.Length, switches.Distinct(StringComparer.Ordinal).Count());
        Assert.All(switches, value => Assert.StartsWith("--", value, StringComparison.Ordinal));
    }
}
