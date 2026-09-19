namespace Parking.Contracts;

public sealed class CompletePaymentRequest
{
    public Guid PaymentId { get; set; }
    public long ParkingSessionId { get; set; }
    public long SiteId { get; set; }
    public long OriginalFee { get; set; }
    public long DiscountFee { get; set; }
    public long PaidAmount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string ApprovalNumber { get; set; } = "";
    public string? TerminalId { get; set; }
    public DateTimeOffset PaidAt { get; set; }
}
