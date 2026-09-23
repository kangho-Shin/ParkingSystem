using System.Net;
using System.Text;
using APSMain.Integration.EdgeService;

namespace APSMain.EdgeIntegration.Tests;

public sealed class EdgeServiceClientTests
{
    [Fact]
    public async Task Search_encodes_car_number_and_returns_candidates()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, "{\"Candidates\":[{\"ParkingSessionId\":7,\"CarNumber\":\"12가 3456\",\"Groupnum\":1,\"CarType\":0,\"InDateTime\":\"2026-09-20T01:00:00+09:00\",\"InImage\":null}]}");
        EdgeServiceClient client = CreateClient(handler);

        EdgeCallResult<ParkingSearchResult> result = await client.SearchParkingAsync("12가 3456", DateTimeOffset.Parse("2026-09-20T02:00:00+09:00"));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Candidates);
        Assert.Contains("siteId=9001", handler.RequestUri!.Query);
        Assert.Contains("groupnum=2", handler.RequestUri.Query);
        Assert.Contains("carNumber=12%EA%B0%80%203456", handler.RequestUri!.Query);
    }

    [Fact]
    public async Task Search_maps_not_found_to_empty_result()
    {
        EdgeServiceClient client = CreateClient(new RecordingHandler(HttpStatusCode.NotFound, ""));
        EdgeCallResult<ParkingSearchResult> result = await client.SearchParkingAsync("3456", DateTimeOffset.UtcNow);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Candidates);
    }

    [Fact]
    public async Task Search_maps_service_unavailable_to_transient_failure()
    {
        EdgeServiceClient client = CreateClient(new RecordingHandler(HttpStatusCode.ServiceUnavailable, ""));
        EdgeCallResult<ParkingSearchResult> result = await client.SearchParkingAsync("3456", DateTimeOffset.UtcNow);
        Assert.Equal(EdgeCallStatus.TransientFailure, result.Status);
    }

    [Fact]
    public async Task Complete_posts_external_kiosk_identity_without_device_id()
    {
        RecordingHandler handler = new(
            HttpStatusCode.OK,
            "{\"EventId\":\"11111111111111111111111111111111\",\"Accepted\":true,\"ParkingSessionId\":681,\"ResultCode\":\"EXIT_ACCEPTED\",\"DisplayMessage\":\"OK\",\"OpenBarrier\":true}");
        EdgeServiceClient client = CreateClient(handler);

        EdgeCallResult<bool> result = await client.CompleteKioskEventAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/api/v1/local/kiosks/events/11111111111111111111111111111111/complete", handler.RequestUri!.AbsolutePath);
        Assert.Equal("{\"Sitenum\":9001,\"Groupnum\":2,\"Devicenum\":201}", handler.RequestBody);
        Assert.DoesNotContain("DeviceId", handler.RequestBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Complete_rejects_response_with_different_event_id()
    {
        RecordingHandler handler = new(
            HttpStatusCode.OK,
            "{\"EventId\":\"22222222222222222222222222222222\",\"Accepted\":true,\"ParkingSessionId\":681,\"ResultCode\":\"EXIT_ACCEPTED\",\"DisplayMessage\":\"OK\",\"OpenBarrier\":true}");
        EdgeServiceClient client = CreateClient(handler);

        EdgeCallResult<bool> result = await client.CompleteKioskEventAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.Equal(EdgeCallStatus.InvalidResponse, result.Status);
    }

    [Fact]
    public async Task Complete_payment_posts_edge_payment_contract()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, "{\"PaymentId\":\"11111111-1111-1111-1111-111111111111\",\"ParkingSessionId\":681,\"Accepted\":true,\"ResultCode\":\"PAYMENT_ACCEPTED\",\"Message\":\"OK\",\"ExitAllowed\":true}");
        EdgeServiceClient client = CreateClient(handler);
        EdgePaymentRequest request = new()
        {
            PaymentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ParkingSessionId = 681,
            SiteId = 9001,
            OriginalFee = 800,
            DiscountFee = 200,
            PaidAmount = 600,
            PaymentMethod = "Card",
            ApprovalNumber = "12345678",
            TerminalId = "APS-201",
            PaidAt = DateTimeOffset.Parse("2026-09-21T13:12:00+09:00")
        };

        EdgeCallResult<EdgePaymentResponse> result = await client.CompletePaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Accepted);
        Assert.Equal("/api/v1/local/payments/complete", handler.RequestUri!.AbsolutePath);
        Assert.Contains("\"PaymentId\":\"11111111111111111111111111111111\"", handler.RequestBody);
        Assert.Contains("\"SiteId\":9001", handler.RequestBody);
        Assert.Contains("\"PaidAmount\":600", handler.RequestBody);
    }

    [Fact]
    public async Task Complete_payment_rejects_different_payment_id()
    {
        RecordingHandler handler = new(HttpStatusCode.OK,
            "{\"PaymentId\":\"22222222222222222222222222222222\",\"ParkingSessionId\":681,\"Accepted\":true,\"ResultCode\":\"PAYMENT_COMPLETED\",\"Message\":\"OK\",\"ExitAllowed\":true}");
        EdgeServiceClient client = CreateClient(handler);
        EdgePaymentRequest request = new()
        {
            PaymentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ParkingSessionId = 681,
            SiteId = 9001,
            PaidAmount = 600,
            PaymentMethod = "Card",
            ApprovalNumber = "12345678",
            PaidAt = DateTimeOffset.Parse("2026-09-21T13:12:00+09:00")
        };

        EdgeCallResult<EdgePaymentResponse> result = await client.CompletePaymentAsync(request);

        Assert.Equal(EdgeCallStatus.InvalidResponse, result.Status);
    }

    [Fact]
    public async Task Complete_payment_accepts_durable_pending_sync_response()
    {
        RecordingHandler handler = new(
            HttpStatusCode.Accepted,
            "{\"Accepted\":true,\"ResultCode\":\"PAYMENT_PENDING_SYNC\",\"Message\":\"queued\"}");
        EdgeServiceClient client = CreateClient(handler);
        EdgePaymentRequest request = new()
        {
            PaymentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ParkingSessionId = 681,
            SiteId = 9001,
            PaidAmount = 600,
            PaymentMethod = "Card",
            ApprovalNumber = "12345678",
            PaidAt = DateTimeOffset.Parse("2026-09-21T13:12:00+09:00")
        };

        EdgeCallResult<EdgePaymentResponse> result = await client.CompletePaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(request.PaymentId, result.Value!.PaymentId);
        Assert.Equal(request.ParkingSessionId, result.Value.ParkingSessionId);
        Assert.True(result.Value.Accepted);
        Assert.Equal("PAYMENT_PENDING_SYNC", result.Value.ResultCode);
        Assert.False(result.Value.ExitAllowed);
    }

    [Fact]
    public async Task Display_service_unavailable_is_transient_failure()
    {
        EdgeServiceClient client = CreateClient(
            new RecordingHandler(HttpStatusCode.ServiceUnavailable, ""));

        EdgeCallResult<bool> result = await client.DisplayFeeAsync("12가3456", 1000);

        Assert.Equal(EdgeCallStatus.TransientFailure, result.Status);
    }

    private static EdgeServiceClient CreateClient(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5200/") },
        new EdgeServiceOptions(new Uri("http://localhost:5200/"), 9001, 2, 201));

    private sealed class RecordingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string RequestBody { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Method = request.Method;
            RequestBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
