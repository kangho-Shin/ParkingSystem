using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

public sealed class CompletePaymentEndpointTests
{
    [Fact]
    public async Task 정상_결제완료요청은_출차허용결과를_반환한다()
    {
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        CompletePaymentRequest request = CreateRequest();

        HttpResponseMessage response = await PostAsync(client, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PaymentCompleteResponse? result = JsonConvert.DeserializeObject<PaymentCompleteResponse>(
            await response.Content.ReadAsStringAsync());
        Assert.NotNull(result);
        Assert.True(result.Accepted);
        Assert.True(result.ExitAllowed);
        Assert.Equal(request.PaymentId, result.PaymentId);
        Assert.Equal(request.ParkingSessionId, result.ParkingSessionId);
    }

    [Fact]
    public async Task 결제금액이_최종요금과_다르면_요청을_거부한다()
    {
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        CompletePaymentRequest request = CreateRequest();
        request.PaidAmount = 500;

        HttpResponseMessage response = await PostAsync(client, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static CompletePaymentRequest CreateRequest() => new()
    {
        PaymentId = Guid.NewGuid(),
        ParkingSessionId = 123,
        SiteId = 1,
        OriginalFee = 1_000,
        DiscountFee = 400,
        PaidAmount = 600,
        PaymentMethod = "Card",
        ApprovalNumber = "12345678",
        TerminalId = "KIOSK-01",
        PaidAt = DateTimeOffset.UtcNow
    };

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, CompletePaymentRequest request)
    {
        string json = JsonConvert.SerializeObject(request);
        return client.PostAsync(
            "/api/v1/payments/complete",
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private sealed class TestApplication : WebApplicationFactory<global::Parking.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
                services.AddSingleton<IPaymentRepository>(new FakePaymentRepository()));
        }
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public Task<PaymentCompleteResponse> CompleteAsync(
            CompletePaymentRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentCompleteResponse(
                request.PaymentId,
                request.ParkingSessionId,
                true,
                "PAYMENT_COMPLETED",
                "결제가 완료되었습니다.",
                true));
    }
}
