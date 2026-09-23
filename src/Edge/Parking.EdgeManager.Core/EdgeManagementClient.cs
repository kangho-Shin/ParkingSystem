using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.EdgeManager.Core;

public sealed class EdgeManagementClient : IEdgeManagementClient
{
    private readonly HttpClient _httpClient;
    private readonly ImageServerClient _imageServerClient;

    public EdgeManagementClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _imageServerClient = new ImageServerClient(httpClient);
    }

    public async Task<EdgeSetupResponse?> GetSetupAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            "api/v1/local/setup", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return JsonConvert.DeserializeObject<EdgeSetupResponse>(
            await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public async Task<EdgeSetupResponse> SaveSetupAsync(
        EdgeSetupRequest request,
        CancellationToken cancellationToken)
    {
        string json = JsonConvert.SerializeObject(request);
        using StringContent content = new(json, System.Text.Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _httpClient.PutAsync(
            "api/v1/local/setup", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonConvert.DeserializeObject<EdgeSetupResponse>(
            await response.Content.ReadAsStringAsync(cancellationToken))
            ?? throw new InvalidOperationException("최초 설정 저장 응답이 없습니다.");
    }

    public async Task<CentralConnectionResponse?> GetCentralConnectionAsync(
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            "api/v1/local/central-connection", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return JsonConvert.DeserializeObject<CentralConnectionResponse>(
            await response.Content.ReadAsStringAsync(cancellationToken));
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

    public Task SaveSiteAsync(ParkingSite value, CancellationToken token) =>
        PutAsync("api/v1/local/config/site", value, token);
    public Task SaveLaneAsync(ParkingLane value, CancellationToken token) =>
        PutAsync($"api/v1/local/config/lanes/{value.LaneId}", value, token);
    public Task DeleteLaneAsync(long laneId, CancellationToken token) =>
        DeleteAsync($"api/v1/local/config/lanes/{laneId}", token);
    public Task SaveDeviceAsync(ParkingDevice value, CancellationToken token) =>
        PutAsync($"api/v1/local/config/devices/{value.DeviceId}", value, token);
    public Task DeleteDeviceAsync(long deviceId, CancellationToken token) =>
        DeleteAsync($"api/v1/local/config/devices/{deviceId}", token);
    public Task SaveDeviceLinkAsync(ParkingDeviceLink value, CancellationToken token) =>
        PutAsync("api/v1/local/config/device-links", value, token);
    public Task DeleteDeviceLinkAsync(long sourceDeviceId, long targetDeviceId, string linkType, CancellationToken token) =>
        DeleteAsync($"api/v1/local/config/device-links?sourceDeviceId={sourceDeviceId}&targetDeviceId={targetDeviceId}&linkType={Uri.EscapeDataString(linkType)}", token);
    public Task SaveOperationVariableAsync(ParkingOperationVariable value, CancellationToken token) =>
        PutAsync("api/v1/local/config/variables", value, token);
    public Task DeleteOperationVariableAsync(int groupnum, string commandType, CancellationToken token) =>
        DeleteAsync($"api/v1/local/config/variables?groupnum={groupnum}&commandType={Uri.EscapeDataString(commandType)}", token);

    public async Task<byte[]?> GetImageAsync(
        string? fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        EdgeSetupResponse? setup = await GetSetupAsync(cancellationToken);
        return setup is null
            ? null
            : await _imageServerClient.DownloadAsync(
                setup.ImageServerUrl, fileName, cancellationToken);
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonConvert.DeserializeObject<T>(json)
            ?? throw new InvalidOperationException("EdgeService 응답이 없습니다.");
    }

    private async Task PutAsync<T>(string uri, T value, CancellationToken token)
    {
        string json = JsonConvert.SerializeObject(value);
        using StringContent content = new(json, System.Text.Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _httpClient.PutAsync(uri, content, token);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteAsync(string uri, CancellationToken token)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync(uri, token);
        response.EnsureSuccessStatusCode();
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(
        string uri,
        CancellationToken cancellationToken)
    {
        List<T>? result = await GetAsync<List<T>>(uri, cancellationToken);
        return result;
    }
}
