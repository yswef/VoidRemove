using System.Diagnostics;
using System.Text.Json;
using VoidRemove.Core.Engine;

namespace VoidRemove.Core.Toolbox;

/// <summary>One installed Windows update.</summary>
public sealed record WindowsUpdate(string HotFixId, string Description, DateTime? InstalledOn);

/// <summary>Lists and removes Windows updates via wusa/DISM.</summary>
public sealed class WindowsUpdatesService
{
    private readonly IProcessRunner _processRunner;

    public WindowsUpdatesService(IProcessRunner processRunner) => _processRunner = processRunner;

    public bool IsSupported => System.OperatingSystem.IsWindows();

    /// <summary>Builds the wusa argument list for one KB.</summary>
    public static string BuildWusaArguments(string hotFixId, bool quiet)
    {
        string kb = hotFixId.ToUpperInvariant().Replace("KB", string.Empty);
        return quiet ? $"/uninstall /kb:{kb} /quiet /norestart" : $"/uninstall /kb:{kb} /norestart";
    }

    public async Task<IReadOnlyList<WindowsUpdate>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            return Array.Empty<WindowsUpdate>();
        }

        string json = await PowerShellWindowsAppServiceShim.RunAsync(
            "Get-HotFix | Select-Object HotFixID, Description, InstalledOn | ConvertTo-Json -Depth 3",
            cancellationToken);

        return ParseUpdates(json);
    }

    public Task<ProcessResult> UninstallAsync(string hotFixId, bool quiet, CancellationToken cancellationToken = default) =>
        _processRunner.RunAsync("wusa.exe", BuildWusaArguments(hotFixId, quiet), TimeSpan.FromMinutes(10), cancellationToken);

    internal static IReadOnlyList<WindowsUpdate> ParseUpdates(string json)
    {
        List<WindowsUpdate> updates = new();
        if (string.IsNullOrWhiteSpace(json))
        {
            return updates;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            List<JsonElement> elements = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray().ToList()
                : new List<JsonElement> { document.RootElement };

            foreach (JsonElement element in elements)
            {
                string id = element.TryGetProperty("HotFixID", out JsonElement idElement) ? idElement.GetString() ?? "" : "";
                if (id.Length == 0)
                {
                    continue;
                }

                string description = element.TryGetProperty("Description", out JsonElement d) ? d.GetString() ?? "" : "";
                DateTime? installed = element.TryGetProperty("InstalledOn", out JsonElement t) && t.ValueKind == JsonValueKind.String
                    && DateTime.TryParse(t.GetString(), out DateTime parsed) ? parsed : null;

                updates.Add(new WindowsUpdate(id, description, installed));
            }
        }
        catch (JsonException)
        {
        }

        return updates;
    }
}

/// <summary>Small stdout-capturing helper shared by the PowerShell-backed services.</summary>
internal static class PowerShellWindowsAppServiceShim
{
    internal static Task<string> RunAsync(string script, CancellationToken cancellationToken) =>
        VoidRemove.Core.WindowsApps.PowerShellWindowsAppService.RunPowerShellAsync(script, cancellationToken);
}
