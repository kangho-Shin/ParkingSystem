using System.Text.Json;

namespace ImageUploadAgent;

public sealed record AgentConfiguration(string ImageServerUrl, string ImageWatchPath);

public sealed class EdgeConfigurationClient
{
    private readonly HttpClient _client;
    public EdgeConfigurationClient(string edgeServiceBaseUrl)
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri(edgeServiceBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<AgentConfiguration?> GetAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _client.GetAsync(
            "api/v1/local/setup", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        SetupDto? value = JsonSerializer.Deserialize<SetupDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return value is null || string.IsNullOrWhiteSpace(value.ImageServerUrl) ||
               string.IsNullOrWhiteSpace(value.ImageWatchPath)
            ? null
            : new AgentConfiguration(value.ImageServerUrl, value.ImageWatchPath);
    }

    private sealed class SetupDto
    {
        public string ImageServerUrl { get; set; } = "";
        public string ImageWatchPath { get; set; } = "";
    }
}
