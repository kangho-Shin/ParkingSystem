using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace APSMain.Integration.EdgeService;

public sealed class EdgeServiceClient
{
    private readonly HttpClient _http;
    private readonly EdgeServiceOptions _options;

    public EdgeServiceClient(HttpClient http, EdgeServiceOptions options)
    {
        _http = http;
        _options = options;
        _http.BaseAddress ??= options.BaseAddress;
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<EdgeCallResult<ParkingSearchResult>> SearchParkingAsync(
        string carNumber, DateTimeOffset exitAt, CancellationToken token = default)
    {
        string uri = $"api/v1/local/parking/search?siteId={_options.Sitenum}&groupnum={_options.Groupnum}&carNumber={Uri.EscapeDataString(carNumber.Trim())}&exitAt={Uri.EscapeDataString(exitAt.ToString("O"))}";
        try
        {
            using HttpResponseMessage response = await _http.GetAsync(uri, token);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return EdgeCallResult<ParkingSearchResult>.Success(new(Array.Empty<ParkingSearchCandidate>(), null));
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.GatewayTimeout)
                return EdgeCallResult<ParkingSearchResult>.Failed(EdgeCallStatus.TransientFailure, "EdgeService 또는 중앙 서버에 연결할 수 없습니다.");
            if (!response.IsSuccessStatusCode)
                return EdgeCallResult<ParkingSearchResult>.Failed(EdgeCallStatus.Failure, $"EdgeService 응답 오류: {(int)response.StatusCode}");

            string json = await response.Content.ReadAsStringAsync(token);
            JObject root = JObject.Parse(json);
            JToken? candidatesToken = root.GetValue("Candidates", StringComparison.OrdinalIgnoreCase);
            if (candidatesToken is not null)
            {
                if (candidatesToken.Type != JTokenType.Array)
                    return EdgeCallResult<ParkingSearchResult>.Failed(EdgeCallStatus.InvalidResponse, "차량 후보 응답 형식이 올바르지 않습니다.");
                List<ParkingSearchCandidate>? candidates = candidatesToken.ToObject<List<ParkingSearchCandidate>>();
                if (candidates is null || candidates.Any(x => x.ParkingSessionId <= 0 || string.IsNullOrWhiteSpace(x.CarNumber)))
                    return EdgeCallResult<ParkingSearchResult>.Failed(EdgeCallStatus.InvalidResponse, "차량 후보 필수값이 없습니다.");
                return EdgeCallResult<ParkingSearchResult>.Success(new(candidates, null));
            }
            if (root["ParkingSessionId"] is null || root["CarNumber"] is null || root["Fee"]?.Type != JTokenType.Object || root["PayableAmount"] is null)
                return EdgeCallResult<ParkingSearchResult>.Failed(EdgeCallStatus.InvalidResponse, "요금 응답 필수값이 없습니다.");
            FeeQuote? quote = root.ToObject<FeeQuote>();
            return quote is null || quote.ParkingSessionId <= 0 || string.IsNullOrWhiteSpace(quote.CarNumber) || quote.Fee is null
                ? EdgeCallResult<ParkingSearchResult>.Failed(EdgeCallStatus.InvalidResponse, "차량검색 응답이 비어 있습니다.")
                : EdgeCallResult<ParkingSearchResult>.Success(new(Array.Empty<ParkingSearchCandidate>(), quote));
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return EdgeCallResult<ParkingSearchResult>.Failed(EdgeCallStatus.TransientFailure, "EdgeService 응답 시간이 초과되었습니다.");
        }
        catch (HttpRequestException ex)
        {
            return EdgeCallResult<ParkingSearchResult>.Failed(EdgeCallStatus.TransientFailure, ex.Message);
        }
        catch (JsonException ex)
        {
            return EdgeCallResult<ParkingSearchResult>.Failed(EdgeCallStatus.InvalidResponse, ex.Message);
        }
    }

