using System.Text;
using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed class GatewayClient
    {
        private readonly HttpClient _httpClient;
        private readonly LocalBootstrapStore? _bootstrapStore;
        public GatewayClient(HttpClient httpClient, LocalBootstrapStore? bootstrapStore = null)
        {
            _httpClient = httpClient;
            _bootstrapStore = bootstrapStore;
        }

        public async Task<FieldEventResponse> SendEntryAsync(
            FieldEventRequest request,
            CancellationToken cancellationToken) =>
            ValidateEventId(
                request.EventId,
                await PostAsync<FieldEventRequest, FieldEventResponse>(
                    "api/v1/edge/events", request, cancellationToken));

        public async Task<FieldEventResponse> SendExitAsync(
            ExitEventRequest request,
            CancellationToken cancellationToken) =>
            ValidateEventId(
                request.EventId,
                await PostAsync<ExitEventRequest, FieldEventResponse>(
                    "api/v1/edge/exits", request, cancellationToken));

        public async Task<FieldEventResponse> SendOfflineKioskExitAsync(
            OfflineKioskExitRequest request, CancellationToken cancellationToken) =>
            ValidateEventId(
                request.EventId,
                await PostAsync<OfflineKioskExitRequest, FieldEventResponse>(
                    "api/v1/edge/exits/kiosk-offline-open", request, cancellationToken));

        public Task<HttpRelayResponse> RelayFeeQuoteAsync(
            string json,
            CancellationToken cancellationToken) =>
            PostRawAsync("api/v1/edge/fees/quote", json, cancellationToken);

        public Task<HttpRelayResponse> RelaySessionFeeQuoteAsync(
            string json,
            CancellationToken cancellationToken) =>
            PostRawAsync("api/v1/edge/fees/quote/session", json, cancellationToken);

        public Task<HttpRelayResponse> RelayParkingSearchAsync(
            string queryString,
            CancellationToken cancellationToken) =>
            GetRawAsync($"api/v1/edge/parking/search{queryString}", cancellationToken);

        public Task<HttpRelayResponse> RelayPaymentCompleteAsync(
            string json,
            CancellationToken cancellationToken) =>
            PostRawAsync("api/v1/edge/payments/complete", json, cancellationToken);

        public Task<HttpRelayResponse> RelayCarNumberCorrectionAsync(
            long parkingSessionId, string json, CancellationToken cancellationToken) =>
            PutRawAsync($"api/v1/edge/parking/sessions/{parkingSessionId}/car-number", json, cancellationToken);

        public async Task<GatewayHealthResponse> GetHealthAsync(
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    await GetRequestUriAsync("health", cancellationToken), cancellationToken);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<GatewayHealthResponse>(json)
                ?? throw new InvalidOperationException("Gateway 상태 응답이 없습니다.");
        }

        public async Task<SiteConfiguration> GetSiteConfigurationAsync(
            long siteId, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    await GetRequestUriAsync($"api/v1/edge/config/sites/{siteId}", cancellationToken),
                    cancellationToken);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<SiteConfiguration>(json)
                ?? throw new InvalidOperationException("Gateway 현장 설정 응답이 없습니다.");
        }

        public async Task<VersionedSiteConfiguration?> GetVersionedConfigurationAsync(
            long siteId,
            string siteKey,
            CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new(
                HttpMethod.Get,
                await GetRequestUriAsync(
                    $"api/v1/edge/config/sites/{siteId}/versioned", cancellationToken));
            request.Headers.Add("X-Site-Key", siteKey);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<VersionedSiteConfiguration>(
                await response.Content.ReadAsStringAsync(cancellationToken));
        }

        public async Task<HttpRelayResponse> SaveVersionedConfigurationAsync(
            VersionedSiteConfiguration value,
            string siteKey,
            CancellationToken cancellationToken)
        {
            string json = JsonConvert.SerializeObject(value);
            using HttpRequestMessage request = new(
                HttpMethod.Put,
                await GetRequestUriAsync(
                    $"api/v1/edge/config/sites/{value.Configuration.Site.SiteId}/versioned",
                    cancellationToken));
            request.Headers.Add("X-Site-Key", siteKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            return new HttpRelayResponse(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken));
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(
            string uri, TRequest request, CancellationToken cancellationToken)
        {
            string json = JsonConvert.SerializeObject(request);
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.PostAsync(
                await GetRequestUriAsync(uri, cancellationToken), content, cancellationToken);
            response.EnsureSuccessStatusCode();
            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<TResponse>(responseJson)
                ?? throw new InvalidOperationException("Gateway 응답이 없습니다.");
        }

        private async Task<HttpRelayResponse> PostRawAsync(
            string uri,
            string json,
            CancellationToken cancellationToken)
        {
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.PostAsync(
                await GetRequestUriAsync(uri, cancellationToken), content, cancellationToken);
            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return new HttpRelayResponse((int)response.StatusCode, responseJson);
        }

        private async Task<HttpRelayResponse> PutRawAsync(string uri, string json, CancellationToken cancellationToken)
        {
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.PutAsync(
                await GetRequestUriAsync(uri, cancellationToken), content, cancellationToken);
            return new HttpRelayResponse((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        private async Task<HttpRelayResponse> GetRawAsync(
            string uri,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                await GetRequestUriAsync(uri, cancellationToken), cancellationToken);
            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return new HttpRelayResponse((int)response.StatusCode, responseJson);
        }

        private async Task<Uri> GetRequestUriAsync(
            string relativeUri,
            CancellationToken cancellationToken)
        {
            EdgeBootstrapSettings? bootstrap = _bootstrapStore is null
                ? null
                : await _bootstrapStore.GetAsync(cancellationToken);
            Uri baseUri = bootstrap is not null
                ? new Uri(bootstrap.CentralServerUrl, UriKind.Absolute)
                : _httpClient.BaseAddress
                    ?? throw new InvalidOperationException("중앙 서버 주소가 설정되지 않았습니다.");
            return new Uri(baseUri, relativeUri);
        }

        private static FieldEventResponse ValidateEventId(
            Guid requestEventId,
            FieldEventResponse response)
        {
            if (response.EventId != requestEventId)
                throw new EventIdMismatchException(
                    "Gateway", requestEventId, response.EventId);
            return response;
        }
    }
}
