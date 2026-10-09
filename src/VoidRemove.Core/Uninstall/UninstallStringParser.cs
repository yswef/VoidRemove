using System.Text.RegularExpressions;

namespace VoidRemove.Core.Uninstall;

/// <summary>
/// Turns the free-form "UninstallString" / "QuietUninstallString" registry values into a
/// structured, runnable command. Pure and fully unit tested.
/// </summary>
public static partial class UninstallStringParser
{
    [GeneratedRegex(@"\{?(?<guid>[0-9A-Fa-f]{8}(?:-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12})\}?", RegexOptions.Compiled)]
    private static partial Regex GuidRegex();

    /// <summary>Extracts and normalizes an MSI product code, or null when absent.</summary>
    public static string? ExtractProductCode(string? uninstallString)
    {
        if (string.IsNullOrWhiteSpace(uninstallString))
        {
            return null;
        }

        Match match = GuidRegex().Match(uninstallString);
        return match.Success ? "{" + match.Groups["guid"].Value.ToUpperInvariant() + "}" : null;
    }

    /// <summary>Parses the raw uninstall string into a runnable command.</summary>
    public static UninstallCommand Parse(string? raw, bool isWindowsInstaller = false)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new UninstallCommand { Raw = string.Empty };
        }

        string trimmed = raw.Trim();

        if (IsMsiExec(trimmed) || (isWindowsInstaller && ExtractProductCode(trimmed) is not null))
        {
            return new UninstallCommand
            {
                Kind = UninstallKind.MsiExec,
                Executable = "msiexec.exe",
                Arguments = trimmed,
                ProductCode = ExtractProductCode(trimmed) ?? string.Empty,
                Raw = trimmed,
            };
        }

        if (IsMsix(trimmed))
        {
            return new UninstallCommand
            {
                Kind = UninstallKind.Msix,
                PackageFamilyName = ExtractPackageFamilyName(trimmed),
                Arguments = trimmed,
                Raw = trimmed,
            };
        }

        (string executable, string arguments) = SplitExecutableAndArguments(trimmed);
        if (string.IsNullOrEmpty(executable))
        {
            return new UninstallCommand { Raw = trimmed };
        }

        return new UninstallCommand
        {
            Kind = UninstallKind.Custom,
            Executable = executable,
            Arguments = arguments,
            Raw = trimmed,
        };
    }

    /// <summary>Builds the argument list used to uninstall an MSI quietly.</summary>
    public static string BuildMsiQuietArguments(string productCode) =>
        $"/X{productCode} /qn /norestart";

    private static bool IsMsiExec(string value) =>
        value.Contains("msiexec", StringComparison.OrdinalIgnoreCase) &&
        value.Contains(".exe", StringComparison.OrdinalIgnoreCase);

    private static bool IsMsix(string value) =>
        value.Contains("Remove-AppxPackage", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("Remove-ProvisionedAppxPackage", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("powershell", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("pwsh", StringComparison.OrdinalIgnoreCase);

    private static string ExtractPackageFamilyName(string value)
    {
        // "powershell -command \"Remove-AppxPackage -Package 'Name_1.0.0.0_x64__abc123'\""
        Match quoted = Regex.Match(value, "-Package\\s+'?\"?(?<pfn>[^'\"\\s]+)'?\"?", RegexOptions.IgnoreCase);
        return quoted.Success ? quoted.Groups["pfn"].Value : string.Empty;
    }

    /// <summary>
    /// Splits a command line into the executable and its argument tail, honouring quotes and
    /// the common "path\uninstall.exe" shape where the exe itself is not quoted.
    /// </summary>
    public static (string Executable, string Arguments) SplitExecutableAndArguments(string raw)
    {
        string trimmed = raw.Trim();

        if (trimmed.StartsWith('"'))
        {
            int close = trimmed.IndexOf('"', 1);
            if (close > 0)
            {
                return (trimmed.Substring(1, close - 1), trimmed.Substring(close + 1).TrimStart());
            }

            return (trimmed.Trim('"'), string.Empty);
        }

        int exeEnd = IndexOfIgnoreCase(trimmed, ".exe");
        if (exeEnd >= 0)
        {
            int end = exeEnd + ".exe".Length;
            return (trimmed.Substring(0, end).Trim(), SafeTail(trimmed, end));
        }

        int space = trimmed.IndexOf(' ');
        if (space > 0)
        {
            return (trimmed.Substring(0, space), SafeTail(trimmed, space));
        }

        return (trimmed, string.Empty);
    }

    private static string SafeTail(string value, int index) =>
        index < value.Length ? value.Substring(index).TrimStart() : string.Empty;

    private static int IndexOfIgnoreCase(string value, string token) =>
        value.IndexOf(token, StringComparison.OrdinalIgnoreCase);
}
