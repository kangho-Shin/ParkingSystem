namespace Parking.Contracts;

public sealed record KioskExitNotification(
    Guid EventId,
    long SiteId,
    int Groupnum,
    long LprDeviceId,
    long KioskDeviceId,
    string CarNumber,
    DateTimeOffset OutDateTime,
    string? OutImage,
    string? ResultCode = null,
    string? DisplayMessage = null,
    bool? OpenBarrier = null);

public sealed record KioskRouteResult(
    bool RoutedToKiosk,
    bool OfflineOpened,
    bool Blocked,
    long? KioskDeviceId,
    string ResultCode);
