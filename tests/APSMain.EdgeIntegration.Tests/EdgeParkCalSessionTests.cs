using APSMain.Integration.EdgeService;
using APSMain.Models;

namespace APSMain.EdgeIntegration.Tests;

public sealed class EdgeParkCalSessionTests
{
    [Fact]
    public void Previously_paid_zero_balance_does_not_send_payment_again()
    {
        RejectingHandler handler = new();
        EdgeServiceClient client = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5200/") },
            new EdgeServiceOptions(new Uri("http://localhost:5200/"), 9001, 2, 201));
        FeeQuote quote = new()
        {
            ParkingSessionId = 681,
            PreviousPaidAmount = 600,
            PayableAmount = 0,
            Fee = new FeeResult { OriginalFee = 600, DiscountFee = 0 }
        };
        KioskExitContext context = new()
        {
            Notification = new KioskExitNotification(
                Guid.NewGuid(), 9001, 2, 4002, 2001,
                "12가3456", DateTimeOffset.Now, null)
        };
        EdgeParkCalSession session = new(client, context, quote);

        bool result = session.CompletePayment(new Tparkinfo(), true);

        Assert.True(result);
        Assert.True(session.PaymentCompleted);
        Assert.Equal(0, handler.RequestCount);
    }

    private sealed class RejectingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            throw new InvalidOperationException("기존 결제 건은 결제 API를 다시 호출하면 안 됩니다.");
        }
    }
}
