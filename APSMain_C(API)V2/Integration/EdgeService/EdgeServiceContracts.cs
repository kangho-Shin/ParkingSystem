namespace APSMain.Integration.EdgeService;

public sealed record KioskExitNotification(
    Guid EventId, long SiteId, int Groupnum, long LprDeviceId, long KioskDeviceId,
    string CarNumber, DateTimeOffset OutDateTime, string? OutImage,
    string? ResultCode = null, string? DisplayMessage = null, bool? OpenBarrier = null);

public sealed record ParkingSearchCandidate(
    long ParkingSessionId, string CarNumber, int Groupnum, int CarType,
    DateTimeOffset InDateTime, string? InImage);

public sealed class FeeQuote
{
    public long ParkingSessionId { get; set; }
    public string CarNumber { get; set; } = "";
    public DateTimeOffset EntryAt { get; set; }
    public DateTimeOffset ExitAt { get; set; }
    public FeeResult Fee { get; set; } = new();
    public long PreviousPaidAmount { get; set; }
    public long PayableAmount { get; set; }
    public bool IsPrepayGrace { get; set; }
}

public sealed class FeeResult
{
    public long OriginalFee { get; set; }
    public long FinalFee { get; set; }
    public long DiscountFee { get; set; }
    public int ParkingMinutes { get; set; }
}

public sealed class EdgePaymentRequest
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

public sealed class EdgePaymentResponse
{
    public Guid PaymentId { get; set; }
    public long ParkingSessionId { get; set; }
    public bool Accepted { get; set; }
    public string ResultCode { get; set; } = "";
    public string Message { get; set; } = "";
    public bool ExitAllowed { get; set; }
}

public enum EdgeCallStatus { Success, TransientFailure, InvalidResponse, Failure }

public sealed record EdgeCallResult<T>(EdgeCallStatus Status, T? Value, string? Error)
{
    public bool IsSuccess => Status == EdgeCallStatus.Success;
    public static EdgeCallResult<T> Success(T value) => new(EdgeCallStatus.Success, value, null);
    public static EdgeCallResult<T> Failed(EdgeCallStatus status, string error) => new(status, default, error);
}
