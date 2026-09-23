namespace Parking.Contracts;

public enum ParkingSessionType
{
    General = 1,
    Period = 2
}

public sealed record ParkingManagementQuery(
    long SiteId,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int? Groupnum = null,
    long? DeviceId = null,
    string? CarNumber = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 200);

public sealed record ParkingManagementItem(
    ParkingSessionType SessionType,
    long ParkingSessionId,
    long SiteId,
    int Groupnum,
    string CarNumber,
    int CarType,
    string Status,
    long InLaneId,
    int InDeviceNumber,
    string InDeviceName,
    DateTimeOffset InDateTime,
    DateTimeOffset? PaidAt,
    long? OutLaneId,
    int? OutDeviceNumber,
    string? ProcessDeviceName,
    DateTimeOffset? OutDateTime,
    int ParkingMinutes,
    int? OriginalFee,
    int? DiscountFee,
    int? PaidFee,
    bool IsManual,
    string? InImage,
    string? OutImage);

public sealed record PagedParkingResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);

public sealed record ManualEntryRequest(
    long SiteId,
    int Groupnum,
    long LaneId,
    long DeviceId,
    string CarNumber,
    DateTimeOffset InDateTime,
    int CarType);

public sealed record ManualEntryResponse(
    ParkingSessionType SessionType,
    long? ParkingSessionId,
    bool Accepted,
    string ResultCode,
    string Message);

public sealed record ManagementCarNumberRequest(
    long SiteId,
    string CarNumber);

public sealed record CentralConnectionResponse(
    long SiteId,
    string CentralServerUrl,
    string ParkingApiUrl,
    string SiteAuthKey);
