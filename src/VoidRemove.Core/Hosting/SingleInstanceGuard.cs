using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VoidRemove.Shared;

namespace VoidRemove.Core.Hosting;

/// <summary>
/// Keeps VoidRemove a single instance and lets a second launch hand its arguments to the
/// running window.
/// </summary>
/// <remarks>
/// A named mutex answers "am I the first?", a named pipe carries the payload. That
/// combination is what makes the Explorer context menu work: the shell starts
/// <c>VoidRemove --uninstall "Some App"</c>, the new process finds the mutex taken, sends
/// the argument over the pipe and exits, and the running window opens on that program.
/// </remarks>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly string _mutexName;
    private readonly string _pipeName;
    private readonly ILogger _logger;

    private Mutex? _mutex;
    private bool _disposed;

    public SingleInstanceGuard(ILogger<SingleInstanceGuard>? logger = null, string? mutexName = null, string? pipeName = null)
    {
        _mutexName = mutexName ?? AppInfo.MutexName;
        _pipeName = pipeName ?? AppInfo.NamedPipeName;
        _logger = logger ?? NullLogger<SingleInstanceGuard>.Instance;
    }

    /// <summary>True when this process owns the mutex and should show a window.</summary>
    public bool IsFirstInstance { get; private set; }

    /// <summary>Name of the mutex used for the single instance check.</summary>
    public string MutexName => _mutexName;

    /// <summary>Name of the pipe used to activate the running instance.</summary>
    public string PipeName => _pipeName;

    /// <summary>Tries to become the single instance. Call exactly once at startup.</summary>
    public bool TryAcquire()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_mutex is not null)
        {
            return IsFirstInstance;
        }

        try
        {
            _mutex = new Mutex(initiallyOwned: true, _mutexName, out bool createdNew);
            IsFirstInstance = createdNew;
        }
        catch (UnauthorizedAccessException)
        {
            // Another session already owns it.
            IsFirstInstance = false;
        }
        catch (System.Security.SecurityException)
        {
            IsFirstInstance = false;
        }

        return IsFirstInstance;
    }

    /// <summary>Sends arguments to the running instance. Returns false when nobody is listening.</summary>
    public async Task<bool> SendArgumentsAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        try
        {
            await using NamedPipeClientStream client = new(
                serverName: ".",
                pipeName: _pipeName,
                direction: PipeDirection.Out,
                options: PipeOptions.Asynchronous);

            await client.ConnectAsync(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);

            StringBuilder payload = new();
            foreach (string argument in arguments)
            {
                payload.AppendLine(argument);
            }

            byte[] bytes = Encoding.UTF8.GetBytes(payload.ToString());
            await client.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            await client.FlushAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Listens for activation requests until <paramref name="cancellationToken"/> fires.
    /// Only the first instance should run this.
    /// </summary>
    public async Task ListenAsync(Func<IReadOnlyList<string>, Task> onActivated, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onActivated);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using NamedPipeServerStream server = new(
                    pipeName: _pipeName,
                    direction: PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    transmissionMode: PipeTransmissionMode.Byte,
                    options: PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                IReadOnlyList<string> arguments = await ReadArgumentsAsync(server, cancellationToken).ConfigureAwait(false);
                await onActivated(arguments).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException exception)
            {
                _logger.LogWarning(exception, "The activation pipe reported an error; listening continues.");
            }
        }
    }

    private static async Task<IReadOnlyList<string>> ReadArgumentsAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new();
        byte[] chunk = new byte[4096];

        int read;
        while ((read = await server.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            buffer.Write(chunk, 0, read);
        }

        string payload = Encoding.UTF8.GetString(buffer.ToArray());

        List<string> arguments = new();
        foreach (string line in payload.Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                arguments.Add(trimmed);
            }
        }

        return arguments;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_mutex is not null)
        {
            if (IsFirstInstance)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // The mutex was already abandoned; nothing to clean up.
                }
            }

            _mutex.Dispose();
            _mutex = null;
        }
    }
}
