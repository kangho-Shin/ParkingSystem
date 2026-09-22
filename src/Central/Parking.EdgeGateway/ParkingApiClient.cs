using System.Text;
using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.EdgeGateway
{
    public sealed class ParkingApiClient
    {
        private readonly HttpClient _httpClient;
        public ParkingApiClient(HttpClient httpClient) { _httpClient = httpClient; }

        public async Task<FieldEventResponse> SendAsync(
            FieldEventRequest request,
            CancellationToken cancellationToken) =>
            ValidateEventId(
                request.EventId,
                await PostAsync<FieldEventRequest, FieldEventResponse>(
                    "api/v1/parking/entries", request, cancellationToken));

        public async Task<FieldEventResponse> SendExitAsync(
            ExitEventRequest request,
            CancellationToken cancellationToken) =>
            ValidateEventId(
                request.EventId,
                await PostAsync<ExitEventRequest, FieldEventResponse>(
                    "api/v1/parking/exits", request, cancellationToken));

        public async Task<FieldEventResponse> SendOfflineKioskExitAsync(
            OfflineKioskExitRequest request, CancellationToken cancellationToken) =>
            ValidateEventId(
                request.EventId,
                await PostAsync<OfflineKioskExitRequest, FieldEventResponse>(
                    "api/v1/parking/exits/kiosk-offline-open", request, cancellationToken));

        public Task<HttpRelayResponse> RelayFeeQuoteAsync(
            string json,
            CancellationToken cancellationToken) =>
            PostRawAsync("api/v1/fees/quote", json, cancellationToken);

        public Task<HttpRelayResponse> RelaySessionFeeQuoteAsync(
            string json,
            CancellationToken cancellationToken) =>
            PostRawAsync("api/v1/fees/quote/session", json, cancellationToken);

        public Task<HttpRelayResponse> RelayParkingSearchAsync(
            string queryString,
            CancellationToken cancellationToken) =>
            GetRawAsync($"api/v1/parking/search{queryString}", cancellationToken);

        public Task<HttpRelayResponse> RelayPaymentCompleteAsync(
            string json,
            CancellationToken cancellationToken) =>
            PostRawAsync("api/v1/payments/complete", json, cancellationToken);

        public Task<HttpRelayResponse> RelayCarNumberCorrectionAsync(
            long parkingSessionId, string json, CancellationToken cancellationToken) =>
            PutRawAsync($"api/v1/parking/sessions/{parkingSessionId}/car-number", json, cancellationToken);

        public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync("", cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<SiteConfiguration> GetSiteConfigurationAsync(
            long siteId, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync($"api/v1/config/sites/{siteId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<SiteConfiguration>(json)
                ?? throw new InvalidOperationException("현장 설정 응답이 없습니다.");
        }

        public async Task<VersionedSiteConfiguration?> GetVersionedConfigurationAsync(
            long siteId,
            string siteKey,
            CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new(
                HttpMethod.Get, $"api/v1/config/sites/{siteId}/versioned");
            request.Headers.Add("X-Site-Key", siteKey);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<VersionedSiteConfiguration>(
                await response.Content.ReadAsStringAsync(cancellationToken));
        }

        public async Task<HttpRelayResponse> SaveVersionedConfigurationAsync(
            long siteId,
            string siteKey,
            string json,
            CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new(
                HttpMethod.Put, $"api/v1/config/sites/{siteId}/versioned");
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
            using HttpResponseMessage response = await _httpClient.PostAsync(uri, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<TResponse>(responseJson)
                ?? throw new InvalidOperationException("Parking.Api 응답이 없습니다.");
        }

        private async Task<HttpRelayResponse> PostRawAsync(
            string uri,
            string json,
            CancellationToken cancellationToken)
        {
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.PostAsync(uri, content, cancellationToken);
            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return new HttpRelayResponse((int)response.StatusCode, responseJson);
        }

        private async Task<HttpRelayResponse> PutRawAsync(string uri, string json, CancellationToken cancellationToken)
        {
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _httpClient.PutAsync(uri, content, cancellationToken);
            return new HttpRelayResponse((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        private async Task<HttpRelayResponse> GetRawAsync(
            string uri,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(uri, cancellationToken);
            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return new HttpRelayResponse((int)response.StatusCode, responseJson);
        }

        private static FieldEventResponse ValidateEventId(
            Guid requestEventId,
            FieldEventResponse response)
        {
            if (response.EventId != requestEventId)
                throw new EventIdMismatchException(
                    "Parking.Api", requestEventId, response.EventId);
            return response;
        }
    }
}
