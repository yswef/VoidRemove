using System.Text;

namespace VoidRemove.Core.Residual;

/// <summary>Shared normalization so names, folders and registry keys compare consistently.</summary>
internal static class TextNormalizer
{
    /// <summary>Lowercases and strips everything except a-z0-9, e.g. "My App (x64)" -&gt; "myappx64".</summary>
    internal static string Compact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    /// <summary>Splits a name into lowercase alphanumeric tokens.</summary>
    internal static List<string> Tokens(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        return value
            .ToLowerInvariant()
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 3)
            .Distinct()
            .ToList();
    }

    private static readonly char[] Separators =
    {
        ' ', '\t', '-', '_', '.', '(', ')', '[', ']', '{', '}', ',', '/', '\\', '+', '&',
    };
}
