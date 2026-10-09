using VoidRemove.Shared.Contracts;
using VoidRemove.Shared.Localization;
using VoidRemove.Shared.Theming;

namespace VoidRemove.Core.Settings;

/// <summary>
/// The single validator used by both the settings UI and the settings service, so a value
/// the interface accepts can never be rejected on write.
/// </summary>
public static class SettingsValidator
{
    /// <summary>Returns a localized message per problem. An empty list means the settings are valid.</summary>
    public static IReadOnlyList<string> Validate(AppSettings settings, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(localization);

        List<string> problems = new();

        if (string.IsNullOrWhiteSpace(settings.Language) || !IsKnownLanguage(settings.Language, localization))
        {
            problems.Add(localization[LocalizationKeys.ErrorsLanguageRequired]);
        }

        if (!RgbaColor.TryFromHex(settings.AccentColor, out _))
        {
            problems.Add(localization[LocalizationKeys.ErrorsInvalidColor]);
        }

        if (settings.QuarantineRetentionDays is < SettingsLimits.MinQuarantineRetentionDays
            or > SettingsLimits.MaxQuarantineRetentionDays)
        {
            problems.Add(localization.Format(
                LocalizationKeys.ErrorsValueOutOfRange,
                SettingsLimits.MinQuarantineRetentionDays,
                SettingsLimits.MaxQuarantineRetentionDays));
        }

        if (settings.MaxConcurrentScans is < SettingsLimits.MinMaxConcurrentScans
            or > SettingsLimits.MaxMaxConcurrentScans)
        {
            problems.Add(localization.Format(
                LocalizationKeys.ErrorsValueOutOfRange,
                SettingsLimits.MinMaxConcurrentScans,
                SettingsLimits.MaxMaxConcurrentScans));
        }

        if (!IsQuarantinePathAllowed(settings.QuarantinePath))
        {
            problems.Add(localization[LocalizationKeys.ErrorsInvalidPath]);
        }

        return problems;
    }

    /// <summary>True when the code matches a language shipped with this build.</summary>
    public static bool IsKnownLanguage(string languageCode, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);

        foreach (LanguageOption option in localization.Languages)
        {
            if (string.Equals(option.Code, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Guards the quarantine folder.
    /// </summary>
    /// <remarks>
    /// An empty value means "use the default", which is always allowed. Otherwise the
    /// target must be an absolute path that is not the Windows folder, not inside it, not
    /// a Program Files root and not one of the well known system roots themselves — a
    /// subfolder of <c>%ProgramData%</c> such as the default quarantine is fine.
    /// </remarks>
    public static bool IsQuarantinePathAllowed(string? quarantinePath)
    {
        if (string.IsNullOrWhiteSpace(quarantinePath))
        {
            return true;
        }

        string trimmed = quarantinePath.Trim();

        // A relative path would resolve against whatever the working directory happens to
        // be, which is never what the user meant for a quarantine.
        if (!Path.IsPathRooted(trimmed))
        {
            return false;
        }

        string? normalized = Normalize(trimmed);
        if (normalized is null)
        {
            return false;
        }

        string? windows = Normalize(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        if (windows is not null && IsSameOrInside(normalized, windows))
        {
            return false;
        }

        string? system = Normalize(Environment.GetFolderPath(Environment.SpecialFolder.System));
        if (system is not null && IsSameOrInside(normalized, system))
        {
            return false;
        }

        foreach (string candidate in EnumerateProtectedRoots())
        {
            string? protectedRoot = Normalize(candidate);
            if (protectedRoot is null)
            {
                continue;
            }

            bool isProgramFiles =
                string.Equals(protectedRoot, Normalize(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(protectedRoot, Normalize(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)), StringComparison.OrdinalIgnoreCase);

            // Program Files may not even contain the quarantine; the other roots are only
            // rejected when they are the target itself.
            bool rejected = isProgramFiles
                ? IsSameOrInside(normalized, protectedRoot)
                : string.Equals(normalized, protectedRoot, StringComparison.OrdinalIgnoreCase);

            if (rejected)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<string> EnumerateProtectedRoots()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        foreach (string extra in SettingsLimits.ForbiddenQuarantineNames)
        {
            yield return extra;
        }
    }

    private static bool IsSameOrInside(string path, string root) =>
        string.Equals(path, root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            string full = Path.GetFullPath(path.Trim());
            return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}
