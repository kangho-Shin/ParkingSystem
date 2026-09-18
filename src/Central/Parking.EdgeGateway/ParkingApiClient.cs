using System.Text;
using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.EdgeGateway
{
    public sealed class ParkingApiClient
    {
        private readonly HttpClient _httpClient;
        public ParkingApiClient(HttpClient httpClient) { _httpClient = httpClient; }

        public Task<FieldEventResponse> SendAsync(FieldEventRequest request, CancellationToken cancellationToken) =>
            PostAsync<FieldEventRequest, FieldEventResponse>("api/v1/parking/entries", request, cancellationToken);

        public Task<FieldEventResponse> SendExitAsync(ExitEventRequest request, CancellationToken cancellationToken) =>
            PostAsync<ExitEventRequest, FieldEventResponse>("api/v1/parking/exits", request, cancellationToken);

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
    }
}
