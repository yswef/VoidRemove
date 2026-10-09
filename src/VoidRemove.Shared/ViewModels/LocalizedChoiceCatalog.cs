using VoidRemove.Shared.Localization;
using VoidRemove.Shared.Models;
using VoidRemove.Shared.Theming;

namespace VoidRemove.Shared.ViewModels;

/// <summary>Builds the drop down lists used by the settings and home pages.</summary>
public static class LocalizedChoiceCatalog
{
    public static IReadOnlyList<LocalizedChoice<LayoutMode>> Layouts(ILocalizationService localization) => new[]
    {
        new LocalizedChoice<LayoutMode>(LayoutMode.List, LocalizationKeys.LayoutList, localization),
        new LocalizedChoice<LayoutMode>(LayoutMode.Cards, LocalizationKeys.LayoutCards, localization),
        new LocalizedChoice<LayoutMode>(LayoutMode.Compact, LocalizationKeys.LayoutCompact, localization),
    };

    public static IReadOnlyList<LocalizedChoice<UninstallBehavior>> UninstallBehaviors(ILocalizationService localization) => new[]
    {
        new LocalizedChoice<UninstallBehavior>(UninstallBehavior.Interactive, LocalizationKeys.BehaviorInteractive, localization),
        new LocalizedChoice<UninstallBehavior>(UninstallBehavior.Silent, LocalizationKeys.BehaviorSilent, localization),
        new LocalizedChoice<UninstallBehavior>(UninstallBehavior.Force, LocalizationKeys.BehaviorForce, localization),
    };

    public static IReadOnlyList<LocalizedChoice<SafetyLevel>> SafetyLevels(ILocalizationService localization) => new[]
    {
        new LocalizedChoice<SafetyLevel>(SafetyLevel.Safe, LocalizationKeys.SafetySafe, localization),
        new LocalizedChoice<SafetyLevel>(SafetyLevel.Moderate, LocalizationKeys.SafetyModerate, localization),
        new LocalizedChoice<SafetyLevel>(SafetyLevel.Advanced, LocalizationKeys.SafetyAdvanced, localization),
    };
}
