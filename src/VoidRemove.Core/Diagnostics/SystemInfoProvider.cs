using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using VoidRemove.Shared.Contracts;

namespace VoidRemove.Core.Diagnostics;

/// <summary>Reads what the application needs to know about the machine it runs on.</summary>
public sealed class SystemInfoProvider : ISystemInformation
{
    private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

    private readonly Lazy<string> _operatingSystem;

    public SystemInfoProvider()
    {
        IsAdministrator = ComputeIsAdministrator();
        IsSupportedWindows = System.OperatingSystem.IsWindows() && System.OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);
        _operatingSystem = new Lazy<string>(DescribeOperatingSystem);
    }

    /// <inheritdoc />
    public string OperatingSystem => _operatingSystem.Value;

    /// <inheritdoc />
    public string Architecture => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

    /// <inheritdoc />
    public bool IsAdministrator { get; }

    /// <inheritdoc />
    public bool IsSupportedWindows { get; }

    private static bool ComputeIsAdministrator()
    {
        if (!System.OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    private static string DescribeOperatingSystem()
    {
        if (!System.OperatingSystem.IsWindows())
        {
            return RuntimeInformation.OSDescription;
        }

        string productName = "Windows";
        string displayVersion = string.Empty;
        string build = Environment.OSVersion.Version.Build.ToString(System.Globalization.CultureInfo.InvariantCulture);

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey? key = baseKey.OpenSubKey(CurrentVersionKey, writable: false);

            if (key is not null)
            {
                productName = key.GetValue("ProductName") as string ?? productName;
                displayVersion = key.GetValue("DisplayVersion") as string ?? string.Empty;
                build = key.GetValue("CurrentBuild") as string ?? build;
            }
        }
        catch (System.Security.SecurityException)
        {
            // The generic description below is good enough.
        }
        catch (UnauthorizedAccessException)
        {
        }

        // Windows 11 still reports "Windows 10 <edition>" in ProductName.
        if (int.TryParse(build, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int buildNumber)
            && buildNumber >= 22000
            && productName.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase))
        {
            productName = "Windows 11" + productName.Substring("Windows 10".Length);
        }

        string edition = string.IsNullOrWhiteSpace(displayVersion) ? string.Empty : " " + displayVersion;
        return $"{productName}{edition} (build {build})";
    }
}
