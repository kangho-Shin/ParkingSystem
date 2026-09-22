using APSMain.BaseClass;
using APSMain.Models;

namespace APSMain.Integration.EdgeService;

public sealed class EdgeParkCalSession
{
    private readonly EdgeServiceClient _client;
    private readonly KioskExitContext _context;
    private readonly Guid _paymentId = Guid.NewGuid();
    private readonly DateTimeOffset _paidAt = DateTimeOffset.Now;
    private bool _paymentCompleted;
    private bool _exitCompleted;

    public EdgeParkCalSession(EdgeServiceClient client, KioskExitContext context, FeeQuote quote)
    {
        _client = client;
        _context = context;
        Quote = quote;
    }

    public FeeQuote Quote { get; private set; }
    public bool PaymentCompleted => _paymentCompleted;
    public string? LastError { get; private set; }

    public bool RefreshQuote(IReadOnlyList<int> discountKeys, ParkFeeCalculator calculator, Tparkinfo parkinfo)
    {
        EdgeCallResult<FeeQuote> result = Task.Run(() => _client.QuoteSessionAsync(
            Quote.ParkingSessionId, DateTimeOffset.Now, discountKeys)).GetAwaiter().GetResult();
        if (!result.IsSuccess || result.Value is null) return false;
        Quote = result.Value;
        ApplyQuote(calculator, parkinfo);
        return true;
    }

    public int ApplyQuote(ParkFeeCalculator calculator, Tparkinfo parkinfo)
    {
        calculator.totalFee = checked((int)Quote.Fee.OriginalFee);
        calculator.totalDiscountFee = checked((int)Quote.Fee.DiscountFee);
        calculator.totalRemainFee = checked((int)Quote.PayableAmount);
        calculator.totalTimeMinute = Quote.Fee.ParkingMinutes;
        calculator.prePay = checked((int)Quote.PreviousPaidAmount);
        parkinfo.Parkmoney = calculator.totalFee;
        parkinfo.Parktime = calculator.totalTimeMinute;
        parkinfo.Salemoney = calculator.totalDiscountFee;
        return calculator.totalRemainFee;
    }

    public async Task<bool> DisplayFeeAsync(CancellationToken token = default)
    {
        if (Quote.PayableAmount <= 0) return true;
        EdgeCallResult<bool> result = await _client.DisplayFeeAsync(
            Quote.CarNumber, Quote.PayableAmount, token);
        LastError = result.IsSuccess ? null : result.Error ?? "요금 전광판 표시에 실패했습니다.";
        return result.IsSuccess;
    }

    public bool CompletePayment(Tparkinfo parkinfo, bool paymentApproved)
    {
        if (_paymentCompleted) return true;
        if (Quote.PayableAmount == 0 && Quote.PreviousPaidAmount > 0)
        {
            _paymentCompleted = true;
            LastError = null;
            return true;
        }
        if (!paymentApproved) return true;

        bool free = Quote.PayableAmount == 0;
        EdgePaymentRequest request = new()
        {
            PaymentId = _paymentId,
            ParkingSessionId = Quote.ParkingSessionId,
            SiteId = _context.Notification.SiteId,
            OriginalFee = Quote.Fee.OriginalFee,
            DiscountFee = Quote.Fee.DiscountFee,
            PaidAmount = Quote.PayableAmount,
            PaymentMethod = free ? "Free" : "Card",
            ApprovalNumber = free ? "FREE" : parkinfo.Acceptno ?? "CARD",
            TerminalId = $"APS-{APSConfig.APSNUM:D3}",
            PaidAt = _paidAt
        };
        EdgeCallResult<EdgePaymentResponse> result = Task.Run(() =>
            _client.CompletePaymentAsync(request)).GetAwaiter().GetResult();
        _paymentCompleted = result.IsSuccess && result.Value?.Accepted == true;
        LastError = _paymentCompleted ? null : result.Error ?? result.Value?.Message ?? "결제완료 처리에 실패했습니다.";
        return _paymentCompleted;
    }

    public async Task<bool> CompleteExitAsync(CancellationToken token = default)
    {
        if (_exitCompleted) return true;
        EdgeCallResult<bool> result = _context.IsEventDriven
            ? await _client.CompleteKioskEventAsync(_context.Notification.EventId, token)
            : await _client.CompleteManualExitAsync(Quote.CarNumber, DateTimeOffset.Now, token);
        _exitCompleted = result.IsSuccess;
        LastError = _exitCompleted
            ? null
            : result.Error ?? "출차 완료 처리에 실패했습니다.";
        return _exitCompleted;
    }
}
