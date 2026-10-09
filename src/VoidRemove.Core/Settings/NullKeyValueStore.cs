namespace VoidRemove.Core.Settings;

/// <summary>
/// No-op store used on non-Windows hosts (unit tests, CI containers) and whenever the
/// registry mirror is unavailable. Settings keep working; only the mirror is missing.
/// </summary>
public sealed class NullKeyValueStore : IKeyValueStore
{
    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public string Description => "none";

    /// <inheritdoc />
    public bool TrySetString(string name, string value) => false;

    /// <inheritdoc />
    public bool TrySetInt32(string name, int value) => false;

    /// <inheritdoc />
    public bool TrySetBool(string name, bool value) => false;

    /// <inheritdoc />
    public bool TryGetString(string name, out string value)
    {
        value = string.Empty;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetInt32(string name, out int value)
    {
        value = 0;
        return false;
    }

    /// <inheritdoc />
    public bool TryDelete(string name) => false;
}
