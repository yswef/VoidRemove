using VoidRemove.Core.Programs;
using VoidRemove.Core.Residual;
using VoidRemove.Shared.Models;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class ResidualMatcherTests
{
    private static ProgramRecord Identity(string installLocation = "") => new()
    {
        KeyName = "acme-rocket",
        DisplayName = "Acme Rocket",
        Publisher = "Acme Corporation",
        ProductCode = "{11111111-2222-3333-4444-555555555555}",
        InstallLocation = installLocation,
    };

    private static ResidualCandidate Dir(string path) => new() { Kind = ResidualKind.Directory, Path = path };

    [Fact]
    public void ProductCodeIsASafeMatch()
    {
        ResidualMatch match = ResidualMatcher.Match(Identity(), Dir(@"C:\Config\11111111222233334444555555555555"));

        Assert.True(match.IsMatch);
        Assert.Equal(SafetyLevel.Safe, match.Level);
    }

    [Fact]
    public void AnythingInsideTheInstallLocationIsSafe()
    {
        ResidualMatch match = ResidualMatcher.Match(
            Identity(@"C:\Apps\AcmeRocket"),
            Dir(@"C:\Apps\AcmeRocket\cache"));

        Assert.True(match.IsMatch);
        Assert.Equal(SafetyLevel.Safe, match.Level);
    }

    [Fact]
    public void ExactFolderNameIsSafe()
    {
        ResidualMatch match = ResidualMatcher.Match(Identity(), Dir(@"C:\Program Files\Acme Rocket"));

        Assert.True(match.IsMatch);
        Assert.Equal(SafetyLevel.Safe, match.Level);
    }

    [Fact]
    public void TwoOverlappingNameTokensAreModerate()
    {
        ResidualMatch match = ResidualMatcher.Match(Identity(), Dir(@"C:\ProgramData\rocket acme data"));

        Assert.True(match.IsMatch);
        Assert.Equal(SafetyLevel.Moderate, match.Level);
    }

    [Fact]
    public void OneDistinctiveTokenIsModerate()
    {
        ResidualMatch match = ResidualMatcher.Match(Identity(), Dir(@"C:\AppData\rocket"));

        Assert.True(match.IsMatch);
        Assert.Equal(SafetyLevel.Moderate, match.Level);
    }

    [Fact]
    public void AShortSharedTokenIsOnlyAdvanced()
    {
        ResidualMatch match = ResidualMatcher.Match(Identity(), Dir(@"C:\Tools\acme"));

        Assert.True(match.IsMatch);
        Assert.Equal(SafetyLevel.Advanced, match.Level);
    }

    [Fact]
    public void GenericFoldersNeverMatch()
    {
        Assert.False(ResidualMatcher.Match(Identity(), Dir(@"C:\Program Files\installer")).IsMatch);
        Assert.False(ResidualMatcher.Match(Identity(), Dir(@"C:\Program Files\update")).IsMatch);
        Assert.False(ResidualMatcher.Match(Identity(), Dir(@"C:\Windows")).IsMatch);
    }

    [Fact]
    public void UnrelatedNamesDoNotMatch()
    {
        Assert.False(ResidualMatcher.Match(Identity(), Dir(@"C:\Program Files\Zebra Paint")).IsMatch);
    }
}
