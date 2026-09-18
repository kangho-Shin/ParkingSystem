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

        public async Task<FieldEventResponse> SendAsync(FieldEventRequest request, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/v1/edge/events", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FieldEventResponse>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Gateway 응답이 없습니다.");
        }
    }
}
