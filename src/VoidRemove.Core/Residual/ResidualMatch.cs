using VoidRemove.Shared.Models;

namespace VoidRemove.Core.Residual;

/// <summary>The verdict of matching one candidate against one program identity.</summary>
public sealed record ResidualMatch
{
    public bool IsMatch { get; init; }

    public SafetyLevel Level { get; init; } = SafetyLevel.Advanced;

    /// <summary>Confidence between 0 and 1.</summary>
    public double Score { get; init; }

    /// <summary>Human-readable explanation shown in the confirm dialog.</summary>
    public string Reason { get; init; } = string.Empty;

    public static ResidualMatch None { get; } = new() { IsMatch = false, Reason = "no identity overlap" };
}
