namespace VoidRemove.Shared.Contracts;

/// <summary>Opens folders and web pages. Each front end implements it with its own shell API.</summary>
public interface IExternalLauncher
{
    /// <summary>Opens a folder in Explorer, creating it first when it is missing.</summary>
    void OpenFolder(string path);

    /// <summary>Opens a URL in the default browser.</summary>
    void OpenUrl(string url);

    /// <summary>Copies text to the clipboard.</summary>
    void CopyToClipboard(string text);
}
