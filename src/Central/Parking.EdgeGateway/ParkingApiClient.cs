using System.Net.Http.Json;
using System.Text.Json;
using Parking.Contracts;

namespace Parking.EdgeGateway
{
    public sealed class ParkingApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = null
        };

        public ParkingApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<FieldEventResponse> SendAsync(
            FieldEventRequest request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                "api/v1/parking/entries",
                request,
                _jsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<FieldEventResponse>(
                       _jsonOptions,
                       cancellationToken)
                   ?? throw new InvalidOperationException(
                       "Parking.Api 응답이 없습니다.");
        }
    }
}