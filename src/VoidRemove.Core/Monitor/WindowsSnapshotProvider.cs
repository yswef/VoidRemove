using Microsoft.Win32;

namespace VoidRemove.Core.Monitor;

/// <summary>
/// Captures files, uninstall registry keys, services and scheduled tasks. Enumeration is
/// capped so a busy machine cannot stall the monitor.
/// </summary>
public sealed class WindowsSnapshotProvider : ISnapshotProvider
{
    private const int FileCap = 250_000;

    public bool IsSupported => System.OperatingSystem.IsWindows();

    public SystemSnapshot Capture(CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            return SystemSnapshot.Empty;
        }

        return new SystemSnapshot
        {
            Files = CaptureFiles(cancellationToken),
            RegistryKeys = CaptureUninstallKeys(),
            Services = CaptureSubKeyNames(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services"),
            ScheduledTasks = CaptureTasks(),
        };
    }

    private static HashSet<string> CaptureFiles(CancellationToken cancellationToken)
    {
        HashSet<string> files = new(StringComparer.OrdinalIgnoreCase);

        foreach (string root in WatchedRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            var enumerator = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    files.Add(enumerator.Current);
                    if (files.Count >= FileCap)
                    {
                        return files;
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        return files;
    }

    private static IEnumerable<string> WatchedRoots()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    }

    private static HashSet<string> CaptureUninstallKeys()
    {
        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);

        foreach ((RegistryHive hive, string name) in new[]
                 {
                     (RegistryHive.LocalMachine, "HKLM"),
                     (RegistryHive.CurrentUser, "HKCU"),
                 })
        {
            foreach (string subKey in CaptureSubKeyNames(hive, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                keys.Add(name + "\\" + subKey);
            }
        }

        return keys;
    }

    private static HashSet<string> CaptureSubKeyNames(RegistryHive hive, string keyPath)
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using RegistryKey? key = baseKey.OpenSubKey(keyPath, writable: false);
            if (key is not null)
            {
                foreach (string subKey in key.GetSubKeyNames())
                {
                    names.Add(subKey);
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return names;
    }

    private static HashSet<string> CaptureTasks()
    {
        HashSet<string> tasks = new(StringComparer.OrdinalIgnoreCase);
        string tasksRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "System32", "Tasks");

        if (!Directory.Exists(tasksRoot))
        {
            return tasks;
        }

        try
        {
            foreach (string file in Directory.EnumerateFiles(tasksRoot, "*", SearchOption.AllDirectories))
            {
                tasks.Add(Path.GetRelativePath(tasksRoot, file));
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return tasks;
    }
}
