namespace VoidRemove.Shared.Theming;

/// <summary>How the program list is laid out.</summary>
public enum LayoutMode
{
    /// <summary>One row per program with all columns. The default.</summary>
    List = 0,

    /// <summary>Large cards with the icon, name, publisher and size.</summary>
    Cards = 1,

    /// <summary>Dense rows that fit more programs on screen.</summary>
    Compact = 2,
}
