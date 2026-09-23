using System.Net;
using APSMain.Integration.EdgeService;

namespace APSMain.EdgeIntegration.Tests;

public sealed class KioskExitCoordinatorTests
{
    [Fact]
    public async Task Other_site_notification_is_rejected_without_http_call()
    {
        CountingHandler handler = new(HttpStatusCode.OK, "{}");
        EdgeServiceOptions options = Options();
        KioskExitCoordinator coordinator = new(
            Client(handler, options), new KioskEventTracker(), options, TimeSpan.Zero);
        KioskExitNotification notification = new(
            Guid.NewGuid(), 9002, 2, 4002, 2001,
            "12가3456", DateTimeOffset.Now, null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.HandleAsync(notification));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task Permanent_search_failure_is_not_retried()
    {
        CountingHandler handler = new(HttpStatusCode.BadRequest, "");
        EdgeServiceOptions options = Options();
        KioskExitCoordinator coordinator = new(
            Client(handler, options), new KioskEventTracker(), options, TimeSpan.Zero);
        KioskExitNotification notification = new(
            Guid.NewGuid(), 9001, 2, 4002, 2001,
            "12가3456", DateTimeOffset.Now, null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.HandleAsync(notification));

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public void SignalR_notification_must_match_registered_internal_kiosk_device()
    {
        KioskExitNotification notification = new(
            Guid.NewGuid(), 9001, 2, 4002, 2002,
            "12가3456", DateTimeOffset.Now, null);

        Assert.False(KioskSignalRClient.MatchesRegistration(notification, 9001, 2, 2001));
        Assert.True(KioskSignalRClient.MatchesRegistration(
            notification with { KioskDeviceId = 2001 }, 9001, 2, 2001));
    }

    private static EdgeServiceOptions Options() =>
        new(new Uri("http://localhost:5200/"), 9001, 2, 201);

    private static EdgeServiceClient Client(HttpMessageHandler handler, EdgeServiceOptions options) =>
        new(new HttpClient(handler) { BaseAddress = options.BaseAddress }, options);

    private sealed class CountingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body)
            });
        }
    }
}
