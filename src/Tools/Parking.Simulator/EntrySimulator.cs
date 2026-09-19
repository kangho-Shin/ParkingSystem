using System.Net.Http.Json;
using System.Text.Json;
using Parking.Contracts;

namespace Parking.Simulator;

public sealed class EntrySimulator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly HttpClient _httpClient;

    public EntrySimulator(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FieldEventResponse> SendAsync(
        Guid eventId,
        long siteId,
        long laneId,
        long deviceId,
        string carNumber,
        CancellationToken cancellationToken,
        int groupnum = 1,
        string? inImage = null)
    {
        DateTimeOffset inDateTime = DateTimeOffset.Now;
        inImage ??= VehicleImageName.Create(
            siteId, groupnum, laneId, ParkingEventType.Entry,
            inDateTime, carNumber, eventId);
        FieldEventRequest request = new(
            eventId,
            siteId,
            laneId,
            deviceId,
            carNumber,
            inDateTime,
            groupnum,
            ParkingEventType.Entry,
            inImage);

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "/api/v1/edge/events",
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
