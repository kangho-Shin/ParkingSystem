namespace Parking.Contracts;

public sealed record CorrectCarNumberRequest(string CarNumber);
public sealed record CorrectCarNumberResponse(long ParkingSessionId, string CarNumber, bool Updated, string ResultCode, string Message);
public sealed record OperatorBarrierRequest(long SiteId, long LaneId, long DeviceId, string CarNumber);
