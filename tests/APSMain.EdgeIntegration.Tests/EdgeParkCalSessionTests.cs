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

    [Fact]
    public async Task Manual_settlement_completes_exit_through_linked_exit_lpr()
    {
        RecordingHandler handler = new();
        EdgeServiceClient client = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5200/") },
            new EdgeServiceOptions(new Uri("http://localhost:5200/"), 9001, 2, 201));
        FeeQuote quote = new()
        {
            ParkingSessionId = 682,
            CarNumber = "12가3456",
            PayableAmount = 0,
            Fee = new FeeResult()
        };
        KioskExitContext context = new()
        {
            Notification = new KioskExitNotification(
                Guid.NewGuid(), 9001, 2, 0, 0,
                "12가3456", DateTimeOffset.Now, null),
            IsEventDriven = false
        };
        EdgeParkCalSession session = new(client, context, quote);

        bool result = await session.CompleteExitAsync();

        Assert.True(result);
        Assert.Equal("api/v1/local/kiosks/manual-exit/complete", handler.RequestPath);
        Assert.Contains("12가3456", handler.RequestBody);
    }

    [Fact]
    public async Task Payable_fee_is_sent_to_kiosk_display_for_100_seconds()
    {
        RecordingHandler handler = new();
        EdgeServiceClient client = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5200/") },
            new EdgeServiceOptions(new Uri("http://localhost:5200/"), 9001, 2, 201));
        FeeQuote quote = new()
        {
            ParkingSessionId = 683,
            CarNumber = "12가3456",
            PayableAmount = 1200,
            Fee = new FeeResult { OriginalFee = 1200 }
        };
        KioskExitContext context = new()
        {
            Notification = new KioskExitNotification(
                Guid.NewGuid(), 9001, 2, 0, 0,
                "12가3456", DateTimeOffset.Now, null)
        };
        EdgeParkCalSession session = new(client, context, quote);

        bool result = await session.DisplayFeeAsync();

        Assert.True(result);
        Assert.Equal("api/v1/local/kiosks/display", handler.RequestPath);
        Assert.Contains("요금 1,200원", handler.RequestBody);
        Assert.Contains("\"DisplaySeconds\":100", handler.RequestBody);
    }

    [Fact]
    public async Task Previous_or_home_resets_display_to_clock()
    {
        RecordingHandler handler = new();
        EdgeServiceClient client = new(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5200/") },
            new EdgeServiceOptions(new Uri("http://localhost:5200/"), 9001, 2, 201));
        EdgeParkCalSession session = new(client, new KioskExitContext(), new FeeQuote());

        bool result = await session.ResetDisplayAsync();

        Assert.True(result);
        Assert.Equal("api/v1/local/kiosks/display/reset", handler.RequestPath);
        Assert.Contains("\"Sitenum\":9001", handler.RequestBody);
        Assert.Contains("\"Groupnum\":2", handler.RequestBody);
        Assert.Contains("\"Devicenum\":201", handler.RequestBody);
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

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? RequestPath { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath.TrimStart('/');
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"Accepted\":true,\"DisplayMessage\":\"정산 완료되었습니다.\"}")
            };
        }
    }
}
