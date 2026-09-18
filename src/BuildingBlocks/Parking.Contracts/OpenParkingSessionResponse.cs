namespace Parking.Contracts;

public sealed record OpenParkingSessionResponse(
    long ParkingSessionId,
    long SiteId,
    string CarNumber,
    long EntryLaneId,
    DateTimeOffset EntryAt,
    string Status);
