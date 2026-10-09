using Microsoft.Win32;

namespace VoidRemove.Core.Extensions;

/// <summary>
/// Discovers extensions for IE/BHO and the Chromium family from the registry, plus Firefox
/// add-ons from profile folders, and flags policy force-installed entries. Local only.
/// </summary>
public sealed class WindowsBrowserExtensionScanner : IBrowserExtensionScanner
{
    public bool IsSupported => System.OperatingSystem.IsWindows();

    public Task<IReadOnlyList<BrowserExtension>> ScanAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            return Task.FromResult<IReadOnlyList<BrowserExtension>>(Array.Empty<BrowserExtension>());
        }

        List<BrowserExtension> extensions = new();

        extensions.AddRange(ScanChromiumFamily(Browser.Chrome, RegistryHive.LocalMachine, @"Software\Google\Chrome\Extensions", Forcelist(RegistryHive.LocalMachine, @"Software\Policies\Google\Chrome")));
        extensions.AddRange(ScanChromiumFamily(Browser.Edge, RegistryHive.LocalMachine, @"Software\Microsoft\Edge\Extensions", Forcelist(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Edge")));
        extensions.AddRange(ScanChromiumFamily(Browser.Brave, RegistryHive.CurrentUser, @"Software\BraveSoftware\Brave-Browser\Extensions", Array.Empty<string>()));
        extensions.AddRange(ScanBrowserHelperObjects());
        extensions.AddRange(ScanFirefox());

        return Task.FromResult<IReadOnlyList<BrowserExtension>>(
            extensions.OrderBy(static e => e.BrowserKind).ThenBy(static e => e.Name, StringComparer.OrdinalIgnoreCase).ToList());
    }

    // ------------------------------------------------------- chromium registry

    private static IEnumerable<BrowserExtension> ScanChromiumFamily(Browser browser, RegistryHive hive, string extensionsKey, IReadOnlyCollection<string> forced)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? key = baseKey.OpenSubKey(extensionsKey, writable: false);
        if (key is null)
        {
            yield break;
        }

        foreach (string id in key.GetSubKeyNames())
        {
            using RegistryKey? entry = key.OpenSubKey(id, writable: false);
            if (entry is null)
            {
                continue;
            }

            bool forceInstalled = forced.Any(f => f.Contains(id, StringComparison.OrdinalIgnoreCase));
            string publisher = entry.GetValue("update_url") is string update && update.Contains("chrome.google.com", StringComparison.OrdinalIgnoreCase)
                ? "Chrome Web Store"
                : string.Empty;

            int score = ExtensionScorer.ComputeScore(0, 0, publisher.Length > 0, forceInstalled);

            yield return new BrowserExtension
            {
                BrowserKind = browser,
                Id = id,
                Name = entry.GetValue("name") is string name && !name.StartsWith("@", StringComparison.Ordinal) ? name : id,
                Version = entry.GetValue("version") as string ?? string.Empty,
                Publisher = publisher,
                TrustScore = score,
                IsForceInstalled = forceInstalled,
                SourcePath = extensionsKey + "\\" + id,
            };
        }
    }

    private static IReadOnlyList<string> Forcelist(RegistryHive hive, string policyKey)
    {
        List<string> forced = new();
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? key = baseKey.OpenSubKey(policyKey + @"\ExtensionInstallForcelist", writable: false);
        if (key is not null)
        {
            foreach (string value in key.GetValueNames())
            {
                if (key.GetValue(value) is string entry)
                {
                    forced.Add(entry);
                }
            }
        }

        return forced;
    }

    // ------------------------------------------------------- IE / BHO

    private static IEnumerable<BrowserExtension> ScanBrowserHelperObjects()
    {
        using RegistryKey? bho = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
            .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects", writable: false);
        if (bho is null)
        {
            yield break;
        }

        foreach (string clsid in bho.GetSubKeyNames())
        {
            string name = clsid;
            using RegistryKey? cls = Registry.ClassesRoot.OpenSubKey("CLSID\\" + clsid, writable: false);
            if (cls?.GetValue(null) is string friendly && friendly.Length > 0)
            {
                name = friendly;
            }

            yield return new BrowserExtension
            {
                BrowserKind = Browser.InternetExplorer,
                Id = clsid,
                Name = name,
                TrustScore = ExtensionScorer.ComputeScore(0, 0, hasKnownPublisher: false, forceInstalled: false),
                SourcePath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\" + clsid,
            };
        }
    }

    // ------------------------------------------------------- firefox

    private static IEnumerable<BrowserExtension> ScanFirefox()
    {
        string profilesRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Mozilla", "Firefox", "Profiles");

        if (!Directory.Exists(profilesRoot))
        {
            yield break;
        }

        foreach (string profile in Directory.EnumerateDirectories(profilesRoot))
        {
            string extensionsDir = Path.Combine(profile, "extensions");
            if (!Directory.Exists(extensionsDir))
            {
                continue;
            }

            foreach (string extension in Directory.EnumerateDirectories(extensionsDir))
            {
                string id = Path.GetFileName(extension);
                yield return new BrowserExtension
                {
                    BrowserKind = Browser.Firefox,
                    Id = id,
                    Name = id,
                    TrustScore = ExtensionScorer.ComputeScore(0, 0, hasKnownPublisher: true, forceInstalled: false),
                    SourcePath = extension,
                };
            }
        }
    }
}
