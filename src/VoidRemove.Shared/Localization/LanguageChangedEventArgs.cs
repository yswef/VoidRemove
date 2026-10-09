namespace VoidRemove.Shared.Localization;

/// <summary>Raised after the active language changed.</summary>
public sealed class LanguageChangedEventArgs : EventArgs
{
    public LanguageChangedEventArgs(string previousLanguage, string currentLanguage)
    {
        PreviousLanguage = previousLanguage;
        CurrentLanguage = currentLanguage;
    }

    /// <summary>Language code that was active before the switch.</summary>
    public string PreviousLanguage { get; }

    /// <summary>Language code that is active now.</summary>
    public string CurrentLanguage { get; }
}
