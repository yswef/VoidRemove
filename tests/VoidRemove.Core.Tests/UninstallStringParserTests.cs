using VoidRemove.Core.Uninstall;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class UninstallStringParserTests
{
    [Fact]
    public void MsiExecWithProductCodeIsClassified()
    {
        UninstallCommand command = UninstallStringParser.Parse(
            "MsiExec.exe /I{11111111-2222-3333-4444-555555555555}");

        Assert.Equal(UninstallKind.MsiExec, command.Kind);
        Assert.Equal("msiexec.exe", command.Executable);
        Assert.Equal("{11111111-2222-3333-4444-555555555555}", command.ProductCode);
        Assert.True(command.IsRunnable);
    }

    [Fact]
    public void WindowsInstallerFlagForcesMsiEvenWithoutMsiExec()
    {
        UninstallCommand command = UninstallStringParser.Parse(
            "setup.exe /uninstall {AAAABBBB-CCCC-DDDD-EEEE-FFFF00001111}", isWindowsInstaller: true);

        Assert.Equal(UninstallKind.MsiExec, command.Kind);
        Assert.Equal("{AAAABBBB-CCCC-DDDD-EEEE-FFFF00001111}", command.ProductCode);
    }

    [Fact]
    public void QuotedExecutableIsSplitFromArguments()
    {
        UninstallCommand command = UninstallStringParser.Parse(
            "\"C:\\Program Files\\App\\uninstall.exe\" /S /quiet");

        Assert.Equal(UninstallKind.Custom, command.Kind);
        Assert.Equal("C:\\Program Files\\App\\uninstall.exe", command.Executable);
        Assert.Equal("/S /quiet", command.Arguments);
    }

    [Fact]
    public void UnquotedExecutableWithExeExtensionIsSplit()
    {
        UninstallCommand command = UninstallStringParser.Parse(
            "C:\\Apps\\tool\\unins000.exe /verysilent");

        Assert.Equal(UninstallKind.Custom, command.Kind);
        Assert.Equal("C:\\Apps\\tool\\unins000.exe", command.Executable);
        Assert.Equal("/verysilent", command.Arguments);
    }

    [Fact]
    public void PowerShellAppxRemovalIsMsix()
    {
        UninstallCommand command = UninstallStringParser.Parse(
            "powershell -command \"Remove-AppxPackage -Package 'Contoso.App_1.0.0.0_x64__8wekyb3d8bbwe'\"");

        Assert.Equal(UninstallKind.Msix, command.Kind);
        Assert.Equal("Contoso.App_1.0.0.0_x64__8wekyb3d8bbwe", command.PackageFamilyName);
    }

    [Fact]
    public void EmptyStringIsNotRunnable()
    {
        Assert.False(UninstallStringParser.Parse(null).IsRunnable);
        Assert.False(UninstallStringParser.Parse("   ").IsRunnable);
    }

    [Fact]
    public void QuietMsiArgumentsAreBuiltFromTheProductCode()
    {
        Assert.Equal(
            "/X{11111111-2222-3333-4444-555555555555} /qn /norestart",
            UninstallStringParser.BuildMsiQuietArguments("{11111111-2222-3333-4444-555555555555}"));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("no guid here", null)]
    [InlineData("x{11111111-2222-3333-4444-555555555555}y", "{11111111-2222-3333-4444-555555555555}")]
    public void ExtractProductCodeNormalizes(string? input, string? expected)
    {
        Assert.Equal(expected, UninstallStringParser.ExtractProductCode(input));
    }
}
