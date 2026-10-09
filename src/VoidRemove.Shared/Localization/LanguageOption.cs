namespace VoidRemove.Shared.Localization;

/// <summary>A language the user can switch to.</summary>
/// <param name="Code">BCP-47 two letter code, e.g. <c>en</c> or <c>ar</c>.</param>
/// <param name="NativeName">Name written in that language, used by the language picker.</param>
public sealed record LanguageOption(string Code, string NativeName)
{
    /// <summary>The default language used when nothing else matches.</summary>
    public const string FallbackCode = "en";

    public override string ToString() => NativeName;
}
