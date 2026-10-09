namespace VoidRemove.Core.AppData;

/// <summary>Expands and rewrites the environment tokens used by profiles.</summary>
public static class EnvExpander
{
    private static readonly (string Token, Func<string> Value)[] Tokens =
    {
        ("%AppData%", () => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)),
        ("%LocalAppData%", () => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
        ("%LocalLow%", () => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow")),
        ("%ProgramData%", () => Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)),
        ("%UserProfile%", () => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
        ("%ProgramFiles%", () => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
    };

    public static string Expand(string template)
    {
        string result = template;
        foreach ((string token, Func<string> value) in Tokens)
        {
            result = result.Replace(token, value(), StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    /// <summary>Replaces concrete folders with tokens again, for compact display.</summary>
    public static string Contract(string path)
    {
        string result = path;
        foreach ((string token, Func<string> value) in Tokens)
        {
            string concrete = value();
            if (concrete.Length > 0)
            {
                result = result.Replace(concrete, token, StringComparison.OrdinalIgnoreCase);
            }
        }

        return result;
    }
}
