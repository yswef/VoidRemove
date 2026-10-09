using VoidRemove.Core.Shell;
using VoidRemove.Shared;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class ExplorerContextMenuTests
{
    [Fact]
    public void CommandLinesCarryTheCliSwitchesAndTarget()
    {
        (string uninstall, string reset) = ExplorerContextMenuService.BuildCommandLines(@"C:\Tools\VoidRemove.Wpf.exe");

        Assert.Contains(AppInfo.ArgUninstall, uninstall);
        Assert.Contains(AppInfo.ArgResetApp, reset);
        Assert.Contains("\"%1\"", uninstall);
        Assert.Contains("\"%1\"", reset);
        Assert.StartsWith("\"C:\\Tools\\VoidRemove.Wpf.exe\"", uninstall, StringComparison.Ordinal);
    }

    [Fact]
    public void VerbIdsAreDistinctAndStable()
    {
        Assert.NotEqual(AppInfo.ExplorerVerbUninstallId, AppInfo.ExplorerVerbResetId);
        Assert.StartsWith(AppInfo.Id, AppInfo.ExplorerVerbUninstallId, StringComparison.Ordinal);
    }
}
