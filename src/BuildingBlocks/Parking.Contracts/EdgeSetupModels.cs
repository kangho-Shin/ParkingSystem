namespace Parking.Contracts;

public sealed record EdgeSetupRequest(
    long SiteId,
    string CentralServerUrl,
    string ImageServerUrl,
    string ImageWatchPath,
    string SiteAuthKey);

public sealed record EdgeSetupResponse(
    long SiteId,
    string CentralServerUrl,
    string ImageServerUrl,
    string ImageWatchPath,
    DateTimeOffset UpdatedAtUtc);
