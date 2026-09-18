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
            new CommandDefinition("SELECT site_id SiteId,site_name SiteName,enabled Enabled FROM parking_site WHERE site_id=@SiteId;",
                new { SiteId = siteId }, cancellationToken: cancellationToken));
        if (site is null) return null;

        IReadOnlyList<ParkingLane> lanes = (await connection.QueryAsync<ParkingLane>(
            new CommandDefinition("""
                SELECT lane_id LaneId,site_id SiteId,group_number GroupNumber,lane_name LaneName,
                       direction Direction,enabled Enabled
                FROM parking_lane WHERE site_id=@SiteId ORDER BY group_number;
                """, new { SiteId = siteId }, cancellationToken: cancellationToken))).AsList();

        IReadOnlyList<ParkingDevice> devices = (await connection.QueryAsync<ParkingDevice>(
            new CommandDefinition("""
                SELECT device_id DeviceId,site_id SiteId,lane_id LaneId,device_number DeviceNumber,
                       device_type DeviceType,device_name DeviceName,ip_address IpAddress,enabled Enabled
                FROM parking_device WHERE site_id=@SiteId ORDER BY device_number;
                """, new { SiteId = siteId }, cancellationToken: cancellationToken))).AsList();

        return new SiteConfiguration(site, lanes, devices);
    }

    public Task SaveSiteAsync(ParkingSite site, CancellationToken cancellationToken) =>
        ExecuteAsync("""
            INSERT INTO parking_site(site_id,site_name,enabled) VALUES(@SiteId,@SiteName,@Enabled)
            ON DUPLICATE KEY UPDATE site_name=@SiteName,enabled=@Enabled;
            """, site, cancellationToken);

    public Task SaveLaneAsync(ParkingLane lane, CancellationToken cancellationToken) =>
        ExecuteAsync("""
            INSERT INTO parking_lane(lane_id,site_id,group_number,lane_name,direction,enabled)
            VALUES(@LaneId,@SiteId,@GroupNumber,@LaneName,@Direction,@Enabled)
            ON DUPLICATE KEY UPDATE site_id=@SiteId,group_number=@GroupNumber,lane_name=@LaneName,
                                    direction=@Direction,enabled=@Enabled;
            """, lane, cancellationToken);

    public Task SaveDeviceAsync(ParkingDevice device, CancellationToken cancellationToken) =>
        ExecuteAsync("""
            INSERT INTO parking_device(device_id,site_id,lane_id,device_number,device_type,device_name,ip_address,enabled)
            VALUES(@DeviceId,@SiteId,@LaneId,@DeviceNumber,@DeviceType,@DeviceName,@IpAddress,@Enabled)
            ON DUPLICATE KEY UPDATE site_id=@SiteId,lane_id=@LaneId,device_number=@DeviceNumber,
                                    device_type=@DeviceType,device_name=@DeviceName,ip_address=@IpAddress,enabled=@Enabled;
            """, device, cancellationToken);

    private async Task ExecuteAsync(string sql, object value, CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, value, cancellationToken: cancellationToken));
    }
}
