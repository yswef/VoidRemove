using VoidRemove.Core.History;
using VoidRemove.Core.Programs;
using VoidRemove.Core.Safety;
using VoidRemove.Core.Storage;
using Xunit;

namespace VoidRemove.Core.Tests;

public sealed class ExclusionListTests
{
    [Fact]
    public async Task SystemRootsAreAlwaysExcluded()
    {
        using TempDirectory temp = new();
        ExclusionList list = new(new AppPaths(temp.FullPath));
        await list.LoadAsync();

        Assert.True(list.IsExcluded(@"C:\Windows\System32"));
        Assert.True(list.IsExcluded(@"C:\Program Files\Microsoft"));
    }

    [Fact]
    public async Task UserEntriesExcludeTheirWholeSubtree()
    {
        using TempDirectory temp = new();
        ExclusionList list = new(new AppPaths(temp.FullPath));
        await list.LoadAsync();

        await list.AddAsync(@"C:\Keep\This");

        Assert.True(list.IsExcluded(@"C:\Keep\This\Folder\file.txt"));
        Assert.False(list.IsExcluded(@"C:\Keep\Other"));
    }

    [Fact]
    public async Task EntriesPersistAcrossLoads()
    {
        using TempDirectory temp = new();
        AppPaths paths = new(temp.FullPath);

        ExclusionList writer = new(paths);
        await writer.LoadAsync();
        await writer.AddAsync(@"D:\Never");

        ExclusionList reader = new(paths);
        await reader.LoadAsync();

        Assert.True(reader.IsExcluded(@"D:\Never\touch"));
        Assert.Contains(reader.Entries, e => e.Length > 0);
    }

    [Fact]
    public async Task RemovingAnEntryReenablesMatching()
    {
        using TempDirectory temp = new();
        ExclusionList list = new(new AppPaths(temp.FullPath));
        await list.LoadAsync();

        await list.AddAsync(@"C:\Tmp\X");
        Assert.True(list.IsExcluded(@"C:\Tmp\X"));

        await list.RemoveAsync(@"C:\Tmp\X");
        Assert.False(list.IsExcluded(@"C:\Tmp\X"));
    }
}

public sealed class JsonUninstallHistoryStoreTests
{
    [Fact]
    public async Task ReportsRoundTripNewestFirst()
    {
        using TempDirectory temp = new();
        JsonUninstallHistoryStore store = new(new AppPaths(temp.FullPath));

        await store.AddAsync(new UninstallReport { ProgramName = "First", Succeeded = true });
        await store.AddAsync(new UninstallReport { ProgramName = "Second", Succeeded = false });

        IReadOnlyList<UninstallReport> reports = await store.ListAsync();

        Assert.Equal(2, reports.Count);
        Assert.Equal("Second", reports[0].ProgramName);
        Assert.Equal("First", reports[1].ProgramName);
    }

    [Fact]
    public async Task ClearRemovesEverything()
    {
        using TempDirectory temp = new();
        JsonUninstallHistoryStore store = new(new AppPaths(temp.FullPath));

        await store.AddAsync(new UninstallReport { ProgramName = "X" });
        await store.ClearAsync();

        Assert.Empty(await store.ListAsync());
    }

    [Fact]
    public async Task MissingFileYieldsEmptyList()
    {
        using TempDirectory temp = new();

        Assert.Empty(await new JsonUninstallHistoryStore(new AppPaths(temp.FullPath)).ListAsync());
    }
}

public sealed class RegistryProgramReaderParsingTests
{
    [Theory]
    [InlineData("20260102", true)]
    [InlineData("20261302", false)]   // month 13
    [InlineData("garbage", false)]
    [InlineData("", false)]
    public void InstallDateParsesOnlyValidDates(string raw, bool expected)
    {
        Assert.Equal(expected, RegistryProgramReader.ParseInstallDate(raw).HasValue);
    }

    [Fact]
    public void InstallDateExtractsDigits()
    {
        Assert.Equal(new DateTime(2024, 5, 6), RegistryProgramReader.ParseInstallDate("20240506"));
    }

    [Fact]
    public void ThirtyTwoBitViewsReportX86()
    {
        Assert.Equal(ProgramArchitecture.X86,
            RegistryProgramReader.DetectArchitecture(ProgramSource.Hklm32Bit, string.Empty));
        Assert.Equal(ProgramArchitecture.X86,
            RegistryProgramReader.DetectArchitecture(ProgramSource.Hklm, @"C:\Program Files (x86)\App"));
        Assert.Equal(ProgramArchitecture.X64,
            RegistryProgramReader.DetectArchitecture(ProgramSource.Hklm, @"C:\Program Files\App"));
    }
}
