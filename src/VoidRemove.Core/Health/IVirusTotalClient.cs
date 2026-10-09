namespace VoidRemove.Core.Health;

/// <summary>Verdict returned by the optional online lookup.</summary>
public sealed record VirusTotalVerdict(int MaliciousDetections, int TotalEngines)
{
    public bool IsSuspicious => MaliciousDetections > 0;
}

/// <summary>
/// Optional VirusTotal integration that only ever submits a SHA-256 hash, never a file.
/// Disabled by default; the default implementation is fully offline.
/// </summary>
public interface IVirusTotalClient
{
    bool IsEnabled { get; }

    Task<VirusTotalVerdict?> CheckSha256Async(string sha256, CancellationToken cancellationToken = default);
}

/// <summary>The offline default: never contacts the network.</summary>
public sealed class OfflineVirusTotalClient : IVirusTotalClient
{
    public bool IsEnabled => false;

    public Task<VirusTotalVerdict?> CheckSha256Async(string sha256, CancellationToken cancellationToken = default) =>
        Task.FromResult<VirusTotalVerdict?>(null);
}

/// <summary>Queries the VirusTotal v3 file-report endpoint with a hash only.</summary>
public sealed class HttpVirusTotalClient : IVirusTotalClient, IDisposable
{
    private readonly HttpClient _client;

    public HttpVirusTotalClient(string apiKey)
    {
        _client = new HttpClient { BaseAddress = new Uri("https://www.virustotal.com/api/v3/") };
        _client.DefaultRequestHeaders.Add("x-apikey", apiKey);
    }

    public bool IsEnabled => true;

    public async Task<VirusTotalVerdict?> CheckSha256Async(string sha256, CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await _client.GetAsync("files/" + sha256, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json);

            System.Text.Json.JsonElement stats = document.RootElement
                .GetProperty("data").GetProperty("attributes")
                .GetProperty("last_analysis_stats");

            return new VirusTotalVerdict(
                MaliciousDetections: stats.GetProperty("malicious").GetInt32(),
                TotalEngines: stats.GetProperty("malicious").GetInt32() + stats.GetProperty("undetected").GetInt32());
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public void Dispose() => _client.Dispose();
}
