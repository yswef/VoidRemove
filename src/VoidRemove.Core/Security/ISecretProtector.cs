namespace VoidRemove.Core.Security;

/// <summary>
/// Protects secrets that have to be persisted, such as the VirusTotal API key.
/// </summary>
/// <remarks>
/// The stored form carries a prefix so a value written by an older build (or by a
/// non-Windows test run) can still be read back instead of being decrypted into garbage.
/// </remarks>
public interface ISecretProtector
{
    /// <summary>False when the platform cannot offer real protection.</summary>
    bool IsAvailable { get; }

    /// <summary>Marker written in front of a protected value.</summary>
    string Prefix { get; }

    /// <summary>Protects a secret. Empty input is returned unchanged.</summary>
    string Protect(string plainText);

    /// <summary>Reads a stored secret back. Values without the prefix are returned as they are.</summary>
    string Unprotect(string storedValue);
}
