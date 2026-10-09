using System.ComponentModel;

namespace VoidRemove.Shared.Localization;

/// <summary>
/// Resolves localized strings and broadcasts language changes.
/// </summary>
/// <remarks>
/// Implements <see cref="INotifyPropertyChanged"/> and raises <c>Item[]</c> so that
/// <c>{Binding Loc[some.key]}</c> bindings refresh on the fly in both WPF and WinUI.
/// <para>
/// The layout never mirrors: <see cref="LayoutIsMirrored"/> is permanently <c>false</c>.
/// Arabic changes the words, not the geometry.
/// </para>
/// </remarks>
public interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>Returns the translation for <paramref name="key"/>, or the key itself when missing.</summary>
    string this[string key] { get; }

    /// <summary>Active language code.</summary>
    string CurrentLanguage { get; }

    /// <summary>Languages available in this build, default language first.</summary>
    IReadOnlyList<LanguageOption> Languages { get; }

    /// <summary>Always <c>false</c>: the application layout is permanently LTR.</summary>
    bool LayoutIsMirrored { get; }

    /// <summary>Raised whenever <see cref="CurrentLanguage"/> changes.</summary>
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    /// <summary>Translation for <paramref name="key"/> with <c>{0}</c>-style placeholders filled in.</summary>
    string Format(string key, params object?[] args);

    /// <summary>Switches the active language. Unknown codes fall back to English.</summary>
    void SetLanguage(string languageCode);

    /// <summary>True when the key resolves to a real translation.</summary>
    bool TryGetString(string key, out string value);
}
