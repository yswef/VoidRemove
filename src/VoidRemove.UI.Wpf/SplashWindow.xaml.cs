using System.Windows;
using VoidRemove.Shared;
using VoidRemove.Shared.Localization;

namespace VoidRemove.UI.Wpf;

/// <summary>
/// The startup splash. It shows the identity while the composition root, the settings file
/// and the program cache warm up, then gets out of the way.
/// </summary>
public partial class SplashWindow : Window
{
    public SplashWindow(ILocalizationService localization)
    {
        InitializeComponent();
        Title = AppInfo.WindowTitle;
        TaglineText.Text = localization[LocalizationKeys.AppTagline];
    }
}
