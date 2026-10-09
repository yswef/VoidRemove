namespace VoidRemove.Core.Residual;

/// <summary>The kind of leftover a scan can discover.</summary>
public enum ResidualKind
{
    Directory,
    File,
    Shortcut,
    RegistryKey,
    RegistryValue,
    Service,
    ScheduledTask,
    FirewallRule,
    PathEntry,
    ShellExtension,
}
