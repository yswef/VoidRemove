using VoidRemove.Core.Hosting;
using Xunit;

namespace VoidRemove.Core.Tests;

/// <summary>
/// The single instance handshake behind the Explorer context menu and the global hotkey.
/// </summary>
public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void TheSecondInstanceDoesNotOwnTheMutex()
    {
        string mutexName = UniqueMutexName();

        using SingleInstanceGuard first = new(mutexName: mutexName, pipeName: UniquePipeName());
        using SingleInstanceGuard second = new(mutexName: mutexName, pipeName: UniquePipeName());

        Assert.True(first.TryAcquire());
        Assert.True(first.IsFirstInstance);

        Assert.False(second.TryAcquire());
        Assert.False(second.IsFirstInstance);
    }

    [Fact]
    public void AcquireIsIdempotent()
    {
        using SingleInstanceGuard guard = new(mutexName: UniqueMutexName(), pipeName: UniquePipeName());

        Assert.True(guard.TryAcquire());
        Assert.True(guard.TryAcquire());
    }

    [Fact]
    public void NamesComeFromAppInfoByDefault()
    {
        using SingleInstanceGuard guard = new();

        Assert.Contains("VoidRemove", guard.MutexName, StringComparison.Ordinal);
        Assert.Contains("VoidRemove", guard.PipeName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ArgumentsReachTheRunningInstance()
    {
        string pipeName = UniquePipeName();

        using SingleInstanceGuard listener = new(mutexName: UniqueMutexName(), pipeName: pipeName);
        using SingleInstanceGuard sender = new(mutexName: UniqueMutexName(), pipeName: pipeName);
        using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(15));

        TaskCompletionSource<IReadOnlyList<string>> received = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task loop = listener.ListenAsync(
            arguments =>
            {
                received.TrySetResult(arguments);
                return Task.CompletedTask;
            },
            cancellation.Token);

        await Task.Delay(250, CancellationToken.None);

        bool sent = await sender.SendArgumentsAsync(new[] { "--uninstall", "Some Program" }, cancellation.Token);

        Assert.True(sent);

        IReadOnlyList<string> arguments = await received.Task;

        Assert.Equal(new[] { "--uninstall", "Some Program" }, arguments);

        cancellation.Cancel();

        // The listener swallows the cancellation and returns, so the loop must settle.
        await loop.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SendingWithoutAListenerReportsFailure()
    {
        using SingleInstanceGuard sender = new(mutexName: UniqueMutexName(), pipeName: UniquePipeName());
        using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(5));

        bool sent = await sender.SendArgumentsAsync(new[] { "--activate" }, cancellation.Token);

        Assert.False(sent);
    }

    private static string UniqueMutexName() => @"Local\VoidRemove.Tests." + Guid.NewGuid().ToString("N");

    private static string UniquePipeName() => "VoidRemove.Tests." + Guid.NewGuid().ToString("N");
}
