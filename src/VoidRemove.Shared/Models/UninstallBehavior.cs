namespace VoidRemove.Shared.Models;

/// <summary>How the removal engine should run a program's uninstaller.</summary>
public enum UninstallBehavior
{
    /// <summary>Run the vendor uninstaller and let the user interact with it.</summary>
    Interactive = 0,

    /// <summary>Run the vendor uninstaller with its quiet switches and wait for it.</summary>
    Silent = 1,

    /// <summary>Skip the vendor uninstaller entirely and clean up by install path and name.</summary>
    Force = 2,
}