    public async Task<EdgeCallResult<bool>> CompleteKioskEventAsync(
        Guid eventId,
        CancellationToken token = default)
    {
        string json = JsonConvert.SerializeObject(new
        {
            _options.Sitenum,
            _options.Groupnum,
            _options.Devicenum
        });
        try
        {
            using HttpResponseMessage response = await _http.PostAsync(
                $"api/v1/local/kiosks/events/{eventId}/complete",
                new StringContent(json, Encoding.UTF8, "application/json"),
                token);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return EdgeCallResult<bool>.Failed(EdgeCallStatus.Failure, "완료할 출차 사건을 찾을 수 없습니다.");
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.GatewayTimeout)
                return EdgeCallResult<bool>.Failed(EdgeCallStatus.TransientFailure, "EdgeService에 연결할 수 없습니다.");
            return response.IsSuccessStatusCode
                ? EdgeCallResult<bool>.Success(true)
                : EdgeCallResult<bool>.Failed(EdgeCallStatus.Failure, $"출차 완료 응답 오류: {(int)response.StatusCode}");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return EdgeCallResult<bool>.Failed(EdgeCallStatus.TransientFailure, "출차 완료 응답 시간이 초과되었습니다.");
        }
        catch (HttpRequestException ex)
        {
            return EdgeCallResult<bool>.Failed(EdgeCallStatus.TransientFailure, ex.Message);
        }
    }

    public async Task<EdgeCallResult<bool>> DisplayFeeAsync(
        string carNumber,
        long payableAmount,
        CancellationToken token = default)
    {
        string json = JsonConvert.SerializeObject(new
        {
            Device = new
            {
                _options.Sitenum,
                _options.Groupnum,
                _options.Devicenum
            },
            CarNumber = carNumber,
            DisplayMessage = $"주차요금 {payableAmount:N0}원",
            DisplaySeconds = 100
        });
        try
        {
            using HttpResponseMessage response = await _http.PostAsync(
                "api/v1/local/kiosks/display",
                new StringContent(json, Encoding.UTF8, "application/json"), token);
            return response.IsSuccessStatusCode
                ? EdgeCallResult<bool>.Success(true)
                : EdgeCallResult<bool>.Failed(
                    EdgeCallStatus.Failure,
                    $"전광판 표시 응답 오류: {(int)response.StatusCode}");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return EdgeCallResult<bool>.Failed(
                EdgeCallStatus.TransientFailure, "전광판 표시 응답 시간이 초과되었습니다.");
        }
        catch (HttpRequestException ex)
        {
            return EdgeCallResult<bool>.Failed(EdgeCallStatus.TransientFailure, ex.Message);
        }
    }

    public async Task<EdgeCallResult<bool>> CompleteManualExitAsync(
        string carNumber,
        DateTimeOffset exitAt,
        CancellationToken token = default)
    {
        string json = JsonConvert.SerializeObject(new
        {
            Device = new
            {
                _options.Sitenum,
                _options.Groupnum,
                _options.Devicenum
            },
            CarNumber = carNumber,
            ExitAt = exitAt
        });
        try
        {
            using HttpResponseMessage response = await _http.PostAsync(
                "api/v1/local/kiosks/manual-exit/complete",
                new StringContent(json, Encoding.UTF8, "application/json"), token);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return EdgeCallResult<bool>.Failed(EdgeCallStatus.Failure, "연결된 출차 LPR을 찾을 수 없습니다.");
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.GatewayTimeout)
                return EdgeCallResult<bool>.Failed(EdgeCallStatus.TransientFailure, "출차 완료 서버에 연결할 수 없습니다.");
            if (!response.IsSuccessStatusCode)
                return EdgeCallResult<bool>.Failed(EdgeCallStatus.Failure, $"출차 완료 응답 오류: {(int)response.StatusCode}");

            JObject body = JObject.Parse(await response.Content.ReadAsStringAsync(token));
            bool accepted = body.Value<bool?>("Accepted") ?? body.Value<bool?>("accepted") ?? false;
            return accepted
                ? EdgeCallResult<bool>.Success(true)
                : EdgeCallResult<bool>.Failed(
                    EdgeCallStatus.Failure,
                    body.Value<string>("DisplayMessage") ?? body.Value<string>("displayMessage") ?? "출차가 승인되지 않았습니다.");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return EdgeCallResult<bool>.Failed(EdgeCallStatus.TransientFailure, "출차 완료 응답 시간이 초과되었습니다.");
        }
        catch (HttpRequestException ex)
        {
            return EdgeCallResult<bool>.Failed(EdgeCallStatus.TransientFailure, ex.Message);
        }
        catch (JsonException ex)
        {
            return EdgeCallResult<bool>.Failed(EdgeCallStatus.InvalidResponse, ex.Message);
        }
    }

    public async Task<EdgeCallResult<FeeQuote>> QuoteSessionAsync(long sessionId, DateTimeOffset exitAt, IReadOnlyList<int> discountKeys, CancellationToken token = default)
    {
        string json = JsonConvert.SerializeObject(new { ParkingSessionId = sessionId, ExitAt = exitAt, DiscountKeys = discountKeys });
        try
        {
            using HttpResponseMessage response = await _http.PostAsync("api/v1/local/fees/quote/session", new StringContent(json, Encoding.UTF8, "application/json"), token);
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.GatewayTimeout)
                return EdgeCallResult<FeeQuote>.Failed(EdgeCallStatus.TransientFailure, "요금조회 서버에 연결할 수 없습니다.");
            if (!response.IsSuccessStatusCode)
                return EdgeCallResult<FeeQuote>.Failed(EdgeCallStatus.Failure, $"요금조회 오류: {(int)response.StatusCode}");
            JObject root = JObject.Parse(await response.Content.ReadAsStringAsync(token));
            if (root["ParkingSessionId"] is null || root["CarNumber"] is null || root["Fee"]?.Type != JTokenType.Object || root["PayableAmount"] is null)
                return EdgeCallResult<FeeQuote>.Failed(EdgeCallStatus.InvalidResponse, "요금조회 응답 필수값이 없습니다.");
            FeeQuote? quote = root.ToObject<FeeQuote>();
            return quote is null || quote.ParkingSessionId <= 0 || string.IsNullOrWhiteSpace(quote.CarNumber) || quote.Fee is null
                ? EdgeCallResult<FeeQuote>.Failed(EdgeCallStatus.InvalidResponse, "요금조회 응답이 비어 있습니다.")
                : EdgeCallResult<FeeQuote>.Success(quote);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return EdgeCallResult<FeeQuote>.Failed(EdgeCallStatus.TransientFailure, "요금조회 시간이 초과되었습니다."); }
        catch (HttpRequestException ex) { return EdgeCallResult<FeeQuote>.Failed(EdgeCallStatus.TransientFailure, ex.Message); }
        catch (JsonException ex) { return EdgeCallResult<FeeQuote>.Failed(EdgeCallStatus.InvalidResponse, ex.Message); }
    }

    public async Task<EdgeCallResult<EdgePaymentResponse>> CompletePaymentAsync(
        EdgePaymentRequest request, CancellationToken token = default)
    {
        try
        {
            string json = JsonConvert.SerializeObject(request);
            using HttpResponseMessage response = await _http.PostAsync(
                "api/v1/local/payments/complete",
                new StringContent(json, Encoding.UTF8, "application/json"), token);
            string body = await response.Content.ReadAsStringAsync(token);
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.GatewayTimeout)
                return EdgeCallResult<EdgePaymentResponse>.Failed(EdgeCallStatus.TransientFailure, "결제결과가 EdgeService 전송 대기 상태입니다.");
            if (!response.IsSuccessStatusCode)
                return EdgeCallResult<EdgePaymentResponse>.Failed(EdgeCallStatus.Failure, $"결제완료 오류: {(int)response.StatusCode}");
            EdgePaymentResponse? result = JsonConvert.DeserializeObject<EdgePaymentResponse>(body);
            return result is null || !result.Accepted
                ? EdgeCallResult<EdgePaymentResponse>.Failed(EdgeCallStatus.InvalidResponse, result?.Message ?? "결제완료 응답이 비어 있습니다.")
                : EdgeCallResult<EdgePaymentResponse>.Success(result);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return EdgeCallResult<EdgePaymentResponse>.Failed(EdgeCallStatus.TransientFailure, "결제완료 응답 시간이 초과되었습니다.");
        }
        catch (HttpRequestException ex)
        {
            return EdgeCallResult<EdgePaymentResponse>.Failed(EdgeCallStatus.TransientFailure, ex.Message);
        }
        catch (JsonException ex)
        {
            return EdgeCallResult<EdgePaymentResponse>.Failed(EdgeCallStatus.InvalidResponse, ex.Message);
        }
    }
}
