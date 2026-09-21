using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parking.Contracts;

namespace Parking.Operator;

public sealed class OperatorClient
{
    private readonly HttpClient _httpClient;
    public OperatorClient(HttpClient httpClient) => _httpClient = httpClient;

    public Task<EdgeServiceStatus> GetStatusAsync(CancellationToken token) =>
        GetAsync<EdgeServiceStatus>("api/v1/management/status", token);
    public Task<EdgeSetupResponse> GetSetupAsync(CancellationToken token) =>
        GetAsync<EdgeSetupResponse>("api/v1/local/setup", token);
    public async Task<SiteConfiguration> GetConfigurationAsync(CancellationToken token)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/v1/local/config", token);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("EdgeService 현장설정이 없습니다. EdgeManager에서 최초 설정과 현장설정을 먼저 저장하세요.");
        response.EnsureSuccessStatusCode();
        return Deserialize<SiteConfiguration>(await response.Content.ReadAsStringAsync(token));
    }

    public async Task<OperatorSearchResult> SearchAsync(long siteId, int groupnum, string carNumber, DateTimeOffset exitAt, CancellationToken token)
    {
        string uri = $"api/v1/local/parking/search?siteId={siteId}&groupnum={groupnum}&carNumber={Uri.EscapeDataString(carNumber.Trim())}&exitAt={Uri.EscapeDataString(exitAt.ToString("O"))}";
        using HttpResponseMessage response = await _httpClient.GetAsync(uri, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return new(Array.Empty<ParkingSearchCandidate>(), null);
        response.EnsureSuccessStatusCode();
        JObject json = JObject.Parse(await response.Content.ReadAsStringAsync(token));
        JToken? candidates = json["Candidates"];
        return candidates is null
            ? new(Array.Empty<ParkingSearchCandidate>(), json.ToObject<OperatorFeeQuote>())
            : new(candidates.ToObject<List<ParkingSearchCandidate>>() ?? new(), null);
    }

    public Task<OperatorFeeQuote> QuoteAsync(long sessionId, DateTimeOffset exitAt, IReadOnlyList<int> discountKeys, CancellationToken token) =>
        PostAsync<object, OperatorFeeQuote>("api/v1/local/fees/quote/session", new { ParkingSessionId = sessionId, ExitAt = exitAt, DiscountKeys = discountKeys }, token);

    public Task<PaymentCompleteResponse> PayAsync(CompletePaymentRequest request, CancellationToken token) =>
        PostAsync<CompletePaymentRequest, PaymentCompleteResponse>("api/v1/local/payments/complete", request, token);

    public Task<FieldEventResponse> ManualEntryAsync(FieldEventRequest request, CancellationToken token) =>
        PostAsync<FieldEventRequest, FieldEventResponse>("api/v1/edge/events", request, token);
    public Task<FieldEventResponse> ManualExitAsync(ExitEventRequest request, CancellationToken token) =>
        PostAsync<ExitEventRequest, FieldEventResponse>("api/v1/edge/exits", request, token);
    public Task<CorrectCarNumberResponse> CorrectCarNumberAsync(long sessionId, string carNumber, CancellationToken token) =>
        PutAsync<CorrectCarNumberRequest, CorrectCarNumberResponse>($"api/v1/local/parking/sessions/{sessionId}/car-number", new(carNumber), token);
    public Task<JObject> OpenBarrierAsync(OperatorBarrierRequest request, CancellationToken token) =>
        PostAsync<OperatorBarrierRequest, JObject>("api/v1/local/operator/barrier/open", request, token);

    public async Task<byte[]?> GetImageAsync(string? fileName, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        EdgeSetupResponse setup = await GetSetupAsync(token);
        Uri uri = new(new Uri(setup.ImageServerUrl.TrimEnd('/') + "/"), $"api/image/download?fileName={Uri.EscapeDataString(fileName)}");
        using HttpResponseMessage response = await _httpClient.GetAsync(uri, token);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(token) : null;
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken token)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(uri, token);
        response.EnsureSuccessStatusCode();
        return Deserialize<T>(await response.Content.ReadAsStringAsync(token));
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string uri, TRequest value, CancellationToken token) =>
        await SendAsync<TRequest, TResponse>(HttpMethod.Post, uri, value, token);
    private async Task<TResponse> PutAsync<TRequest, TResponse>(string uri, TRequest value, CancellationToken token) =>
        await SendAsync<TRequest, TResponse>(HttpMethod.Put, uri, value, token);

    private async Task<TResponse> SendAsync<TRequest, TResponse>(HttpMethod method, string uri, TRequest value, CancellationToken token)
    {
        using HttpRequestMessage request = new(method, uri) { Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json") };
        using HttpResponseMessage response = await _httpClient.SendAsync(request, token);
        string json = await response.Content.ReadAsStringAsync(token);
        response.EnsureSuccessStatusCode();
        return Deserialize<TResponse>(json);
    }

    private static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json)
        ?? throw new InvalidOperationException("EdgeService 응답이 없습니다.");
}

public sealed record OperatorSearchResult(IReadOnlyList<ParkingSearchCandidate> Candidates, OperatorFeeQuote? Quote);
public sealed class OperatorFeeQuote
{
    public long ParkingSessionId { get; set; }
    public string CarNumber { get; set; } = "";
    public DateTimeOffset EntryAt { get; set; }
    public DateTimeOffset ExitAt { get; set; }
    public OperatorFeeResult Fee { get; set; } = new();
    public long PreviousPaidAmount { get; set; }
    public long PayableAmount { get; set; }
    public bool IsPrepayGrace { get; set; }
}
public sealed class OperatorFeeResult
{
    public long OriginalFee { get; set; }
    public long FinalFee { get; set; }
    public long DiscountFee { get; set; }
    public int ParkingMinutes { get; set; }
}
