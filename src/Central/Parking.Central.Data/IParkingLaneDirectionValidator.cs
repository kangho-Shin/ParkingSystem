namespace Parking.Central.Data;

public interface IParkingLaneDirectionValidator
{
    Task<bool> IsValidAsync(
        long siteId,
        int groupnum,
        long laneId,
        long deviceId,
        string eventType,
        CancellationToken cancellationToken);
}
