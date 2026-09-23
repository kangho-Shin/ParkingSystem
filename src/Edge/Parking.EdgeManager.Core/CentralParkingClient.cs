using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parking.Contracts;

namespace Parking.EdgeManager.Core;

public sealed class CentralParkingClient : ICentralParkingClient
{
    private readonly HttpClient _httpClient;
    private readonly string _siteAuthKey;

    public CentralParkingClient(HttpClient httpClient, string siteAuthKey)
    {
        _httpClient = httpClient;
        _siteAuthKey = siteAuthKey;
    }

    public Task<PagedParkingResult<ParkingManagementItem>> SearchEntriesAsync(
        ParkingManagementQuery query,
        CancellationToken cancellationToken) =>
        GetAsync<PagedParkingResult<ParkingManagementItem>>(
            $"api/v1/management/parking/entries?{BuildQuery(query, false)}",
            cancellationToken);

    public Task<PagedParkingResult<ParkingManagementItem>> SearchExitsAsync(
        ParkingManagementQuery query,
        CancellationToken cancellationToken) =>
        GetAsync<PagedParkingResult<ParkingManagementItem>>(
            $"api/v1/management/parking/exits?{BuildQuery(query, true)}",
            cancellationToken);

    public Task<ManualEntryResponse> CreateManualEntryAsync(
        ManualEntryRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<ManualEntryRequest, ManualEntryResponse>(
            HttpMethod.Post,
            "api/v1/management/parking/manual-entries",
            request,
            cancellationToken);

    public async Task CorrectCarNumberAsync(
        ParkingSessionType sessionType,
        long parkingSessionId,
        ManagementCarNumberRequest request,
        CancellationToken cancellationToken) =>
        await SendAsync<ManagementCarNumberRequest, JObject>(
            HttpMethod.Put,
            $"api/v1/management/parking/sessions/{sessionType}/{parkingSessionId}/car-number",
            request,
            cancellationToken);

    private async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri);
        return await SendAsync<T>(request, cancellationToken);
    }

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        HttpMethod method,
        string uri,
        TRequest value,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreateRequest(method, uri);
        request.Content = new StringContent(
            JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
        return await SendAsync<TResponse>(request, cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(
                request, cancellationToken);
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(ErrorMessage(json, response.ReasonPhrase));
            return JsonConvert.DeserializeObject<T>(json)
                ?? throw new InvalidOperationException("중앙 API 응답이 없습니다.");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ParkingCentralUnavailableException("중앙 API 응답 시간이 초과되었습니다.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new ParkingCentralUnavailableException("중앙 API에 연결할 수 없습니다.", exception);
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri)
    {
        HttpRequestMessage request = new(method, uri);
        request.Headers.Add("X-Site-Key", _siteAuthKey);
        return request;
    }

    private static string BuildQuery(ParkingManagementQuery query, bool includeStatus)
    {
        List<string> values = [$"siteId={query.SiteId.ToString(CultureInfo.InvariantCulture)}"];
        if (query.From.HasValue) values.Add($"from={Escape(query.From.Value.ToString("O", CultureInfo.InvariantCulture))}");
        if (query.To.HasValue) values.Add($"to={Escape(query.To.Value.ToString("O", CultureInfo.InvariantCulture))}");
        if (includeStatus && !string.IsNullOrWhiteSpace(query.Status)) values.Add($"status={Escape(query.Status)}");
        if (query.Groupnum.HasValue) values.Add($"groupnum={query.Groupnum.Value.ToString(CultureInfo.InvariantCulture)}");
        if (query.DeviceId.HasValue) values.Add($"deviceId={query.DeviceId.Value.ToString(CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrWhiteSpace(query.CarNumber)) values.Add($"carNumber={Escape(query.CarNumber.Trim())}");
        values.Add($"page={Math.Max(query.Page, 1).ToString(CultureInfo.InvariantCulture)}");
        int pageSize = query.PageSize <= 0 ? 200 : Math.Min(query.PageSize, 500);
        values.Add($"pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}");
        return string.Join("&", values);
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);

    private static string ErrorMessage(string json, string? fallback)
    {
        try
        {
            return JObject.Parse(json)["Message"]?.ToString()
                ?? fallback
                ?? "중앙 API 요청이 실패했습니다.";
        }
        catch (JsonException)
        {
            return fallback ?? "중앙 API 요청이 실패했습니다.";
        }
    }
}
