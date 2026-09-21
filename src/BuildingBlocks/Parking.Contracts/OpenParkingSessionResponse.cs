namespace Parking.Contracts;

public sealed record OpenParkingSessionResponse(
    long ParkingSessionId,
    long SiteId,
    string CarNumber,
    int Groupnum,
    int CarType,
    long EntryLaneId,
    DateTimeOffset EntryAt,
    DateTimeOffset? Paydate,
    string Status);
