namespace VoidRemove.Core.Settings;

/// <summary>
/// Machine wide key/value store used to mirror the settings so services and scripts can
/// read them without parsing JSON. Backed by <c>HKLM\Software\VoidRemove</c> on Windows.
/// </summary>
public interface IKeyValueStore
{
    /// <summary>False when the store cannot be opened, e.g. without administrator rights.</summary>
    bool IsAvailable { get; }

    /// <summary>Human readable location, used in logs.</summary>
    string Description { get; }

    bool TrySetString(string name, string value);

    bool TrySetInt32(string name, int value);

    bool TrySetBool(string name, bool value);

    bool TryGetString(string name, out string value);

    bool TryGetInt32(string name, out int value);

    bool TryDelete(string name);
}
