namespace Parking.Contracts;

public sealed record LprRecognition(
    Guid EventId,
    long SiteId,
    int Groupnum,
    long DeviceId,
    long LaneId,
    string Direction,
    DateTimeOffset RecognizedAt,
    string CarNumber,
    string ImageFileName);

public sealed record LprParseResult(
    bool Success,
    LprRecognition? Recognition,
    string? ErrorCode,
    Guid? EventId);

public sealed record LprProcessResult(
    bool Acknowledged,
    Guid? EventId,
    string? ErrorCode,
    FieldEventResponse? ParkingResponse);
