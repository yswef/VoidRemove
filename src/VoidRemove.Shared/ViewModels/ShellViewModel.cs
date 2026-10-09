using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoidRemove.Shared.Icons;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Shared.ViewModels;

/// <summary>
/// Drives the application shell: the sidebar, the selected section and the page shown in
/// the content area. Both the WPF and the WinUI window bind to this one object, which is
/// why the two front ends navigate identically. Front ends register their section pages
/// through <see cref="RegisterSection"/> so the sidebar grows with the available features.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    public const string HomeId = "home";
    public const string SettingsId = "settings";

    private readonly ILocalizationService _localization;
    private readonly Dictionary<string, ObservableObject> _pages;
    private readonly List<(string GroupId, string TitleKey, List<NavigationItem> Items)> _groupDefinitions;
    private List<NavigationGroup> _groups;
    private List<NavigationItem> _items;

    [ObservableProperty]
    private NavigationItem? _selectedItem;

    [ObservableProperty]
    private object? _currentPage;

    public ShellViewModel(
        ILocalizationService localization,
        HomeViewModel home,
        SettingsViewModel settings)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        Home = home ?? throw new ArgumentNullException(nameof(home));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));

        HomeItem = new NavigationItem(HomeId, LocalizationKeys.NavHome, IconGeometry.Home, localization);
        SettingsItem = new NavigationItem(SettingsId, LocalizationKeys.NavSettings, IconGeometry.Settings, localization);

        _pages = new Dictionary<string, ObservableObject>(StringComparer.Ordinal)
        {
            [HomeId] = Home,
            [SettingsId] = Settings,
        };

        _groupDefinitions = new List<(string, string, List<NavigationItem>)>
        {
            (LocalizationKeys.NavGroupLibrary, LocalizationKeys.NavGroupLibrary, new List<NavigationItem> { HomeItem, SettingsItem }),
        };

        _groups = new List<NavigationGroup>();
        _items = new List<NavigationItem>();
        RebuildNavigation();

        _localization.LanguageChanged += OnLanguageChanged;
        SelectedItem = HomeItem;
    }

    /// <summary>Home dashboard.</summary>
    public HomeViewModel Home { get; }

    /// <summary>Settings page.</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>Sidebar groups, in display order.</summary>
    public IReadOnlyList<NavigationGroup> Groups => _groups;

    /// <summary>Every sidebar entry, flattened. Used by keyboard navigation.</summary>
    public IReadOnlyList<NavigationItem> Items => _items;

    /// <summary>Sidebar entry for the home page.</summary>
    public NavigationItem HomeItem { get; }

    /// <summary>Sidebar entry for the settings page.</summary>
    public NavigationItem SettingsItem { get; }

    /// <summary>Window caption, built from <see cref="AppInfo"/>.</summary>
    public string WindowTitle => AppInfo.WindowTitle;

    /// <summary>Product name, localized where it makes sense.</summary>
    public string ProductName => _localization[LocalizationKeys.AppName];

    /// <summary>One line product description.</summary>
    public string Tagline => _localization[LocalizationKeys.AppTagline];

    /// <summary>"Version x.y.z" in the active language.</summary>
    public string VersionText => _localization.Format(LocalizationKeys.AppVersion, AppInfo.Version);

    /// <summary>Offline-first notice shown under the logo.</summary>
    public string OfflineNotice => _localization[LocalizationKeys.AppOffline];

    /// <summary>
    /// Adds a sidebar entry bound to a page view model. The front end calls this once per
    /// section it implements; the sidebar and the page map update together.
    /// </summary>
    public NavigationItem RegisterSection(string groupId, string groupTitleKey, string id, string titleKey, string iconPath, ObservableObject page)
    {
        NavigationItem item = new(id, titleKey, iconPath, _localization);

        List<NavigationItem>? existingItems = null;
        foreach ((string groupIdCandidate, _, List<NavigationItem> groupItems) in _groupDefinitions)
        {
            if (groupIdCandidate == groupId)
            {
                existingItems = groupItems;
                break;
            }
        }

        if (existingItems is null)
        {
            _groupDefinitions.Add((groupId, groupTitleKey, new List<NavigationItem> { item }));
        }
        else
        {
            existingItems.Add(item);
        }

        _pages[id] = page;
        RebuildNavigation();
        return item;
    }

    /// <summary>Selects the entry with the given id. Unknown ids are ignored.</summary>
    public void Select(string id)
    {
        foreach (NavigationItem item in Items)
        {
            if (string.Equals(item.Id, id, StringComparison.Ordinal))
            {
                SelectedItem = item;
                return;
            }
        }
    }

    /// <summary>Jumps to the settings page. Used by the top level menu.</summary>
    [RelayCommand]
    private void OpenSettings() => Select(SettingsId);

    /// <summary>Jumps to the home page.</summary>
    [RelayCommand]
    private void OpenHome() => Select(HomeId);

    partial void OnSelectedItemChanged(NavigationItem? value)
    {
        CurrentPage = value is not null && _pages.TryGetValue(value.Id, out ObservableObject? page) ? page : null;
    }

    private void RebuildNavigation()
    {
        _groups = _groupDefinitions
            .Select(definition => new NavigationGroup(definition.TitleKey, definition.Items, _localization))
            .ToList();

        _items = _groups.SelectMany(static group => group.Items).ToList();

        OnPropertyChanged(nameof(Groups));
        OnPropertyChanged(nameof(Items));
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        RebuildNavigation();

        foreach (NavigationItem item in _items)
        {
            item.Refresh();
        }

        Home.Refresh();
        Settings.Refresh();

        OnPropertyChanged(nameof(ProductName));
        OnPropertyChanged(nameof(Tagline));
        OnPropertyChanged(nameof(VersionText));
        OnPropertyChanged(nameof(OfflineNotice));
    }
}
