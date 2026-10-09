using Microsoft.Win32;
using VoidRemove.Shared;

namespace VoidRemove.Core.Shell;

/// <summary>
/// Owns the Explorer context-menu integration. It writes and clears only VoidRemove's own
/// shell verbs under the file classes, never touching other handlers, and is fully removed
/// by the uninstaller.
/// </summary>
public sealed class ExplorerContextMenuService
{
    private static readonly string[] FileClasses = { ".exe", ".lnk", ".msi" };

    public bool IsSupported => System.OperatingSystem.IsWindows();

    /// <summary>Builds the two command lines the shell verbs will launch.</summary>
    public static (string Uninstall, string ResetApp) BuildCommandLines(string executablePath) =>
        (
            $"\"{executablePath}\" {AppInfo.ArgUninstall} \"%1\"",
            $"\"{executablePath}\" {AppInfo.ArgResetApp} \"%1\""
        );

    /// <summary>Writes the VoidRemove verbs for .exe/.lnk/.msi.</summary>
    public void Install(string executablePath, string uninstallLabel, string resetLabel)
    {
        if (!IsSupported)
        {
            return;
        }

        (string uninstallCommand, string resetCommand) = BuildCommandLines(executablePath);

        foreach (string fileClass in FileClasses)
        {
            WriteVerb(fileClass, AppInfo.ExplorerVerbUninstallId, uninstallLabel, uninstallCommand);
            WriteVerb(fileClass, AppInfo.ExplorerVerbResetId, resetLabel, resetCommand);
        }
    }

    /// <summary>Removes every VoidRemove verb; called from Settings and the uninstaller.</summary>
    public void Uninstall()
    {
        if (!IsSupported)
        {
            return;
        }

        foreach (string fileClass in FileClasses)
        {
            RemoveVerb(fileClass, AppInfo.ExplorerVerbUninstallId);
            RemoveVerb(fileClass, AppInfo.ExplorerVerbResetId);
        }
    }

    /// <summary>True when at least one verb is present.</summary>
    public bool IsInstalled()
    {
        if (!IsSupported)
        {
            return false;
        }

        using RegistryKey? classes = Registry.ClassesRoot;
        using RegistryKey? verb = classes?.OpenSubKey($"{FileClasses[0]}\\shell\\{AppInfo.ExplorerVerbUninstallId}");
        return verb is not null;
    }

    private static void WriteVerb(string fileClass, string verbId, string label, string command)
    {
        string verbPath = $"{fileClass}\\shell\\{verbId}";
        using RegistryKey verb = Registry.ClassesRoot.CreateSubKey(verbPath);
        verb.SetValue(string.Empty, label);
        verb.SetValue("Icon", "%1", RegistryValueKind.ExpandString);

        using RegistryKey commandKey = Registry.ClassesRoot.CreateSubKey(verbPath + "\\command");
        commandKey.SetValue(string.Empty, command);
    }

    private static void RemoveVerb(string fileClass, string verbId)
    {
        try
        {
            Registry.ClassesRoot.DeleteSubKeyTree($"{fileClass}\\shell\\{verbId}", throwOnMissingSubKey: false);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
