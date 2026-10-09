namespace VoidRemove.Core.Security;

/// <summary>
/// Pass-through protector used by tests and on platforms without DPAPI.
/// </summary>
/// <remarks>
/// It exists so the settings service has no platform branches in its own code. Production
/// always resolves <see cref="DpapiSecretProtector"/> on Windows.
/// </remarks>
public sealed class PlainTextSecretProtector : ISecretProtector
{
    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public string Prefix => string.Empty;

    /// <inheritdoc />
    public string Protect(string plainText) => plainText ?? string.Empty;

    /// <inheritdoc />
    public string Unprotect(string storedValue) => storedValue ?? string.Empty;
}
