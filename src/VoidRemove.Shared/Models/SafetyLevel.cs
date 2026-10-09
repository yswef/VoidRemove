namespace VoidRemove.Shared.Models;

/// <summary>
/// How risky it is to delete a discovered leftover.
/// </summary>
/// <remarks>
/// The removal engine grades every leftover it finds; the UI shows the grade and only
/// pre-selects items up to the ceiling configured in the settings. <see cref="Advanced"/>
/// items are never pre-selected.
/// </remarks>
public enum SafetyLevel
{
    /// <summary>Clearly belongs to the removed program. Safe to delete.</summary>
    Safe = 0,

    /// <summary>Very probably belongs to the program, but could be shared. Deleted only when the user agrees.</summary>
    Moderate = 1,

    /// <summary>Shared or system adjacent. Never pre-selected; requires an explicit user decision.</summary>
    Advanced = 2,
}
