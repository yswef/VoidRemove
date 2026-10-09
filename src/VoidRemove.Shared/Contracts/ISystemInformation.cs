namespace VoidRemove.Shared.Contracts;

/// <summary>Read-only view of the machine the application runs on.</summary>
/// <remarks>Implemented by <c>VoidRemove.Core</c>; consumed by the shared view models.</remarks>
public interface ISystemInformation
{
    /// <summary>Friendly operating system name, e.g. "Windows 11 Pro 23H2 (build 22631)".</summary>
    string OperatingSystem { get; }

    /// <summary>Process architecture, e.g. "x64".</summary>
    string Architecture { get; }

    /// <summary>True when the process is elevated. VoidRemove needs this for HKLM and Program Files.</summary>
    bool IsAdministrator { get; }

    /// <summary>True on Windows 10 1809 or newer, the minimum the app supports.</summary>
    bool IsSupportedWindows { get; }
}
