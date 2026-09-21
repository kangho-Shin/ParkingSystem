using Dapper;
using MySqlConnector;
using Parking.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace Parking.Central.Data;

public sealed class SiteConfigurationRepository : ISiteConfigurationRepository
{
    private readonly string _connectionString;
    public SiteConfigurationRepository(string connectionString) { _connectionString = connectionString; }

    public async Task<SiteConfiguration?> GetAsync(long siteId, CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        ParkingSite? site = await connection.QuerySingleOrDefaultAsync<ParkingSite>(
            new CommandDefinition("SELECT sitenum SiteId,sitename SiteName,useflag Enabled FROM parking_site WHERE sitenum=@SiteId;",
                new { SiteId = siteId }, cancellationToken: cancellationToken));
        if (site is null) return null;

        IReadOnlyList<ParkingLane> lanes = (await connection.QueryAsync<ParkingLane>(
            new CommandDefinition("""
                SELECT laneid LaneId,sitenum SiteId,groupnum GroupNumber,lanename LaneName,
                       direction Direction,useflag Enabled
                FROM parking_lane WHERE sitenum=@SiteId ORDER BY groupnum;
                """, new { SiteId = siteId }, cancellationToken: cancellationToken))).AsList();

        IReadOnlyList<ParkingDevice> devices = (await connection.QueryAsync<ParkingDevice>(
            new CommandDefinition("""
                SELECT deviceid DeviceId,sitenum SiteId,laneid LaneId,devicenum DeviceNumber,
                       devicetype DeviceType,devicename DeviceName,ipaddr IpAddress,
                       useflag Enabled,port Port
                FROM parking_device WHERE sitenum=@SiteId ORDER BY devicenum;
                """, new { SiteId = siteId }, cancellationToken: cancellationToken))).AsList();

        IReadOnlyList<ParkingDeviceLink> links = (await connection.QueryAsync<ParkingDeviceLink>(
            new CommandDefinition("""
                SELECT sitenum SiteId,sourcedeviceid SourceDeviceId,targetdeviceid TargetDeviceId,
                       linktype LinkType,useflag Enabled
                FROM parking_device_link WHERE sitenum=@SiteId
                ORDER BY sourcedeviceid,targetdeviceid;
                """, new { SiteId = siteId }, cancellationToken: cancellationToken))).AsList();

        IReadOnlyList<ParkingOperationVariable> variables =
            (await connection.QueryAsync<ParkingOperationVariable>(new CommandDefinition("""
                SELECT groupnum Groupnum,cmd_type CommandType,val Value
                FROM tparkvariable
                WHERE sitenum=@SiteId
                ORDER BY groupnum,cmd_type;
                """, new { SiteId = siteId }, cancellationToken: cancellationToken))).AsList();

        return new SiteConfiguration(site, lanes, devices, links, variables);
    }

    public Task SaveSiteAsync(ParkingSite site, CancellationToken cancellationToken) =>
        ExecuteAsync("""
            INSERT INTO parking_site(sitenum,sitename,useflag) VALUES(@SiteId,@SiteName,@Enabled)
            ON DUPLICATE KEY UPDATE sitename=@SiteName,useflag=@Enabled;
            """, site, cancellationToken);

    public Task SaveLaneAsync(ParkingLane lane, CancellationToken cancellationToken) =>
        ExecuteAsync("""
            INSERT INTO parking_lane(laneid,sitenum,groupnum,lanename,direction,useflag)
            VALUES(@LaneId,@SiteId,@GroupNumber,@LaneName,@Direction,@Enabled)
            ON DUPLICATE KEY UPDATE sitenum=@SiteId,groupnum=@GroupNumber,lanename=@LaneName,
                                    direction=@Direction,useflag=@Enabled;
            """, lane, cancellationToken);

    public Task SaveDeviceAsync(ParkingDevice device, CancellationToken cancellationToken) =>
        ExecuteAsync("""
            INSERT INTO parking_device(deviceid,sitenum,laneid,devicenum,devicetype,devicename,ipaddr,port,useflag)
            VALUES(@DeviceId,@SiteId,@LaneId,@DeviceNumber,@DeviceType,@DeviceName,@IpAddress,@Port,@Enabled)
            ON DUPLICATE KEY UPDATE sitenum=@SiteId,laneid=@LaneId,devicenum=@DeviceNumber,
                                    devicetype=@DeviceType,devicename=@DeviceName,ipaddr=@IpAddress,
                                    port=@Port,useflag=@Enabled;
            """, device, cancellationToken);

    public Task SaveDeviceLinkAsync(ParkingDeviceLink link, CancellationToken cancellationToken) =>
        ExecuteAsync("""
            INSERT INTO parking_device_link(sitenum,sourcedeviceid,targetdeviceid,linktype,useflag)
            VALUES(@SiteId,@SourceDeviceId,@TargetDeviceId,@LinkType,@Enabled)
            ON DUPLICATE KEY UPDATE useflag=@Enabled;
            """, link, cancellationToken);

