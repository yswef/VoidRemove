using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace VoidRemove.Core.Toolbox;

/// <summary>
/// Irreversibly destroys a file by overwriting it several times before unlinking it.
/// The warning lives in the UI; the guarantee lives here.
/// </summary>
public sealed class FileShredder
{
    private readonly ILogger<FileShredder>? _logger;

    public FileShredder(ILogger<FileShredder>? logger = null) => _logger = logger;

    /// <summary>Overwrites then deletes; returns true on success.</summary>
    public async Task<bool> ShredAsync(string path, int passes = 3, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            FileInfo info = new(path);
            long length = info.Length;

            // Clear read-only so we can open for write.
            if (info.IsReadOnly)
            {
                info.IsReadOnly = false;
            }

            await Task.Run(() =>
            {
                for (int pass = 0; pass < Math.Max(1, passes); pass++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Overwrite(path, length, pass);
                }
            }, cancellationToken);

            File.Delete(path);
            _logger?.LogInformation("Shredded {Path} with {Passes} passes.", path, passes);
            return true;
        }
        catch (IOException exception)
        {
            _logger?.LogWarning(exception, "Could not shred {Path}.", path);
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger?.LogWarning(exception, "Could not shred {Path}.", path);
            return false;
        }
    }

    private static void Overwrite(string path, long length, int pass)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Write, FileShare.None);
        byte[] buffer = new byte[Math.Min(65536, length == 0 ? 1 : length)];

        FillPattern(buffer, pass);

        long remaining = length;
        while (remaining > 0)
        {
            int toWrite = (int)Math.Min(buffer.Length, remaining);
            stream.Write(buffer, 0, toWrite);
            remaining -= toWrite;
        }

        stream.Flush(flushToDisk: true);
    }

    private static void FillPattern(byte[] buffer, int pass)
    {
        switch (pass % 3)
        {
            case 0:
                Array.Clear(buffer, 0, buffer.Length);
                break;
            case 1:
                Array.Fill(buffer, (byte)0xFF);
                break;
            default:
                RandomNumberGenerator.Fill(buffer);
                break;
        }
    }
}
