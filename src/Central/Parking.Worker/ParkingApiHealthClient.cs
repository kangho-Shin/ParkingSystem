namespace Parking.Worker;

public sealed class ParkingApiHealthClient
{
    private readonly HttpClient _httpClient;

    public ParkingApiHealthClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<bool> CheckAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
