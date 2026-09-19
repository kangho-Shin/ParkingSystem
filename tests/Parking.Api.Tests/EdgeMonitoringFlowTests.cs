using System.Net;
using System.Text;
using Newtonsoft.Json;
using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class EdgeMonitoringFlowTests
{
    [Fact]
    public async Task 오프라인입차는_재전송성공후_세션번호를_가진다()
    {
        await using TestContext context = await TestContext.CreateAsync();
        FieldEventRequest request = CreateEntry();
        GatewayClient offlineClient = new(new HttpClient(new ThrowHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        });
        EdgeEventService service = new(context.Outbox, offlineClient, context.Monitoring);

        await service.AcceptEntryAsync(request, CancellationToken.None);
        EdgeEntryItem pending = Assert.Single(
            await context.Monitoring.GetEntriesAsync(1000, CancellationToken.None));
        Assert.Equal(EdgeDeliveryState.Pending, pending.DeliveryState);
        Assert.Null(pending.ParkingSessionId);

        FieldEventResponse centralResponse = new(
            request.EventId, true, 77, "OK", "입차", true);
        GatewayClient onlineClient = CreateClient(HttpStatusCode.OK, centralResponse);
        OutboxWorker worker = new(context.Outbox, onlineClient, context.Monitoring);
        await worker.ProcessOnceAsync(CancellationToken.None);

        EdgeEntryItem completed = Assert.Single(
            await context.Monitoring.GetEntriesAsync(1000, CancellationToken.None));
        Assert.Equal(EdgeDeliveryState.Completed, completed.DeliveryState);
        Assert.Equal(77, completed.ParkingSessionId);
    }

    [Fact]
    public async Task 정산후_허용출차는_두처리행을_남기고_입차행을_삭제한다()
    {
        await using TestContext context = await TestContext.CreateAsync();
        FieldEventRequest entry = CreateEntry();
        EdgeEventService entryService = new(
            context.Outbox,
            CreateClient(HttpStatusCode.OK,
                new FieldEventResponse(entry.EventId, true, 88, "OK", "입차", true)),
            context.Monitoring);
        await entryService.AcceptEntryAsync(entry, CancellationToken.None);

        CompletePaymentRequest payment = new()
        {
            PaymentId = Guid.NewGuid(),
            ParkingSessionId = 88,
            SiteId = 1,
            OriginalFee = 1000,
            DiscountFee = 200,
            PaidAmount = 800,
            PaymentMethod = "Card",
            PaidAt = DateTimeOffset.UtcNow
        };
        PaymentRelayService paymentService = new(
            context.Outbox,
            CreateRawClient(HttpStatusCode.OK, "{\"ResultCode\":\"OK\"}"),
            context.Monitoring);
        await paymentService.CompleteAsync(
            payment.PaymentId,
            JsonConvert.SerializeObject(payment),
            CancellationToken.None);

        ExitEventRequest exit = new(
            Guid.NewGuid(), 1, 20, 201, entry.CarNumber, DateTimeOffset.UtcNow,
            Groupnum: 1, OutImage: "out.jpg");
        EdgeEventService exitService = new(
            context.Outbox,
            CreateClient(HttpStatusCode.OK,
                new FieldEventResponse(exit.EventId, true, 88, "OK", "출차", true)),
            context.Monitoring);
        await exitService.AcceptExitAsync(exit, CancellationToken.None);

        IReadOnlyList<EdgeActivityItem> activities =
            await context.Monitoring.GetActivitiesAsync(1000, CancellationToken.None);
        Assert.Equal(2, activities.Count);
        Assert.Contains(activities, x => x.ActivityType == EdgeActivityType.Payment);
        Assert.Contains(activities, x => x.ActivityType == EdgeActivityType.Exit);
        Assert.Empty(await context.Monitoring.GetEntriesAsync(1000, CancellationToken.None));
    }

    [Fact]
    public async Task 차단출차는_처리행을_남기고_입차행을_유지한다()
    {
        await using TestContext context = await TestContext.CreateAsync();
        FieldEventRequest entry = CreateEntry();
        await context.Monitoring.RecordEntryAsync(entry, CancellationToken.None);
        ExitEventRequest exit = new(
            Guid.NewGuid(), 1, 20, 201, entry.CarNumber, DateTimeOffset.UtcNow,
            Groupnum: 1, OutImage: "out.jpg");
        EdgeEventService service = new(
            context.Outbox,
            CreateClient(HttpStatusCode.OK,
                new FieldEventResponse(exit.EventId, false, null, "UNPAID", "미결제", false)),
            context.Monitoring);

        await service.AcceptExitAsync(exit, CancellationToken.None);

        Assert.Single(await context.Monitoring.GetEntriesAsync(1000, CancellationToken.None));
        EdgeActivityItem activity = Assert.Single(
            await context.Monitoring.GetActivitiesAsync(1000, CancellationToken.None));
        Assert.False(activity.OpenBarrier);
        Assert.Equal("UNPAID", activity.ResultCode);
    }

    private static FieldEventRequest CreateEntry() => new(
        Guid.NewGuid(), 1, 10, 101, "12가3456", DateTimeOffset.UtcNow,
        Groupnum: 1, InImage: "in.jpg");

    private static GatewayClient CreateClient(HttpStatusCode status, FieldEventResponse response) =>
        CreateRawClient(status, JsonConvert.SerializeObject(response));

    private static GatewayClient CreateRawClient(HttpStatusCode status, string json) => new(
        new HttpClient(new ResponseHandler(status, json))
        {
            BaseAddress = new Uri("http://localhost/")
        });

    private sealed class ResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _json;

        public ResponseHandler(HttpStatusCode status, string json)
        {
            _status = status;
            _json = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
    }

    private sealed class ThrowHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("offline");
    }

    private sealed class TestContext : IAsyncDisposable
    {
        private TestContext(
            string path,
            SqliteOutboxRepository outbox,
            EdgeMonitoringRepository monitoring)
        {
            Path = path;
            Outbox = outbox;
            Monitoring = monitoring;
        }

        private string Path { get; }
        public SqliteOutboxRepository Outbox { get; }
        public EdgeMonitoringRepository Monitoring { get; }

        public static async Task<TestContext> CreateAsync()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"parking-monitor-flow-{Guid.NewGuid():N}.db");
            string connectionString = $"Data Source={path};Pooling=False";
            SqliteOutboxRepository outbox = new(connectionString);
            EdgeMonitoringRepository monitoring = new(connectionString);
            await outbox.InitializeAsync(CancellationToken.None);
            await monitoring.InitializeAsync(CancellationToken.None);
            return new TestContext(path, outbox, monitoring);
        }

        public ValueTask DisposeAsync()
        {
            if (File.Exists(Path))
                File.Delete(Path);
            return ValueTask.CompletedTask;
        }
    }
}
