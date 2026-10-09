using VoidRemove.Core.Backup;
using VoidRemove.Core.Engine;
using VoidRemove.Core.History;
using VoidRemove.Core.Programs;
using VoidRemove.Core.Residual;
using VoidRemove.Core.Safety;
using VoidRemove.Core.Storage;
using VoidRemove.Shared.Models;
using Xunit;

namespace VoidRemove.Core.Tests;

internal sealed class FakeScanner : IResidualScanner
{
    private readonly IReadOnlyList<ResidualCandidate> _candidates;

    public FakeScanner(IReadOnlyList<ResidualCandidate> candidates) => _candidates = candidates;

    public bool IsSupported => true;

    public Task<IReadOnlyList<ResidualCandidate>> ScanAsync(int maxConcurrency, CancellationToken cancellationToken = default) =>
        Task.FromResult(_candidates);
}

internal sealed class FakeProcessRunner : IProcessRunner
{
    public int Calls;

    public Task<ProcessResult> RunAsync(string executable, string arguments, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(new ProcessResult(0, false, string.Empty));
    }
}

public sealed class UninstallEngineTests
{
    private static ProgramRecord Identity(string installLocation) => new()
    {
        KeyName = "acme-rocket",
        DisplayName = "Acme Rocket",
        Publisher = "Acme Corporation",
        UninstallString = @"C:\Apps\uninstall.exe /S",
        InstallLocation = installLocation,
    };

    private static UninstallEngine BuildEngine(
        TempDirectory temp,
        IReadOnlyList<ResidualCandidate> candidates,
        FakeProcessRunner runner,
        out JsonUninstallHistoryStore history)
    {
        AppPaths paths = new(temp.FullPath);
        history = new JsonUninstallHistoryStore(paths);

        return new UninstallEngine(
            new FakeScanner(candidates),
            new QuarantineService(paths),
            new RegistryBackupService(paths),
            history,
            new ExclusionList(paths),
            runner,
            new NullRestorePointService());
    }

    private static string CreateDir(TempDirectory temp, string relative)
    {
        string path = temp.Combine(relative);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "marker.txt"), "x");
        return path;
    }

    [Fact]
    public async Task RunQuarantinesOnlyResidualsWithinTheSafetyCeiling()
    {
        using TempDirectory temp = new();
        string install = CreateDir(temp, "install/AcmeRocket");

        List<ResidualCandidate> candidates = new()
        {
            new() { Kind = ResidualKind.Directory, Path = CreateDir(temp, "install/AcmeRocket/cache") }, // safe: inside install
            new() { Kind = ResidualKind.Directory, Path = CreateDir(temp, "scan/Acme Rocket") },        // safe: exact name
            new() { Kind = ResidualKind.Directory, Path = CreateDir(temp, "scan/rocket acme data") },   // moderate
            new() { Kind = ResidualKind.Directory, Path = CreateDir(temp, "scan/rocket") },             // moderate
            new() { Kind = ResidualKind.Directory, Path = CreateDir(temp, "scan/acme") },               // advanced -> skipped
            new() { Kind = ResidualKind.Directory, Path = CreateDir(temp, "scan/installer") },          // generic -> no match
        };

        FakeProcessRunner runner = new();
        UninstallEngine engine = BuildEngine(temp, candidates, runner, out JsonUninstallHistoryStore history);

        UninstallReport report = await engine.RunAsync(
            Identity(install),
            new UninstallOptions { MaxSafetyLevel = SafetyLevel.Moderate },
            maxConcurrency: 4);

        Assert.True(report.Succeeded);
        Assert.Equal(1, runner.Calls);
        Assert.Equal(4, report.ResidualsRemoved);
        Assert.NotNull(report.QuarantineManifestId);

        IReadOnlyList<UninstallReport> stored = await history.ListAsync();
        Assert.Single(stored);
        Assert.Equal("Acme Rocket", stored[0].ProgramName);
    }

    [Fact]
    public async Task RaisingTheCeilingIncludesAdvancedResiduals()
    {
        using TempDirectory temp = new();
        string install = CreateDir(temp, "install/AcmeRocket");

        List<ResidualCandidate> candidates = new()
        {
            new() { Kind = ResidualKind.Directory, Path = CreateDir(temp, "scan/acme") }, // advanced
        };

        FakeProcessRunner runner = new();
        UninstallEngine engine = BuildEngine(temp, candidates, runner, out _);

        UninstallReport report = await engine.RunAsync(
            Identity(install),
            new UninstallOptions { MaxSafetyLevel = SafetyLevel.Advanced },
            maxConcurrency: 4);

        Assert.Equal(1, report.ResidualsRemoved);
    }

    [Fact]
    public async Task DryRunTouchesNothing()
    {
        using TempDirectory temp = new();
        string install = CreateDir(temp, "install/AcmeRocket");
        string residual = CreateDir(temp, "scan/Acme Rocket");

        List<ResidualCandidate> candidates = new()
        {
            new() { Kind = ResidualKind.Directory, Path = residual },
        };

        FakeProcessRunner runner = new();
        UninstallEngine engine = BuildEngine(temp, candidates, runner, out _);

        UninstallReport report = await engine.RunAsync(
            Identity(install),
            new UninstallOptions { DryRun = true },
            maxConcurrency: 4);

        Assert.True(report.WasDryRun);
        Assert.Equal(0, runner.Calls);
        Assert.Equal(0, report.ResidualsRemoved);
        Assert.Null(report.QuarantineManifestId);
        Assert.True(Directory.Exists(residual), "dry run must not remove anything");
    }

    [Fact]
    public async Task PreviewListsWhatWouldBeRemoved()
    {
        using TempDirectory temp = new();
        string install = CreateDir(temp, "install/AcmeRocket");

        List<ResidualCandidate> candidates = new()
        {
            new() { Kind = ResidualKind.Directory, Path = CreateDir(temp, "scan/Acme Rocket") },
            new() { Kind = ResidualKind.Directory, Path = CreateDir(temp, "scan/rocket") },
            new() { Kind = ResidualKind.Directory, Path = CreateDir(temp, "scan/acme") },
        };

        FakeProcessRunner runner = new();
        UninstallEngine engine = BuildEngine(temp, candidates, runner, out _);

        IReadOnlyList<MatchedResidual> preview = await engine.PreviewAsync(
            Identity(install),
            new UninstallOptions { MaxSafetyLevel = SafetyLevel.Moderate },
            maxConcurrency: 4);

        Assert.Equal(2, preview.Count);
        Assert.Equal(0, runner.Calls);
    }
}
