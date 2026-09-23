namespace Parking.Contracts;

public sealed record EdgeBootstrapSettings(
    long SiteId,
    string CentralServerUrl,
    string ParkingApiUrl,
    string ImageServerUrl,
    string ImageWatchPath,
    string SiteAuthKey,
    DateTimeOffset UpdatedAtUtc);

public sealed record VersionedSiteConfiguration(
    SiteConfiguration Configuration,
    long Version,
    DateTimeOffset UpdatedAtUtc,
    bool Deleted = false);
