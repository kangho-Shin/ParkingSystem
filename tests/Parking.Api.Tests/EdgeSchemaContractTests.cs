using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class EdgeSchemaContractTests
{
    [Fact]
    public async Task Edge와_Gateway는_EventId를_32자리문자열로_전송한다()
    {
        Guid eventId = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        string responseJson = JsonConvert.SerializeObject(
            new FieldEventResponse(eventId, true, 77, "OK", "처리", true));
        CaptureHandler edgeHandler = new(HttpStatusCode.OK, responseJson);
        CaptureHandler gatewayHandler = new(HttpStatusCode.OK, responseJson);
        FieldEventRequest request = new(
            eventId, 9001, 9010, 4001, "12가3456", DateTimeOffset.UtcNow, 2);

        await new GatewayClient(new HttpClient(edgeHandler)
        {
            BaseAddress = new Uri("http://localhost/")
        }).SendEntryAsync(request, CancellationToken.None);
        await new Parking.EdgeGateway.ParkingApiClient(new HttpClient(gatewayHandler)
        {
            BaseAddress = new Uri("http://localhost/")
        }).SendAsync(request, CancellationToken.None);

        Assert.Equal(
            "123456781234123412341234567890ab",
            JObject.Parse(edgeHandler.RequestContent).Value<string>("EventId"));
        Assert.Equal(
            "123456781234123412341234567890ab",
            JObject.Parse(gatewayHandler.RequestContent).Value<string>("EventId"));
    }

    [Fact]
    public async Task Gateway는_다른_EventId의_중앙응답을_거부한다()
    {
        Guid requestId = Guid.NewGuid();
        CaptureHandler handler = new(
            HttpStatusCode.OK,
            JsonConvert.SerializeObject(new FieldEventResponse(
                Guid.NewGuid(), true, 77, "OK", "처리", true)));
        Parking.EdgeGateway.ParkingApiClient client = new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SendAsync(
                new FieldEventRequest(
                    requestId, 9001, 9010, 4001, "12가3456", DateTimeOffset.UtcNow, 2),
                CancellationToken.None));

        Assert.Contains("EventId", exception.Message);
    }

    [Fact]
    public async Task Gateway는_EventId불일치를_502로_반환한다()
    {
        Guid requestId = Guid.NewGuid();
        CaptureHandler handler = new(
            HttpStatusCode.OK,
            JsonConvert.SerializeObject(new FieldEventResponse(
                Guid.NewGuid(), true, 77, "OK", "처리", true)));
        Parking.EdgeGateway.ParkingApiClient client = new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        });
        Parking.EdgeGateway.EdgeGatewayController controller = new(
            new Parking.EdgeGateway.FieldEventRelay(client), client)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        IActionResult result = await controller.EntryAsync(
            new FieldEventRequest(
                requestId, 9001, 9010, 4001, "12가3456", DateTimeOffset.UtcNow, 2),
            CancellationToken.None);

        StatusCodeResult status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
    }

    [Fact]
    public async Task EdgeService는_Gateway의_HTTP오류를_오프라인입차성공으로_바꾸지_않는다()
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(), $"parking-edge-integrity-{Guid.NewGuid():N}.db");
        try
        {
            SqliteOutboxRepository outbox = new(
                $"Data Source={databasePath};Pooling=False");
            await outbox.InitializeAsync(CancellationToken.None);
            CaptureHandler handler = new(HttpStatusCode.BadGateway, "");
            EdgeEventService service = new(
                outbox,
                new GatewayClient(new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://localhost/")
                }));
            FieldEventRequest request = new(
                Guid.NewGuid(), 9001, 9010, 4001, "12가3456", DateTimeOffset.UtcNow, 2);

            await Assert.ThrowsAsync<HttpRequestException>(
                () => service.AcceptEntryAsync(request, CancellationToken.None));

            Assert.Contains(
                await outbox.GetPendingAsync(10, CancellationToken.None),
                item => item.EventId == request.EventId);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task EdgeService는_Gateway의_503을_기존_오프라인정책으로_처리한다()
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(), $"parking-edge-offline-{Guid.NewGuid():N}.db");
        try
        {
            SqliteOutboxRepository outbox = new(
                $"Data Source={databasePath};Pooling=False");
            await outbox.InitializeAsync(CancellationToken.None);
            CaptureHandler handler = new(HttpStatusCode.ServiceUnavailable, "");
            EdgeEventService service = new(
                outbox,
                new GatewayClient(new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://localhost/")
                }));

            FieldEventResponse entryResponse = await service.AcceptEntryAsync(
                new FieldEventRequest(
                    Guid.NewGuid(), 9001, 9010, 4001, "12가3456", DateTimeOffset.UtcNow, 2),
                CancellationToken.None);
            FieldEventResponse exitResponse = await service.AcceptExitAsync(
                new ExitEventRequest(
                    Guid.NewGuid(), 9001, 9020, 4002, "12가3456", DateTimeOffset.UtcNow, 2),
                CancellationToken.None);

            Assert.True(entryResponse.Accepted);
            Assert.Equal("EDGE_OFFLINE_ENTRY", entryResponse.ResultCode);
            Assert.False(exitResponse.Accepted);
            Assert.Equal("CENTRAL_OFFLINE_EXIT_BLOCKED", exitResponse.ResultCode);
            Assert.Equal(2, (await outbox.GetPendingAsync(10, CancellationToken.None)).Count);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Edge와_Gateway는_출차_EventId도_32자리문자열로_전송한다()
    {
        Guid eventId = Guid.Parse("abcdefab-cdef-cdef-cdef-abcdefabcdef");
        string responseJson = JsonConvert.SerializeObject(
            new FieldEventResponse(eventId, true, 77, "OK", "출차", true));
        CaptureHandler edgeHandler = new(HttpStatusCode.OK, responseJson);
        CaptureHandler gatewayHandler = new(HttpStatusCode.OK, responseJson);
        ExitEventRequest request = new(
            eventId, 9001, 9020, 4002, "12가3456", DateTimeOffset.UtcNow, 2);

        await new GatewayClient(new HttpClient(edgeHandler)
        {
            BaseAddress = new Uri("http://localhost/")
        }).SendExitAsync(request, CancellationToken.None);
        await new Parking.EdgeGateway.ParkingApiClient(new HttpClient(gatewayHandler)
        {
            BaseAddress = new Uri("http://localhost/")
        }).SendExitAsync(request, CancellationToken.None);

        Assert.Equal(
            "abcdefabcdefcdefcdefabcdefabcdef",
            JObject.Parse(edgeHandler.RequestContent).Value<string>("EventId"));
        Assert.Equal(
            "abcdefabcdefcdefcdefabcdefabcdef",
            JObject.Parse(gatewayHandler.RequestContent).Value<string>("EventId"));
    }

    [Fact]
    public async Task Gateway는_사이트키를_버전설정요청에_전달한다()
    {
        VersionedSiteConfiguration value = new(
            new SiteConfiguration(
                new ParkingSite(9001, "시험현장", true),
                new[] { new ParkingLane(9010, 9001, 2, "입차", "ENTRY", true) },
                new[] { new ParkingDevice(4001, 9001, 9010, 401, "LPR", "입차LPR", null, true) }),
            3,
            DateTimeOffset.UtcNow);
        CaptureHandler handler = new(
            HttpStatusCode.OK,
            JsonConvert.SerializeObject(value));
        Parking.EdgeGateway.ParkingApiClient client = new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        });

        VersionedSiteConfiguration? result = await client.GetVersionedConfigurationAsync(
            9001, "site-9001-key", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("site-9001-key", handler.SiteKey);
        Assert.Equal(401, result.Configuration.Devices.Single().DeviceNumber);
    }

    [Fact]
    public async Task 잘못된_사이트키의_401응답을_503으로_변경하지_않는다()
    {
        CaptureHandler handler = new(HttpStatusCode.Unauthorized, "");
        Parking.EdgeGateway.ParkingApiClient client = new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        });
        Parking.EdgeGateway.EdgeGatewayController controller = new(
            new Parking.EdgeGateway.FieldEventRelay(client), client)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers["X-Site-Key"] = "wrong-key";

        IActionResult result = await controller.GetVersionedConfigurationAsync(
            9001, CancellationToken.None);

        StatusCodeResult status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, status.StatusCode);
    }

    [Fact]
    public async Task Outbox재전송은_처음_EventId를_32자리로_그대로_전송한다()
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(), $"parking-edge-schema-{Guid.NewGuid():N}.db");
        try
        {
            SqliteOutboxRepository outbox = new(
                $"Data Source={databasePath};Pooling=False");
            await outbox.InitializeAsync(CancellationToken.None);
            Guid eventId = Guid.Parse("10203040-5060-7080-90a0-b0c0d0e0f000");
            FieldEventRequest request = new(
                eventId, 9001, 9010, 4001, "12가3456", DateTimeOffset.UtcNow, 2);
            await outbox.EnqueueEntryAsync(request, CancellationToken.None);
            CaptureHandler handler = new(
                HttpStatusCode.OK,
                JsonConvert.SerializeObject(new FieldEventResponse(
                    eventId, true, 77, "OK", "입차", true)));
            OutboxWorker worker = new(
                outbox,
                new GatewayClient(new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://localhost/")
                }));

            await worker.ProcessOnceAsync(CancellationToken.None);

            Assert.Equal(
                "102030405060708090a0b0c0d0e0f000",
                JObject.Parse(handler.RequestContent).Value<string>("EventId"));
            Assert.Empty(await outbox.GetPendingAsync(10, CancellationToken.None));
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseContent;

        public string RequestContent { get; private set; } = "";
        public string? SiteKey { get; private set; }

        public CaptureHandler(HttpStatusCode statusCode, string responseContent)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestContent = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            SiteKey = request.Headers.TryGetValues("X-Site-Key", out IEnumerable<string>? values)
                ? values.Single()
                : null;
            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(
                    _responseContent, Encoding.UTF8, "application/json")
            };
        }
    }
}
