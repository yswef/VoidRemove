namespace VoidRemove.Core.Tests;

/// <summary>A throwaway folder that deletes itself, so tests never touch real data.</summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        FullPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "voidremove-tests-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(FullPath);
    }

    /// <summary>Absolute path of the folder.</summary>
    public string FullPath { get; }

    /// <summary>Combines a relative name with the folder.</summary>
    public string Combine(string relative) => System.IO.Path.Combine(FullPath, relative);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(FullPath))
            {
                Directory.Delete(FullPath, recursive: true);
            }
        }
        catch (IOException)
        {
            // A file handle may still be open; the OS temp cleaner will finish the job.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
