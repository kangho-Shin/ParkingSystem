namespace Parking.Contracts;

public sealed record FieldEventResponse(
    Guid EventId,
    bool Accepted,
    long? ParkingSessionId,
    string ResultCode,
    string DisplayMessage,
    bool OpenBarrier);