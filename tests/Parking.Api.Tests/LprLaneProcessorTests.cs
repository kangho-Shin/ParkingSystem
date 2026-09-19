using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class LprLaneProcessorTests
{
    [Fact]
    public async Task 정상입차는_기존입차흐름을_호출하고_ACK한다()
    {
        Guid eventId = Guid.NewGuid();
        FieldEventResponse response = new(eventId, true, 77, "OK", "입차", true);
        await using TestContext context = await TestContext.CreateAsync(
            "Entry", HttpStatusCode.OK, JsonConvert.SerializeObject(response));
        string fileName = $"001_002_101_010_Entry_20260919153025123_12가3456_{eventId:N}.jpg";

        LprProcessResult result = await context.Processor.ProcessAsync(
            fileName, CancellationToken.None);

        Assert.True(result.Acknowledged);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(77, result.ParkingResponse!.ParkingSessionId);
        Assert.Empty(await context.Outbox.GetPendingAsync(10, CancellationToken.None));
    }

    [Fact]
    public async Task 차로방향불일치는_NAK하고_Outbox에_저장하지않는다()
    {
        Guid eventId = Guid.NewGuid();
        await using TestContext context = await TestContext.CreateAsync(
            "Exit", HttpStatusCode.OK, "{}");
        string fileName = $"001_002_101_010_Entry_20260919153025123_12가3456_{eventId:N}.jpg";

        LprProcessResult result = await context.Processor.ProcessAsync(
            fileName, CancellationToken.None);

        Assert.False(result.Acknowledged);
        Assert.Equal("DIRECTION_MISMATCH", result.ErrorCode);
        Assert.Empty(await context.Outbox.GetPendingAsync(10, CancellationToken.None));
    }

    [Fact]
    public async Task 미정산출차도_정상처리된패킷이므로_ACK한다()
    {
        Guid eventId = Guid.NewGuid();
        FieldEventResponse response = new(eventId, false, null, "UNPAID", "미정산", false);
        await using TestContext context = await TestContext.CreateAsync(
            "Exit", HttpStatusCode.OK, JsonConvert.SerializeObject(response));
        string fileName = $"001_002_101_010_Exit_20260919153025123_12가3456_{eventId:N}.jpg";

        LprProcessResult result = await context.Processor.ProcessAsync(
            fileName, CancellationToken.None);

        Assert.True(result.Acknowledged);
        Assert.False(result.ParkingResponse!.OpenBarrier);
    }

    private sealed class TestContext : IAsyncDisposable
    {
        private TestContext(
            string path,
            LprLaneProcessor processor,
            SqliteOutboxRepository outbox)
        {
            Path = path;
            Processor = processor;
            Outbox = outbox;
        }

        private string Path { get; }
        public LprLaneProcessor Processor { get; }
        public SqliteOutboxRepository Outbox { get; }

        public static async Task<TestContext> CreateAsync(
            string laneDirection,
            HttpStatusCode status,
            string responseJson)
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), $"parking-lpr-{Guid.NewGuid():N}.db");
            string connectionString = $"Data Source={path};Pooling=False";
            SqliteOutboxRepository outbox = new(connectionString);
            LocalConfigurationStore store = new(connectionString);
            await outbox.InitializeAsync(CancellationToken.None);
            await store.InitializeAsync(CancellationToken.None);
            await store.SaveAsync(new SiteConfiguration(
                new ParkingSite(1, "시험현장", true),
                new[] { new ParkingLane(10, 1, 2, "시험차로", laneDirection, true) },
                new[] { new ParkingDevice(101, 1, 10, 101, "LPR", "시험LPR", null, true) }),
                CancellationToken.None);
            GatewayClient gateway = new(new HttpClient(new ResponseHandler(status, responseJson))
            {
                BaseAddress = new Uri("http://localhost/")
            });
            EdgeEventService eventService = new(outbox, gateway);
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Edge:SiteId"] = "1" })
                .Build();
            LprLaneProcessor processor = new(
                configuration,
                new LprFileNameParser(),
                store,
                eventService,
                NullLogger<LprLaneProcessor>.Instance);
            return new TestContext(path, processor, outbox);
        }

        public ValueTask DisposeAsync()
        {
            if (File.Exists(Path)) File.Delete(Path);
            return ValueTask.CompletedTask;
        }
    }

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
}
