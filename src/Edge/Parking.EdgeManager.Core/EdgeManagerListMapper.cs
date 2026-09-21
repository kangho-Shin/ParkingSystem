using Parking.Contracts;

namespace Parking.EdgeManager.Core;

public sealed record EdgeEntryListItem(
    Guid EventId,
    int? DeviceNumber,
    string DeviceName,
    string CarNumber,
    DateTimeOffset InDateTime,
    long? ParkingSessionId,
    int Groupnum,
    long LaneId,
    EdgeDeliveryState DeliveryState,
    string? ResultCode);

public sealed record EdgeActivityListItem(
    Guid ActivityId,
    EdgeActivityType ActivityType,
    string ActivityName,
    int? DeviceNumber,
    string DeviceName,
    string CarNumber,
    DateTimeOffset OccurredAt,
    long? ParkingSessionId,
    int Groupnum,
    long? LaneId,
    long? PaidAmount,
    bool? OpenBarrier,
    EdgeDeliveryState DeliveryState,
    string? ResultCode);

public static class EdgeManagerListMapper
{
    public static IReadOnlyList<EdgeEntryListItem> MapEntries(
        IReadOnlyList<EdgeEntryItem> entries,
        IReadOnlyList<ParkingDevice> devices)
    {
        Dictionary<long, ParkingDevice> deviceMap = devices.ToDictionary(x => x.DeviceId);
        return entries.Select(entry =>
        {
            deviceMap.TryGetValue(entry.DeviceId, out ParkingDevice? device);
            return new EdgeEntryListItem(
                entry.EventId,
                device?.DeviceNumber,
                device?.DeviceName ?? "",
                entry.CarNumber,
                entry.InDateTime,
                entry.ParkingSessionId,
                entry.Groupnum,
                entry.LaneId,
                entry.DeliveryState,
                entry.ResultCode);
        }).ToList();
    }

    public static IReadOnlyList<EdgeActivityListItem> MapActivities(
        IReadOnlyList<EdgeActivityItem> activities,
        IReadOnlyList<ParkingDevice> devices)
    {
        Dictionary<long, ParkingDevice> deviceMap = devices.ToDictionary(x => x.DeviceId);
        return activities.Select(activity =>
        {
            ParkingDevice? device = null;
            if (activity.DeviceId is long deviceId)
                deviceMap.TryGetValue(deviceId, out device);
            return new EdgeActivityListItem(
                activity.ActivityId,
                activity.ActivityType,
                activity.ActivityType == EdgeActivityType.Exit ? "출차" : "정산",
                device?.DeviceNumber,
                device?.DeviceName ?? "",
                activity.CarNumber,
                activity.OccurredAt,
                activity.ParkingSessionId,
                activity.Groupnum,
                activity.LaneId,
                activity.PaidAmount,
                activity.OpenBarrier,
                activity.DeliveryState,
                activity.ResultCode);
        }).ToList();
    }
}
