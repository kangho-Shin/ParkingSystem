namespace Parking.Contracts;

public sealed record PeriodMember(
    long MemberId,
    long SiteId,
    int Groupnum,
    long CardNumber,
    string Name,
    string CarNumber,
    int CarType,
    DateTime? EndDate);

public sealed record OpenPeriodSession(
    long PeriodSessionId,
    long MemberId,
    long SiteId,
    int Groupnum,
    string CarNumber,
    DateTimeOffset InDateTime,
    string? InImage,
    string OutFlag);
