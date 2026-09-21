using System.Net.Http.Json;
using System.Text.Json;
using Parking.Contracts;

namespace Parking.Simulator;

public sealed class ExitSimulator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly HttpClient _httpClient;

    public ExitSimulator(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FieldEventResponse> SendAsync(
        Guid eventId,
        long siteId,
        int groupnum,
        long laneId,
        long deviceId,
        string carNumber,
        CancellationToken cancellationToken,
        string? outImage = null,
        int? deviceNumber = null)
    {
        DateTimeOffset outDateTime = DateTimeOffset.Now;
        outImage ??= VehicleImageName.Create(
            siteId, groupnum, deviceNumber ?? checked((int)deviceId), laneId,
            ParkingEventType.Exit,
            outDateTime, carNumber, eventId);
        ExitEventRequest request = new(
            eventId,
            siteId,
            laneId,
            deviceId,
            carNumber,
            outDateTime,
            groupnum,
            1,
            null,
            ParkingEventType.Exit,
            outImage);

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "/api/v1/edge/exits",
            request,
            JsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FieldEventResponse>(
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("EdgeService 응답이 없습니다.");
    }
}
