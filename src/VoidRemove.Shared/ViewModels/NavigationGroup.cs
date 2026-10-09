using CommunityToolkit.Mvvm.ComponentModel;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Shared.ViewModels;

/// <summary>A labeled group of sidebar entries, e.g. "Library" or "Tools".</summary>
public sealed class NavigationGroup : ObservableObject
{
    private readonly ILocalizationService _localization;
    private string _title;

    public NavigationGroup(string titleKey, IReadOnlyList<NavigationItem> items, ILocalizationService localization)
    {
        TitleKey = titleKey;
        Items = items ?? throw new ArgumentNullException(nameof(items));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _title = localization[titleKey];
    }

    /// <summary>Localization key for the group header.</summary>
    public string TitleKey { get; }

    /// <summary>Entries shown under the header.</summary>
    public IReadOnlyList<NavigationItem> Items { get; }

    /// <summary>Localized group header.</summary>
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    /// <summary>Re-reads the header from the active catalog.</summary>
    public void Refresh() => Title = _localization[TitleKey];
}
