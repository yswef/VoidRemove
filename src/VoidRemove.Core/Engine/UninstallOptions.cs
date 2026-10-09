using VoidRemove.Shared.Models;

namespace VoidRemove.Core.Engine;

/// <summary>The knobs the confirm dialog binds to before a removal run.</summary>
public sealed record UninstallOptions
{
    public bool CreateRestorePoint { get; init; }

    public bool RemoveResiduals { get; init; } = true;

    /// <summary>The safety ceiling; residuals riskier than this are left alone.</summary>
    public SafetyLevel MaxSafetyLevel { get; init; } = SafetyLevel.Moderate;

    /// <summary>When true, everything is computed and reported but nothing is touched.</summary>
    public bool DryRun { get; init; }

    /// <summary>Prefer the quiet uninstall string when available.</summary>
    public bool PreferQuiet { get; init; } = true;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(10);
}
