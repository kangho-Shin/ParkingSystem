using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class EdgeManagementEndpointTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task 목록limit범위가_아니면_400이다(int limit)
    {
        await using TestContext context = await TestContext.CreateAsync();

        ActionResult<IReadOnlyList<EdgeEntryItem>> result =
            await context.Controller.GetEntriesAsync(limit, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task Gateway연결실패도_EdgeService상태는_응답한다()
    {
        await using TestContext context = await TestContext.CreateAsync();

        ActionResult<EdgeServiceStatus> result =
            await context.Controller.GetStatusAsync(CancellationToken.None);

        EdgeServiceStatus status = Assert.IsType<EdgeServiceStatus>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.True(status.ServiceConnected);
        Assert.False(status.GatewayConnected);
        Assert.False(status.CentralConnected);
    }

    [Fact]
    public async Task 중앙연결정보는_loopback요청에만_응답한다()
    {
        await using TestContext context = await TestContext.CreateAsync();

        LocalConfigurationController local = context.CreateLocalController(
            System.Net.IPAddress.Loopback);
        OkObjectResult ok = Assert.IsType<OkObjectResult>(
            await local.GetCentralConnectionAsync(CancellationToken.None));
        CentralConnectionResponse connection = Assert.IsType<CentralConnectionResponse>(ok.Value);
        Assert.Equal(1, connection.SiteId);
        Assert.Equal("test-key", connection.SiteAuthKey);

        local = context.CreateLocalController(System.Net.IPAddress.Parse("10.0.0.10"));
        Assert.IsType<ForbidResult>(
            await local.GetCentralConnectionAsync(CancellationToken.None));

        local.HttpContext.Connection.RemoteIpAddress = null;
        Assert.IsType<ForbidResult>(
            await local.GetCentralConnectionAsync(CancellationToken.None));
    }

    private sealed class TestContext : IAsyncDisposable
    {
        private TestContext(
            string directory,
            ManagementController controller,
            LocalBootstrapStore bootstrapStore,
            LocalConfigurationService configurationService)
        {
            Directory = directory;
            Controller = controller;
            BootstrapStore = bootstrapStore;
            ConfigurationService = configurationService;
        }

        private string Directory { get; }
        private LocalBootstrapStore BootstrapStore { get; }
        private LocalConfigurationService ConfigurationService { get; }
        public ManagementController Controller { get; }

        public LocalConfigurationController CreateLocalController(
            System.Net.IPAddress remoteAddress)
        {
            LocalConfigurationController controller = new(
                BootstrapStore, ConfigurationService);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.HttpContext.Connection.RemoteIpAddress = remoteAddress;
            return controller;
        }

        public static async Task<TestContext> CreateAsync()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), $"parking-management-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(directory);
            string connectionString =
                $"Data Source={Path.Combine(directory, "edge.db")};Pooling=False";
            EdgeMonitoringRepository monitoring = new(connectionString);
            SqliteOutboxRepository outbox = new(connectionString);
            LocalConfigurationStore configurationStore = new(connectionString);
            LocalBootstrapStore bootstrapStore = new(connectionString);
            await monitoring.InitializeAsync(CancellationToken.None);
            await outbox.InitializeAsync(CancellationToken.None);
            await configurationStore.InitializeAsync(CancellationToken.None);
            await bootstrapStore.InitializeAsync(CancellationToken.None);
            await bootstrapStore.SaveAsync(new EdgeBootstrapSettings(
                1, "http://localhost:5100/", "http://localhost:5400/",
                @"D:\\LPR\\IMAGE", "test-key", DateTimeOffset.UtcNow), CancellationToken.None);
            GatewayClient gatewayClient = new(new HttpClient(new ThrowHandler())
            {
                BaseAddress = new Uri("http://localhost:5100/")
            });
            LocalConfigurationService configurationService = new(bootstrapStore, configurationStore);
            EdgeManagementService service = new(
                monitoring, outbox, configurationService, gatewayClient,
                new LprConnectionTracker());
            return new TestContext(
                directory,
                new ManagementController(service),
                bootstrapStore,
                configurationService);
        }

        public ValueTask DisposeAsync()
        {
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, true);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("offline");
    }
}
