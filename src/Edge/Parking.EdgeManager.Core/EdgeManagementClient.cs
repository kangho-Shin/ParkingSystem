using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.EdgeManager.Core;

public sealed class EdgeManagementClient : IEdgeManagementClient
{
    private readonly HttpClient _httpClient;

    public EdgeManagementClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<EdgeServiceStatus> GetStatusAsync(CancellationToken cancellationToken) =>
        GetAsync<EdgeServiceStatus>("api/v1/management/status", cancellationToken);

    public Task<IReadOnlyList<EdgeEntryItem>> GetEntriesAsync(
        CancellationToken cancellationToken) =>
        GetListAsync<EdgeEntryItem>("api/v1/management/entries?limit=1000", cancellationToken);

    public Task<IReadOnlyList<EdgeActivityItem>> GetActivitiesAsync(
        CancellationToken cancellationToken) =>
        GetListAsync<EdgeActivityItem>("api/v1/management/activities?limit=1000", cancellationToken);

    public async Task<SiteConfiguration?> GetConfigurationAsync(
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            "api/v1/management/configuration", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return JsonConvert.DeserializeObject<SiteConfiguration>(
            await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public async Task<byte[]?> GetImageAsync(
        string? fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;
        using HttpResponseMessage response = await _httpClient.GetAsync(
            $"api/v1/management/images/{Uri.EscapeDataString(fileName)}",
            cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonConvert.DeserializeObject<T>(json)
            ?? throw new InvalidOperationException("EdgeService 응답이 없습니다.");
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(
        string uri,
        CancellationToken cancellationToken)
    {
        List<T>? result = await GetAsync<List<T>>(uri, cancellationToken);
        return result;
    }
}
