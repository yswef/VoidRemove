using VoidRemove.Core.Security;

namespace VoidRemove.Core.Tests;

/// <summary>
/// Deterministic stand-in for DPAPI so the encryption wiring can be tested on any host.
/// It is reversible but obviously not secret, which is exactly what the tests need.
/// </summary>
internal sealed class ReversingSecretProtector : ISecretProtector
{
    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public string Prefix => "test:";

    /// <inheritdoc />
    public string Protect(string plainText) =>
        string.IsNullOrEmpty(plainText) ? string.Empty : Prefix + Reverse(plainText);

    /// <inheritdoc />
    public string Unprotect(string storedValue)
    {
        if (string.IsNullOrEmpty(storedValue))
        {
            return string.Empty;
        }

        return storedValue.StartsWith(Prefix, StringComparison.Ordinal)
            ? Reverse(storedValue.Substring(Prefix.Length))
            : storedValue;
    }

    private static string Reverse(string value)
    {
        char[] characters = value.ToCharArray();
        Array.Reverse(characters);
        return new string(characters);
    }
}
