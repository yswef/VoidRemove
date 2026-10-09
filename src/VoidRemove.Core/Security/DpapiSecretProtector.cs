using System.Security.Cryptography;
using System.Text;
using VoidRemove.Shared;

namespace VoidRemove.Core.Security;

/// <summary>
/// DPAPI (<see cref="DataProtectionScope.LocalMachine"/>) protection for persisted secrets.
/// </summary>
/// <remarks>
/// Machine scope is deliberate: VoidRemove runs elevated and its settings belong to the
/// machine, not to whoever happens to be logged on. The optional entropy is not a secret —
/// DPAPI is the protection — it only stops a value from being replayed into an unrelated
/// application.
/// </remarks>
public sealed class DpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes(AppInfo.Id + ".secret.v1");

    /// <inheritdoc />
    public bool IsAvailable => OperatingSystem.IsWindows();

    /// <inheritdoc />
    public string Prefix => "dpapi:";

    /// <inheritdoc />
    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText) || !IsAvailable)
        {
            return plainText ?? string.Empty;
        }

        byte[] cipher = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plainText),
            Entropy,
            DataProtectionScope.LocalMachine);

        return Prefix + Convert.ToBase64String(cipher);
    }

    /// <inheritdoc />
    public string Unprotect(string storedValue)
    {
        if (string.IsNullOrEmpty(storedValue))
        {
            return string.Empty;
        }

        if (!storedValue.StartsWith(Prefix, StringComparison.Ordinal))
        {
            // Written by an older build or by the plain text protector.
            return storedValue;
        }

        if (!IsAvailable)
        {
            return string.Empty;
        }

        try
        {
            byte[] plain = ProtectedData.Unprotect(
                Convert.FromBase64String(storedValue.Substring(Prefix.Length)),
                Entropy,
                DataProtectionScope.LocalMachine);

            return Encoding.UTF8.GetString(plain);
        }
        catch (CryptographicException)
        {
            // A key that cannot be decrypted is treated as absent rather than fatal.
            return string.Empty;
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }
}
