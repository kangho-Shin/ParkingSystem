using Microsoft.AspNetCore.Mvc;
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

    [Theory]
    [InlineData("../secret.jpg")]
    [InlineData("C:\\secret.jpg")]
    [InlineData("sub/image.jpg")]
    [InlineData("image.txt")]
    public async Task 허용되지않은_이미지경로는_거부한다(string fileName)
    {
        await using TestContext context = await TestContext.CreateAsync();

        IActionResult result = context.Controller.GetImage(fileName);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task 설정폴더의_JPEG이미지를_반환한다()
    {
        await using TestContext context = await TestContext.CreateAsync();
        string imagePath = Path.Combine(context.ImageDirectory, "entry.jpg");
        await File.WriteAllBytesAsync(imagePath, new byte[] { 1, 2, 3 });

        IActionResult result = context.Controller.GetImage("entry.jpg");

        PhysicalFileResult file = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal(imagePath, file.FileName);
        Assert.Equal("image/jpeg", file.ContentType);
    }

    private sealed class TestContext : IAsyncDisposable
    {
        private TestContext(string directory, ManagementController controller)
        {
            Directory = directory;
            Controller = controller;
        }

        private string Directory { get; }
        public string ImageDirectory => Path.Combine(Directory, "images");
        public ManagementController Controller { get; }

        public static async Task<TestContext> CreateAsync()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), $"parking-management-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(directory);
            string imageDirectory = Path.Combine(directory, "images");
            System.IO.Directory.CreateDirectory(imageDirectory);
            string connectionString =
                $"Data Source={Path.Combine(directory, "edge.db")};Pooling=False";
            EdgeMonitoringRepository monitoring = new(connectionString);
            SqliteOutboxRepository outbox = new(connectionString);
            LocalConfigurationStore configurationStore = new(connectionString);
            await monitoring.InitializeAsync(CancellationToken.None);
            await outbox.InitializeAsync(CancellationToken.None);
            await configurationStore.InitializeAsync(CancellationToken.None);
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Edge:SiteId"] = "1",
                    ["Edge:ImageDirectory"] = imageDirectory
                })
                .Build();
            GatewayClient gatewayClient = new(new HttpClient(new ThrowHandler())
            {
                BaseAddress = new Uri("http://localhost:5100/")
            });
            EdgeManagementService service = new(
                configuration, monitoring, outbox, configurationStore, gatewayClient);
            return new TestContext(
                directory,
                new ManagementController(service, configuration));
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