    public async Task<bool> ValidateSiteKeyAsync(
        long siteId,
        string siteKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(siteKey)) return false;
        await using MySqlConnection connection = new(_connectionString);
        string? stored = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT authkeyhash FROM parking_site_sync WHERE sitenum=@SiteId;",
            new { SiteId = siteId }, cancellationToken: cancellationToken));
        if (stored is null) return false;
        string supplied = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(siteKey)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(stored.ToUpperInvariant()),
            Encoding.ASCII.GetBytes(supplied));
    }

    public async Task<VersionedSiteConfiguration?> GetVersionedAsync(
        long siteId,
        CancellationToken cancellationToken)
    {
        SiteConfiguration? configuration = await GetAsync(siteId, cancellationToken);
        if (configuration is null) return null;
        await using MySqlConnection connection = new(_connectionString);
        SyncRow? state = await connection.QuerySingleOrDefaultAsync<SyncRow>(new CommandDefinition(
            "SELECT configversion Version,updatedat UpdatedAt FROM parking_site_sync WHERE sitenum=@SiteId;",
            new { SiteId = siteId }, cancellationToken: cancellationToken));
        return new VersionedSiteConfiguration(
            configuration,
            state?.Version ?? 0,
            state is null
                ? DateTimeOffset.MinValue
                : new DateTimeOffset(
                    DateTime.SpecifyKind(state.UpdatedAt, DateTimeKind.Utc)));
    }

    public async Task<bool> SaveVersionedAsync(
        VersionedSiteConfiguration value,
        CancellationToken cancellationToken)
    {
        long siteId = value.Configuration.Site.SiteId;
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        long current = await connection.QuerySingleOrDefaultAsync<long>(new CommandDefinition(
            "SELECT configversion FROM parking_site_sync WHERE sitenum=@SiteId FOR UPDATE;",
            new { SiteId = siteId }, transaction, cancellationToken: cancellationToken));
        if (value.Version <= current) { await transaction.RollbackAsync(cancellationToken); return false; }

        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO parking_site(sitenum,sitename,useflag) VALUES(@SiteId,@SiteName,@Enabled)
            ON DUPLICATE KEY UPDATE sitename=@SiteName,useflag=@Enabled;
            UPDATE parking_lane SET useflag=0 WHERE sitenum=@SiteId;
            UPDATE parking_device SET useflag=0 WHERE sitenum=@SiteId;
            UPDATE parking_device_link SET useflag=0 WHERE sitenum=@SiteId;
            DELETE FROM tparkvariable WHERE sitenum=@SiteId;
            """, value.Configuration.Site, transaction, cancellationToken: cancellationToken));

        foreach (ParkingLane lane in value.Configuration.Lanes)
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO parking_lane(laneid,sitenum,groupnum,lanename,direction,useflag)
                VALUES(@LaneId,@SiteId,@GroupNumber,@LaneName,@Direction,@Enabled)
                ON DUPLICATE KEY UPDATE sitenum=@SiteId,groupnum=@GroupNumber,lanename=@LaneName,direction=@Direction,useflag=@Enabled;
                """, lane, transaction, cancellationToken: cancellationToken));
        foreach (ParkingDevice device in value.Configuration.Devices)
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO parking_device(deviceid,sitenum,laneid,devicenum,devicetype,devicename,ipaddr,port,useflag)
                VALUES(@DeviceId,@SiteId,@LaneId,@DeviceNumber,@DeviceType,@DeviceName,@IpAddress,@Port,@Enabled)
                ON DUPLICATE KEY UPDATE sitenum=@SiteId,laneid=@LaneId,devicenum=@DeviceNumber,devicetype=@DeviceType,devicename=@DeviceName,ipaddr=@IpAddress,port=@Port,useflag=@Enabled;
                """, device, transaction, cancellationToken: cancellationToken));
        foreach (ParkingDeviceLink link in value.Configuration.DeviceLinks ?? [])
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO parking_device_link(sitenum,sourcedeviceid,targetdeviceid,linktype,useflag)
                VALUES(@SiteId,@SourceDeviceId,@TargetDeviceId,@LinkType,@Enabled)
                ON DUPLICATE KEY UPDATE useflag=@Enabled;
                """, link, transaction, cancellationToken: cancellationToken));
        foreach (ParkingOperationVariable variable in value.Configuration.OperationVariables ?? [])
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO tparkvariable(sitenum,groupnum,cmd_type,val)
                VALUES(@SiteId,@Groupnum,@CommandType,@Value);
                """, new { SiteId = siteId, variable.Groupnum, variable.CommandType, variable.Value }, transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE parking_site_sync
            SET configversion=@Version,updatedat=@UpdatedAt
            WHERE sitenum=@SiteId;
            """, new { SiteId = siteId, value.Version, UpdatedAt = value.UpdatedAtUtc.UtcDateTime }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task TouchVersionAsync(long siteId, CancellationToken cancellationToken) =>
        ExecuteAsync("""
            UPDATE parking_site_sync
            SET configversion=configversion+1,updatedat=UTC_TIMESTAMP(6)
            WHERE sitenum=@SiteId;
            """, new { SiteId = siteId }, cancellationToken);

    private async Task ExecuteAsync(string sql, object value, CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, value, cancellationToken: cancellationToken));
    }

    private sealed record SyncRow(long Version, DateTime UpdatedAt);
}
