using System.Diagnostics;
using System.Text.Json;

namespace VoidRemove.Core.WindowsApps;

/// <summary>
/// Talks to the package manager through PowerShell so it compiles without WinRT references.
/// Parsing is isolated in <see cref="ParseApps"/> for unit testing.
/// </summary>
public sealed class PowerShellWindowsAppService : IWindowsAppService
{
    public bool IsSupported => System.OperatingSystem.IsWindows();

    public async Task<IReadOnlyList<WindowsApp>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            return Array.Empty<WindowsApp>();
        }

        string json = await RunPowerShellAsync(
            "Get-AppxPackage | Select-Object Name, PackageFamilyName, Version, IsFramework, IsResourcePackage, SignatureKind, InstallLocation | ConvertTo-Json -Depth 3",
            cancellationToken);

        return ParseApps(json);
    }

    public async Task<bool> RemoveAsync(string packageFamilyName, CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            return false;
        }

        string output = await RunPowerShellAsync(
            $"Get-AppxPackage -PackageFamilyName '{packageFamilyName}' | Remove-AppxPackage",
            cancellationToken);

        return output.Length == 0;
    }

    // ------------------------------------------------------- parsing

    internal static IReadOnlyList<WindowsApp> ParseApps(string json)
    {
        List<WindowsApp> apps = new();
        if (string.IsNullOrWhiteSpace(json))
        {
            return apps;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            List<JsonElement> elements = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray().ToList()
                : new List<JsonElement> { document.RootElement };

            foreach (JsonElement element in elements)
            {
                string name = GetString(element, "Name");
                string family = GetString(element, "PackageFamilyName");
                if (family.Length == 0)
                {
                    continue;
                }

                bool framework = GetBool(element, "IsFramework");
                bool resource = GetBool(element, "IsResourcePackage");
                string signature = GetString(element, "SignatureKind");

                apps.Add(new WindowsApp
                {
                    Name = name,
                    PackageFamilyName = family,
                    Version = GetString(element, "Version"),
                    IsFramework = framework,
                    IsDependency = resource,
                    Tag = WindowsApp.ClassifyTag(framework, resource, signature),
                });
            }
        }
        catch (JsonException)
        {
        }

        return apps.OrderBy(static a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static bool GetBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) &&
        (value.ValueKind == JsonValueKind.True || (value.ValueKind == JsonValueKind.String && value.GetString() == "True"));

    // ------------------------------------------------------- powershell

    internal static async Task<string> RunPowerShellAsync(string script, CancellationToken cancellationToken)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -Command \"" + script.Replace("\"", "\\\"") + "\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            process.Start();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            return string.Empty;
        }

        string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return stdout;
    }
}
