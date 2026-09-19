namespace Parking.Contracts;

public sealed record ParkingSearchCandidate(
    long ParkingSessionId,
    string CarNumber,
    int Groupnum,
    int CarType,
    DateTimeOffset InDateTime,
    string? InImage);

public sealed record ParkingSearchResponse(
    IReadOnlyList<ParkingSearchCandidate> Candidates);
