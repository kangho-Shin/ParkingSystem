namespace Parking.Contracts;

public sealed record PaymentCompleteResponse(
    Guid PaymentId,
    long ParkingSessionId,
    bool Accepted,
    string ResultCode,
    string Message,
    bool ExitAllowed);
