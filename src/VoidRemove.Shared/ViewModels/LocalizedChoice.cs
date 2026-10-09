using CommunityToolkit.Mvvm.ComponentModel;
using VoidRemove.Shared.Localization;

namespace VoidRemove.Shared.ViewModels;

/// <summary>
/// One entry of a drop down: a value plus its localized label.
/// </summary>
/// <remarks>
/// The label refreshes itself when the language changes, which is how the pickers in both
/// front ends stay correct after an instant language switch.
/// </remarks>
public sealed class LocalizedChoice<T> : ObservableObject
    where T : notnull
{
    private readonly ILocalizationService _localization;
    private string _display;

    public LocalizedChoice(T value, string displayKey, ILocalizationService localization)
    {
        Value = value;
        DisplayKey = displayKey;
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _display = localization[displayKey];
    }

    /// <summary>The value stored in the settings.</summary>
    public T Value { get; }

    /// <summary>Localization key backing <see cref="Display"/>.</summary>
    public string DisplayKey { get; }

    /// <summary>Localized label.</summary>
    public string Display
    {
        get => _display;
        private set => SetProperty(ref _display, value);
    }

    /// <summary>Re-reads the label from the active catalog.</summary>
    public void Refresh() => Display = _localization[DisplayKey];

    /// <inheritdoc />
    public override string ToString() => Display;
}
