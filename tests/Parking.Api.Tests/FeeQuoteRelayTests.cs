using System.Net;
using System.Text;
using Parking.EdgeService;
using Parking.Contracts;

namespace Parking.Api.Tests;

public sealed class FeeQuoteRelayTests
{
    [Fact]
    public async Task Gateway는_요금조회_JSON과_응답상태를_그대로_전달한다()
    {
        const string requestJson = "{\"Sitenum\":1,\"CarNumber\":\"12가3456\"}";
        CaptureHandler handler = new(HttpStatusCode.NotFound, "{\"Result\":\"NotFound\"}");
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost/") };
        Parking.EdgeGateway.ParkingApiClient client = new(httpClient);

        HttpRelayResponse result = await client.RelayFeeQuoteAsync(
            requestJson,
            CancellationToken.None);

        Assert.Equal("/api/v1/fees/quote", handler.RequestPath);
        Assert.Equal(requestJson, handler.RequestContent);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("{\"Result\":\"NotFound\"}", result.Content);
    }

    [Fact]
    public async Task EdgeService는_요금조회_JSON을_Gateway로_전달한다()
    {
        const string requestJson = "{\"Sitenum\":1,\"CarNumber\":\"12가3456\"}";
        CaptureHandler handler = new(HttpStatusCode.OK, "{\"PayableAmount\":600}");
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost/") };
        Parking.EdgeService.GatewayClient client = new(httpClient);

        HttpRelayResponse result = await client.RelayFeeQuoteAsync(
            requestJson,
            CancellationToken.None);

        Assert.Equal("/api/v1/edge/fees/quote", handler.RequestPath);
        Assert.Equal(requestJson, handler.RequestContent);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("{\"PayableAmount\":600}", result.Content);
    }

    [Fact]
    public async Task Gateway는_결제완료_JSON을_ParkingApi로_전달한다()
    {
        const string requestJson = "{\"PaymentId\":\"11111111-1111-1111-1111-111111111111\",\"PaidAmount\":600}";
        CaptureHandler handler = new(HttpStatusCode.OK, "{\"Accepted\":true}");
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost/") };
        Parking.EdgeGateway.ParkingApiClient client = new(httpClient);

        HttpRelayResponse result = await client.RelayPaymentCompleteAsync(
            requestJson,
            CancellationToken.None);

        Assert.Equal("/api/v1/payments/complete", handler.RequestPath);
        Assert.Equal(requestJson, handler.RequestContent);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("{\"Accepted\":true}", result.Content);
    }

    [Fact]
    public async Task EdgeService는_결제완료_JSON을_Gateway로_전달한다()
    {
        const string requestJson = "{\"PaymentId\":\"11111111-1111-1111-1111-111111111111\",\"PaidAmount\":600}";
        CaptureHandler handler = new(HttpStatusCode.Conflict, "{\"Accepted\":false}");
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost/") };
        Parking.EdgeService.GatewayClient client = new(httpClient);

        HttpRelayResponse result = await client.RelayPaymentCompleteAsync(
            requestJson,
            CancellationToken.None);

        Assert.Equal("/api/v1/edge/payments/complete", handler.RequestPath);
        Assert.Equal(requestJson, handler.RequestContent);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("{\"Accepted\":false}", result.Content);
    }

    [Fact]
    public async Task 결제완료요청은_전송전에_SQLite_Outbox에_저장한다()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"parking-{Guid.NewGuid():N}.db");
        try
        {
            SqliteOutboxRepository repository = new($"Data Source={databasePath};Pooling=False");
            await repository.InitializeAsync(CancellationToken.None);
            Guid paymentId = Guid.NewGuid();
            string json = $"{{\"PaymentId\":\"{paymentId:D}\",\"PaidAmount\":600}}";

            await repository.EnqueuePaymentAsync(paymentId, json, CancellationToken.None);
            IReadOnlyList<OutboxMessage> messages = await repository.GetPendingAsync(
                10,
                CancellationToken.None);

            OutboxMessage message = Assert.Single(messages);
            Assert.Equal(paymentId, message.EventId);
            Assert.Equal("Payment", message.EventType);
            Assert.Equal(json, message.PayloadJson);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task 중앙전송이_실패한_결제결과는_대기상태로_남긴다()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"parking-{Guid.NewGuid():N}.db");
        try
        {
            SqliteOutboxRepository repository = new($"Data Source={databasePath};Pooling=False");
            await repository.InitializeAsync(CancellationToken.None);
            CaptureHandler handler = new(HttpStatusCode.ServiceUnavailable, "");
            GatewayClient gatewayClient = new(new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost/")
            });
            PaymentRelayService service = new(repository, gatewayClient);
            Guid paymentId = Guid.NewGuid();
            string json = $"{{\"PaymentId\":\"{paymentId:D}\",\"PaidAmount\":600}}";

            HttpRelayResponse result = await service.CompleteAsync(
                paymentId,
                json,
                CancellationToken.None);
            IReadOnlyList<OutboxMessage> messages = await repository.GetPendingAsync(
                10,
                CancellationToken.None);

            Assert.Equal(202, result.StatusCode);
            Assert.Equal(paymentId, Assert.Single(messages).EventId);
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

        public string RequestPath { get; private set; } = "";
        public string RequestContent { get; private set; } = "";

        public CaptureHandler(HttpStatusCode statusCode, string responseContent)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath ?? "";
            RequestContent = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
            };
        }
    }
}
