using Microsoft.Win32;

namespace VoidRemove.Core.Settings;

/// <summary>
/// <see cref="IKeyValueStore"/> over <c>HKLM\Software\VoidRemove</c>.
/// </summary>
/// <remarks>
/// Every call is defensive: a missing key, a locked hive or a non elevated process must
/// never take the application down. Failures are reported through the return values and
/// logged by the caller.
/// </remarks>
public sealed class RegistryKeyValueStore : IKeyValueStore
{
    private readonly string _subKeyPath;
    private readonly RegistryView _view;

    public RegistryKeyValueStore(string subKeyPath, RegistryView view = RegistryView.Registry64)
    {
        if (string.IsNullOrWhiteSpace(subKeyPath))
        {
            throw new ArgumentException("A registry sub key path is required.", nameof(subKeyPath));
        }

        _subKeyPath = subKeyPath;
        _view = view;
    }

    /// <inheritdoc />
    public bool IsAvailable => OperatingSystem.IsWindows();

    /// <inheritdoc />
    public string Description => $@"HKLM\{_subKeyPath}";

    /// <inheritdoc />
    public bool TrySetString(string name, string value) => Write(name, value ?? string.Empty);

    /// <inheritdoc />
    public bool TrySetInt32(string name, int value) => Write(name, value);

    /// <inheritdoc />
    public bool TrySetBool(string name, bool value) => Write(name, value ? 1 : 0);

    /// <inheritdoc />
    public bool TryGetString(string name, out string value)
    {
        value = string.Empty;

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, _view);
            using RegistryKey? key = baseKey.OpenSubKey(_subKeyPath, writable: false);
            if (key?.GetValue(name) is string text)
            {
                value = text;
                return true;
            }
        }
        catch (System.Security.SecurityException)
        {
            // Not elevated: the mirror is optional.
        }
        catch (UnauthorizedAccessException)
        {
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryGetInt32(string name, out int value)
    {
        value = 0;

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, _view);
            using RegistryKey? key = baseKey.OpenSubKey(_subKeyPath, writable: false);
            if (key?.GetValue(name) is int number)
            {
                value = number;
                return true;
            }
        }
        catch (System.Security.SecurityException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryDelete(string name)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, _view);
            using RegistryKey? key = baseKey.OpenSubKey(_subKeyPath, writable: true);
            if (key is null)
            {
                return false;
            }

            key.DeleteValue(name, throwOnMissingValue: false);
            return true;
        }
        catch (System.Security.SecurityException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return false;
    }

    private bool Write(string name, object value)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, _view);
            using RegistryKey? key = baseKey.CreateSubKey(_subKeyPath, writable: true);
            if (key is null)
            {
                return false;
            }

            key.SetValue(name, value);
            return true;
        }
        catch (System.Security.SecurityException)
        {
            // Running without elevation: settings still work, only the mirror is skipped.
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
