namespace VoidRemove.Shared.Theming;

/// <summary>Raised after the active theme or accent color changed.</summary>
public sealed class ThemeChangedEventArgs : EventArgs
{
    public ThemeChangedEventArgs(ThemeDefinition theme)
    {
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    /// <summary>The theme that is active now, accent override already applied.</summary>
    public ThemeDefinition Theme { get; }
}
