using Dapper;
using MySqlConnector;
using Parking.Contracts;

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
                       devicetype DeviceType,devicename DeviceName,ipaddr IpAddress,useflag Enabled
                FROM parking_device WHERE sitenum=@SiteId ORDER BY devicenum;
                """, new { SiteId = siteId }, cancellationToken: cancellationToken))).AsList();

        return new SiteConfiguration(site, lanes, devices);
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
            INSERT INTO parking_device(deviceid,sitenum,laneid,devicenum,devicetype,devicename,ipaddr,useflag)
            VALUES(@DeviceId,@SiteId,@LaneId,@DeviceNumber,@DeviceType,@DeviceName,@IpAddress,@Enabled)
            ON DUPLICATE KEY UPDATE sitenum=@SiteId,laneid=@LaneId,devicenum=@DeviceNumber,
                                    devicetype=@DeviceType,devicename=@DeviceName,ipaddr=@IpAddress,useflag=@Enabled;
            """, device, cancellationToken);

    private async Task ExecuteAsync(string sql, object value, CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, value, cancellationToken: cancellationToken));
    }
}
