using System.Net.Http.Json;
using System.Text.Json;
using Parking.Contracts;

namespace Parking.EdgeGateway
{
    public sealed class ParkingApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = null };
        public ParkingApiClient(HttpClient httpClient) { _httpClient = httpClient; }

        public Task<FieldEventResponse> SendAsync(FieldEventRequest request, CancellationToken cancellationToken) =>
            PostAsync<FieldEventRequest, FieldEventResponse>("api/v1/parking/entries", request, cancellationToken);

        public Task<FieldEventResponse> SendExitAsync(ExitEventRequest request, CancellationToken cancellationToken) =>
            PostAsync<ExitEventRequest, FieldEventResponse>("api/v1/parking/exits", request, cancellationToken);

        public async Task<SiteConfiguration> GetSiteConfigurationAsync(long siteId, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync($"api/v1/config/sites/{siteId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SiteConfiguration>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("현장 설정 응답이 없습니다.");
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(
            string uri, TRequest request, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(uri, request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Parking.Api 응답이 없습니다.");
        }
    }
}
