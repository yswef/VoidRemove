using CommunityToolkit.Mvvm.ComponentModel;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Shared.ViewModels;

/// <summary>One sidebar entry.</summary>
public sealed class NavigationItem : ObservableObject
{
    private readonly ILocalizationService _localization;
    private string _title;

    public NavigationItem(string id, string titleKey, string icon, ILocalizationService localization)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A navigation item needs an id.", nameof(id));
        }

        Id = id;
        TitleKey = titleKey;
        Icon = icon;
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _title = localization[titleKey];
    }

    /// <summary>Stable identifier used to route to the right page.</summary>
    public string Id { get; }

    /// <summary>Localization key backing <see cref="Title"/>.</summary>
    public string TitleKey { get; }

    /// <summary>Path data from <see cref="Icons.IconGeometry"/> on a 24x24 grid.</summary>
    public string Icon { get; }

    /// <summary>Localized label.</summary>
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    /// <summary>Re-reads the label from the active catalog.</summary>
    public void Refresh() => Title = _localization[TitleKey];

    /// <inheritdoc />
    public override string ToString() => Title;
}
