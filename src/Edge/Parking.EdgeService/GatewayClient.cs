using System.Net.Http.Json;
using System.Text.Json;
using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed class GatewayClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = null };
        public GatewayClient(HttpClient httpClient) { _httpClient = httpClient; }

        public Task<FieldEventResponse> SendEntryAsync(FieldEventRequest request, CancellationToken cancellationToken) =>
            PostAsync<FieldEventRequest, FieldEventResponse>("api/v1/edge/events", request, cancellationToken);

        public Task<FieldEventResponse> SendExitAsync(ExitEventRequest request, CancellationToken cancellationToken) =>
            PostAsync<ExitEventRequest, FieldEventResponse>("api/v1/edge/exits", request, cancellationToken);

        public async Task<SiteConfiguration> GetSiteConfigurationAsync(long siteId, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync($"api/v1/edge/config/sites/{siteId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SiteConfiguration>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Gateway 현장 설정 응답이 없습니다.");
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(
            string uri, TRequest request, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(uri, request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Gateway 응답이 없습니다.");
        }
    }
}
