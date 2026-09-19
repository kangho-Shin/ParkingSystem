using System.Net;
using System.Net.Http.Json;
using Parking.Contracts;
using Parking.Simulator;

namespace Parking.Api.Tests;

public sealed class SimulatorEntryCommandTests
{
    [Fact]
    public async Task 입차명령은_지정한_EventId를_EdgeService로_전송한다()
    {
        Guid eventId = Guid.NewGuid();
        CaptureHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost/") };
        EntrySimulator simulator = new(httpClient);

        await simulator.SendAsync(
            eventId, 1, 10, 101, "12가3456", CancellationToken.None);

        Assert.Equal("/api/v1/edge/events", handler.RequestPath);
        Assert.NotNull(handler.Request);
        Assert.Equal(eventId, handler.Request.EventId);
        Assert.Equal(1, handler.Request.SiteId);
        Assert.Equal(10, handler.Request.LaneId);
        Assert.Equal(101, handler.Request.DeviceId);
        Assert.Equal("12가3456", handler.Request.CarNumber);
        Assert.Equal(ParkingEventType.Entry, handler.Request.EventType);
        Assert.EndsWith(".jpg", handler.Request.InImage);
        Assert.StartsWith("001_001_010_Entry_", handler.Request.InImage);
    }

    [Fact]
    public async Task 출차명령은_OutImage와_Exit방향을_전송한다()
    {
        Guid eventId = Guid.NewGuid();
        ExitCaptureHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost/") };

        await new ExitSimulator(httpClient).SendAsync(
            eventId, 1, 2, 20, 201, "34나5678", CancellationToken.None);

        Assert.Equal("/api/v1/edge/exits", handler.RequestPath);
        Assert.NotNull(handler.Request);
        Assert.Equal(ParkingEventType.Exit, handler.Request.EventType);
        Assert.StartsWith("001_002_020_Exit_", handler.Request.OutImage);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string RequestPath { get; private set; } = "";
        public FieldEventRequest? Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath ?? "";
            Request = await request.Content!.ReadFromJsonAsync<FieldEventRequest>(
                cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new FieldEventResponse(
                    Request!.EventId,
                    true,
                    1,
                    "ENTRY_ACCEPTED",
                    "입차 허용",
                    true))
            };
        }
    }

    private sealed class ExitCaptureHandler : HttpMessageHandler
    {
        public string RequestPath { get; private set; } = "";
        public ExitEventRequest? Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath ?? "";
            Request = await request.Content!.ReadFromJsonAsync<ExitEventRequest>(
                cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new FieldEventResponse(
                    Request!.EventId, true, 1, "EXIT_ACCEPTED", "출차 허용", true))
            };
        }
    }
}
