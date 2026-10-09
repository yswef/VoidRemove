using Microsoft.UI.Xaml.Data;
using VoidRemove.Shared.ViewModels;

namespace VoidRemove.UI.WinUI.Converters;

/// <summary>
/// Maps a sidebar entry to a Segoe Fluent Icons glyph.
/// </summary>
/// <remarks>
/// The shared icon set is stroked path data, which WinUI cannot bind to a
/// <c>Path.Data</c> without a converter. Until the full WinUI design system lands, the
/// navigation uses font glyphs for the two well known code points below.
/// </remarks>
public sealed class NavGlyphConverter : IValueConverter
{
    private const string HomeGlyph = "\uE80F";
    private const string SettingsGlyph = "\uE713";
    private const string FallbackGlyph = "\uE7F4";

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language) => value switch
    {
        NavigationItem { Id: ShellViewModel.HomeId } => HomeGlyph,
        NavigationItem { Id: ShellViewModel.SettingsId } => SettingsGlyph,
        _ => FallbackGlyph,
    };

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
