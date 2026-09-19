namespace Parking.Contracts;

public enum EdgeActivityType
{
    Payment,
    Exit
}

public enum EdgeDeliveryState
{
    Pending,
    Completed,
    Failed
}

public sealed record EdgeServiceStatus(
    bool ServiceConnected,
    bool GatewayConnected,
    bool CentralConnected,
    DateTimeOffset? ConfigurationSyncedAt,
    int PendingOutboxCount);

public sealed record GatewayHealthResponse(
    bool GatewayConnected,
    bool CentralConnected);

public sealed record EdgeEntryItem(
    Guid EventId,
    long? ParkingSessionId,
    long SiteId,
    int Groupnum,
    long LaneId,
    long DeviceId,
    string CarNumber,
    DateTimeOffset InDateTime,
    string? InImage,
    EdgeDeliveryState DeliveryState,
    string? ResultCode);

public sealed record EdgeActivityItem(
    Guid ActivityId,
    EdgeActivityType ActivityType,
    long? ParkingSessionId,
    long SiteId,
    int Groupnum,
    long? LaneId,
    long? DeviceId,
    string CarNumber,
    DateTimeOffset OccurredAt,
    string? InImage,
    string? OutImage,
    long? OriginalFee,
    long? DiscountFee,
    long? PaidAmount,
    string? PaymentMethod,
    bool? OpenBarrier,
    EdgeDeliveryState DeliveryState,
    string? ResultCode);

public sealed record EdgeManagementSnapshot(
    IReadOnlyList<EdgeEntryItem> Entries,
    IReadOnlyList<EdgeActivityItem> Activities);
